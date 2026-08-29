using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace BlockEngine
{
    // A project manifest only names the engine and entry document. Reusable
    // source remains explicit through local imports.
    public class BlockProjectManifest
    {
        public string name { get; set; }
        public string version { get; set; }
        public string engine { get; set; }
        public string entry { get; set; }

        public BlockProjectManifest()
        {
            name = "my-block-project";
            version = "0.1.0";
            engine = "standard";
            entry = "main.blk";
        }
    }

    public static class ProjectWorkspace
    {
        public const string ProjectManifestName = "block.project.json";
        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer { MaxJsonLength = (int)SecurityLimits.MaxJsonBytes };

        public static string FindProjectRoot(string startPath)
        {
            string candidate = string.IsNullOrEmpty(startPath) ? Environment.CurrentDirectory : startPath;
            candidate = Path.GetFullPath(candidate);
            if (File.Exists(candidate) || (!Directory.Exists(candidate) && Path.HasExtension(candidate)))
                candidate = Path.GetDirectoryName(candidate);

            DirectoryInfo current = new DirectoryInfo(candidate);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, ProjectManifestName)))
                    return current.FullName;
                current = current.Parent;
            }
            return Path.GetFullPath(candidate);
        }

        public static BlockProjectManifest LoadProject(string projectRoot)
        {
            string root = Path.GetFullPath(projectRoot);
            string path = Path.Combine(root, ProjectManifestName);
            if (!File.Exists(path))
                throw new FileNotFoundException("No block.project.json found in project directory.", path);

            FileInfo manifestInfo = new FileInfo(path);
            if (manifestInfo.Length > SecurityLimits.MaxJsonBytes)
                throw new InvalidDataException("block.project.json exceeds the 4 MiB safety limit.");
            string json = File.ReadAllText(path, Encoding.UTF8);
            BlockProjectManifest manifest = Serializer.Deserialize<BlockProjectManifest>(json);
            if (manifest == null) manifest = new BlockProjectManifest();
            if (string.IsNullOrWhiteSpace(manifest.name)) manifest.name = "my-block-project";
            if (string.IsNullOrWhiteSpace(manifest.version)) manifest.version = "0.1.0";
            if (string.IsNullOrWhiteSpace(manifest.engine)) manifest.engine = "standard";
            if (string.IsNullOrWhiteSpace(manifest.entry)) manifest.entry = "main.blk";

            if (Path.IsPathRooted(manifest.entry))
                throw new InvalidDataException("Project entry must be a relative path inside the project directory.");
            string entryPath = Path.GetFullPath(Path.Combine(root, manifest.entry));
            string normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!entryPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) &&
                !entryPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Project entry escapes the project directory.");
            string extension = Path.GetExtension(entryPath).ToLowerInvariant();
            if (extension != ".blk" && extension != ".block" && extension != ".blkl" &&
                extension != ".blocklite" && extension != ".blkp" && extension != ".blockplus")
                throw new InvalidDataException("Project entry must be a Block source document.");
            return manifest;
        }

        public static void RunCli(string[] args)
        {
            string command = args.Length > 1 ? (args[1] ?? "").ToLowerInvariant() : "help";
            try
            {
                if (command == "init")
                {
                    string directory = args.Length > 2 ? args[2] : Environment.CurrentDirectory;
                    string name = args.Length > 3 ? args[3] : null;
                    InitProject(directory, name);
                }
                else if (command == "list")
                {
                    string directory = args.Length > 2 ? args[2] : Environment.CurrentDirectory;
                    ListProject(directory);
                }
                else if (command == "help" || command == "--help" || command == "-h")
                {
                    PrintHelp();
                }
                else
                {
                    CliDiagnostics.ReportUsage("project " + command,
                        "block project init [directory] [name] | list [directory] | root [path] | run [path]",
                        "Third-party package commands were removed. Use <import src=\"...\" /> for reviewed local Block files.");
                }
            }
            catch (Exception ex)
            {
                CliDiagnostics.Report(ex, "project " + command);
            }
        }

        public static void InitProject(string directory, string projectName)
        {
            string root = Path.GetFullPath(directory ?? Environment.CurrentDirectory);
            Directory.CreateDirectory(root);
            string manifestPath = Path.Combine(root, ProjectManifestName);
            if (File.Exists(manifestPath))
            {
                Console.WriteLine("[Block Project] Project already exists: " + manifestPath);
                return;
            }

            BlockProjectManifest manifest = new BlockProjectManifest();
            manifest.name = !string.IsNullOrWhiteSpace(projectName) ? projectName : new DirectoryInfo(root).Name;
            WriteJson(manifestPath, manifest);

            string entryPath = Path.Combine(root, manifest.entry);
            if (!File.Exists(entryPath))
            {
                File.WriteAllText(entryPath,
                    "# Block project entry\nmessage = \"Hello from Block\"\nprint(message)\n",
                    new UTF8Encoding(false));
            }
            Console.WriteLine("[Block Project] Initialized: " + root);
            Console.WriteLine("[Block Project] Entry: " + manifest.entry);
        }

        public static void ListProject(string directory)
        {
            string root = FindProjectRoot(directory);
            BlockProjectManifest manifest = LoadProject(root);
            Console.WriteLine("[Block Project] " + manifest.name + " v" + manifest.version);
            Console.WriteLine("  Root: " + root);
            Console.WriteLine("  Engine: " + manifest.engine);
            Console.WriteLine("  Entry: " + manifest.entry);
        }

        private static void WriteJson(string path, object value)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, Serializer.Serialize(value), new UTF8Encoding(false));
            if (!File.Exists(path))
            {
                File.Move(temp, path);
                return;
            }

            try
            {
                File.Replace(temp, path, null);
            }
            catch (Exception ex)
            {
                if (!(ex is PlatformNotSupportedException) && !(ex is IOException) && !(ex is UnauthorizedAccessException))
                    throw;
                string backup = path + ".backup-" + Guid.NewGuid().ToString("N");
                File.Move(path, backup);
                try
                {
                    File.Move(temp, path);
                    try { File.Delete(backup); } catch { }
                }
                catch
                {
                    if (!File.Exists(path) && File.Exists(backup)) File.Move(backup, path);
                    throw;
                }
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Block project commands:");
            Console.WriteLine("  block project init [directory] [name]");
            Console.WriteLine("  block project list [directory]");
            Console.WriteLine("  block project root [path]");
            Console.WriteLine("  block project run [path]");
        }
    }
}
