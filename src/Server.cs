#if !BLOCK_LITE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BlockEngine
{
    public static class Server
    {
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer { MaxJsonLength = (int)SecurityLimits.MaxJsonBytes };
        private static readonly SemaphoreSlim _requestSlots = new SemaphoreSlim(SecurityLimits.MaxConcurrentRequests, SecurityLimits.MaxConcurrentRequests);

        // H8: Fix: Use per-instance dictionaries instead of shared static fields
        // These are now set fresh each time ParseAndRunServer is called
        private static int serverPort = 3000;
        private static Dictionary<string, string> serverRoutes = new Dictionary<string, string>();
        private static Dictionary<string, string> staticDirs = new Dictionary<string, string>();
        
        // M6: Fix: Pre-compiled route blocks cache — parse AST once, reuse per request
        private static readonly ConcurrentDictionary<string, List<CodeBlock>> _routeBlockCache
            = new ConcurrentDictionary<string, List<CodeBlock>>(StringComparer.OrdinalIgnoreCase);
        
        private static EngineConfig _cfg;
        private static string _scriptPath;
        private const long MaxBodyBytes = 4 * 1024 * 1024; // 4MB

        private static bool ContainsReparsePoint(string path)
        {
            string current = Path.GetFullPath(path);
            while (!string.IsNullOrEmpty(current))
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        return true;
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                string parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
                    break;
                current = parent;
            }
            return false;
        }

        private static bool IsPathInSandbox(string fullPath, string sandboxRoot)
        {
            string normalSandbox = sandboxRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string normalPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return normalPath.StartsWith(normalSandbox, StringComparison.OrdinalIgnoreCase)
                && !ContainsReparsePoint(normalSandbox)
                && !ContainsReparsePoint(normalPath);
        }

        private static bool IsTrustedOrigin(string origin)
        {
            if (string.IsNullOrEmpty(origin)) return true;
            try
            {
                Uri uri = new Uri(origin);
                bool httpScheme = uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                                  uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
                return httpScheme && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase));
            }
            catch { return false; }
        }

        private static async Task<string> ReadBodyAsync(Stream input, Encoding encoding, long maxBytes)
        {
            if (maxBytes <= 0 || maxBytes > SecurityLimits.MaxJsonBytes)
                throw new InvalidOperationException("Invalid request body limit.");
            using (MemoryStream body = new MemoryStream())
            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(SecurityLimits.RequestReadTimeoutSeconds)))
            {
                byte[] buffer = new byte[8192];
                long total = 0;
                while (true)
                {
                    int read = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false);
                    if (read <= 0) break;
                    total += read;
                    if (total > maxBytes) throw new InvalidOperationException("Request body too large.");
                    body.Write(buffer, 0, read);
                }
                return encoding.GetString(body.ToArray());
            }
        }

        public static bool ParseAndRunServer(string code, EngineConfig cfg, string scriptPath)
        {
            // H8: Fix: Reset dictionaries each time to prevent cross-script route pollution
            serverPort = 3000;
            serverRoutes = new Dictionary<string, string>();
            staticDirs = new Dictionary<string, string>();
            _routeBlockCache.Clear();
            _cfg = cfg;
            _scriptPath = scriptPath;

            using (StringReader sr = new StringReader(code))
            {
                string line;
                string collectingRoute = null;
                List<string> routeCode = new List<string>();
                bool hasServer = false;
                bool serverOpen = false;

                while ((line = sr.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    Match serverMatch = Regex.Match(trimmed, @"^<server(?:\s+port=[""']?(\d+)[""']?)?\s*>$", RegexOptions.IgnoreCase);
                    if (collectingRoute == null && !serverOpen && serverMatch.Success)
                    {
                        if (serverMatch.Groups[1].Success)
                        {
                            int requestedPort;
                            if (!int.TryParse(serverMatch.Groups[1].Value, out requestedPort) || requestedPort < 1 || requestedPort > 65535)
                                throw new InvalidOperationException("Server port must be an integer between 1 and 65535.");
                            serverPort = requestedPort;
                        }
                        hasServer = true;
                        serverOpen = true;
                        continue;
                    }
                    if (trimmed == "</server>")
                    {
                        if (!serverOpen || collectingRoute != null)
                            throw new InvalidOperationException("Unmatched or misplaced </server> tag.");
                        StartServer(cfg, scriptPath);
                        return true;
                    }
#if BLOCK_PLUS
                    Match staticMatch = Regex.Match(trimmed, @"^<static\s+path=[""']([^""']+)[""']\s+dir=[""']([^""']+)[""']\s*>$", RegexOptions.IgnoreCase);
                    if (serverOpen && collectingRoute == null && staticMatch.Success)
                    {
                        staticDirs[staticMatch.Groups[1].Value] = staticMatch.Groups[2].Value;
                        continue;
                    }
#endif
                    Match routeMatch = Regex.Match(trimmed, @"^<route\s+path=[""']([^""']+)[""']\s*>$", RegexOptions.IgnoreCase);
                    if (serverOpen && collectingRoute == null && routeMatch.Success)
                    {
                        collectingRoute = routeMatch.Groups[1].Value;
                        routeCode.Clear();
                        continue;
                    }
                    if (trimmed == "</route>")
                    {
                        if (collectingRoute == null)
                            throw new InvalidOperationException("Unmatched </route> tag.");
                        serverRoutes[collectingRoute] = string.Join("\n", routeCode);
                        collectingRoute = null;
                        continue;
                    }
                    if (collectingRoute != null)
                    {
                        routeCode.Add(line);
                        continue;
                    }
                }

                if (collectingRoute != null)
                    throw new InvalidOperationException("Unclosed <route> block.");
                if (serverOpen || hasServer)
                    throw new InvalidOperationException("Unclosed <server> block.");
            }
            return false;
        }

        private static void StartServer(EngineConfig cfg, string scriptPath)
        {
            Console.WriteLine(string.Format("\n[ BLOCK SERVER IS RUNNING ON HTTP://LOCALHOST:{0} ]\n", serverPort));
            if (string.IsNullOrEmpty(cfg.ApiToken))
                cfg.ApiToken = Guid.NewGuid().ToString("N");
            Console.WriteLine("=> Server Security Token: " + cfg.ApiToken);
            
            // M6: Fix: Pre-compile all route ASTs before serving (parse once!)
            foreach (var kvp in serverRoutes)
            {
                // A route containing <define> must be parsed per request so its
                // inline language definition is alive when the route executes.
                if (kvp.Value.IndexOf("<define", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                try
                {
                    _routeBlockCache[kvp.Key] = Parser.ParseBlocks(kvp.Value, scriptPath, cfg);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[Server Warning] Failed to pre-parse route '{0}': {1}", kvp.Key, ex.Message));
                }
            }
            
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(string.Format("http://localhost:{0}/", serverPort));
            listener.Start();

            // M7: Fix: Run the accept loop in a proper async fashion
            Task acceptTask = Task.Run(() => AcceptLoopAsync(listener, cfg, scriptPath));
            
            Console.WriteLine("Press Ctrl+C or Enter to stop the server.");
            Console.ReadLine();
            listener.Stop();
            try { acceptTask.Wait(2000); } catch (AggregateException ex) { Console.Error.WriteLine("[Block Server] Shutdown warning: " + ex.GetBaseException().Message); }
            listener.Close();
        }

        private static async Task AcceptLoopAsync(HttpListener listener, EngineConfig cfg, string scriptPath)
        {
            while (listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);
                    // Handle each request on its own task — true async concurrency
                    ObserveBackgroundTask(Task.Run(() => HandleRequestAsync(context, cfg, scriptPath)));
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[Block Server] Accept error: " + ex.Message);
                    Thread.Sleep(100);
                }
            }
        }

        private static void ObserveBackgroundTask(Task task)
        {
            if (task == null) return;
            task.ContinueWith(faultedTask =>
            {
                if (faultedTask.Exception != null)
                    Console.Error.WriteLine("[Block Server] Unhandled request error: " + faultedTask.Exception.GetBaseException().Message);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static async Task HandleRequestAsync(HttpListenerContext context, EngineConfig cfg, string scriptPath)
        {
            HttpListenerRequest req = context.Request;
            HttpListenerResponse res = context.Response;

            if (!_requestSlots.Wait(0))
            {
                res.StatusCode = 429;
                res.Close();
                return;
            }
            
            try
            {
                string origin = req.Headers["Origin"] ?? "";
                if (IsTrustedOrigin(origin))
                    res.Headers.Add("Access-Control-Allow-Origin", string.IsNullOrEmpty(origin) ? "http://localhost" : origin);
                res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Api-Token, X-Block-Language, X-Block-Engine, X-Block-Timeout-Ms, X-Block-Max-Parallel, X-Block-Network-Blocked, X-Block-Cache");
                
                if (req.HttpMethod == "OPTIONS")
                {
                    res.StatusCode = 204;
                    res.Close();
                    return;
                }

                // Enforce API token validation for non-options requests if token is configured.
                // Never accept the token in the query string: URLs are commonly logged or copied
                // into referrers, which would expose the bearer credential.
                if (!string.IsNullOrEmpty(cfg.ApiToken))
                {
                    string token = req.Headers["X-Api-Token"];
                    if (string.IsNullOrEmpty(token) || token != cfg.ApiToken)
                    {
                        res.StatusCode = 403;
                        res.Close();
                        return;
                    }
                }

                string path = req.Url.AbsolutePath;

#if BLOCK_PLUS
                foreach (var kvp in staticDirs)
                {
                    string routePrefix = kvp.Key.EndsWith("/") ? kvp.Key : kvp.Key + "/";
                    if (path == kvp.Key || path.StartsWith(routePrefix))
                    {
                        string relFile = path.Substring(kvp.Key.Length).TrimStart('/', '\\');
                        if (string.IsNullOrEmpty(relFile)) relFile = "index.html";
                        
                        string scriptDir = Path.GetDirectoryName(scriptPath);
                        // Prevent specifying absolute root paths like C:\
                        string configuredBase = kvp.Value.TrimStart('/', '\\');
                        string staticBase = Path.GetFullPath(Path.Combine(scriptDir, configuredBase));
                        string sandboxRoot = Path.GetFullPath(cfg.SandboxDir);
                        string trustedScriptRoot = Path.GetFullPath(Path.GetDirectoryName(scriptPath));
                        if (!IsPathInSandbox(staticBase, sandboxRoot) && !IsPathInSandbox(staticBase, trustedScriptRoot))
                        {
                            res.StatusCode = 403;
                            res.Close();
                            return;
                        }
                        string fullFilePath = Path.GetFullPath(Path.Combine(staticBase, relFile));
                        
                        if (!IsPathInSandbox(fullFilePath, staticBase))
                        {
                            res.StatusCode = 403;
                            res.Close();
                            return;
                        }
                        
                        if (File.Exists(fullFilePath))
                        {
                            FileInfo fi = new FileInfo(fullFilePath);
                            // Enforce 10 MB max static file size limit
                            if (fi.Length > 10 * 1024 * 1024)
                            {
                                res.StatusCode = 413; // Payload Too Large
                                res.Close();
                                return;
                            }
                            byte[] fileBytes = File.ReadAllBytes(fullFilePath);
                            res.ContentType = GetMimeType(Path.GetExtension(fullFilePath));
                            await res.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length).ConfigureAwait(false);
                            res.Close();
                            return;
                        }
                    }
                }
#endif

                if (serverRoutes.ContainsKey(path))
                {

                    // C7: Fix: Enforce max body size
                    if (req.ContentLength64 > MaxBodyBytes)
                    {
                        res.StatusCode = 413;
                        res.Close();
                        return;
                    }

                    Dictionary<string, object> reqObj = new Dictionary<string, object>();
                    reqObj["method"] = req.HttpMethod;
                    reqObj["path"] = path;
                    
                    Dictionary<string, string> queryObj = new Dictionary<string, string>();
                    foreach (string key in req.QueryString.AllKeys)
                    {
                        if (key != null) queryObj[key] = req.QueryString[key];
                    }
                    reqObj["query"] = queryObj;

                    string bodyStr = await ReadBodyAsync(req.InputStream, req.ContentEncoding, MaxBodyBytes).ConfigureAwait(false);
                    
                    object bodyObj = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(bodyStr))
                    {
                        try { bodyObj = _serializer.DeserializeObject(bodyStr); }
                        catch { 
                            var rawObj = new Dictionary<string, string>();
                            rawObj["raw"] = bodyStr;
                            bodyObj = rawObj;
                        }
                    }
                    reqObj["body"] = bodyObj;
                    
                    // F1: Fix: Each request gets its OWN initialState — no shared global state pollution
                    Dictionary<string, object> initialState = new Dictionary<string, object>();
                    initialState["REQ"] = reqObj;
                    
                    // M6: Fix: Use pre-compiled route block cache instead of re-parsing every request
                    List<CodeBlock> blocks;
                    if (!_routeBlockCache.TryGetValue(path, out blocks))
                    {
                        blocks = Parser.ParseBlocks(serverRoutes[path], scriptPath, cfg);
                    }
                    
                    await Executor.ExecuteBlocksAsync(blocks, cfg, scriptPath, initialState, res).ConfigureAwait(false);
                }
                else
                {
                    res.StatusCode = 404;
                    res.ContentType = "application/json; charset=utf-8";
                    byte[] nb = Encoding.UTF8.GetBytes("{\"error\":\"Route not found\"}");
                    res.OutputStream.Write(nb, 0, nb.Length);
                    res.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server Route Error: " + ex.Message);
                try {
                    res.StatusCode = 500;
                    byte[] buf = Encoding.UTF8.GetBytes("{\"error\":\"Internal Server Error\"}");
                    res.ContentType = "application/json; charset=utf-8";
                    res.OutputStream.Write(buf, 0, buf.Length); // avoid await in catch for .NET 4.x
                } catch { }
            }
            finally
            {
                _requestSlots.Release();
                try { res.Close(); } catch { }
            }
        }

        private static string GetMimeType(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return "application/octet-stream";
            switch (ext.ToLower())
            {
                case ".html": case ".htm": return "text/html; charset=utf-8";
                case ".css": return "text/css; charset=utf-8";
                case ".js": return "application/javascript; charset=utf-8";
                case ".json": return "application/json; charset=utf-8";
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".svg": return "image/svg+xml";
                case ".ico": return "image/x-icon";
                case ".txt": return "text/plain; charset=utf-8";
                default: return "application/octet-stream";
            }
        }
    }
}
#endif
