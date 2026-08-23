#if !BLOCK_LITE
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BlockEngine
{
    public static class ApiServer
    {
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer { MaxJsonLength = (int)SecurityLimits.MaxJsonBytes };
        private static readonly SemaphoreSlim _requestSlots = new SemaphoreSlim(SecurityLimits.MaxConcurrentRequests, SecurityLimits.MaxConcurrentRequests);

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
            try
            {
                string resolvedSandbox = Path.GetFullPath(sandboxRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string resolvedPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return resolvedPath.StartsWith(resolvedSandbox, StringComparison.OrdinalIgnoreCase)
                    && !ContainsReparsePoint(resolvedSandbox)
                    && !ContainsReparsePoint(resolvedPath);
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

        private static EngineConfig BuildRequestConfig(EngineConfig cfg, HttpListenerRequest req)
        {
            EngineConfig requestCfg = new EngineConfig
            {
                PythonEnabled = cfg.PythonEnabled,
                JsEnabled = cfg.JsEnabled,
                PhpEnabled = cfg.PhpEnabled,
                RubyEnabled = cfg.RubyEnabled,
                LuaEnabled = cfg.LuaEnabled,
                PowerShellEnabled = cfg.PowerShellEnabled,
                SqlEnabled = cfg.SqlEnabled,
                NetworkBlocked = cfg.NetworkBlocked,
                AllowCustomDefinitions = cfg.AllowCustomDefinitions,
                SandboxDir = cfg.SandboxDir,
                ApiToken = cfg.ApiToken,
                ExecutionTimeoutSeconds = cfg.ExecutionTimeoutSeconds,
                MaxRequestBodyBytes = cfg.MaxRequestBodyBytes
            };

            int requestedTimeoutMs;
            if (int.TryParse(req.Headers["X-Block-Timeout-Ms"], out requestedTimeoutMs))
            {
                requestedTimeoutMs = Math.Max(1000, Math.Min(120000, requestedTimeoutMs));
                int requestedSeconds = (int)Math.Ceiling(requestedTimeoutMs / 1000.0);
                // A client may tighten the server budget, never loosen it.
                requestCfg.ExecutionTimeoutSeconds = Math.Min(cfg.ExecutionTimeoutSeconds, requestedSeconds);
            }

            // A client can opt into a stricter guard, but cannot disable the server's guard.
            if (req.Headers["X-Block-Network-Blocked"] == "1") requestCfg.NetworkBlocked = true;
            return requestCfg;
        }

        public static void StartApiServer(EngineConfig cfg, int port)
        {
            if (string.IsNullOrEmpty(cfg.ApiToken))
            {
                cfg.ApiToken = Guid.NewGuid().ToString("N");
            }
            
            Console.WriteLine(string.Format("\n[ BLOCK API SERVER IS RUNNING ON HTTP://LOCALHOST:{0} ]\n", port));
            Console.WriteLine(string.Format("=> Security Token: {0}", cfg.ApiToken));
            Console.WriteLine("=> Sandbox Directory: " + cfg.SandboxDir);
            Console.WriteLine("=> Max Request Body: " + (cfg.MaxRequestBodyBytes / 1024) + " KB");
            Console.WriteLine("Awaiting remote code execution requests...\n");
            
            HttpListener listener;
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(string.Format("http://localhost:{0}/", port));
                listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", port));
                listener.Start();
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("[ERROR] The local HTTP listener is not supported by this runtime/platform.");
                return;
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    Console.WriteLine("[ERROR] Access Denied! Please run your terminal as Administrator to start the API server.");
                    Console.WriteLine(string.Format("Alternatively, run once as admin: netsh http add urlacl url=http://localhost:{0}/ user=\"{1}\"", port, Environment.UserName));
                }
                else
                {
                    Console.WriteLine("[ERROR] Failed to start API Server: " + ex.Message);
                }
                return;
            }

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                Task.Run(async () => 
                {
                    HttpListenerRequest req = context.Request;
                    HttpListenerResponse res = context.Response;
                    bool slotAcquired = false;

                    try
                    {
                        // Security Fix: Exact match for localhost CORS origins to prevent http://localhost.attacker.com bypass
                        string origin = req.Headers["Origin"] ?? "";
                        bool isTrustedOrigin = string.IsNullOrEmpty(origin);
                        if (!isTrustedOrigin)
                        {
                            try
                            {
                                Uri originUri = new Uri(origin);
                                if (originUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                                    originUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                                {
                                    isTrustedOrigin = true;
                                }
                            }
                            catch { isTrustedOrigin = false; }
                        }
                        
                        if (isTrustedOrigin && !string.IsNullOrEmpty(origin))
                        {
                            res.Headers.Add("Access-Control-Allow-Origin", origin);
                        }
                        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-Api-Token, X-Block-Language, X-Block-Engine, X-Block-Timeout-Ms, X-Block-Max-Parallel, X-Block-Network-Blocked, X-Block-Cache");
                        
                        if (req.HttpMethod == "OPTIONS")
                        {
                            res.StatusCode = 204;
                            res.Close();
                            return;
                        }

                        // Security Fix: Strict token enforcement across ALL endpoints (including /api/run and /api/status)
                        string token = req.Headers["X-Api-Token"];
                        if (string.IsNullOrEmpty(token) || token != cfg.ApiToken)
                        {
                            res.StatusCode = 403;
                            byte[] errBuf = Encoding.UTF8.GetBytes("{\"error\":\"Forbidden: Invalid or missing API token. Use X-Api-Token header.\"}");
                            res.OutputStream.Write(errBuf, 0, errBuf.Length);
                            res.Close();
                            return;
                        }

                        // Do not let unauthenticated clients consume execution slots.
                        if (!_requestSlots.Wait(0))
                        {
                            res.StatusCode = 429;
                            res.Close();
                            return;
                        }
                        slotAcquired = true;

                        if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/api/status")
                        {
                            res.StatusCode = 200;
                            res.ContentType = "application/json; charset=utf-8";
                            var statusObj = new Dictionary<string, object>();
                            statusObj["status"] = "online";
                            statusObj["version"] = "v" + BlockVersion.Value;
                            byte[] buf = Encoding.UTF8.GetBytes(_serializer.Serialize(statusObj));
                            await res.OutputStream.WriteAsync(buf, 0, buf.Length).ConfigureAwait(false);
                            res.Close();
                            return;
                        }

                        if (req.HttpMethod == "POST" && (req.Url.AbsolutePath == "/api/run" || req.Url.AbsolutePath == "/api/run-file"))
                        {
                            try
                            {
                                // C3: Fix: Enforce max body size to prevent OOM DoS
                                if (req.ContentLength64 > cfg.MaxRequestBodyBytes)
                                {
                                    res.StatusCode = 413;
                                    byte[] errBuf = Encoding.UTF8.GetBytes(string.Format("{{\"error\":\"Request body too large. Max {0} bytes.\"}}", cfg.MaxRequestBodyBytes));
                                    res.OutputStream.Write(errBuf, 0, errBuf.Length);
                                    res.Close();
                                    return;
                                }

                                string body = "";
                                body = await ReadBodyAsync(req.InputStream, req.ContentEncoding, cfg.MaxRequestBodyBytes).ConfigureAwait(false);
                                
                                string code = "";
                                string targetPath = Path.Combine(cfg.SandboxDir, "remote.blk");
                                
                                if (req.Url.AbsolutePath == "/api/run-file")
                                {
                                    string requestedPath = "";
                                    try
                                    {
                                        var jsonDict = _serializer.Deserialize<Dictionary<string, object>>(body);
                                        if (jsonDict != null && jsonDict.ContainsKey("filePath")) requestedPath = Convert.ToString(jsonDict["filePath"]);
                                        else if (jsonDict != null && jsonDict.ContainsKey("path")) requestedPath = Convert.ToString(jsonDict["path"]);
                                    }
                                    catch { requestedPath = body.Trim(); }

                                    if (string.IsNullOrEmpty(requestedPath))
                                        throw new Exception("Missing 'filePath' parameter in JSON request.");

                                    requestedPath = Path.GetFullPath(requestedPath);
                                    
                                    // C4: Fix: Use boundary-aware sandbox check
                                    string sandboxRoot = Path.GetFullPath(cfg.SandboxDir);
                                    if (!IsPathInSandbox(requestedPath, sandboxRoot))
                                        throw new UnauthorizedAccessException(string.Format("Security: path '{0}' escapes sandbox '{1}'.", requestedPath, sandboxRoot));

                                    if (!File.Exists(requestedPath))
                                        throw new Exception(string.Format("File not found: {0}", requestedPath));

                                    FileInfo requestedInfo = new FileInfo(requestedPath);
                                    if (requestedInfo.Length > SecurityLimits.MaxScriptBytes)
                                        throw new InvalidOperationException(string.Format("Script file exceeds the {0} MiB limit.", SecurityLimits.MaxScriptBytes / (1024 * 1024)));

                                    code = File.ReadAllText(requestedPath);
                                    targetPath = requestedPath;
                                }
                                else
                                {
                                    try
                                    {
                                        var jsonDict = _serializer.Deserialize<Dictionary<string, object>>(body);
                                        if (jsonDict != null && jsonDict.ContainsKey("code")) code = Convert.ToString(jsonDict["code"]);
                                        else code = body;
                                    }
                                    catch { code = body; }
                                }

                                StringBuilder outputBuffer = new StringBuilder();
                                Action<string> captureOutput = (text) => { SecurityLimits.AppendOutput(outputBuffer, text); };
                                EngineConfig requestCfg = BuildRequestConfig(cfg, req);
                                
                                Dictionary<string, object> initialState = new Dictionary<string, object>();
                                var blocks = Parser.ParseBlocks(code, targetPath, requestCfg);
                                
                                await Executor.ExecuteBlocksAsync(blocks, requestCfg, targetPath, initialState, null, captureOutput).ConfigureAwait(false);
                                
                                res.StatusCode = 200;
                                res.ContentType = "application/json; charset=utf-8";
                                
                                var result = new Dictionary<string, object>();
                                result["status"] = "success";
                                result["output"] = outputBuffer.ToString();
                                byte[] buf = Encoding.UTF8.GetBytes(_serializer.Serialize(result));
                                await res.OutputStream.WriteAsync(buf, 0, buf.Length).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                res.StatusCode = ex is UnauthorizedAccessException ? 403 : 500;
                                res.ContentType = "application/json; charset=utf-8";
                                var result = new Dictionary<string, object>();
                                result["status"] = "error";
                                // Do not disclose local paths or stack details through the API.
                                result["error"] = ex is UnauthorizedAccessException
                                    ? "Request path is outside the configured sandbox."
                                    : "Execution request failed.";
                                byte[] buf = Encoding.UTF8.GetBytes(_serializer.Serialize(result));
                                try { res.OutputStream.Write(buf, 0, buf.Length); } catch { }
                            }
                            finally
                            {
                                try { res.Close(); } catch { }
                            }
                        }
                        else
                        {
                            res.StatusCode = 404;
                            res.Close();
                        }
                    }
                    catch (Exception)
                    {
                        try { res.Close(); } catch { }
                    }
                    finally
                    {
                        if (slotAcquired) _requestSlots.Release();
                    }
                });
            }
        }
    }
}
#endif
