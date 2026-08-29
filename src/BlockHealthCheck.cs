using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace BlockEngine
{
    // Read-only repository health checks for the creator's daily review.  The
    // checker parses scripts and inspects metadata; it never executes a Block
    // document, downloads code, or changes the active configuration.
    public static class BlockHealthCheck
    {
        private const int MaxFiles = 512;
        private const int MaxIssues = 256;
        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer { MaxJsonLength = (int)SecurityLimits.MaxJsonBytes };

        public static void Run(string[] args)
        {
            string root = Environment.CurrentDirectory;
            string reportPath = null;
            bool strict = false;
            for (int i = 1; i < (args == null ? 0 : args.Length); i++)
            {
                string arg = args[i] ?? "";
                if (string.Equals(arg, "--strict", StringComparison.OrdinalIgnoreCase)) { strict = true; continue; }
                if (string.Equals(arg, "--root", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    root = args[++i];
                    continue;
                }
                if (string.Equals(arg, "--report", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    reportPath = args[++i];
                    continue;
                }
            }

            root = Path.GetFullPath(root);
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            int scripts = 0;
            try
            {
                if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Health-check root not found: " + root);
                EngineConfig cfg = Config.LoadConfig();
                cfg.SandboxDir = root;
                foreach (string file in EnumerateFiles(root, errors))
                {
                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    if (extension == ".blk" || extension == ".blkl" || extension == ".blkp")
                    {
                        scripts++;
                        try
                        {
                            FileInfo info = new FileInfo(file);
                            if (info.Length > SecurityLimits.MaxScriptBytes) throw new InvalidDataException("script exceeds size limit");
                            Parser.ParseBlocks(File.ReadAllText(file, Encoding.UTF8), file, cfg);
                        }
                        catch (Exception ex) { AddIssue(errors, "script: " + file + " — " + ex.Message); }
                    }
                }

                CheckWebsite(root, warnings);
                CheckRepositoryMetadata(root, warnings);
                if (scripts == 0) warnings.Add("No Block scripts were found under the selected root.");
            }
            catch (Exception ex)
            {
                AddIssue(errors, ex.Message);
            }

            Dictionary<string, object> report = new Dictionary<string, object>();
            report["schema"] = "block-health/v1";
            report["engine"] = BlockVersion.Value;
            report["generatedUtc"] = DateTime.UtcNow.ToString("o");
            report["root"] = root;
            report["scriptsChecked"] = scripts;
            report["errors"] = errors;
            report["warnings"] = warnings;
            report["status"] = errors.Count == 0 && (!strict || warnings.Count == 0) ? "pass" : "fail";

            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                string fullReport = Path.GetFullPath(reportPath);
                string parent = Path.GetDirectoryName(fullReport);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                string temp = fullReport + ".tmp";
                File.WriteAllText(temp, Serializer.Serialize(report) + Environment.NewLine, new UTF8Encoding(false));
                if (File.Exists(fullReport)) File.Replace(temp, fullReport, null);
                else File.Move(temp, fullReport);
                Console.WriteLine("[Block Doctor] Report: " + fullReport);
            }

            Console.WriteLine("Block Health Check v" + BlockVersion.Value);
            Console.WriteLine("  Root: " + root);
            Console.WriteLine("  Scripts checked: " + scripts);
            Console.WriteLine("  Errors: " + errors.Count + "; warnings: " + warnings.Count);
            foreach (string warning in warnings) Console.WriteLine("  [warning] " + warning);
            foreach (string error in errors) Console.Error.WriteLine("  [error] " + error);
            if (errors.Count > 0 || (strict && warnings.Count > 0)) Environment.ExitCode = 1;
        }

        private static IEnumerable<string> EnumerateFiles(string root, List<string> errors)
        {
            int count = 0;
            Stack<string> pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                string directory = pending.Pop();
                string name = Path.GetFileName(directory);
                if (name == ".git" || name == ".blocklang" || name == "bin" || name == "obj" || name == "node_modules") continue;
                string[] files = null;
                string[] children = null;
                try
                {
                    if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) continue;
                    files = Directory.GetFiles(directory);
                    children = Directory.GetDirectories(directory);
                }
                catch (Exception ex) { AddIssue(errors, "scan: " + directory + " — " + ex.Message); }
                if (files == null) continue;
                foreach (string file in files)
                {
                    if (++count > MaxFiles) { AddIssue(errors, "file scan limit exceeded (" + MaxFiles + ")"); yield break; }
                    yield return file;
                }
                if (children != null)
                    foreach (string child in children) pending.Push(child);
            }
        }

        private static void CheckWebsite(string root, List<string> warnings)
        {
            string index = Path.Combine(root, "index.html");
            if (!File.Exists(index)) return;
            string html = File.ReadAllText(index, Encoding.UTF8);
            if (html.IndexOf("rel=\"canonical\"", StringComparison.OrdinalIgnoreCase) < 0)
                warnings.Add("index.html has no canonical link.");
            if (html.IndexOf("application/ld+json", StringComparison.OrdinalIgnoreCase) < 0)
                warnings.Add("index.html has no JSON-LD metadata.");
            if (html.IndexOf("O-O1112/Block_lang", StringComparison.OrdinalIgnoreCase) < 0)
                warnings.Add("index.html does not expose the official repository link.");
        }

        private static void CheckRepositoryMetadata(string root, List<string> warnings)
        {
            string[] required = { "README.md", "LICENSE", "SECURITY.md", "CONTRIBUTING.md" };
            foreach (string name in required)
                if (!File.Exists(Path.Combine(root, name))) warnings.Add("missing repository file: " + name);
        }

        private static void AddIssue(List<string> issues, string message)
        {
            if (issues.Count < MaxIssues) issues.Add(message);
            else if (issues.Count == MaxIssues) issues.Add("additional issues omitted");
        }
    }
}
