using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace BlockEngine
{
    public sealed class BlockSyntaxTree
    {
        public int SchemaVersion { get; set; }
        public string Kind { get; set; }
        public List<BlockSyntaxNode> Blocks { get; set; }
        public List<BlockSyntaxDiagnostic> Diagnostics { get; set; }
    }

    public sealed class BlockSyntaxNode
    {
        public string Kind { get; set; }
        public string Language { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string Code { get; set; }
    }

    public sealed class BlockSyntaxDiagnostic
    {
        public string Severity { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    // Stable, execution-free document API for editors and third-party tools.
    // It intentionally describes Block's top-level language boundaries without
    // loading imports, packages, runtimes, or custom command definitions.
    public static class BlockSyntax
    {
        private static readonly Regex Tag = new Regex(@"^<(\/)?\s*([a-zA-Z0-9_\-]+)\s*>$", RegexOptions.Compiled);
        private static readonly HashSet<string> KnownLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "py", "python", "js", "javascript", "php", "ruby", "rb", "lua", "ps", "powershell",
            "sql", "html", "json", "c", "cpp", "c++", "go", "golang", "rust", "rs", "java",
            "ts", "typescript", "cs", "csharp", "kotlin", "kt", "dart", "zig", "perl", "pl",
            "bash", "sh", "r", "server", "route", "del"
        };

        public static BlockSyntaxTree Parse(string code)
        {
            string normalized = (code ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split('\n');
            BlockSyntaxTree tree = new BlockSyntaxTree
            {
                SchemaVersion = 1,
                Kind = "Document",
                Blocks = new List<BlockSyntaxNode>(),
                Diagnostics = new List<BlockSyntaxDiagnostic>()
            };

            string currentLanguage = "block";
            int contentStart = 1;
            List<string> buffer = new List<string>();

            for (int index = 0; index < lines.Length; index++)
            {
                int lineNumber = index + 1;
                Match match = Tag.Match(lines[index].Trim());
                if (!match.Success || !KnownLanguages.Contains(match.Groups[2].Value))
                {
                    buffer.Add(lines[index]);
                    continue;
                }

                bool closing = match.Groups[1].Success;
                string language = match.Groups[2].Value.ToLowerInvariant();
                if (!closing && currentLanguage == "block")
                {
                    AddNode(tree.Blocks, currentLanguage, contentStart, lineNumber - 1, buffer);
                    buffer.Clear();
                    currentLanguage = language;
                    contentStart = lineNumber + 1;
                    continue;
                }

                if (closing && currentLanguage != "block")
                {
                    if (!string.Equals(currentLanguage, language, StringComparison.OrdinalIgnoreCase))
                    {
                        AddDiagnostic(tree, "BLK1002", "Mismatched closing tag </" + language + ">; expected </" + currentLanguage + ">.", lineNumber);
                        buffer.Add(lines[index]);
                        continue;
                    }

                    AddNode(tree.Blocks, currentLanguage, contentStart, lineNumber - 1, buffer);
                    buffer.Clear();
                    currentLanguage = "block";
                    contentStart = lineNumber + 1;
                    continue;
                }

                if (closing)
                    AddDiagnostic(tree, "BLK1001", "Unmatched closing tag </" + language + ">.", lineNumber);
                else
                    AddDiagnostic(tree, "BLK1003", "Nested language tag <" + language + "> inside <" + currentLanguage + "> is not supported.", lineNumber);
                buffer.Add(lines[index]);
            }

            if (currentLanguage != "block")
                AddDiagnostic(tree, "BLK1004", "Unclosed language block <" + currentLanguage + ">.", Math.Max(1, contentStart - 1));

            AddNode(tree.Blocks, currentLanguage, contentStart, lines.Length, buffer);
            return tree;
        }

        public static string ToJson(BlockSyntaxTree tree)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = (int)SecurityLimits.MaxJsonBytes };
            return serializer.Serialize(tree);
        }

        private static void AddNode(List<BlockSyntaxNode> nodes, string language, int startLine, int endLine, List<string> lines)
        {
            if (lines.Count == 0) return;
            string code = string.Join("\n", lines);
            if (language == "block" && string.IsNullOrWhiteSpace(code)) return;
            nodes.Add(new BlockSyntaxNode
            {
                Kind = "LanguageBlock",
                Language = language,
                StartLine = Math.Max(1, startLine),
                EndLine = Math.Max(Math.Max(1, startLine), endLine),
                Code = code
            });
        }

        private static void AddDiagnostic(BlockSyntaxTree tree, string code, string message, int line)
        {
            tree.Diagnostics.Add(new BlockSyntaxDiagnostic
            {
                Severity = "error",
                Code = code,
                Message = message,
                Line = line,
                Column = 1
            });
        }
    }
}
