#if BLOCK_PLUS
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace BlockEngine
{
    public class CustomLangDef
    {
        public string cmd { get; set; }
        public string args { get; set; }
        public string ext { get; set; }
        public string compile { get; set; }
        public string compileArgs { get; set; }
    }

    public static class CustomLangRegistry
    {
        private static readonly HashSet<string> ReservedLanguageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "block", "del", "html", "json", "py", "python", "js", "javascript", "php", "ruby", "rb", "lua",
            "sql", "ps", "powershell", "c", "cpp", "c++", "go", "golang", "rust", "rs", "java", "ts",
            "typescript", "cs", "csharp", "kotlin", "kt", "dart", "zig", "perl", "pl", "bash", "sh", "r"
        };

        private static readonly string RegistryPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                         ".blocklang", "languages.json");

        private static Dictionary<string, CustomLangDef> _globalCache = null;
        private static readonly object _globalLock = new object();

        // Inline definitions belong to one parse/execution flow. A process-global
        // dictionary lets concurrent API requests overwrite or clear one another.
        // AsyncLocal keeps the existing API while isolating each request context.
        private static readonly System.Threading.AsyncLocal<Dictionary<string, CustomLangDef>> _inlineScope =
            new System.Threading.AsyncLocal<Dictionary<string, CustomLangDef>>();

        private static Dictionary<string, CustomLangDef> GetInlineScope()
        {
            if (_inlineScope.Value == null)
                _inlineScope.Value = new Dictionary<string, CustomLangDef>(StringComparer.OrdinalIgnoreCase);
            return _inlineScope.Value;
        }

        public static Dictionary<string, CustomLangDef> LoadGlobal()
        {
            if (_globalCache != null) return _globalCache;
            lock (_globalLock)
            {
                if (_globalCache != null) return _globalCache;
                if (!File.Exists(RegistryPath))
                {
                    _globalCache = new Dictionary<string, CustomLangDef>(StringComparer.OrdinalIgnoreCase);
                    return _globalCache;
                }
                // L4: Fix: Report error to user instead of silently failing
                try
                {
                    FileInfo registryInfo = new FileInfo(RegistryPath);
                    if (registryInfo.Length > SecurityLimits.MaxJsonBytes)
                        throw new InvalidDataException("languages.json exceeds the 4 MiB safety limit.");
                    string json = File.ReadAllText(RegistryPath);
                    var serializer = new JavaScriptSerializer { MaxJsonLength = (int)SecurityLimits.MaxJsonBytes };
                    Dictionary<string, CustomLangDef> parsed = serializer.Deserialize<Dictionary<string, CustomLangDef>>(json);
                    _globalCache = new Dictionary<string, CustomLangDef>(StringComparer.OrdinalIgnoreCase);
                    if (parsed != null)
                    {
                        foreach (KeyValuePair<string, CustomLangDef> item in parsed)
                        {
                            try
                            {
                                ValidateDefinition(item.Key, item.Value);
                                if (_globalCache.Count >= SecurityLimits.MaxCustomLanguageDefinitions)
                                {
                                    Console.WriteLine("[Block+ Warning] languages.json definition limit reached; remaining entries were ignored.");
                                    break;
                                }
                                _globalCache[item.Key.ToLowerInvariant()] = item.Value;
                            }
                            catch (Exception validationError)
                            {
                                Console.WriteLine(string.Format("[Block+ Warning] Ignored invalid custom language '{0}': {1}", item.Key, validationError.Message));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[Block+ Warning] Failed to load languages.json: {0}", ex.Message));
                    _globalCache = new Dictionary<string, CustomLangDef>(StringComparer.OrdinalIgnoreCase);
                }
                return _globalCache;
            }
        }

        public static void RegisterInline(string lang, CustomLangDef def)
        {
            if (!string.IsNullOrEmpty(lang) && def != null)
            {
                ValidateDefinition(lang, def);
                GetInlineScope()[lang.ToLowerInvariant()] = def;
            }
        }

        public static void ValidateDefinition(string lang, CustomLangDef def)
        {
            string normalized = (lang ?? "").Trim().ToLowerInvariant();
            if (normalized.Length == 0 || normalized.Length > 32 || !IsLanguageIdentifier(normalized))
                throw new InvalidDataException("Language identifiers must match [a-z][a-z0-9_-]{0,31}.");
            if (ReservedLanguageNames.Contains(normalized))
                throw new InvalidDataException("Custom definitions cannot shadow a built-in language or Block directive.");
            if (def == null) throw new InvalidDataException("Custom language definition is empty.");

            ValidateText(def.cmd, SecurityLimits.MaxCustomLanguageCommandChars, "cmd", false);
            ValidateText(def.args, SecurityLimits.MaxCustomLanguageArgsChars, "args", true);
            ValidateText(def.compile, SecurityLimits.MaxCustomLanguageCommandChars, "compile", true);
            ValidateText(def.compileArgs, SecurityLimits.MaxCustomLanguageArgsChars, "compileArgs", true);

            string extension = string.IsNullOrEmpty(def.ext) ? "." + normalized : def.ext.Trim();
            if (!extension.StartsWith(".", StringComparison.Ordinal) || extension.Length > SecurityLimits.MaxCustomLanguageExtensionChars)
                throw new InvalidDataException("Custom language extensions must be short and start with '.'.");
            for (int i = 1; i < extension.Length; i++)
            {
                char c = extension[i];
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                    throw new InvalidDataException("Custom language extensions cannot contain path separators or shell syntax.");
            }
        }

        private static void ValidateText(string value, int maxLength, string name, bool allowEmpty)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (!allowEmpty) throw new InvalidDataException("Custom language '" + name + "' is required.");
                return;
            }
            if (value.Length > maxLength || value.IndexOfAny(new[] { '\0', '\r', '\n' }) >= 0)
                throw new InvalidDataException("Custom language '" + name + "' exceeds its safe format limit.");
        }

        private static bool IsLanguageIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || !char.IsLower(value[0])) return false;
            for (int i = 1; i < value.Length; i++)
            {
                char c = value[i];
                if (!(char.IsLower(c) || char.IsDigit(c) || c == '_' || c == '-')) return false;
            }
            return true;
        }

        public static bool TryGet(string lang, out CustomLangDef def)
        {
            Dictionary<string, CustomLangDef> inline = _inlineScope.Value;
            if (inline != null && inline.TryGetValue(lang, out def)) return true;
            return LoadGlobal().TryGetValue(lang, out def);
        }

        public static void ClearInline()
        {
            _inlineScope.Value = null;
        }
    }
}
#endif
