using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BlockEngine
{
    // Read-only CLI diagnostics shared by all editions. These commands never
    // execute a Block document or change the user's configuration.
    public static class CliCommands
    {
        private sealed class RuntimeSpec
        {
            public string Name;
            public string[] Executables;
            public Func<EngineConfig, bool> IsEnabled;

            public RuntimeSpec(string name, string[] executables, Func<EngineConfig, bool> isEnabled)
            {
                Name = name;
                Executables = executables;
                IsEnabled = isEnabled;
            }
        }

        public static string EditionName
        {
            get
            {
#if BLOCK_LITE
                return "Lite";
#elif BLOCK_PLUS
                return "Plus";
#else
                return "Standard";
#endif
            }
        }

        public static void RunCheck(string filePath)
        {
            try
            {
                string path = ResolveExistingFile(filePath, "check");
                string code = ReadScriptFile(path);
                List<CodeBlock> blocks = Parser.ParseBlocks(code, path, Config.LoadConfig());
                Console.WriteLine("[Block Check] Syntax Check Passed! Found " + blocks.Count + " block(s).");
                foreach (CodeBlock block in blocks)
                {
                    string language = block.Language ?? "unknown";
                    int length = block.Code == null ? 0 : block.Code.Length;
                    Console.WriteLine(string.Format("  - <{0}> ({1} characters)", language, length));
                }
            }
            catch (Exception ex)
            {
                CliDiagnostics.Report(ex, "check", filePath);
            }
        }

        public static void RunInfo(string filePath)
        {
            EngineConfig cfg = Config.LoadConfig();
            Console.WriteLine("Block Engine information");
            Console.WriteLine("  Edition: " + EditionName);
            Console.WriteLine("  Version: " + BlockVersion.Value);
            Console.WriteLine("  Config: " + Config.GetConfigPath());
            Console.WriteLine("  Sandbox: " + cfg.SandboxDir);
            Console.WriteLine("  Workspace: " + (string.IsNullOrWhiteSpace(cfg.WorkspaceDir) ? "(not set)" : cfg.WorkspaceDir));
            Console.WriteLine("  Timeout: " + cfg.ExecutionTimeoutSeconds + "s");
            Console.WriteLine("  Advisory network guard: " + (cfg.NetworkBlocked ? "on" : "off"));
            Console.WriteLine("  Custom definitions: " + (cfg.AllowCustomDefinitions ? "enabled" : "disabled"));
            Console.WriteLine("  Commands: run, check, ast, info/capabilities, runtimes, doctor, workspace, find, project, config");

            if (string.IsNullOrWhiteSpace(filePath)) return;

            try
            {
                string path = ResolveExistingFile(filePath, "info");
                string code = ReadScriptFile(path);
                List<CodeBlock> blocks = Parser.ParseBlocks(code, path, cfg);
                Console.WriteLine("  Script: " + path);
                Console.WriteLine("  Blocks: " + blocks.Count);
                int index = 1;
                foreach (CodeBlock block in blocks)
                {
                    string language = block.Language ?? "unknown";
                    int length = block.Code == null ? 0 : block.Code.Length;
                    Console.WriteLine(string.Format("    {0}. <{1}> ({2} characters)", index++, language, length));
                }
            }
            catch (Exception ex)
            {
                CliDiagnostics.Report(ex, "info", filePath);
            }
        }

        public static void RunRuntimes(bool doctor)
        {
            EngineConfig cfg = Config.LoadConfig();
            List<RuntimeSpec> specs = CreateRuntimeSpecs();
            Console.WriteLine(doctor ? "Block Doctor" : "Block Runtime Diagnostics");
            Console.WriteLine("  Edition: " + EditionName);
            Console.WriteLine("  OS: " + Environment.OSVersion.VersionString);
            Console.WriteLine("  64-bit OS: " + (Environment.Is64BitOperatingSystem ? "yes" : "no"));
            Console.WriteLine("  Config: " + Config.GetConfigPath());
            Console.WriteLine();
            Console.WriteLine("  Runtime       Enabled  Status   Executable");
            Console.WriteLine("  ------------  -------  -------  ------------------------------");

            int missingEnabled = 0;
            foreach (RuntimeSpec spec in specs)
            {
                string executable = FindOnPath(spec.Executables);
                bool enabled = spec.IsEnabled == null || spec.IsEnabled(cfg);
                string status = executable == null ? "missing" : "found";
                if (enabled && executable == null) missingEnabled++;
                string location = executable ?? "not found on PATH";
                if (executable != null)
                {
                    string version = GetFileVersion(executable);
                    if (!string.IsNullOrEmpty(version)) location += " (" + version + ")";
                }
                Console.WriteLine(string.Format("  {0,-12}  {1,-7}  {2,-7}  {3}",
                    spec.Name, enabled ? "yes" : "no", status, location));
            }

            Console.WriteLine();
            Console.WriteLine("  Sandbox: " + cfg.SandboxDir + (Directory.Exists(cfg.SandboxDir) ? " [exists]" : " [missing]"));
            Console.WriteLine("  Timeout: " + cfg.ExecutionTimeoutSeconds + "s");
            Console.WriteLine("  Advisory network guard: " + (cfg.NetworkBlocked ? "on" : "off"));
            Console.WriteLine("  Custom definitions: " + (cfg.AllowCustomDefinitions ? "enabled" : "disabled"));

            if (doctor && !Directory.Exists(cfg.SandboxDir))
            {
                CliDiagnostics.Report(new DirectoryNotFoundException("Configured sandbox directory does not exist."),
                    "doctor", cfg.SandboxDir,
                    "Open 'block config' and select an existing sandbox directory.");
            }
            else if (doctor && missingEnabled > 0)
            {
                Console.WriteLine("[Block Doctor] Some enabled optional runtimes are not installed. Native Block stages remain available.");
            }
        }

        public static void RunConfigShow()
        {
            EngineConfig cfg = Config.LoadConfig();
            Console.WriteLine("Block configuration");
            Console.WriteLine("  Path: " + Config.GetConfigPath());
            Console.WriteLine("  File exists: " + (Config.ConfigFileExists ? "yes" : "no"));
            Console.WriteLine("  Python: " + OnOff(cfg.PythonEnabled));
            Console.WriteLine("  JavaScript: " + OnOff(cfg.JsEnabled));
            Console.WriteLine("  PHP: " + OnOff(cfg.PhpEnabled));
            Console.WriteLine("  Ruby: " + OnOff(cfg.RubyEnabled));
            Console.WriteLine("  Lua: " + OnOff(cfg.LuaEnabled));
            Console.WriteLine("  PowerShell: " + OnOff(cfg.PowerShellEnabled));
            Console.WriteLine("  SQL: " + OnOff(cfg.SqlEnabled));
            Console.WriteLine("  Advisory network guard: " + OnOff(cfg.NetworkBlocked));
            Console.WriteLine("  Custom definitions: " + OnOff(cfg.AllowCustomDefinitions));
            Console.WriteLine("  Timeout: " + cfg.ExecutionTimeoutSeconds + "s");
            Console.WriteLine("  Sandbox: " + cfg.SandboxDir);
            Console.WriteLine("  Workspace: " + (string.IsNullOrWhiteSpace(cfg.WorkspaceDir) ? "(not set)" : cfg.WorkspaceDir));
        }

        public static void RunConfigPath()
        {
            Console.WriteLine(Config.GetConfigPath());
        }

        public static void RunWorkspace(string[] args)
        {
            try
            {
                string action = args.Length > 1 ? (args[1] ?? "").ToLowerInvariant() : "show";
                EngineConfig cfg = Config.LoadConfig();

                if (action == "show")
                {
                    Console.WriteLine("Block workspace");
                    Console.WriteLine("  Configured: " + (string.IsNullOrWhiteSpace(cfg.WorkspaceDir) ? "(not set)" : cfg.WorkspaceDir));
                    Console.WriteLine("  Environment: " + (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BLOCK_WORKSPACE")) ? "(not set)" : Environment.GetEnvironmentVariable("BLOCK_WORKSPACE")));
                    Console.WriteLine("  Search roots:");
                    foreach (string root in BlockPathResolver.GetSearchRoots(cfg, null)) Console.WriteLine("    - " + root);
                    return;
                }

                if (action == "set")
                {
                    if (args.Length < 3) throw new ArgumentException("Usage: block workspace set <directory>");
                    string path = Path.GetFullPath(string.Join(" ", args, 2, args.Length - 2));
                    if (!Directory.Exists(path)) throw new DirectoryNotFoundException("Workspace directory not found: " + path);
                    cfg.WorkspaceDir = path;
                    Config.SaveConfig(cfg);
                    Console.WriteLine("[Block Workspace] Set to: " + path);
                    return;
                }

                if (action == "clear" || action == "unset")
                {
                    cfg.WorkspaceDir = "";
                    Config.SaveConfig(cfg);
                    Console.WriteLine("[Block Workspace] Cleared.");
                    return;
                }

                CliDiagnostics.ReportUsage("workspace",
                    "block workspace show | set <directory> | clear",
                    "Use 'show' to inspect both configured and environment search roots.");
            }
            catch (Exception ex)
            {
                CliDiagnostics.Report(ex, "workspace");
            }
        }

        public static void RunFind(string query)
        {
            try
            {
                EngineConfig cfg = Config.LoadConfig();
                List<string> roots = BlockPathResolver.GetSearchRoots(cfg, null);
                List<string> matches = BlockPathResolver.FindScripts(query, cfg);
                Console.WriteLine("Block script search");
                Console.WriteLine("  Query: " + (string.IsNullOrWhiteSpace(query) ? "(all scripts in search roots)" : query));
                Console.WriteLine("  Roots:");
                foreach (string root in roots) Console.WriteLine("    - " + root);
                Console.WriteLine("  Matches:");
                if (matches.Count == 0) Console.WriteLine("    (none)");
                foreach (string match in matches) Console.WriteLine("    - " + match);
            }
            catch (Exception ex)
            {
                CliDiagnostics.Report(ex, "find", query);
            }
        }

        public static void RunProjectCommand(string[] args)
        {
            try
            {
                string action = args.Length > 1 ? (args[1] ?? "").ToLowerInvariant() : "root";
                EngineConfig cfg = Config.LoadConfig();
                string hint = args.Length > 2 ? string.Join(" ", args, 2, args.Length - 2) : null;

                if (action == "root")
                {
                    string root = BlockPathResolver.ResolveProjectRoot(hint, cfg);
                    BlockProjectManifest project = ProjectWorkspace.LoadProject(root);
                    Console.WriteLine(root);
                    Console.WriteLine("  Name: " + project.name);
                    Console.WriteLine("  Entry: " + project.entry);
                    return;
                }

                if (action == "run")
                {
                    string path = BlockPathResolver.ResolveScript(hint, cfg, "project run");
                    Program.ExecuteScript(path, cfg);
                    return;
                }

                CliDiagnostics.ReportUsage("project",
                    "block project root [path] | run [file|project-directory]",
                    "Use 'block find <name>' if you do not know the project entry path.");
            }
            catch (Exception ex)
            {
                CliDiagnostics.Report(ex, "project");
            }
        }

        private static string OnOff(bool value)
        {
            return value ? "enabled" : "disabled";
        }

        private static List<RuntimeSpec> CreateRuntimeSpecs()
        {
            List<RuntimeSpec> specs = new List<RuntimeSpec>();
            specs.Add(new RuntimeSpec("Python", new[] { "python.exe", "python3.exe" }, delegate(EngineConfig cfg) { return cfg.PythonEnabled; }));
            specs.Add(new RuntimeSpec("Node.js", new[] { "node.exe" }, delegate(EngineConfig cfg) { return cfg.JsEnabled; }));
            specs.Add(new RuntimeSpec("PHP", new[] { "php.exe" }, delegate(EngineConfig cfg) { return cfg.PhpEnabled; }));
            specs.Add(new RuntimeSpec("Ruby", new[] { "ruby.exe" }, delegate(EngineConfig cfg) { return cfg.RubyEnabled; }));
            specs.Add(new RuntimeSpec("Lua", new[] { "lua.exe" }, delegate(EngineConfig cfg) { return cfg.LuaEnabled; }));
            specs.Add(new RuntimeSpec("PowerShell", new[] { "pwsh.exe", "powershell.exe" }, delegate(EngineConfig cfg) { return cfg.PowerShellEnabled; }));
            specs.Add(new RuntimeSpec("SQLite", new[] { "sqlite3.exe" }, delegate(EngineConfig cfg) { return cfg.SqlEnabled; }));
            return specs;
        }

        private static string FindOnPath(string[] executableNames)
        {
            if (executableNames == null) return null;
            string pathValue = Environment.GetEnvironmentVariable("PATH") ?? "";
            string[] directories = pathValue.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string executableName in executableNames)
            {
                if (Path.IsPathRooted(executableName) && File.Exists(executableName)) return executableName;
                foreach (string rawDirectory in directories)
                {
                    string directory = rawDirectory.Trim().Trim('"');
                    if (directory.Length == 0) continue;
                    try
                    {
                        string candidate = Path.Combine(directory, executableName);
                        if (File.Exists(candidate)) return candidate;
                    }
                    catch (ArgumentException) { }
                    catch (PathTooLongException) { }
                }
            }
            return null;
        }

        private static string GetFileVersion(string path)
        {
            try
            {
                string version = FileVersionInfo.GetVersionInfo(path).FileVersion;
                return string.IsNullOrWhiteSpace(version) ? null : version;
            }
            catch { return null; }
        }

        private static string ResolveExistingFile(string filePath, string command)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Usage: block " + command + " <file>");
            return BlockPathResolver.ResolveScript(filePath, Config.LoadConfig(), command);
        }

        private static string ReadScriptFile(string path)
        {
            FileInfo info = new FileInfo(path);
            if (info.Length > SecurityLimits.MaxScriptBytes)
                throw new InvalidOperationException(string.Format("Script file exceeds the {0} MiB limit.", SecurityLimits.MaxScriptBytes / (1024 * 1024)));
            return File.ReadAllText(path, Encoding.UTF8);
        }
    }
}
