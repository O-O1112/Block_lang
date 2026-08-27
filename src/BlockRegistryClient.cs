using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace BlockEngine
{
    // The package marketplace is deliberately metadata-first.  A package is
    // never executed while it is being discovered or installed.  Remote
    // content is restricted to this repository's HTTPS raw files and every
    // file is checked against the digest published in registry/index.json.
    public sealed class BlockRegistryIndex
    {
        public string schema { get; set; }
        public string generated { get; set; }
        public List<BlockRegistryPackage> packages { get; set; }
    }

    public sealed class BlockRegistryPackage
    {
        public string name { get; set; }
        public string version { get; set; }
        public string description { get; set; }
        public string engine { get; set; }
        public string license { get; set; }
        public string repository { get; set; }
        public List<string> permissions { get; set; }
        public List<string> keywords { get; set; }
        public string manifestUrl { get; set; }
        public string manifestSha256 { get; set; }
        public string entryUrl { get; set; }
        public string entrySha256 { get; set; }
    }

    public static class BlockRegistryClient
    {
        private const string RemoteIndexUrl = "https://raw.githubusercontent.com/O-O1112/Block_lang/main/registry/index.json";
        private const string OfficialRawPrefix = "https://raw.githubusercontent.com/O-O1112/Block_lang/";
        private const int MaxCatalogBytes = 1024 * 1024;
        private const int MaxPackageFileBytes = 4 * 1024 * 1024;
        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer { MaxJsonLength = (int)SecurityLimits.MaxJsonBytes };

        public static void Search(string query)
        {
            BlockRegistryIndex index = LoadIndex();
            string normalized = (query ?? "").Trim();
            Console.WriteLine("Block package marketplace");
            Console.WriteLine("  Source: official Block_lang registry");
            int count = 0;
            foreach (BlockRegistryPackage package in SafePackages(index))
            {
                string haystack = (package.name + " " + package.description + " " + Join(package.keywords)).ToLowerInvariant();
                if (normalized.Length > 0 && haystack.IndexOf(normalized.ToLowerInvariant(), StringComparison.Ordinal) < 0)
                    continue;
                Console.WriteLine(string.Format("  {0} v{1} — {2}", package.name, package.version, package.description));
                count++;
            }
            Console.WriteLine("  Results: " + count);
        }

        public static void Info(string name)
        {
            BlockRegistryPackage package = FindPackage(LoadIndex(), name);
            if (package == null) throw new ArgumentException("Package is not listed in the official registry: " + name);
            Console.WriteLine("Block package: " + package.name);
            Console.WriteLine("  Version: " + package.version);
            Console.WriteLine("  Engine: " + package.engine);
            Console.WriteLine("  License: " + package.license);
            Console.WriteLine("  Description: " + package.description);
            Console.WriteLine("  Repository: " + package.repository);
            Console.WriteLine("  Permissions: " + (package.permissions == null || package.permissions.Count == 0 ? "none" : Join(package.permissions)));
            Console.WriteLine("  Install: block pkg install " + package.name + " --remote");
        }

        public static void Install(string packageNameOrDirectory, string projectDirectory, bool remote)
        {
            if (Directory.Exists(packageNameOrDirectory))
            {
                Ecosystem.AddPackage(projectDirectory, packageNameOrDirectory);
                return;
            }
            if (!remote)
                throw new InvalidOperationException("Remote package installation is opt-in. Re-run with --remote after reviewing 'block pkg info " + packageNameOrDirectory + "'.");

            BlockRegistryPackage package = FindPackage(LoadIndex(), packageNameOrDirectory);
            if (package == null) throw new ArgumentException("Package is not listed in the official registry: " + packageNameOrDirectory);
            ValidateRegistryEntry(package);

            string projectRoot = Ecosystem.FindProjectRoot(projectDirectory);
            Ecosystem.LoadProject(projectRoot);
            string stagingRoot = Path.Combine(projectRoot, ".blocklang", "package-staging-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stagingRoot);
            try
            {
                byte[] manifestBytes = DownloadBytes(new Uri(package.manifestUrl), MaxCatalogBytes);
                VerifySha256(manifestBytes, package.manifestSha256, package.name + "/block.package.json");
                byte[] entryBytes = DownloadBytes(new Uri(package.entryUrl), MaxPackageFileBytes);
                VerifySha256(entryBytes, package.entrySha256, package.name + "/main.blk");

                File.WriteAllBytes(Path.Combine(stagingRoot, Ecosystem.PackageManifestName), manifestBytes);
                File.WriteAllBytes(Path.Combine(stagingRoot, "main.blk"), entryBytes);
                BlockPackageManifest manifest = Ecosystem.LoadPackage(stagingRoot);
                if (manifest == null || !string.Equals(manifest.name, package.name, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Package manifest name does not match the registry entry.");
                if (!string.Equals(manifest.version, package.version, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Package manifest version does not match the registry entry.");
                ValidatePermissions(manifest.permissions);
                Ecosystem.AddPackage(projectRoot, stagingRoot);
                Console.WriteLine("[Block Registry] Installed and verified " + package.name + " v" + package.version + ".");
            }
            finally
            {
                try { if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, true); } catch { }
            }
        }

        public static void Verify(string projectDirectoryOrPackage)
        {
            string root = Path.GetFullPath(projectDirectoryOrPackage ?? Environment.CurrentDirectory);
            if (File.Exists(Path.Combine(root, Ecosystem.PackageManifestName)))
            {
                VerifyPackage(root);
                return;
            }

            string projectRoot = Ecosystem.FindProjectRoot(root);
            Ecosystem.LoadProject(projectRoot);
            string packagesRoot = Path.Combine(projectRoot, "packages");
            int checkedCount = 0;
            if (Directory.Exists(packagesRoot))
            {
                foreach (string directory in Directory.GetDirectories(packagesRoot))
                {
                    if (Path.GetFileName(directory).StartsWith(".", StringComparison.Ordinal)) continue;
                    VerifyPackage(directory);
                    checkedCount++;
                }
            }
            Console.WriteLine("[Block Registry] Verified " + checkedCount + " installed package(s); no executable code was run.");
        }

        private static void VerifyPackage(string packageRoot)
        {
            if ((File.GetAttributes(packageRoot) & FileAttributes.ReparsePoint) != 0)
                throw new UnauthorizedAccessException("Package directory is a reparse point: " + packageRoot);
            BlockPackageManifest manifest = Ecosystem.LoadPackage(packageRoot);
            if (manifest == null) throw new InvalidDataException("Missing " + Ecosystem.PackageManifestName + " in " + packageRoot);
            if (!string.Equals(manifest.name, new DirectoryInfo(packageRoot).Name, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Package name does not match its directory: " + packageRoot);
            ValidatePermissions(manifest.permissions);
            string entry = Path.GetFullPath(Path.Combine(packageRoot, manifest.main));
            if (!Ecosystem.IsPathInSandbox(entry, packageRoot) || !File.Exists(entry))
                throw new InvalidDataException("Package entry is missing or escapes the package directory: " + manifest.main);
            if (new FileInfo(entry).Length > MaxPackageFileBytes)
                throw new InvalidDataException("Package entry exceeds the marketplace file limit.");
            Console.WriteLine("  OK " + manifest.name + " v" + manifest.version + " (" + manifest.main + ")");
        }

        private static BlockRegistryIndex LoadIndex()
        {
            string local = FindLocalIndex();
            if (!string.IsNullOrEmpty(local))
            {
                BlockRegistryIndex localIndex = DeserializeIndex(File.ReadAllText(local, Encoding.UTF8));
                ValidateIndex(localIndex);
                return localIndex;
            }

            byte[] bytes = DownloadBytes(new Uri(RemoteIndexUrl), MaxCatalogBytes);
            BlockRegistryIndex remoteIndex = DeserializeIndex(Encoding.UTF8.GetString(bytes));
            ValidateIndex(remoteIndex);
            return remoteIndex;
        }

        private static string FindLocalIndex()
        {
            string current = Path.GetFullPath(Environment.CurrentDirectory);
            while (!string.IsNullOrEmpty(current))
            {
                string candidate = Path.Combine(current, "registry", "index.json");
                if (File.Exists(candidate)) return candidate;
                string parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase)) break;
                current = parent;
            }
            string adjacent = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "registry", "index.json");
            return File.Exists(adjacent) ? adjacent : null;
        }

        private static BlockRegistryIndex DeserializeIndex(string json)
        {
            BlockRegistryIndex index = Serializer.Deserialize<BlockRegistryIndex>(json);
            if (index == null) throw new InvalidDataException("Registry index is empty.");
            return index;
        }

        private static void ValidateIndex(BlockRegistryIndex index)
        {
            if (!string.Equals(index.schema, "block-registry/v1", StringComparison.Ordinal))
                throw new InvalidDataException("Unsupported Block registry schema.");
            if (index.packages == null) index.packages = new List<BlockRegistryPackage>();
            foreach (BlockRegistryPackage package in index.packages) ValidateRegistryEntry(package);
        }

        private static void ValidateRegistryEntry(BlockRegistryPackage package)
        {
            if (package == null || string.IsNullOrWhiteSpace(package.name)) throw new InvalidDataException("Registry contains an invalid package entry.");
            ValidatePackageName(package.name);
            if (string.IsNullOrWhiteSpace(package.version)) throw new InvalidDataException("Registry package has no version: " + package.name);
            if (!string.IsNullOrWhiteSpace(package.license) && !string.Equals(package.license, "MIT", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Only MIT packages are accepted by the official registry in v1: " + package.name);
            ValidatePermissions(package.permissions);
            ValidateOfficialUrl(package.manifestUrl, package.name + " manifest");
            ValidateOfficialUrl(package.entryUrl, package.name + " entry");
            RequireSha256(package.manifestSha256, package.name + " manifest");
            RequireSha256(package.entrySha256, package.name + " entry");
        }

        private static void ValidateOfficialUrl(string value, string label)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(uri.Host, "raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsoluteUri.StartsWith(OfficialRawPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Registry URL is not an allowed official HTTPS raw URL (" + label + ").");
        }

        private static void ValidatePermissions(List<string> permissions)
        {
            if (permissions == null) return;
            string[] allowed = { "native", "runtime", "read_workspace", "write_workspace", "network", "graphics" };
            foreach (string permission in permissions)
            {
                bool found = false;
                foreach (string item in allowed) if (string.Equals(item, permission, StringComparison.OrdinalIgnoreCase)) found = true;
                if (!found) throw new InvalidDataException("Unknown package permission: " + permission);
            }
        }

        private static void RequireSha256(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || !IsHex(value))
                throw new InvalidDataException("Registry entry has no valid SHA-256 digest: " + label);
        }

        private static void VerifySha256(byte[] content, string expected, string label)
        {
            RequireSha256(expected, label);
            using (SHA256 sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(content)).Replace("-", "").ToLowerInvariant();
                if (!string.Equals(actual, expected.ToLowerInvariant(), StringComparison.Ordinal))
                    throw new InvalidDataException("SHA-256 mismatch for " + label + ".");
            }
        }

        private static byte[] DownloadBytes(Uri uri, int maxBytes)
        {
            ValidateOfficialUrl(uri.AbsoluteUri, "download");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.UserAgent = "BlockEngine/" + BlockVersion.Value + " package-client";
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            request.AllowAutoRedirect = false;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK) throw new InvalidDataException("Registry download returned HTTP " + (int)response.StatusCode + ".");
                using (Stream input = response.GetResponseStream())
                using (MemoryStream output = new MemoryStream())
                {
                    byte[] buffer = new byte[8192];
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (output.Length + read > maxBytes) throw new InvalidDataException("Registry content exceeds the safe download limit.");
                        output.Write(buffer, 0, read);
                    }
                    return output.ToArray();
                }
            }
        }

        private static BlockRegistryPackage FindPackage(BlockRegistryIndex index, string name)
        {
            foreach (BlockRegistryPackage package in SafePackages(index))
                if (string.Equals(package.name, name, StringComparison.OrdinalIgnoreCase)) return package;
            return null;
        }

        private static IEnumerable<BlockRegistryPackage> SafePackages(BlockRegistryIndex index)
        {
            if (index == null || index.packages == null) yield break;
            foreach (BlockRegistryPackage package in index.packages)
            {
                if (package != null) yield return package;
            }
        }

        private static void ValidatePackageName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 64) throw new InvalidDataException("Invalid package name.");
            for (int i = 0; i < name.Length; i++)
                if (!(char.IsLetterOrDigit(name[i]) || name[i] == '-' || name[i] == '_' || name[i] == '.'))
                    throw new InvalidDataException("Invalid package name: " + name);
        }

        private static bool IsHex(string value)
        {
            foreach (char c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            return true;
        }

        private static string Join(List<string> values)
        {
            return values == null ? "" : string.Join(", ", values.ToArray());
        }
    }
}
