using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BlockEngine
{
    public static class Parser
    {
        private const int MaxImportDepth = 16; // H3: Fix: Prevent StackOverflow via deep recursion

        // C2: Fix: Proper directory boundary check (not just StartsWith)
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
            string normalSandbox = Path.GetFullPath(sandboxRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                   + Path.DirectorySeparatorChar;
            string normalPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                + Path.DirectorySeparatorChar;
            return normalPath.StartsWith(normalSandbox, StringComparison.OrdinalIgnoreCase)
                && !ContainsReparsePoint(normalSandbox)
                && !ContainsReparsePoint(normalPath);
        }

        public static List<CodeBlock> ParseBlocks(string code, string currentScriptPath = null, EngineConfig cfg = null, HashSet<string> visitedFiles = null, int depth = 0)
        {
            if (cfg == null) cfg = new EngineConfig();
            if (visitedFiles == null) visitedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(currentScriptPath)) visitedFiles.Add(Path.GetFullPath(currentScriptPath));

            int importCount = 0;
            long importBytes = 0;
            string trustedImportRoot = GetTrustedImportRoot(currentScriptPath);
            return ParseBlocksInternal(code, currentScriptPath, cfg, visitedFiles, depth, trustedImportRoot,
                ref importCount, ref importBytes);
        }

        private static List<CodeBlock> ParseBlocksInternal(string code, string currentScriptPath, EngineConfig cfg,
            HashSet<string> visitedFiles, int depth, string trustedImportRoot,
            ref int importCount, ref long importBytes)
        {

            List<CodeBlock> blocks = new List<CodeBlock>();
            string currentLang = "block";
            List<string> buffer = new List<string>();
            int lineNumber = 0;
            int blockStartLine = 0;

            using (StringReader sr = new StringReader(code))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    lineNumber++;
#if !BLOCK_LITE
                    var importMatch = Regex.Match(line.Trim(), @"^<\s*import\s+src=[""']([^""']+)[""']\s*\/?>$", RegexOptions.IgnoreCase);
                    if (importMatch.Success)
                    {
                        string importRel = importMatch.Groups[1].Value;
                        string basePath = !string.IsNullOrEmpty(currentScriptPath) ? Path.GetDirectoryName(currentScriptPath) : Environment.CurrentDirectory;
                        string fullImportPath = Path.GetFullPath(Path.Combine(basePath, importRel));
                        if (buffer.Count > 0)
                        {
                            blocks.Add(new CodeBlock { Language = currentLang, Code = string.Join("\n", buffer) });
                            buffer.Clear();
                        }
                        try
                        {
                            blocks.AddRange(LoadImportedBlocks(fullImportPath, cfg, visitedFiles, depth, trustedImportRoot,
                                ref importCount, ref importBytes));
                        }
                        catch (BlockDiagnosticException) { throw; }
                        catch (Exception ex)
                        {
                            throw new BlockDiagnosticException("BLK1201", "Import failed", ex.Message,
                                currentScriptPath, lineNumber, 1,
                                "Check the <import src=\"...\"> path and keep imported files inside the configured sandbox.");
                        }
                        continue;
                    }

                    var removedPackageMatch = Regex.Match(line.Trim(), @"^<\s*use\s+package=[""'][^""']+[""'](?:\s+entry=[""'][^""']+[""'])?\s*\/?>$", RegexOptions.IgnoreCase);
                    if (removedPackageMatch.Success)
                    {
                        throw new BlockDiagnosticException("BLK1301", "Third-party packages are not supported",
                            "The <use package=\"...\"> directive was removed from Block.",
                            currentScriptPath, lineNumber, 1,
                            "Copy reviewed Block source into the project and load it with <import src=\"relative/path.blk\" />.");
                    }

#if BLOCK_PLUS
                    var defMatch = Regex.Match(line.Trim(), @"^<\s*define\s+lang=[""']([^""']+)[""']\s+cmd=[""']([^""']+)[""'](?:\s+ext=[""']([^""']+)[""'])?\s*\/?>$", RegexOptions.IgnoreCase);
                    if (defMatch.Success)
                    {
                        if (!cfg.AllowCustomDefinitions)
                        {
                            throw new BlockDiagnosticException("BLK2101", "Custom language definition blocked",
                                "The <define> tag is disabled because AllowCustomDefinitions=false.",
                                currentScriptPath, lineNumber, 1,
                                "Use a built-in runtime, or review the file before enabling custom definitions in 'block config'.");
                        }
                        string dLang = defMatch.Groups[1].Value.ToLower();
                        string dCmd = defMatch.Groups[2].Value;
                        string dExt = defMatch.Groups[3].Success ? defMatch.Groups[3].Value : "." + dLang;
                        if (!dExt.StartsWith(".")) dExt = "." + dExt;
                        CustomLangRegistry.RegisterInline(dLang, new CustomLangDef { cmd = dCmd, ext = dExt });
                        continue;
                    }
#endif

#endif

                    // L3: Flexible tag matching with Whitelist validation to prevent arbitrary HTML tags from opening language blocks
                    var match = Regex.Match(line.Trim(), @"^<(\/)?\s*([a-zA-Z0-9_\-]+)\s*>$");
                    if (match.Success)
                    {
                        bool isClosing = match.Groups[1].Value == "/";
                        string lang = match.Groups[2].Value.ToLower();

                        if (isClosing && IsValidLanguageTag(lang))
                        {
                            if (currentLang == "block")
                                throw new BlockDiagnosticException("BLK1101", "Unmatched closing tag",
                                    string.Format("Found </{0}> without a matching opening tag.", lang),
                                    currentScriptPath, lineNumber, 1,
                                    string.Format("Remove </{0}> or add <{0}> before this line.", lang));
                            if (!string.Equals(currentLang, lang, StringComparison.OrdinalIgnoreCase))
                                throw new BlockDiagnosticException("BLK1101", "Mismatched closing tag",
                                    string.Format("Found </{0}> while <{1}> is still open.", lang, currentLang),
                                    currentScriptPath, lineNumber, 1,
                                    string.Format("Replace </{0}> with </{1}>, or close <{1}> before this line.", lang, currentLang));

                            blocks.Add(new CodeBlock { Language = currentLang, Code = string.Join("\n", buffer) });
                            currentLang = "block";
                            blockStartLine = 0;
                            buffer.Clear();
                        }
                        else if (!isClosing && currentLang == "block" && IsValidLanguageTag(lang))
                        {
                            if (buffer.Count > 0)
                            {
                                blocks.Add(new CodeBlock { Language = currentLang, Code = string.Join("\n", buffer) });
                                buffer.Clear();
                            }
                            currentLang = lang;
                            blockStartLine = lineNumber;
                        }
                        else
                        {
                            buffer.Add(line);
                        }
                        continue;
                    }
                    buffer.Add(line);
                }
            } // Close using block
            
            if (!string.Equals(currentLang, "block", StringComparison.OrdinalIgnoreCase))
                throw new BlockDiagnosticException("BLK1101", "Unclosed language block",
                    string.Format("The <{0}> block opened here has no closing </{0}> tag.", currentLang),
                    currentScriptPath, blockStartLine > 0 ? blockStartLine : lineNumber, 1,
                    string.Format("Add </{0}> after the final line of this block.", currentLang));

            if (buffer.Count > 0)
                blocks.Add(new CodeBlock { Language = currentLang, Code = string.Join("\n", buffer) });

            return OptimizeBlocks(blocks);
        }

        private static List<CodeBlock> LoadImportedBlocks(string fullImportPath, EngineConfig cfg,
            HashSet<string> visitedFiles, int depth, string trustedImportRoot,
            ref int importCount, ref long importBytes)
        {
            if (depth >= MaxImportDepth)
                throw new InvalidOperationException(string.Format("Import depth limit ({0}) exceeded. Possible circular or excessively nested imports.", MaxImportDepth));

            string sandboxRoot = Path.GetFullPath(cfg.SandboxDir);
            bool insideConfiguredSandbox = IsPathInSandbox(fullImportPath, sandboxRoot);
            bool insideScriptProject = !string.IsNullOrEmpty(trustedImportRoot) &&
                                       IsPathInSandbox(fullImportPath, trustedImportRoot);
            if (!insideConfiguredSandbox && !insideScriptProject)
                throw new UnauthorizedAccessException(string.Format(
                    "Import path '{0}' escapes both the script project '{1}' and configured sandbox '{2}'.",
                    fullImportPath, trustedImportRoot ?? "(none)", sandboxRoot));
            if (!File.Exists(fullImportPath))
                throw new FileNotFoundException(string.Format("Import file not found: {0}", fullImportPath));
            if (!IsBlockSourceFile(fullImportPath))
                throw new InvalidDataException("Imports must reference a Block source file (.blk, .blkl, .blkp, or a documented alias).");

            FileInfo importInfo = new FileInfo(fullImportPath);
            if (importCount >= SecurityLimits.MaxImportFiles ||
                importInfo.Length > SecurityLimits.MaxImportedBytes - importBytes)
                throw new InvalidOperationException("Import resource limit exceeded.");
            importCount++;
            importBytes += importInfo.Length;

            string normalizedPath = Path.GetFullPath(fullImportPath);
            if (visitedFiles.Contains(normalizedPath))
                throw new InvalidOperationException(string.Format("Circular import detected: {0}", normalizedPath));

            string importedCode = File.ReadAllText(normalizedPath);
            HashSet<string> childVisited = new HashSet<string>(visitedFiles, StringComparer.OrdinalIgnoreCase);
            childVisited.Add(normalizedPath);
            return ParseBlocksInternal(importedCode, normalizedPath, cfg, childVisited, depth + 1, trustedImportRoot,
                ref importCount, ref importBytes);
        }

        private static string GetTrustedImportRoot(string currentScriptPath)
        {
            if (string.IsNullOrWhiteSpace(currentScriptPath))
                return Path.GetFullPath(Environment.CurrentDirectory);

            string scriptPath = Path.GetFullPath(currentScriptPath);
            string scriptDirectory = Path.GetDirectoryName(scriptPath);
            try
            {
                string projectRoot = ProjectWorkspace.FindProjectRoot(scriptDirectory);
                if (File.Exists(Path.Combine(projectRoot, ProjectWorkspace.ProjectManifestName)))
                    return projectRoot;
            }
            catch (Exception) { }
            return scriptDirectory;
        }

        private static bool IsBlockSourceFile(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".blk", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".block", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".blkl", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".blocklite", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".blkp", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".blockplus", StringComparison.OrdinalIgnoreCase);
        }

        // OPTIMIZATION: Merge consecutive blocks of the same language to reduce Process Spawning
        private static readonly HashSet<string> _knownLangs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "py", "python", "js", "javascript", "php", "ruby", "rb", "lua", "ps", "powershell",
            "sql", "html", "json", "c", "cpp", "c++", "go", "golang", "rust", "rs", "java",
            "ts", "typescript", "cs", "csharp", "kotlin", "kt", "dart", "zig", "perl", "pl",
            "bash", "sh", "r", "server", "route", "del", "import", "define"
        };

        private static bool IsValidLanguageTag(string tag)
        {
            if (_knownLangs.Contains(tag)) return true;
#if BLOCK_PLUS
            CustomLangDef dummy;
            if (CustomLangRegistry.TryGet(tag, out dummy)) return true;
#endif
            return false;
        }

        private static List<CodeBlock> OptimizeBlocks(List<CodeBlock> blocks)
        {
            List<CodeBlock> merged = new List<CodeBlock>();
            foreach (var block in blocks)
            {
                // Ignore empty 'block' (text) chunks to allow merging across whitespace
                if (block.Language == "block" && string.IsNullOrWhiteSpace(block.Code))
                    continue;

                if (merged.Count > 0 && merged[merged.Count - 1].Language == block.Language)
                {
                    merged[merged.Count - 1].Code += "\n" + block.Code;
                }
                else
                {
                    merged.Add(block);
                }
            }
            return merged;
        }
    }
}
