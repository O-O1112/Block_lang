using System;
using System.Collections.Generic;
using System.IO;

namespace BlockEngine
{
    // Centralizes safe, deterministic script discovery for every CLI command.
    // The resolver never scans an entire drive: it only checks explicit paths,
    // the current project, and the configured workspace root.
    public static class BlockPathResolver
    {
        private static readonly string[] ScriptExtensions =
        {
            ".blk", ".block", ".blkl", ".blocklite", ".blkp", ".blockplus"
        };

        public static string ResolveScript(string input, EngineConfig cfg, string command)
        {
            List<string> candidates = BuildCandidates(input, cfg, true);
            List<string> existing = new List<string>();
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) AddUniqueFile(existing, candidate);
            }

            if (existing.Count == 1) return existing[0];
            if (existing.Count > 1)
            {
                throw new IOException(BuildAmbiguousMessage(input, existing));
            }

            // A workspace may contain one or more project folders. Search only
            // one directory level and accept the result only when unambiguous.
            if (!string.IsNullOrWhiteSpace(input) && IsSimpleFileName(input))
            {
                List<string> workspaceMatches = FindScripts(input, cfg);
                if (workspaceMatches.Count == 1) return workspaceMatches[0];
                if (workspaceMatches.Count > 1)
                    throw new IOException(BuildAmbiguousMessage(input, workspaceMatches));
            }

            string requested = string.IsNullOrWhiteSpace(input) ? "the project entry file" : input;
            throw new FileNotFoundException(
                string.Format("Could not find {0} '{1}'. Searched the current directory, project root, and configured workspace. Use 'block find <name>' to inspect candidates, or provide an absolute path.", command, requested));
        }

        public static string ResolveProjectRoot(string hint, EngineConfig cfg)
        {
            List<string> roots = GetSearchRoots(cfg, hint);
            foreach (string root in roots)
            {
                if (File.Exists(Path.Combine(root, ProjectWorkspace.ProjectManifestName))) return root;
            }

            List<string> workspaceProjects = GetWorkspaceProjectRoots(cfg);
            if (workspaceProjects.Count == 1) return workspaceProjects[0];
            if (workspaceProjects.Count > 1)
                throw new IOException("Multiple Block projects were found in the configured workspace. Provide a project directory.");

            string location = string.IsNullOrWhiteSpace(hint) ? Environment.CurrentDirectory : hint;
            throw new FileNotFoundException(
                "No block.project.json was found from '" + Path.GetFullPath(location) + "' or the configured workspace.");
        }

        public static List<string> FindScripts(string query, EngineConfig cfg)
        {
            List<string> results = new List<string>();
            List<string> roots = GetSearchRoots(cfg, null);
            foreach (string projectRoot in GetWorkspaceProjectRoots(cfg)) AddUniqueDirectory(roots, projectRoot);
            string normalizedQuery = (query ?? "").Trim();
            bool hasDirectory = normalizedQuery.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
                                normalizedQuery.IndexOf(Path.AltDirectorySeparatorChar) >= 0;

            foreach (string candidate in BuildCandidates(normalizedQuery, cfg, false))
            {
                AddUniqueFile(results, candidate);
            }

            if (!hasDirectory)
            {
                foreach (string root in roots)
                {
                    try
                    {
                        foreach (string file in Directory.GetFiles(root))
                        {
                            string name = Path.GetFileName(file);
                            if (!IsScriptFile(name)) continue;
                            if (string.IsNullOrEmpty(normalizedQuery) ||
                                string.Equals(name, normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(Path.GetFileNameWithoutExtension(name), normalizedQuery, StringComparison.OrdinalIgnoreCase))
                                AddUniqueFile(results, file);
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }
                }
            }
            return results;
        }

        public static List<string> GetSearchRoots(EngineConfig cfg, string hint)
        {
            List<string> roots = new List<string>();
            AddUniqueDirectory(roots, GetDirectoryHint(hint));

            string projectRoot = TryFindProjectRoot(GetDirectoryHint(hint));
            AddUniqueDirectory(roots, projectRoot);

            string workspace = GetWorkspaceDirectory(cfg);
            AddUniqueDirectory(roots, workspace);
            return roots;
        }

        public static string GetWorkspaceDirectory(EngineConfig cfg)
        {
            string configured = cfg == null ? null : cfg.WorkspaceDir;
            if (string.IsNullOrWhiteSpace(configured))
                configured = Environment.GetEnvironmentVariable("BLOCK_WORKSPACE");
            if (string.IsNullOrWhiteSpace(configured)) return null;

            try
            {
                string path = Path.GetFullPath(configured);
                return Directory.Exists(path) ? path : null;
            }
            catch (ArgumentException) { return null; }
            catch (NotSupportedException) { return null; }
        }

        private static List<string> BuildCandidates(string input, EngineConfig cfg, bool includeProjectEntry)
        {
            List<string> candidates = new List<string>();
            string value = (input ?? "").Trim();
            if (string.IsNullOrEmpty(value))
            {
                if (includeProjectEntry)
                {
                    List<string> roots = GetSearchRoots(cfg, null);
                    foreach (string projectRoot in GetWorkspaceProjectRoots(cfg)) AddUniqueDirectory(roots, projectRoot);
                    foreach (string root in roots)
                    {
                        string manifest = Path.Combine(root, ProjectWorkspace.ProjectManifestName);
                        if (!File.Exists(manifest)) continue;
                        try
                        {
                            BlockProjectManifest project = ProjectWorkspace.LoadProject(root);
                            AddCandidate(candidates, Path.Combine(root, project.entry));
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException("Could not load project entry from " + manifest + ": " + ex.Message);
                        }
                    }
                }
                return candidates;
            }

            if (Directory.Exists(value))
            {
                string root = Path.GetFullPath(value);
                string manifest = Path.Combine(root, ProjectWorkspace.ProjectManifestName);
                if (File.Exists(manifest))
                {
                    BlockProjectManifest project = ProjectWorkspace.LoadProject(root);
                    AddCandidate(candidates, Path.Combine(root, project.entry));
                }
                return candidates;
            }

            AddCandidate(candidates, value);
            foreach (string root in GetSearchRoots(cfg, null))
                AddCandidate(candidates, Path.Combine(root, value));
            return candidates;
        }

        private static string GetDirectoryHint(string hint)
        {
            if (string.IsNullOrWhiteSpace(hint)) return Environment.CurrentDirectory;
            try
            {
                string full = Path.GetFullPath(hint);
                if (File.Exists(full) || (!Directory.Exists(full) && Path.HasExtension(full)))
                    return Path.GetDirectoryName(full);
                return full;
            }
            catch (ArgumentException) { return Environment.CurrentDirectory; }
            catch (NotSupportedException) { return Environment.CurrentDirectory; }
        }

        private static string TryFindProjectRoot(string startDirectory)
        {
            try
            {
                string candidate = ProjectWorkspace.FindProjectRoot(startDirectory);
                return File.Exists(Path.Combine(candidate, ProjectWorkspace.ProjectManifestName)) ? candidate : null;
            }
            catch (Exception) { return null; }
        }

        private static List<string> GetWorkspaceProjectRoots(EngineConfig cfg)
        {
            List<string> projects = new List<string>();
            string workspace = GetWorkspaceDirectory(cfg);
            if (string.IsNullOrWhiteSpace(workspace)) return projects;
            try
            {
                foreach (string directory in Directory.GetDirectories(workspace))
                {
                    if (File.Exists(Path.Combine(directory, ProjectWorkspace.ProjectManifestName)))
                        AddUniqueDirectory(projects, directory);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            return projects;
        }

        private static bool IsScriptFile(string name)
        {
            string extension = Path.GetExtension(name);
            foreach (string allowed in ScriptExtensions)
                if (string.Equals(extension, allowed, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void AddCandidate(List<string> candidates, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try
            {
                string full = Path.GetFullPath(value);
                if (!ContainsIgnoreCase(candidates, full)) candidates.Add(full);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
        }

        private static void AddUniqueFile(List<string> results, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && File.Exists(value)) AddCandidate(results, value);
        }

        private static void AddUniqueDirectory(List<string> roots, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try
            {
                string full = Path.GetFullPath(value);
                if (Directory.Exists(full) && !ContainsIgnoreCase(roots, full)) roots.Add(full);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
        }

        private static bool IsSimpleFileName(string value)
        {
            return value.IndexOf(Path.DirectorySeparatorChar) < 0 &&
                   value.IndexOf(Path.AltDirectorySeparatorChar) < 0 &&
                   !Path.IsPathRooted(value);
        }

        private static string BuildAmbiguousMessage(string input, List<string> matches)
        {
            List<string> lines = new List<string>();
            foreach (string match in matches) lines.Add("  - " + match);
            return "Multiple Block scripts matched '" + input + "'. Provide a project directory or an absolute path:\n" + string.Join("\n", lines.ToArray());
        }

        private static bool ContainsIgnoreCase(List<string> values, string value)
        {
            foreach (string item in values)
                if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
