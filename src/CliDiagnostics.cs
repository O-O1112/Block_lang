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
            public string Title;
            public string Detail;
            public string FilePath;
            public int Line;
            public int Column;
            public string Hint;
        }

        public static void Report(Exception exception, string operation, string filePath = null, string hint = null)
        {
            Exception error = Unwrap(exception);
            DiagnosticView view = Classify(error, filePath, hint);

            Console.Error.WriteLine(string.Format("error[{0}]: {1}", view.Code, view.Title));
            WriteField("operation", string.IsNullOrWhiteSpace(operation) ? "unknown" : operation);
            if (!string.IsNullOrWhiteSpace(view.FilePath)) WriteField("file", SafeFullPath(view.FilePath));
            if (view.Line > 0)
            {
                string location = view.Column > 0
                    ? string.Format("{0}:{1}", view.Line, view.Column)
                    : view.Line.ToString();
                WriteField("location", location);
            }
            WriteField("detail", string.IsNullOrWhiteSpace(view.Detail) ? "No additional detail was provided." : view.Detail);
            WriteSourceExcerpt(view.FilePath, view.Line, view.Column);
            if (!string.IsNullOrWhiteSpace(view.Hint)) WriteField("hint", view.Hint);

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
            WriteField("usage", usage);
            WriteField("hint", string.IsNullOrWhiteSpace(hint) ? "Run 'block help' to list available commands." : hint);
            Environment.ExitCode = 1;
        }

        private static DiagnosticView Classify(Exception error, string filePath, string hint)
        {
            BlockDiagnosticException diagnostic = error as BlockDiagnosticException;
            if (diagnostic != null)
            {
                return new DiagnosticView
                {
                    Code = diagnostic.DiagnosticCode,
                    Title = diagnostic.DiagnosticTitle,
                    Detail = diagnostic.Message,
                    FilePath = string.IsNullOrWhiteSpace(diagnostic.FilePath) ? filePath : diagnostic.FilePath,
                    Line = diagnostic.Line,
                    Column = diagnostic.Column,
                    Hint = string.IsNullOrWhiteSpace(hint) ? diagnostic.Hint : hint
                };
            }

            DiagnosticView view = new DiagnosticView
            {
                Code = "BLK9001",
                Title = "Operation failed",
                Detail = error == null ? null : error.Message,
                FilePath = filePath,
                Hint = hint
            };

            FileNotFoundException missingFile = error as FileNotFoundException;
            if (missingFile != null)
            {
                view.Code = "BLK1001";
                view.Title = "File not found";
                if (string.IsNullOrWhiteSpace(view.FilePath)) view.FilePath = missingFile.FileName;
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Run 'block find <name>', quote paths that contain spaces, or provide an absolute path.";
                return view;
            }

            if (error is DirectoryNotFoundException)
            {
                view.Code = "BLK1002";
                view.Title = "Directory not found";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Check the path, then use 'block workspace show' to inspect configured search roots.";
                return view;
            }

            if (error is UnauthorizedAccessException)
            {
                view.Code = "BLK2001";
                view.Title = "Blocked by the safety policy";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Review the sandbox and workspace with 'block config show'. Keep imported files inside the allowed project boundary.";
                return view;
            }

            if (error is InvalidDataException)
            {
                view.Code = "BLK3001";
                view.Title = "Invalid or untrusted data";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "For packages, run 'block pkg verify .'. Re-download files whose digest or manifest does not match.";
                return view;
            }

            string message = error == null ? "" : (error.Message ?? "");
            if (Contains(message, "timed out") || Contains(message, "timeout"))
            {
                view.Code = "BLK4001";
                view.Title = "Execution timed out";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Check for an infinite loop, then review the execution limit with 'block config show'.";
                return view;
            }

            if (Contains(message, "Could not find executable") || Contains(message, "not found on PATH"))
            {
                view.Code = "BLK4002";
                view.Title = "Required runtime not found";
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Run 'block runtimes' to see which runtime is missing and whether it is enabled.";
                return view;
            }

            if (error is ArgumentException)
            {
                view.Code = "BLK0002";
                view.Title = "Invalid command input";
                if (string.IsNullOrWhiteSpace(view.Hint)) view.Hint = "Run 'block help' and check the command syntax.";
                return view;
            }

            if (error is InvalidOperationException && IsSyntaxMessage(message))
            {
                view.Code = "BLK1101";
                view.Title = "Syntax error";
                view.Line = ExtractLine(message);
                if (string.IsNullOrWhiteSpace(view.Hint))
                    view.Hint = "Check opening and closing tags, then run 'block check <file>' again.";
                return view;
            }

            if (string.IsNullOrWhiteSpace(view.Hint))
                view.Hint = "Run 'block doctor --full' to inspect the environment and project.";
            return view;
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
