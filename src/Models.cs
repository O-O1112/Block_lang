using System;
using System.IO;

namespace BlockEngine
{
    public class EngineConfig
    {
        public EngineConfig() 
        { 
            PythonEnabled = true; 
            JsEnabled = true; 
            PhpEnabled = true; 
            RubyEnabled = true; 
            LuaEnabled = true; 
            PowerShellEnabled = false; 
            SqlEnabled = true; 
            // Safer default for new installations. Existing user config is preserved.
            NetworkBlocked = true; 
            // Custom definitions execute arbitrary commands; opt in explicitly.
            AllowCustomDefinitions = false;
            SandboxDir = Environment.CurrentDirectory;
            WorkspaceDir = "";
            ExecutionTimeoutSeconds = 15;
            MaxRequestBodyBytes = 1024 * 1024 * 4; // 4 MB
        }

        public bool PythonEnabled { get; set; }
        public bool JsEnabled { get; set; }
        public bool PhpEnabled { get; set; }
        public bool RubyEnabled { get; set; }
        public bool LuaEnabled { get; set; }
        public bool PowerShellEnabled { get; set; }
        public bool SqlEnabled { get; set; }
        public bool NetworkBlocked { get; set; }
        public bool AllowCustomDefinitions { get; set; }
        public string WorkspaceDir { get; set; }

        // F2: Configurable sandbox dir with validation
        private string _sandboxDir;
        public string SandboxDir
        {
            get { return _sandboxDir; }
            set
            {
                // L5: Validate SandboxDir is not empty
                if (string.IsNullOrWhiteSpace(value))
                    _sandboxDir = Environment.CurrentDirectory;
                else
                    _sandboxDir = value;
            }
        }

        public string ApiToken { get; set; }

        // F2: Now configurable instead of hardcoded 15s
        private int _executionTimeoutSeconds;
        public int ExecutionTimeoutSeconds
        {
            get { return _executionTimeoutSeconds; }
            set { _executionTimeoutSeconds = Math.Max(1, Math.Min(value, 3600)); }
        }

        // C3/C7: Request body size limit
        private long _maxRequestBodyBytes;
        public long MaxRequestBodyBytes
        {
            get { return _maxRequestBodyBytes; }
            set { _maxRequestBodyBytes = Math.Max(1, Math.Min(value, SecurityLimits.MaxJsonBytes)); }
        }
    }

    public class CodeBlock
    {
        public string Language { get; set; }
        public string Code { get; set; }
    }
}
