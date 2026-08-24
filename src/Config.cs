using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace BlockEngine
{
    public static class Config
    {
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer { MaxJsonLength = (int)SecurityLimits.MaxJsonBytes };

        private static string configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".blocklang", "config.json");

        public static string GetConfigPath()
        {
            return configPath;
        }

        public static bool ConfigFileExists
        {
            get { return File.Exists(configPath); }
        }

        // M8: Fix: File lock to prevent concurrent read/write corruption
        private static readonly SemaphoreSlim _configLock = new SemaphoreSlim(1, 1);

        public static EngineConfig LoadConfig()
        {
            if (!File.Exists(configPath)) return ApplyProcessOverrides(new EngineConfig());
            _configLock.Wait();
            try
            {
                string json = File.ReadAllText(configPath, Encoding.UTF8);
                // H6: Use JavaScriptSerializer (cross-platform .NET Framework compatible)
                return ApplyProcessOverrides(_serializer.Deserialize<EngineConfig>(json) ?? new EngineConfig());
            }
            catch
            {
                return ApplyProcessOverrides(new EngineConfig());
            }
            finally
            {
                _configLock.Release();
            }
        }

        private static EngineConfig ApplyProcessOverrides(EngineConfig config)
        {
            // A parent process may opt into stricter networking for one run.
            // There is intentionally no environment override that disables the
            // configured guard.
            if (Environment.GetEnvironmentVariable("BLOCK_NETWORK_BLOCKED_OVERRIDE") == "1")
                config.NetworkBlocked = true;
            return config;
        }

        public static void SaveConfig(EngineConfig config)
        {
            string dir = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            _configLock.Wait();
            try
            {
                string json = _serializer.Serialize(config);
                // M8: Fix: Atomic write — write to temp file then replace to avoid partial-write corruption
                string tempPath = configPath + ".tmp";
                File.WriteAllText(tempPath, json, Encoding.UTF8);
                if (File.Exists(configPath))
                {
                    try
                    {
                        File.Replace(tempPath, configPath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Delete(configPath);
                        File.Move(tempPath, configPath);
                    }
                }
                else
                {
                    File.Move(tempPath, configPath);
                }
            }
            finally
            {
                _configLock.Release();
            }
        }

        public static void RunSettingsCLI()
        {
            EngineConfig cfg = LoadConfig();
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Block Engine Security Configuration ===");
                Console.WriteLine(string.Format("1. Python Execution: {0}", cfg.PythonEnabled ? "[ON]" : "[OFF]"));
                Console.WriteLine(string.Format("2. PHP Execution: {0}", cfg.PhpEnabled ? "[ON]" : "[OFF]"));
                Console.WriteLine(string.Format("3. Ruby Execution: {0}", cfg.RubyEnabled ? "[ON]" : "[OFF]"));
                Console.WriteLine(string.Format("4. Lua Execution: {0}", cfg.LuaEnabled ? "[ON]" : "[OFF]"));
                Console.WriteLine(string.Format("5. PowerShell Execution: {0} (WARNING: HIGH RISK)", cfg.PowerShellEnabled ? "[ON]" : "[OFF]"));
                Console.WriteLine(string.Format("6. SQL Execution: {0}", cfg.SqlEnabled ? "[ON]" : "[OFF]"));
                Console.WriteLine(string.Format("7. JS (Node) Execution: {0}", cfg.JsEnabled ? "[ON]" : "[OFF]"));
                Console.WriteLine(string.Format("8. Advisory Network Guard (best effort): {0}", cfg.NetworkBlocked ? "[ON]" : "[OFF]"));
                Console.WriteLine(string.Format("9. Allow Custom <define> Tags: {0}", cfg.AllowCustomDefinitions ? "[ON]" : "[OFF]"));
                Console.WriteLine(string.Format("T. Execution Timeout: {0}s", cfg.ExecutionTimeoutSeconds));
                Console.WriteLine(string.Format("D. Sandbox Directory: {0}", cfg.SandboxDir));
                Console.WriteLine("S. Save & Exit");
                Console.WriteLine("Q. Exit without saving");
                Console.Write("\nSelect option: ");
                
                string input = Console.ReadLine();
                if (input != null) input = input.ToUpper(); else input = "";
                if (input == "1") cfg.PythonEnabled = !cfg.PythonEnabled;
                else if (input == "2") cfg.PhpEnabled = !cfg.PhpEnabled;
                else if (input == "3") cfg.RubyEnabled = !cfg.RubyEnabled;
                else if (input == "4") cfg.LuaEnabled = !cfg.LuaEnabled;
                else if (input == "5") cfg.PowerShellEnabled = !cfg.PowerShellEnabled;
                else if (input == "6") cfg.SqlEnabled = !cfg.SqlEnabled;
                else if (input == "7") cfg.JsEnabled = !cfg.JsEnabled;
                else if (input == "8") cfg.NetworkBlocked = !cfg.NetworkBlocked;
                else if (input == "9") cfg.AllowCustomDefinitions = !cfg.AllowCustomDefinitions;
                else if (input == "T")
                {
                    Console.Write("Enter timeout in seconds (default 15): ");
                    int t;
                    if (int.TryParse(Console.ReadLine(), out t) && t > 0) cfg.ExecutionTimeoutSeconds = t;
                }
                else if (input == "D")
                {
                    Console.Write("Enter sandbox directory path: ");
                    string d = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(d) && Directory.Exists(d)) cfg.SandboxDir = d;
                    else Console.WriteLine("Invalid directory. Not changed.");
                }
                else if (input == "S") { SaveConfig(cfg); Console.WriteLine("Configuration saved."); break; }
                else if (input == "Q") { break; }
            }
        }
    }
}
