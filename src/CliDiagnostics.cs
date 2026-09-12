using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BlockEngine
{
    public sealed class BlockDiagnosticException : Exception
    {
        public string DiagnosticCode { get; private set; }
        public string DiagnosticTitle { get; private set; }
        public string FilePath { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public string Hint { get; private set; }

        public BlockDiagnosticException(string code, string title, string detail,
            string filePath, int line, int column, string hint)
            : base(detail)
        {
            DiagnosticCode = string.IsNullOrWhiteSpace(code) ? "BLK9001" : code;
            DiagnosticTitle = string.IsNullOrWhiteSpace(title) ? "Operation failed" : title;
            FilePath = filePath;
            Line = line;
            Column = column;
            Hint = hint;
        }
    }

    public static class CliDiagnostics
    {
        private sealed class DiagnosticView
        {
            public string Code;
            public string Category;
            public string Title;
            public string Why;
            public string Detail;
            public string FilePath;
            public int Line;
            public int Column;
            public string Hint;
            public string Docs;
        }

        public static void Report(Exception exception, string operation, string filePath = null, string hint = null)
        {
            Exception error = Unwrap(exception);
            DiagnosticView view = Classify(error, filePath, hint);

            Console.Error.WriteLine(string.Format("error[{0}]: {1}", view.Code, view.Title));
            WriteField("operation", string.IsNullOrWhiteSpace(operation) ? "unknown" : operation);
            if (!string.IsNullOrWhiteSpace(view.Category)) WriteField("category", view.Category);
            if (!string.IsNullOrWhiteSpace(view.FilePath)) WriteField("file", SafeFullPath(view.FilePath));
            if (view.Line > 0)
            {
                string location = view.Column > 0
                    ? string.Format("{0}:{1}", view.Line, view.Column)
                    : view.Line.ToString();
                WriteField("location", location);
            }
            if (!string.IsNullOrWhiteSpace(view.Why)) WriteField("why", view.Why);
            WriteField("detail", string.IsNullOrWhiteSpace(view.Detail) ? "No additional detail was provided." : view.Detail);
            WriteSourceExcerpt(view.FilePath, view.Line, view.Column);
            if (!string.IsNullOrWhiteSpace(view.Hint)) WriteField("hint", view.Hint);
            if (!string.IsNullOrWhiteSpace(view.Docs)) WriteField("docs", view.Docs);

            if (IsDebugEnabled())
            {
                WriteField("exception", error.GetType().FullName);
                if (!string.IsNullOrWhiteSpace(error.StackTrace)) WriteField("stack", error.StackTrace);
            }
            else if (view.Code == "BLK9001")
            {
                WriteField("debug", "Set BLOCK_DEBUG=1 and run the command again to include technical details.");
            }

            Environment.ExitCode = 1;
        }

        public static void ReportUsage(string operation, string usage, string hint = null)
        {
            Console.Error.WriteLine("error[BLK0001]: Missing or invalid command arguments");
            WriteField("operation", string.IsNullOrWhiteSpace(operation) ? "command" : operation);
            WriteField("category", "CLI");
            WriteField("usage", usage);
            WriteField("hint", string.IsNullOrWhiteSpace(hint) ? "Run 'block help' to list available commands." : hint);
            WriteField("docs", BlockErrorCatalog.DocumentationPath + "#blk0001");
            Environment.ExitCode = 1;
        }

        private static DiagnosticView Classify(Exception error, string filePath, string hint)
        {
            BlockDiagnosticException diagnostic = error as BlockDiagnosticException;
            if (diagnostic != null)
            {
                return ApplyDefinition(new DiagnosticView
                {
                    Code = diagnostic.DiagnosticCode,
                    Title = diagnostic.DiagnosticTitle,
                    Detail = diagnostic.Message,
                    FilePath = string.IsNullOrWhiteSpace(diagnostic.FilePath) ? filePath : diagnostic.FilePath,
                    Line = diagnostic.Line,
                    Column = diagnostic.Column,
                    Hint = string.IsNullOrWhiteSpace(hint) ? diagnostic.Hint : hint
                });
            }

            DiagnosticView view = new DiagnosticView
            {
                Code = "BLK9001",
                Title = "Operation failed",
                Detail = error == null ? null : error.Message,
                FilePath = filePath,
                Hint = hint
            };
            string message = error == null ? "" : (error.Message ?? "");

            FileNotFoundException missingFile = error as FileNotFoundException;
            if (missingFile != null)
            {
                view.Code = "BLK1001";
                view.Title = "File not found";
                if (string.IsNullOrWhiteSpace(view.FilePath)) view.FilePath = missingFile.FileName;
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Run 'block find <name>', quote paths that contain spaces, or provide an absolute path.";
                return ApplyDefinition(view);
            }

            if (error is DirectoryNotFoundException)
            {
                view.Code = "BLK1002";
                view.Title = "Directory not found";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Check the path, then use 'block workspace show' to inspect configured search roots.";
                return ApplyDefinition(view);
            }

            if (error is IOException &&
                (Contains(message, "Multiple Block scripts matched") ||
                 Contains(message, "Multiple Block projects were found")))
            {
                view.Code = "BLK1005";
                view.Title = "Ambiguous script path";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Provide a project directory or an absolute path so Block can select one document deterministically.";
                return ApplyDefinition(view);
            }

            if (error is UnauthorizedAccessException)
            {
                view.Code = "BLK2001";
                view.Title = "Blocked by the safety policy";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Review the sandbox and workspace with 'block config show'. Keep imported files inside the allowed project boundary.";
                return ApplyDefinition(view);
            }

            if (error is InvalidDataException)
            {
                if (Contains(message, "BLOCK_STATE_OUT") || Contains(message, "state payload"))
                {
                    view.Code = "BLK3003";
                    view.Title = "Invalid state payload";
                }
                else if (Contains(message, "script exceeds") || Contains(message, "Script file exceeds"))
                {
                    view.Code = "BLK3002";
                    view.Title = "Script is too large";
                }
                else if (Contains(message, "custom language") || Contains(message, "Language identifiers") ||
                         Contains(message, "Custom definitions"))
                {
                    view.Code = "BLK2102";
                    view.Title = "Invalid custom language definition";
                }
                else
                {
                    view.Code = "BLK3001";
                    view.Title = "Invalid or untrusted data";
                }
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Review the referenced file or manifest, keep it inside the project boundary, and retry with a known-good copy.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Import depth limit") || Contains(message, "Circular import"))
            {
                view.Code = "BLK1202";
                view.Title = Contains(message, "Circular import") ? "Circular import" : "Import graph is too deep";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Remove the circular reference and keep the import graph within the documented depth limit.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Import resource limit"))
            {
                view.Code = "BLK1203";
                view.Title = "Import resource limit exceeded";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Reduce the number or size of imports and keep reusable code in focused local files.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Script file exceeds") || Contains(message, "script exceeds"))
            {
                view.Code = "BLK3002";
                view.Title = "Script is too large";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Split the document into smaller local imports and keep generated input bounded.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Rendered output exceeds") || Contains(message, "output limit"))
            {
                view.Code = "BLK3004";
                view.Title = "Output limit exceeded";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Reduce output volume, paginate it, or write a bounded artifact instead of printing a large stream.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Network access is disabled") || Contains(message, "network access is blocked"))
            {
                view.Code = "BLK2003";
                view.Title = "Network access blocked";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Remove the network dependency or review the trusted execution policy before changing the guard.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "execution is disabled in config"))
            {
                view.Code = "BLK2002";
                view.Title = "Runtime disabled by configuration";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Review the runtime setting with 'block config' and enable it only for trusted source files.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "not supported in Block Lite"))
            {
                view.Code = "BLK1006";
                view.Title = "Language unavailable in this edition";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Use a Lite-supported tag or run the document with an edition that supports this language.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "exclusive to Block+"))
            {
                view.Code = "BLK1007";
                view.Title = "Language requires Block+";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Use Block+ for this language, or replace the block with a runtime supported by the current edition.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Unknown language tag"))
            {
                view.Code = "BLK1008";
                view.Title = "Unknown language tag";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Check the tag spelling, choose a built-in language, or define a reviewed custom runtime in Block+.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Custom language process timed out") || Contains(message, "Process execution timed out") ||
                Contains(message, "process timed out") || Contains(message, "timed out"))
            {
                view.Code = "BLK4001";
                view.Title = "Execution timed out";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Check for an infinite loop or blocked command, reduce the input, and review 'block config show'.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Could not find executable") || Contains(message, "not found on PATH") ||
                Contains(message, "Runtime for '<"))
            {
                view.Code = "BLK4002";
                view.Title = "Required runtime not found";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Run 'block runtimes', install the runtime from its official source, and open a new terminal if PATH changed.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "compilation failed") || Contains(message, "compiler completed without producing"))
            {
                view.Code = "BLK4004";
                view.Title = "Compilation failed";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Read the compiler detail, confirm the toolchain is installed, and check the source for language-specific errors.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "exited with error code") || Contains(message, "Subprocess exited") ||
                Contains(message, "sqlite3 exited") || Contains(message, "executable exited"))
            {
                view.Code = "BLK4003";
                view.Title = "Host runtime failed";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Read the runtime detail, reproduce the native snippet directly, and verify its working directory and inputs.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Native Block error"))
            {
                view.Code = "BLK3101";
                view.Title = NativeBlockTitle(message);
                view.Line = ExtractLine(message);
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = NativeBlockHint(message);
                return ApplyDefinition(view);
            }

            if (Contains(message, "timed out") || Contains(message, "timeout"))
            {
                view.Code = "BLK4001";
                view.Title = "Execution timed out";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Check for an infinite loop, then review the execution limit with 'block config show'.";
                return ApplyDefinition(view);
            }

            if (Contains(message, "Could not find executable") || Contains(message, "not found on PATH"))
            {
                view.Code = "BLK4002";
                view.Title = "Required runtime not found";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Run 'block runtimes' to see which runtime is missing and whether it is enabled.";
                return ApplyDefinition(view);
            }

            if (error is ArgumentException)
            {
                view.Code = "BLK0002";
                view.Title = "Invalid command input";
                if (string.IsNullOrWhiteSpace(view.Hint)) view.Hint = "Run 'block help' and check the command syntax.";
                return ApplyDefinition(view);
            }

            if (error is InvalidOperationException && IsSyntaxMessage(message))
            {
                view.Code = "BLK1101";
                view.Title = "Syntax error";
                view.Line = ExtractLine(message);
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Check opening and closing tags, then run 'block check <file>' again.";
                return ApplyDefinition(view);
            }

            if (string.IsNullOrWhiteSpace(view.Hint))
                view.Hint = "Run 'block doctor --full' to inspect the environment and project.";
            return ApplyDefinition(view);
        }

        private static DiagnosticView ApplyDefinition(DiagnosticView view)
        {
            BlockDiagnosticDefinition definition;
            if (BlockErrorCatalog.TryGet(view.Code, out definition))
            {
                if (string.IsNullOrWhiteSpace(view.Category)) view.Category = definition.Category;
                if (string.IsNullOrWhiteSpace(view.Why)) view.Why = definition.Explanation;
                if (string.IsNullOrWhiteSpace(view.Hint)) view.Hint = definition.DefaultHint;
                view.Docs = BlockErrorCatalog.DocumentationPath + "#" + view.Code.ToLowerInvariant();
            }
            return view;
        }

        private static string NativeBlockTitle(string message)
        {
            if (Contains(message, "Unknown variable")) return "Unknown variable";
            if (Contains(message, "Unknown function")) return "Unknown function";
            if (Contains(message, "expects") || Contains(message, "argument")) return "Invalid function arguments";
            if (Contains(message, "index") || Contains(message, "indexable")) return "Invalid collection access";
            if (Contains(message, "Division by zero")) return "Division by zero";
            if (Contains(message, "loop")) return "Native Block loop error";
            return "Block expression error";
        }

        private static string NativeBlockHint(string message)
        {
            if (Contains(message, "Unknown variable")) return "Check the variable spelling and assign it before use, for example: name = value.";
            if (Contains(message, "Unknown function")) return "Check the function name or define it before calling it.";
            if (Contains(message, "expects") || Contains(message, "argument")) return "Check the function signature and pass the expected number and type of arguments.";
            if (Contains(message, "index") || Contains(message, "indexable")) return "Check that the value is a list, string, or map and that the index is valid.";
            if (Contains(message, "Division by zero")) return "Check the divisor before dividing and handle the zero case explicitly.";
            if (Contains(message, "loop")) return "Check the loop condition, iterator, and 10,000-iteration safety limit.";
            return "Check the expression on the reported line for invalid syntax, value types, or an unavailable name.";
        }

        private static Exception Unwrap(Exception error)
        {
            if (error == null) return new Exception("Unknown failure.");
            AggregateException aggregate = error as AggregateException;
            if (aggregate != null)
            {
                AggregateException flat = aggregate.Flatten();
                if (flat.InnerExceptions.Count == 1) return Unwrap(flat.InnerExceptions[0]);
            }
            return error;
        }

        private static bool IsSyntaxMessage(string message)
        {
            return Contains(message, "syntax") || Contains(message, "closing tag") ||
                   Contains(message, "unclosed") || Contains(message, "mismatched") ||
                   Contains(message, "Native Block error at line") || Contains(message, "Invalid assignment");
        }

        private static int ExtractLine(string message)
        {
            Match match = Regex.Match(message ?? "", @"\bline\s+(\d+)\b", RegexOptions.IgnoreCase);
            int line;
            return match.Success && int.TryParse(match.Groups[1].Value, out line) ? line : 0;
        }

        private static bool Contains(string value, string fragment)
        {
            return (value ?? "").IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void WriteField(string label, string value)
        {
            if (value == null) return;
            string normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split('\n');
            Console.Error.WriteLine(string.Format("  {0,-9}: {1}", label, lines.Length == 0 ? "" : lines[0]));
            for (int i = 1; i < lines.Length; i++) Console.Error.WriteLine("             " + lines[i]);
        }

        private static void WriteSourceExcerpt(string filePath, int line, int column)
        {
            if (line <= 0 || string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            try
            {
                string sourceLine = ReadLine(filePath, line);
                if (sourceLine == null) return;
                string display = sourceLine.Length > 180 ? sourceLine.Substring(0, 177) + "..." : sourceLine;
                WriteField("source", string.Format("{0} | {1}", line, display));
                if (column > 0)
                {
                    int prefix = line.ToString().Length + 3 + Math.Min(column - 1, display.Length);
                    Console.Error.WriteLine("             " + new string(' ', prefix) + "^");
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static string ReadLine(string path, int targetLine)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string line = null;
                for (int index = 1; index <= targetLine; index++)
                {
                    line = reader.ReadLine();
                    if (line == null) return null;
                }
                return line;
            }
        }

        private static string SafeFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            try { return Path.GetFullPath(path); }
            catch (ArgumentException) { return path; }
            catch (NotSupportedException) { return path; }
        }

        private static bool IsDebugEnabled()
        {
            string value = Environment.GetEnvironmentVariable("BLOCK_DEBUG");
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
