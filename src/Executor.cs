using System;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Web;
using System.Web.Script.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BlockEngine
{
    public static class Executor
    {
        // Keep JavaScriptSerializer for .NET Framework 4.x compatibility
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer { MaxJsonLength = (int)SecurityLimits.MaxJsonBytes };
        private static readonly object _pathNormalizationLock = new object();
        private static bool _pathEnvironmentNormalized;

        public static async Task ExecuteBlocksAsync(List<CodeBlock> blocks, EngineConfig cfg, string scriptPath, Dictionary<string, object> currentState, System.Net.HttpListenerResponse response, Action<string> outputCallback = null)
        {
            if (outputCallback == null) outputCallback = Console.Write;
            string tempDir = Path.Combine(Path.GetTempPath(), "BlockEngine_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            
            try
            {
#if BLOCK_PLUS
                // Inline <define> entries are registered while parsing the script,
                // before this method is called. Do not clear them here or every
                // inline custom language would be removed before its first block.
#endif
                foreach (var block in blocks)
                {
        if (string.Equals(block.Language, "block", StringComparison.OrdinalIgnoreCase))
        {
            NativeBlockProgram.Execute(block.Code, currentState, outputCallback);
        }
                    else if (string.Equals(block.Language, "del", StringComparison.OrdinalIgnoreCase))
                    {
                        NativeStateOperations.Delete(block.Code, currentState, outputCallback);
                    }
                    else if (block.Language == "html" || block.Language == "json")
                    {
                        ProcessHtmlJsonBlock(block, currentState, response, outputCallback);
                    }
                    else if (block.Language != "block")
                    {
#if BLOCK_PLUS
                        CustomLangDef customDef;
                        if (cfg.AllowCustomDefinitions && CustomLangRegistry.TryGet(block.Language, out customDef))
                        {
                            await RunCustomLangAsync(block, customDef, scriptPath, currentState, tempDir, cfg.ExecutionTimeoutSeconds, cfg, outputCallback);
                            continue;
                        }
#endif
                        CheckLanguageEnabled(block.Language, cfg);
                        await RunSandboxedProcessAsync(block, scriptPath, currentState, tempDir, cfg, response, outputCallback);
                    }
                }
            }
            finally
            {
#if BLOCK_PLUS
                CustomLangRegistry.ClearInline();
#endif
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static void ProcessHtmlJsonBlock(CodeBlock block, Dictionary<string, object> state, System.Net.HttpListenerResponse response, Action<string> outputCallback)
        {
            string codeToRender = block.Code;
#if BLOCK_PLUS
            codeToRender = RenderBlockPlusTemplate(codeToRender, state, block.Language);
#endif
            string output = RenderTemplateVariables(codeToRender, state, block.Language);

            if (output.Length > SecurityLimits.MaxCapturedOutputChars)
                throw new InvalidOperationException("Rendered output exceeds the configured output limit.");

            if (response != null)
            {
                byte[] buf = Encoding.UTF8.GetBytes(output);
                if (block.Language == "html") response.ContentType = "text/html; charset=utf-8";
                if (block.Language == "json") response.ContentType = "application/json; charset=utf-8";
                response.OutputStream.Write(buf, 0, buf.Length);
            }
            else
            {
                if (block.Language == "html")
                {
                    // M2: Fix: Use unique temp path to avoid concurrent overwrites
                    string outPath = Path.Combine(Path.GetTempPath(), "block_output_" + Guid.NewGuid().ToString("N") + ".html");
                    File.WriteAllText(outPath, output, Encoding.UTF8);
                    outputCallback(string.Format("[HTML] Output written to -> {0}\n", outPath));
                    try
                    {
                        // Clean up old HTML temp output files older than 1 hour to prevent temp accumulation
                        string tempDir = Path.GetTempPath();
                        string[] oldHtmlFiles = Directory.GetFiles(tempDir, "block_output_*.html");
                        DateTime cutoff = DateTime.Now.AddHours(-1);
                        foreach (string file in oldHtmlFiles)
                        {
                            if (file != outPath && File.GetLastWriteTime(file) < cutoff)
                            {
                                try { File.Delete(file); } catch { }
                            }
                        }
                    }
                    catch { }
                }
                else
                {
                    outputCallback(output + "\n");
                }
            }
        }

        private static string RenderTemplateVariables(string template, Dictionary<string, object> state, string language)
        {
            return System.Text.RegularExpressions.Regex.Replace(template, @"\{\{\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\}\}", match =>
            {
                string varName = match.Groups[1].Value;
                if (state.ContainsKey(varName))
                {
                    object val = state[varName];
                    // JSON substitution must serialize every value, not only strings and arrays.
                    // This preserves lowercase booleans, invariant numbers, lists, maps, and null.
                    if (string.Equals(language, "json", StringComparison.OrdinalIgnoreCase))
                        return _serializer.Serialize(val);

                    string rawVal = val == null ? "" : val.ToString();
                    if (string.Equals(language, "html", StringComparison.OrdinalIgnoreCase))
                        return EncodeHtmlTemplateValue(template, match.Index, rawVal);
                    return rawVal;
                }
                return match.Value;
            });
        }

        private static string EncodeHtmlTemplateValue(string template, int placeholderIndex, string value)
        {
            string before = template.Substring(0, placeholderIndex);
            string lowerBefore = before.ToLowerInvariant();

            if (IsInsideRawTextElement(lowerBefore, "script") || IsInsideRawTextElement(lowerBefore, "style"))
                throw new InvalidOperationException("HTML template values cannot be inserted into <script> or <style> content.");

            int lastOpen = before.LastIndexOf('<');
            int lastClose = before.LastIndexOf('>');
            if (lastOpen <= lastClose)
                return WebUtility.HtmlEncode(value);

            string tagPrefix = before.Substring(lastOpen);
            string attributeName;
            bool quoted;
            if (!TryGetAttributeContext(tagPrefix, out attributeName, out quoted))
                throw new InvalidOperationException("HTML template values cannot be used as tag or attribute names.");

            string normalizedAttribute = attributeName.ToLowerInvariant();
            if (normalizedAttribute.StartsWith("on", StringComparison.Ordinal) ||
                normalizedAttribute == "style" || normalizedAttribute == "srcdoc")
                throw new InvalidOperationException("HTML template values are not allowed in executable HTML attributes: " + attributeName + ".");

            if (IsUrlAttribute(normalizedAttribute) && !IsSafeTemplateUrl(value))
                throw new InvalidOperationException("Unsafe URL scheme in HTML template attribute: " + attributeName + ".");

            return quoted ? WebUtility.HtmlEncode(value) : EncodeUnquotedAttribute(value);
        }

        private static bool IsInsideRawTextElement(string lowerBefore, string element)
        {
            int open = lowerBefore.LastIndexOf("<" + element, StringComparison.Ordinal);
            int close = lowerBefore.LastIndexOf("</" + element, StringComparison.Ordinal);
            if (open <= close) return false;
            int openEnd = lowerBefore.IndexOf('>', open);
            return openEnd >= 0;
        }

        private static bool TryGetAttributeContext(string tagPrefix, out string attributeName, out bool quoted)
        {
            attributeName = null;
            quoted = false;
            char activeQuote = '\0';
            int quoteStart = -1;
            for (int i = 0; i < tagPrefix.Length; i++)
            {
                char c = tagPrefix[i];
                if (activeQuote == '\0' && (c == '\'' || c == '"'))
                {
                    activeQuote = c;
                    quoteStart = i;
                }
                else if (activeQuote == c)
                {
                    activeQuote = '\0';
                    quoteStart = -1;
                }
            }

            string beforeValue;
            if (activeQuote != '\0')
            {
                quoted = true;
                beforeValue = tagPrefix.Substring(0, quoteStart);
            }
            else
            {
                int equals = tagPrefix.LastIndexOf('=');
                if (equals < 0) return false;
                string valuePrefix = tagPrefix.Substring(equals + 1);
                if (valuePrefix.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0) return false;
                beforeValue = tagPrefix.Substring(0, equals + 1);
            }

            var nameMatch = System.Text.RegularExpressions.Regex.Match(beforeValue,
                @"([A-Za-z_:][-A-Za-z0-9_:.]*)\s*=\s*$");
            if (!nameMatch.Success) return false;
            attributeName = nameMatch.Groups[1].Value;
            return true;
        }

        private static bool IsUrlAttribute(string name)
        {
            return name == "href" || name == "src" || name == "action" || name == "formaction" ||
                   name == "poster" || name == "cite" || name == "background" || name == "xlink:href";
        }

        private static bool IsSafeTemplateUrl(string value)
        {
            string candidate = (value ?? "").Trim();
            if (candidate.Length == 0 || candidate[0] == '#' || candidate[0] == '/' ||
                candidate.StartsWith("./", StringComparison.Ordinal) || candidate.StartsWith("../", StringComparison.Ordinal) ||
                candidate[0] == '?') return true;

            Uri uri;
            if (!Uri.TryCreate(candidate, UriKind.RelativeOrAbsolute, out uri)) return false;
            if (!uri.IsAbsoluteUri) return true;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ||
                   uri.Scheme == Uri.UriSchemeMailto || uri.Scheme == "tel";
        }

        private static string EncodeUnquotedAttribute(string value)
        {
            StringBuilder encoded = new StringBuilder();
            foreach (char c in value ?? "")
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' || c == ':')
                    encoded.Append(c);
                else
                    encoded.Append("&#x").Append(((int)c).ToString("X")).Append(';');
            }
            return encoded.ToString();
        }

#if BLOCK_PLUS
        private static string RenderBlockPlusTemplate(string template, Dictionary<string, object> state, string language)
        {
            // 1. Process {{#if key}} ... {{else}} ... {{/if}}
            template = System.Text.RegularExpressions.Regex.Replace(template,
                @"\{\{#if\s+([a-zA-Z_][a-zA-Z0-9_]*)\}\}([\s\S]*?)(?:\{\{else\}\}([\s\S]*?))?\{\{\/if\}\}", match =>
            {
                string key = match.Groups[1].Value;
                bool isTrue = state.ContainsKey(key) && state[key] != null &&
                             !state[key].Equals(false) && !state[key].Equals("false") &&
                             !state[key].Equals(0) && !state[key].Equals("");
                return isTrue ? match.Groups[2].Value : (match.Groups[3].Success ? match.Groups[3].Value : "");
            });

            // 2. Process {{#unless key}} ... {{/unless}}
            template = System.Text.RegularExpressions.Regex.Replace(template,
                @"\{\{#unless\s+([a-zA-Z_][a-zA-Z0-9_]*)\}\}([\s\S]*?)\{\{\/unless\}\}", match =>
            {
                string key = match.Groups[1].Value;
                bool isTrue = state.ContainsKey(key) && state[key] != null &&
                             !state[key].Equals(false) && !state[key].Equals("false") &&
                             !state[key].Equals(0) && !state[key].Equals("");
                return !isTrue ? match.Groups[2].Value : "";
            });

            // 3. Process {{#each key}} ... {{/each}}
            template = System.Text.RegularExpressions.Regex.Replace(template,
                @"\{\{#each\s+([a-zA-Z_][a-zA-Z0-9_]*)\}\}([\s\S]*?)\{\{\/each\}\}", match =>
            {
                string key = match.Groups[1].Value;
                string inner = match.Groups[2].Value;
                if (!state.ContainsKey(key) || state[key] == null) return "";

                System.Collections.IEnumerable list = state[key] as System.Collections.IEnumerable;
                if (list == null) return "";

                StringBuilder sb = new StringBuilder();
                int idx = 0;
                foreach (object item in list)
                {
                    string itemResult = inner.Replace("{{@index}}", idx.ToString());
                    Dictionary<string, object> dict = item as Dictionary<string, object>;
                    Dictionary<string, object> itemState = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            itemState[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        itemState["this"] = item;
                    }
                    itemResult = RenderTemplateVariables(itemResult, itemState, language);
                    sb.Append(itemResult);
                    idx++;
                }
                return sb.ToString();
            });

            return template;
        }

        private static async Task RunCustomLangAsync(CodeBlock block, CustomLangDef def, string scriptPath, Dictionary<string, object> state, string tempDir, int timeoutSeconds, EngineConfig cfg, Action<string> outputCallback)
        {
            NormalizeWindowsPathEnvironment();
            string ext = string.IsNullOrEmpty(def.ext) ? "." + block.Language : def.ext;
            if (!ext.StartsWith(".")) ext = "." + ext;
            string tempScript = Path.Combine(tempDir, "script" + ext);
            File.WriteAllText(tempScript, block.Code, new UTF8Encoding(false));

            string cmdExe = (def.cmd ?? "").Trim();
            string cmdArgs = (def.args ?? "").Trim();
            
            // L2: Fix: Handle executable paths with spaces (quoted or unquoted)
            if (cmdExe.StartsWith("\""))
            {
                int closeQuote = cmdExe.IndexOf('"', 1);
                if (closeQuote > 0)
                {
                    string afterQuote = cmdExe.Substring(closeQuote + 1).Trim();
                    cmdExe = cmdExe.Substring(1, closeQuote - 1);
                    if (!string.IsNullOrEmpty(afterQuote))
                        cmdArgs = (afterQuote + " " + cmdArgs).Trim();
                }
            }
            else if (cmdExe.Contains(" "))
            {
                int spaceIdx = cmdExe.IndexOf(' ');
                cmdArgs = (cmdExe.Substring(spaceIdx + 1) + " " + cmdArgs).Trim();
                cmdExe = cmdExe.Substring(0, spaceIdx);
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = GetExecutable(cmdExe);
            psi.Arguments = string.IsNullOrEmpty(cmdArgs) ? string.Format("\"{0}\"", tempScript) : string.Format("{0} \"{1}\"", cmdArgs, tempScript);
            psi.WorkingDirectory = Path.GetDirectoryName(scriptPath);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.EnvironmentVariables["BLOCK_NETWORK_BLOCKED"] = cfg.NetworkBlocked ? "1" : "0";

            using (Process p = new Process { StartInfo = psi })
            {
                StringBuilder sb = new StringBuilder();
                p.OutputDataReceived += (s, e) => { if (e.Data != null) SecurityLimits.AppendOutput(sb, e.Data + Environment.NewLine); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) SecurityLimits.AppendOutput(sb, e.Data + Environment.NewLine); };
                p.Start();
                ProcessSandbox processSandbox = ProcessSandbox.Attach(p);
                try
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();

                    bool finished = await Task.Run(() => p.WaitForExit(timeoutSeconds * 1000)).ConfigureAwait(false);
                    if (!finished)
                    {
                        // H1: Fix: Kill entire process tree
                        KillProcessTree(p);
                        throw new Exception(string.Format("[{0}] Custom language process timed out ({1}s).", block.Language.ToUpper(), timeoutSeconds));
                    }
                    await Task.Run(() => p.WaitForExit()).ConfigureAwait(false);
                    if (p.ExitCode != 0)
                    {
                        string customOutput = sb.ToString().Trim();
                        throw new Exception(string.Format("[{0}] Custom language process exited with error code {1}: {2}",
                            block.Language.ToUpper(), p.ExitCode, customOutput));
                    }
                    outputCallback(string.Format("[{0}] Output:\n{1}", block.Language.ToUpper(), sb.ToString()));
                }
                finally
                {
                    processSandbox.Dispose();
                }
            }
        }

        private static void EnsureLanguageAvailable(string lang)
        {
            string exe = GetExecutable(lang);
            if (File.Exists(exe)) return;

            string[] paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';');
            foreach (string p in paths)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                string testPath = Path.Combine(p.Trim(), exe);
                if (File.Exists(testPath)) return;
            }

            throw new FileNotFoundException(string.Format(
                "Runtime for '<{0}>' was not found. Install it from its official publisher, add '{1}' to PATH, then run 'block runtimes'. Block never installs host runtimes automatically.",
                lang, exe));
        }
#endif

        // H1: Fix: Kill entire process tree to avoid zombie/orphan child processes
        private static void KillProcessTree(Process parent)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var killer = new ProcessStartInfo("taskkill", string.Format("/T /F /PID {0}", parent.Id))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var kp = Process.Start(killer))
                    {
                        if (kp != null) kp.WaitForExit(2000);
                    }
                }
                else
                {
                    // .NET 5+ has p.Kill(entireProcessTree: true). For .NET 4.x on Linux, best-effort:
                    parent.Kill();
                }
            }
            catch { try { parent.Kill(); } catch { } }
        }

        private static async Task RunSandboxedProcessAsync(CodeBlock block, string scriptPath, Dictionary<string, object> state, string tempDir, EngineConfig cfg, System.Net.HttpListenerResponse response, Action<string> outputCallback)
        {
            NormalizeWindowsPathEnvironment();
#if BLOCK_PLUS
            EnsureLanguageAvailable(block.Language);
#endif
            string exePath = GetExecutable(block.Language);
            if (string.IsNullOrEmpty(exePath))
                throw new Exception(string.Format("Could not find executable for language: {0}", block.Language));

            // Compilers and sqlite do not accept a script path in the same way as
            // interpreters. Keep these flows explicit so a successful compilation
            // cannot be reported as a successful execution.
            if (IsSqlLanguage(block.Language))
            {
                await RunSqlProcessAsync(block, scriptPath, cfg, outputCallback).ConfigureAwait(false);
                return;
            }
            if (IsCompileRunLanguage(block.Language))
            {
                await RunCompiledProcessAsync(block, scriptPath, tempDir, state, cfg, outputCallback).ConfigureAwait(false);
                return;
            }

            string wrapperCode = WrapperGenerator.GenerateWrapper(block.Language, block.Code);
            string tempScript = Path.Combine(tempDir, "script." + GetExtension(block.Language));
            File.WriteAllText(tempScript, wrapperCode, new UTF8Encoding(false));

            string stateJson = _serializer.Serialize(state);
            string tempStateFile = Path.Combine(tempDir, "state.in.json");
            string tempStateOut = Path.Combine(tempDir, "state.out.json");
            File.WriteAllText(tempStateFile, stateJson, new UTF8Encoding(false));

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = exePath;
            psi.WorkingDirectory = Path.GetDirectoryName(scriptPath);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            
            // Set state environment variables
            psi.EnvironmentVariables["BLOCK_STATE_JSON"] = stateJson;
            psi.EnvironmentVariables["BLOCK_STATE_FILE"] = tempStateFile;
            psi.EnvironmentVariables["BLOCK_STATE_OUT"] = tempStateOut;
            psi.EnvironmentVariables["BLOCK_NETWORK_BLOCKED"] = cfg.NetworkBlocked ? "1" : "0";

            // Only redirect STDIN if we are not in terminal mode or if response is non-null
            bool isTerminalMode = (response == null);
            psi.RedirectStandardInput = !isTerminalMode;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            
            if (block.Language == "ps" || block.Language == "powershell")
            {
                psi.Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -File \"{0}\"", tempScript);
                // Encoding.UTF8 writes the single BOM PowerShell needs; do not prepend a second BOM.
                File.WriteAllText(tempScript, wrapperCode, Encoding.UTF8);
            }
            else if (block.Language == "go" || block.Language == "golang")
            {
                psi.Arguments = string.Format("run \"{0}\"", tempScript);
            }
            else if (block.Language == "zig")
            {
                psi.Arguments = string.Format("run \"{0}\"", tempScript);
            }
            else
            {
                psi.Arguments = string.Format("\"{0}\"", tempScript);
            }

            using (Process p = new Process { StartInfo = psi })
            {
                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder errorBuilder = new StringBuilder();

                DataReceivedEventHandler outHandler = (s, e) => { if (e.Data != null) SecurityLimits.AppendOutput(outputBuilder, e.Data + Environment.NewLine); };
                DataReceivedEventHandler errHandler = (s, e) => { if (e.Data != null) SecurityLimits.AppendOutput(errorBuilder, e.Data + Environment.NewLine); };

                p.OutputDataReceived += outHandler;
                p.ErrorDataReceived += errHandler;

                p.Start();
                ProcessSandbox processSandbox = ProcessSandbox.Attach(p);

                // If STDIN is redirected (e.g. API mode), write state and close
                if (psi.RedirectStandardInput)
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(stateJson);
                    await p.StandardInput.BaseStream.WriteAsync(inputBytes, 0, inputBytes.Length).ConfigureAwait(false);
                    p.StandardInput.Close();
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                int timeoutMs = cfg.ExecutionTimeoutSeconds * 1000;
                try
                {
                    // Do not rely on Process.Exited here. On .NET Framework it can
                    // race with short-lived interpreter processes on Windows CI.
                    // Waiting on the process handle is deterministic and still
                    // lets us enforce the configured limit and kill descendants.
                    bool finished = await Task.Run(() => p.WaitForExit(timeoutMs)).ConfigureAwait(false);
                    if (!finished)
                    {
                        // H1: Fix: Kill entire process tree
                        KillProcessTree(p);
                        throw new Exception(string.Format("[{0}] Process execution timed out ({1}s limit).", block.Language.ToUpper(), cfg.ExecutionTimeoutSeconds));
                    }

                    // Flush async streams
                    await Task.Run(() => p.WaitForExit()).ConfigureAwait(false);
                }
                finally
                {
                    processSandbox.Dispose();
                    p.OutputDataReceived -= outHandler;
                    p.ErrorDataReceived -= errHandler;
                }

                if (p.ExitCode != 0)
                {
                    string errOut = errorBuilder.ToString();
                    if (string.IsNullOrWhiteSpace(errOut)) errOut = outputBuilder.ToString();
                    throw new Exception(string.Format("[{0}] Subprocess exited with error code {1}: {2}", block.Language.ToUpper(), p.ExitCode, errOut.Trim()));
                }

                string stdout = outputBuilder.ToString();
                string stderr = errorBuilder.ToString();

                if (!string.IsNullOrEmpty(stderr))
                    outputCallback(stderr);

                // 1. Read state from output state file
                if (File.Exists(tempStateOut))
                {
                    try
                    {
                        if (new FileInfo(tempStateOut).Length > SecurityLimits.MaxJsonBytes)
                            throw new InvalidDataException("BLOCK_STATE_OUT exceeds the 4 MiB safety limit.");
                        string fileStateJson = File.ReadAllText(tempStateOut, Encoding.UTF8);
                        if (!string.IsNullOrEmpty(fileStateJson))
                        {
                            var newState = _serializer.Deserialize<Dictionary<string, object>>(fileStateJson);
                            if (newState != null)
                            {
                                foreach (var kvp in newState) state[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidDataException("Runtime produced an invalid BLOCK_STATE_OUT payload.", ex);
                    }
                }

                // 2. Parse clean output and stdout state marker
                string stateMarker = "__BLOCK_STATE__:";
                int markerIndex = stdout.LastIndexOf(stateMarker);
                if (markerIndex >= 0)
                {
                    string cleanOutput = stdout.Substring(0, markerIndex);
                    if (!string.IsNullOrWhiteSpace(cleanOutput)) outputCallback(cleanOutput);
                    
                    if (!File.Exists(tempStateOut))
                    {
                        string newStateJson = stdout.Substring(markerIndex + stateMarker.Length).Trim();
                        if (!string.IsNullOrEmpty(newStateJson))
                        {
                            try
                            {
                                var newState = _serializer.Deserialize<Dictionary<string, object>>(newStateJson);
                                if (newState != null)
                                {
                                    foreach (var kvp in newState) state[kvp.Key] = kvp.Value;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidDataException("Runtime produced an invalid inline state payload.", ex);
                            }
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(stdout)) outputCallback(stdout);
                }
            }
        }

        // Windows/.NET Framework treats environment names case-insensitively, but
        // some hosts expose both PATH and Path. ProcessStartInfo then throws while
        // copying the inherited environment. Normalize the process copy before
        // creating a child process; this does not modify the user's machine PATH.
        private static void NormalizeWindowsPathEnvironment()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;
            lock (_pathNormalizationLock)
            {
                if (_pathEnvironmentNormalized) return;
                try
                {
                    string path = Environment.GetEnvironmentVariable("Path");
                    if (string.IsNullOrEmpty(path)) path = Environment.GetEnvironmentVariable("PATH");
                    if (!string.IsNullOrEmpty(path))
                    {
                        Environment.SetEnvironmentVariable("PATH", null, EnvironmentVariableTarget.Process);
                        Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);
                    }
                    _pathEnvironmentNormalized = true;
                }
                catch { }
            }
        }

        private sealed class ProcessCapture
        {
            public int ExitCode;
            public string Output;
            public string Error;
        }

        private static bool IsSqlLanguage(string lang)
        {
            return string.Equals(lang, "sql", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCompileRunLanguage(string lang)
        {
            return string.Equals(lang, "c", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(lang, "cpp", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(lang, "c++", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(lang, "rust", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(lang, "rs", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task RunSqlProcessAsync(CodeBlock block, string scriptPath, EngineConfig cfg, Action<string> outputCallback)
        {
            ProcessCapture result = await RunProcessCaptureAsync(
                GetExecutable("sql"), ":memory:", Path.GetDirectoryName(scriptPath), block.Code,
                cfg.ExecutionTimeoutSeconds, block.Language).ConfigureAwait(false);
            if (result.ExitCode != 0)
                throw new Exception(string.Format("[SQL] sqlite3 exited with error code {0}: {1}", result.ExitCode, result.Error.Trim()));
            if (!string.IsNullOrWhiteSpace(result.Error)) outputCallback(result.Error);
            if (!string.IsNullOrWhiteSpace(result.Output)) outputCallback(result.Output);
        }

        private static async Task RunCompiledProcessAsync(CodeBlock block, string scriptPath, string tempDir, Dictionary<string, object> state, EngineConfig cfg, Action<string> outputCallback)
        {
            string sourcePath = Path.Combine(tempDir, "program." + GetExtension(block.Language));
            string outputPath = Path.Combine(tempDir, "program.exe");
            string stateJson = _serializer.Serialize(state);
            string stateFile = Path.Combine(tempDir, "state.in.json");
            string stateOutFile = Path.Combine(tempDir, "state.out.json");
            File.WriteAllText(sourcePath, block.Code, new UTF8Encoding(false));
            File.WriteAllText(stateFile, stateJson, new UTF8Encoding(false));

            Dictionary<string, string> environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BLOCK_STATE_JSON", stateJson },
                { "BLOCK_STATE_FILE", stateFile },
                { "BLOCK_STATE_OUT", stateOutFile },
                { "BLOCK_NETWORK_BLOCKED", cfg.NetworkBlocked ? "1" : "0" }
            };

            string compiler = GetExecutable(block.Language);
            string compileArgs;
            if (string.Equals(block.Language, "rust", StringComparison.OrdinalIgnoreCase) || string.Equals(block.Language, "rs", StringComparison.OrdinalIgnoreCase))
                compileArgs = string.Format("\"{0}\" -o \"{1}\"", sourcePath, outputPath);
            else if (string.Equals(block.Language, "cpp", StringComparison.OrdinalIgnoreCase) || string.Equals(block.Language, "c++", StringComparison.OrdinalIgnoreCase))
                compileArgs = string.Format("-std=c++17 -o \"{0}\" \"{1}\"", outputPath, sourcePath);
            else
                compileArgs = string.Format("-o \"{0}\" \"{1}\"", outputPath, sourcePath);

            ProcessCapture compile = await RunProcessCaptureAsync(
                compiler, compileArgs, Path.GetDirectoryName(scriptPath), null,
                cfg.ExecutionTimeoutSeconds, block.Language + " compiler", environment).ConfigureAwait(false);
            if (compile.ExitCode != 0)
            {
                string details = string.IsNullOrWhiteSpace(compile.Error) ? compile.Output : compile.Error;
                throw new Exception(string.Format("[{0}] compilation failed with error code {1}: {2}", block.Language.ToUpper(), compile.ExitCode, details.Trim()));
            }

            if (!File.Exists(outputPath))
                throw new Exception(string.Format("[{0}] compiler completed without producing an executable.", block.Language.ToUpper()));

            ProcessCapture run = await RunProcessCaptureAsync(
                outputPath, "", Path.GetDirectoryName(scriptPath), null,
                cfg.ExecutionTimeoutSeconds, block.Language + " runtime", environment).ConfigureAwait(false);
            if (run.ExitCode != 0)
            {
                string details = string.IsNullOrWhiteSpace(run.Error) ? run.Output : run.Error;
                throw new Exception(string.Format("[{0}] executable exited with error code {1}: {2}", block.Language.ToUpper(), run.ExitCode, details.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(run.Error)) outputCallback(run.Error);
            if (!string.IsNullOrWhiteSpace(run.Output)) outputCallback(run.Output);
            MergeStateFile(state, stateOutFile);
        }

        private static async Task<ProcessCapture> RunProcessCaptureAsync(string executable, string arguments, string workingDirectory,
            string standardInput, int timeoutSeconds, string label, IDictionary<string, string> environment = null)
        {
            return await Task.Run(() =>
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments ?? "",
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = standardInput != null,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                if (environment != null)
                {
                    foreach (KeyValuePair<string, string> variable in environment)
                        psi.EnvironmentVariables[variable.Key] = variable.Value ?? "";
                }

                using (Process process = new Process { StartInfo = psi })
                {
                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) SecurityLimits.AppendOutput(output, e.Data + Environment.NewLine); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) SecurityLimits.AppendOutput(error, e.Data + Environment.NewLine); };
                    process.Start();
                    ProcessSandbox processSandbox = ProcessSandbox.Attach(process);
                    if (standardInput != null)
                    {
                        process.StandardInput.Write(standardInput);
                        process.StandardInput.Close();
                    }
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    try
                    {
                        if (!process.WaitForExit(Math.Max(1, timeoutSeconds) * 1000))
                        {
                            KillProcessTree(process);
                            throw new Exception(string.Format("[{0}] process timed out ({1}s limit).", label.ToUpper(), timeoutSeconds));
                        }
                        process.WaitForExit();
                        return new ProcessCapture { ExitCode = process.ExitCode, Output = output.ToString(), Error = error.ToString() };
                    }
                    finally
                    {
                        processSandbox.Dispose();
                    }
                }
            }).ConfigureAwait(false);
        }

        private static void MergeStateFile(Dictionary<string, object> state, string stateFile)
        {
            if (state == null || string.IsNullOrEmpty(stateFile) || !File.Exists(stateFile)) return;
            try
            {
                if (new FileInfo(stateFile).Length > SecurityLimits.MaxJsonBytes)
                    throw new InvalidDataException("BLOCK_STATE_OUT exceeds the 4 MiB safety limit.");
                string json = File.ReadAllText(stateFile, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return;
                Dictionary<string, object> newState = _serializer.Deserialize<Dictionary<string, object>>(json);
                if (newState == null) return;
                foreach (KeyValuePair<string, object> item in newState) state[item.Key] = item.Value;
            }
            catch (Exception error)
            {
                throw new InvalidDataException("Runtime produced an invalid BLOCK_STATE_OUT payload.", error);
            }
        }

        private static void CheckLanguageEnabled(string lang, EngineConfig cfg)
        {
            string normalized = (lang ?? "").ToLowerInvariant();
#if BLOCK_LITE
            string[] supportedLangs = { "python", "py", "js", "javascript" };
            if (Array.IndexOf(supportedLangs, normalized) < 0)
            {
                throw new Exception(string.Format("Language '<{0}>' is not supported in Block Lite. Block Lite supports <py>/<python>, <js>/<javascript>, and <html>.", lang));
            }
#elif !BLOCK_PLUS
            string[] plusExclusiveLangs = { "c", "cpp", "c++", "go", "golang", "rust", "rs", "java", "ts", "typescript", "cs", "csharp", "kotlin", "kt", "dart", "zig", "perl", "pl", "bash", "sh", "r" };
            foreach (string pl in plusExclusiveLangs)
            {
                if (string.Equals(lang, pl, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(string.Format("Language '<{0}>' is exclusive to Block+ (Block Plus). Please upgrade to Block+ to execute this block.", lang));
                }
            }
            string[] supportedLangs = { "python", "py", "js", "javascript", "php", "ruby", "rb", "lua", "sql", "ps", "powershell" };
            if (Array.IndexOf(supportedLangs, normalized) < 0)
                throw new Exception(string.Format("Unknown language tag '<{0}>'. Use a built-in tag or enable and define a reviewed custom runtime in Block+.", lang));
#else
            string[] supportedLangs = {
                "python", "py", "js", "javascript", "php", "ruby", "rb", "lua", "sql", "ps", "powershell",
                "c", "cpp", "c++", "go", "golang", "rust", "rs", "java", "ts", "typescript", "cs", "csharp",
                "kotlin", "kt", "dart", "zig", "perl", "pl", "bash", "sh", "r"
            };
            if (Array.IndexOf(supportedLangs, normalized) < 0)
                throw new Exception(string.Format("Unknown language tag '<{0}>'. Enable AllowCustomDefinitions and define a reviewed custom runtime before using it.", lang));
#endif
            if ((lang == "python" || lang == "py") && !cfg.PythonEnabled) throw new Exception("Security: Python execution is disabled in config.");
            if (lang == "php" && !cfg.PhpEnabled) throw new Exception("Security: PHP execution is disabled in config.");
            if ((lang == "ruby" || lang == "rb") && !cfg.RubyEnabled) throw new Exception("Security: Ruby execution is disabled in config.");
            if ((lang == "js" || lang == "javascript") && !cfg.JsEnabled) throw new Exception("Security: JS execution is disabled in config.");
            if (lang == "lua" && !cfg.LuaEnabled) throw new Exception("Security: Lua execution is disabled in config.");
            if ((lang == "ps" || lang == "powershell") && !cfg.PowerShellEnabled) throw new Exception("Security: PowerShell execution is disabled in config.");
            if (lang == "sql" && !cfg.SqlEnabled) throw new Exception("Security: SQL execution is disabled in config.");
        }

        private static string GetExecutable(string lang)
        {
            string fallback = lang;
            if (lang == "ps" || lang == "powershell") fallback = "powershell.exe";
            else if (lang == "js" || lang == "javascript") fallback = "node.exe";
            else if (lang == "python" || lang == "py") fallback = "python.exe";
            else if (lang == "ruby" || lang == "rb") fallback = "ruby.exe";
            else if (lang == "php") fallback = "php.exe";
            else if (lang == "lua") fallback = "lua.exe";
            else if (lang == "sql") fallback = "sqlite3.exe";
            else if (lang == "c") fallback = "gcc.exe";
            else if (lang == "cpp" || lang == "c++") fallback = "g++.exe";
            else if (lang == "go" || lang == "golang") fallback = "go.exe";
            else if (lang == "rust" || lang == "rs") fallback = "rustc.exe";
            else if (lang == "java") fallback = "java.exe";
            else if (lang == "ts" || lang == "typescript") fallback = "ts-node.exe";
            else if (lang == "cs" || lang == "csharp") fallback = "csc.exe";
            else if (lang == "kotlin" || lang == "kt") fallback = "kotlinc.exe";
            else if (lang == "dart") fallback = "dart.exe";
            else if (lang == "zig") fallback = "zig.exe";
            else if (lang == "perl" || lang == "pl") fallback = "perl.exe";
            else if (lang == "bash" || lang == "sh") fallback = "bash.exe";
            else if (lang == "r") fallback = "Rscript.exe";
            
            // Smart candidate checks to bypass broken PATH entries (e.g. invalid C:\Python314)
            if (lang == "python" || lang == "py")
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string[] pyCandidates = {
                    Path.Combine(localAppData, @"Programs\Python\Python310\python.exe"),
                    Path.Combine(localAppData, @"Programs\Python\Python312\python.exe"),
                    Path.Combine(localAppData, @"Programs\Python\Python311\python.exe"),
                    Path.Combine(localAppData, @"Programs\Python\Python39\python.exe")
                };
                foreach (var py in pyCandidates) { if (File.Exists(py)) return py; }

                string[] envPaths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';');
                foreach (var p in envPaths)
                {
                    if (string.IsNullOrWhiteSpace(p) || p.IndexOf("Python314", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    string candidate = Path.Combine(p.Trim(), "python.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }

            string langDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".blocklang", "bin", "languages", lang);
            if (Directory.Exists(langDir))
            {
                string[] possibleNames = { lang + ".exe", fallback };
                foreach (string name in possibleNames)
                {
                    string path1 = Path.Combine(langDir, name);
                    if (File.Exists(path1)) return path1;
                    string path2 = Path.Combine(langDir, "bin", name);
                    if (File.Exists(path2)) return path2;
                }
            }

            return fallback;
        }

        private static string GetExtension(string lang)
        {
            if (lang == "python") return "py";
            if (lang == "php") return "php";
            if (lang == "ruby") return "rb";
            if (lang == "ps" || lang == "powershell") return "ps1";
            if (lang == "js") return "js";
            if (lang == "lua") return "lua";
            if (lang == "sql") return "sql";
            if (lang == "c") return "c";
            if (lang == "cpp" || lang == "c++") return "cpp";
            if (lang == "go" || lang == "golang") return "go";
            if (lang == "rust" || lang == "rs") return "rs";
            if (lang == "java") return "java";
            if (lang == "ts" || lang == "typescript") return "ts";
            if (lang == "cs" || lang == "csharp") return "cs";
            if (lang == "kotlin" || lang == "kt") return "kt";
            if (lang == "dart") return "dart";
            if (lang == "zig") return "zig";
            if (lang == "perl" || lang == "pl") return "pl";
            if (lang == "bash" || lang == "sh") return "sh";
            if (lang == "r") return "R";
            return lang;
        }
    }
}
