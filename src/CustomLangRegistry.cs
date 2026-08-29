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
                    _globalCache = serializer.Deserialize<Dictionary<string, CustomLangDef>>(json)
                                   ?? new Dictionary<string, CustomLangDef>(StringComparer.OrdinalIgnoreCase);
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
                GetInlineScope()[lang] = def;
            }
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
