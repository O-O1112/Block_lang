using System;
using System.Collections.Generic;

namespace BlockEngine
{
    // The machine-readable source of truth for the public BLKxxxx diagnostic
    // contract. Keep the explanations short enough for CLI output; the
    // long-form handbook in docs/ERROR-CATALOG.md provides examples and
    // recovery playbooks.
    public sealed class BlockDiagnosticDefinition
    {
        public string Code { get; private set; }
        public string Category { get; private set; }
        public string Title { get; private set; }
        public string Explanation { get; private set; }
        public string DefaultHint { get; private set; }

        public BlockDiagnosticDefinition(string code, string category, string title,
            string explanation, string defaultHint)
        {
            Code = code;
            Category = category;
            Title = title;
            Explanation = explanation;
            DefaultHint = defaultHint;
        }
    }

    public static class BlockErrorCatalog
    {
        public const string DocumentationPath = "docs/ERROR-CATALOG.md";
        private static readonly Dictionary<string, BlockDiagnosticDefinition> Definitions =
            new Dictionary<string, BlockDiagnosticDefinition>(StringComparer.OrdinalIgnoreCase);

        static BlockErrorCatalog()
        {
            Add("BLK0001", "CLI", "Missing or invalid command arguments",
                "The command did not receive the required arguments or received an unsupported shape.",
                "Run 'block help' to see the command syntax and quote paths that contain spaces.");
            Add("BLK0002", "CLI", "Invalid command input",
                "A command argument was present but could not be interpreted safely.",
                "Check the command spelling, value format, and the selected edition, then run 'block help'.");

            Add("BLK1001", "Path", "File not found",
                "Block could not resolve the requested document in the current project or configured workspace.",
                "Run 'block find <name>', check the extension, quote paths containing spaces, or provide an absolute path.");
            Add("BLK1002", "Path", "Directory not found",
                "A project, workspace, or sandbox directory does not exist at the requested location.",
                "Check the directory path and run 'block workspace show' or 'block config show'.");
            Add("BLK1003", "Syntax", "Nested language tag",
                "The structural parser found a language tag inside another language block.",
                "Close the outer language block before opening another one, or move the inner code to a separate top-level block.");
            Add("BLK1004", "Syntax", "Unclosed language block",
                "A structural parse reached the end of the document while a language block was still open.",
                "Add the matching closing tag after the final line of the block.");
            Add("BLK1005", "Path", "Ambiguous script path",
                "More than one safe search candidate matched the requested script name.",
                "Provide a project directory or an absolute path so Block can select one document deterministically.");
            Add("BLK1006", "Compatibility", "Language unavailable in this edition",
                "The document uses a language tag that is not included in the selected Block edition.",
                "Use a language supported by the current edition or run the document with a compatible edition.");
            Add("BLK1007", "Compatibility", "Language requires Block+",
                "The selected language is reserved for the Block+ edition and cannot run in the current edition.",
                "Use Block+ for this language, or replace the block with a runtime supported by the current edition.");
            Add("BLK1008", "Syntax", "Unknown language tag",
                "Block could not match the language tag to a built-in or reviewed custom runtime.",
                "Check the tag spelling and choose a supported language before running the document.");
            Add("BLK1101", "Syntax", "Syntax or tag error",
                "A Block document has an invalid, mismatched, nested, or unclosed language boundary.",
                "Run 'block check <file>', then repair the opening and closing tags at the reported location.");
            Add("BLK1201", "Import", "Import failed",
                "An explicit local import could not be loaded or parsed.",
                "Check the <import src=\"...\" /> path and keep the imported file inside the project or sandbox.");
            Add("BLK1202", "Import", "Invalid import graph",
                "Imports form a cycle or exceed the maximum nesting depth.",
                "Remove the circular reference and keep the import graph at or below the documented depth limit.");
            Add("BLK1203", "Import", "Import resource limit exceeded",
                "The document exceeded the safe count or byte budget for imported files.",
                "Reduce the number or size of imports and keep reusable code in focused local files.");
            Add("BLK1301", "Compatibility", "Third-party packages are not supported",
                "The removed package directive was found in the source document.",
                "Use a reviewed local <import src=\"...\" /> instead of <use package=\"...\" />.");

            Add("BLK2001", "Security", "Blocked by the safety policy",
                "The requested path or operation would cross the configured project or sandbox boundary.",
                "Review 'block config show' and keep imported files and project entries inside the allowed boundary.");
            Add("BLK2002", "Security", "Runtime disabled by configuration",
                "The host runtime is installed or recognized, but its execution switch is disabled.",
                "Review the runtime setting with 'block config' and enable it only for trusted source files.");
            Add("BLK2003", "Security", "Network access blocked",
                "The configured network guard stopped a host runtime from making network-related calls.",
                "Remove the network dependency or review the trusted execution policy before changing the guard.");
            Add("BLK2101", "Security", "Custom language definition blocked",
                "A custom <define> tag was rejected because custom definitions are disabled.",
                "Use a built-in runtime, or review the source and enable AllowCustomDefinitions deliberately.");
            Add("BLK2102", "Security", "Invalid custom language definition",
                "A custom runtime definition failed its identifier, command, extension, or size safety rules.",
                "Use a short identifier and extension, avoid shell syntax, and review the custom runtime policy.");

            Add("BLK3001", "Data", "Invalid or untrusted data",
                "A manifest, configuration value, or serialized input could not be accepted safely.",
                "Review the referenced data, keep it inside the project boundary, and retry with a known-good copy.");
            Add("BLK3002", "Data", "Script is too large",
                "The source document exceeds Block's configured script-size limit.",
                "Split the document into smaller local imports and keep generated input bounded.");
            Add("BLK3003", "Data", "Invalid state payload",
                "A runtime returned state that was too large or was not valid serializable JSON.",
                "Return only strings, numbers, booleans, lists, and maps; keep the state payload below the documented limit.");
            Add("BLK3004", "Data", "Output limit exceeded",
                "A rendered or captured output exceeded Block's configured output budget.",
                "Reduce output volume, paginate it, or write a bounded artifact instead of printing a large stream.");
            Add("BLK3005", "Data", "Request body too large",
                "An API request exceeded the configured maximum body size before execution began.",
                "Send a smaller document or raise the limit only in a controlled, trusted local environment.");
            Add("BLK3101", "Native Block", "Block expression error",
                "The native Block stage could not evaluate an expression, statement, function call, or collection access.",
                "Read the reported source line, check the value type and spelling, then run 'block check <file>'.");

            Add("BLK4001", "Runtime", "Execution timed out",
                "A host process or native operation exceeded the configured execution time limit.",
                "Check for an infinite loop or blocked command, reduce the input, and review 'block config show'.");
            Add("BLK4002", "Runtime", "Required runtime not found",
                "Block could not find the host executable required by the language block.",
                "Run 'block runtimes', install the runtime from its official source, and open a new terminal if PATH changed.");
            Add("BLK4003", "Runtime", "Host runtime failed",
                "A host runtime started but returned a non-zero exit code.",
                "Read the runtime detail, reproduce the native snippet directly, and verify its working directory and inputs.");
            Add("BLK4004", "Runtime", "Compilation failed",
                "A compiled-language toolchain could not produce a runnable artifact.",
                "Read the compiler detail, confirm the toolchain is installed, and check the source for language-specific errors.");
            Add("BLK9001", "Internal", "Unexpected internal failure",
                "Block encountered an error that is not covered by a more specific public diagnostic.",
                "Run 'block doctor --full'; maintainers can set BLOCK_DEBUG=1 for one safe reproduction.");
        }

        public static bool TryGet(string code, out BlockDiagnosticDefinition definition)
        {
            return Definitions.TryGetValue(NormalizeCode(code), out definition);
        }

        public static IList<BlockDiagnosticDefinition> GetAll()
        {
            List<BlockDiagnosticDefinition> result = new List<BlockDiagnosticDefinition>(Definitions.Values);
            result.Sort(delegate(BlockDiagnosticDefinition left, BlockDiagnosticDefinition right)
            {
                return string.Compare(left.Code, right.Code, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        public static string NormalizeCode(string code)
        {
            string value = (code ?? "").Trim().ToUpperInvariant();
            if (!value.StartsWith("BLK", StringComparison.Ordinal)) value = "BLK" + value;
            return value;
        }

        private static void Add(string code, string category, string title, string explanation, string defaultHint)
        {
            Definitions[code] = new BlockDiagnosticDefinition(code, category, title, explanation, defaultHint);
        }
    }
}
