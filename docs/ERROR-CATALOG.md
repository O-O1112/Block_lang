# Block Diagnostic Error Code Catalog

This is the public handbook for Block's `BLKxxxx` diagnostic codes. The
catalog is intentionally versioned with the engine so that the CLI, the
documentation, editors, and CI can use the same names and recovery guidance.

The machine-readable catalog is implemented in `src/BlockErrorCatalog.cs`.
The short command-line lookup is:

```powershell
block errors
block errors BLK1101
```

The same command is available as `block-lite` and `block-plus`. It is
read-only: it does not execute a document, install a runtime, access the
network, or change configuration.

## Reading a diagnostic

An execution or CLI failure has this shape:

```text
error[BLK1101]: Mismatched closing tag
  operation: execute
  category : Syntax
  file     : C:\Projects\demo.blk
  location : 7:1
  why      : A Block document has an invalid, mismatched, nested, or unclosed language boundary.
  detail   : Mismatched closing tag </js>; expected </python>.
  source   : </js>
  hint     : Replace the closing tag with </python>, or close the outer block before starting another language block.
  docs     : docs/ERROR-CATALOG.md#blk1101
```

Fields are designed for both people and tools:

| Field | Meaning |
| --- | --- |
| `error[BLKxxxx]` | Stable public code and short title. Search or report this first. |
| `operation` | Command or pipeline stage that failed. |
| `category` | Broad area: CLI, Path, Syntax, Import, Security, Data, Runtime, or Internal. |
| `file` | The affected file, when one is known. |
| `location` | One-based line and optional column. |
| `why` | Plain-language explanation of the class of failure. |
| `detail` | The specific exception or runtime message. |
| `source` | A safe source excerpt and caret, when the file and line are available. |
| `hint` | The next safe repair or inspection step. |
| `docs` | Stable handbook anchor for the code. |

Stack traces are hidden by default. Maintainers can set `BLOCK_DEBUG=1` for a
single reproduction. Remove private paths, tokens, credentials, and personal
data before sharing any diagnostic output.

## Index

| Code | Category | Short title |
| --- | --- | --- |
| BLK0001 | CLI | Missing or invalid command arguments |
| BLK0002 | CLI | Invalid command input |
| BLK1001 | Path | File not found |
| BLK1002 | Path | Directory not found |
| BLK1003 | Syntax | Nested language tag |
| BLK1004 | Syntax | Unclosed language block |
| BLK1005 | Path | Ambiguous script path |
| BLK1006 | Compatibility | Language unavailable in this edition |
| BLK1007 | Compatibility | Language requires Block+ |
| BLK1008 | Syntax | Unknown language tag |
| BLK1101 | Syntax | Syntax or tag error |
| BLK1201 | Import | Import failed |
| BLK1202 | Import | Invalid import graph |
| BLK1203 | Import | Import resource limit exceeded |
| BLK1301 | Compatibility | Third-party packages are not supported |
| BLK2001 | Security | Blocked by the safety policy |
| BLK2002 | Security | Runtime disabled by configuration |
| BLK2003 | Security | Network access blocked |
| BLK2101 | Security | Custom language definition blocked |
| BLK2102 | Security | Invalid custom language definition |
| BLK3001 | Data | Invalid or untrusted data |
| BLK3002 | Data | Script is too large |
| BLK3003 | Data | Invalid state payload |
| BLK3004 | Data | Output limit exceeded |
| BLK3005 | Data | Request body too large |
| BLK3101 | Native Block | Block expression error |
| BLK4001 | Runtime | Execution timed out |
| BLK4002 | Runtime | Required runtime not found |
| BLK4003 | Runtime | Host runtime failed |
| BLK4004 | Runtime | Compilation failed |
| BLK9001 | Internal | Unexpected internal failure |

## CLI and paths

### BLK0001 — Missing or invalid command arguments

**When it appears:** A command is missing a required path or option, or its
arguments have the wrong shape.

**What it means:** Block cannot safely infer what the user intended to run.
It refuses to guess because guessing a path or action can execute the wrong
document.

**How to fix:** Run `block help`, provide the required argument, and quote a
path containing spaces. For example: `block run "C:\Projects\My App\main.blk"`.

### BLK0002 — Invalid command input

**When it appears:** An argument is present but has an invalid value, such as
an unsupported option, malformed port, or unknown diagnostic code.

**How to fix:** Check spelling and value format, then run `block help` or
`block errors` for the relevant command. Do not paste untrusted command text
into a shell without reviewing it.

### BLK1001 — File not found

**When it appears:** The requested `.blk`, `.blkl`, or `.blkp` document cannot
be resolved in the current project or configured workspace roots.

**How to fix:** Run `block workspace show` and `block find <name>`. Check the
extension, quote paths containing spaces, or pass an absolute path. Block does
not scan the entire drive automatically.

### BLK1002 — Directory not found

**When it appears:** A project, workspace, import root, or sandbox directory
does not exist.

**How to fix:** Check the directory spelling and drive, then inspect
`block config show` or `block workspace show`. Create the directory only when
it is part of the intended project layout.

### BLK1005 — Ambiguous script path

**When it appears:** A name-based search found multiple safe candidate scripts
or projects.

**How to fix:** Use the project root, a more specific name, or an absolute
path. A deterministic path is safer than silently selecting one candidate.

## Syntax and compatibility

### BLK1003 — Nested language tag

**When it appears:** The structural parser finds a language opening tag inside
another open language block, for example `<python>` followed by `<js>` before
`</python>`.

**How to fix:** Close the outer block before opening the next top-level block,
or move the inner code into a separate document and import it through the
supported local import mechanism.

### BLK1004 — Unclosed language block

**When it appears:** The document ends while a language block is still open.

**How to fix:** Add the matching closing tag, such as `</python>`, after the
last line of the block. Run `block check <file>` or `block plan <file>` again.

### BLK1006 — Language unavailable in this edition

**When it appears:** The document asks a Lite or Standard edition to execute a
language that is not included in that edition.

**How to fix:** Choose a supported language or run the document with the
edition that provides the required runtime. `block capabilities` and
`block info` show the selected edition's support.

### BLK1007 — Language requires Block+

**When it appears:** A language or advanced toolchain is intentionally limited
to the Plus edition.

**How to fix:** Use `block-plus`, install the required toolchain from its
official source, or rewrite the stage using a runtime available in the current
edition.

### BLK1008 — Unknown language tag

**When it appears:** A language tag cannot be matched to a built-in runtime or
an allowed custom runtime definition.

**How to fix:** Check the tag spelling and use the canonical name listed by
`block capabilities`. Do not use arbitrary shell commands as language tags.

### BLK1101 — Syntax or tag error

**When it appears:** The execution parser rejects a language boundary because
it is mismatched, nested, unclosed, or otherwise malformed.

**How to fix:** Start with the reported `location` and `source` line. Match
every opening tag with its closing tag, keep blocks top-level, and run
`block check <file>` before executing again.

Example:

```text
<python>
print("hello")
</js>
```

The closing tag must be `</python>`.

## Imports and package compatibility

### BLK1201 — Import failed

**When it appears:** An explicit local import cannot be found, read, or
parsed.

**How to fix:** Check the `src` path, extension, file permissions, and project
boundary. Keep imports local and review the imported file before running it.

### BLK1202 — Invalid import graph

**When it appears:** Imports form a cycle, or the import chain exceeds the
maximum safe nesting depth.

**How to fix:** Draw the import graph, remove cycles, and split deeply nested
documents into a simpler directed structure. Avoid generated imports whose
target changes unexpectedly.

### BLK1203 — Import resource limit exceeded

**When it appears:** The combined count or byte size of imported files exceeds
the configured safety budget.

**How to fix:** Reduce the number and size of imports, remove duplicate
inclusions, and keep reusable code in focused local files. Do not disable a
limit merely to run untrusted source.

### BLK1301 — Third-party packages are not supported

**When it appears:** A removed or unsupported package directive such as
`<use package="..." />` is present.

**How to fix:** Replace it with a reviewed local `<import src="..." />`, or
vendor the code into the project after checking its license and contents.
Block does not execute a package manager as part of document execution.

## Security and policy

### BLK2001 — Blocked by the safety policy

**When it appears:** A path or operation would leave the configured project or
sandbox boundary, or a protected operation is requested.

**How to fix:** Inspect `block config show`, workspace roots, and project
entries. Move the needed input inside the approved boundary and retry. Never
work around this diagnostic by granting broad filesystem access to untrusted
source.

### BLK2002 — Runtime disabled by configuration

**When it appears:** A host runtime is known but its execution switch is off.

**How to fix:** Review the setting with `block config` and enable it only for
source you trust. If the runtime is not needed, remove that language stage.

### BLK2003 — Network access blocked

**When it appears:** The network guard prevents a host runtime or operation from
making a network-related call.

**How to fix:** Prefer a local input or an explicitly reviewed artifact. Only
change the policy in a controlled environment after documenting the endpoint,
data flow, and security impact.

### BLK2101 — Custom language definition blocked

**When it appears:** A `<define>` runtime is requested while custom definitions
are disabled by policy.

**How to fix:** Prefer a built-in runtime. If a custom runtime is necessary,
review its executable, arguments, working directory, and input policy, then
enable `AllowCustomDefinitions` deliberately for trusted local source.

### BLK2102 — Invalid custom language definition

**When it appears:** A custom runtime definition fails identifier, command,
extension, size, or shell-syntax validation.

**How to fix:** Use a short, explicit identifier and extension. Avoid shell
operators, redirection, command substitution, and unbounded arguments. Review
the custom-runtime policy and define only the minimum command needed.

## Data and native Block

### BLK3001 — Invalid or untrusted data

**When it appears:** A manifest, configuration value, serialized input, or
other data cannot be accepted safely.

**How to fix:** Inspect the referenced data, verify its encoding and shape,
keep it inside the project boundary, and retry with a known-good copy. Do not
silently ignore a malformed manifest.

### BLK3002 — Script is too large

**When it appears:** The source document exceeds Block's configured script-size
limit.

**How to fix:** Split the document into smaller local imports, remove
generated duplication, and keep generated input bounded. A size limit protects
startup time and memory; raising it should be a conscious local decision.

### BLK3003 — Invalid state payload

**When it appears:** A runtime returns state that is too large or is not valid
serializable JSON.

**How to fix:** Return only supported values such as strings, numbers,
booleans, lists, and maps. Remove open handles, functions, sockets, and other
process-local objects, and keep the payload below the documented limit.

### BLK3004 — Output limit exceeded

**When it appears:** Rendered or captured output exceeds the configured output
budget.

**How to fix:** Reduce output volume, paginate it, filter debug output, or
write a bounded artifact. Avoid printing unbounded streams from a loop.

### BLK3005 — Request body too large

**When it appears:** An API request body is larger than the configured maximum
before execution begins.

**How to fix:** Send a smaller document or split the request. Increase the
limit only in a controlled, trusted local environment and document the reason.

### BLK3101 — Block expression error

**When it appears:** The native Block stage cannot evaluate an expression,
statement, function call, variable, or collection access.

**How to fix:** Read the detailed title and source location. Common repairs
include defining a variable before use, correcting a function name or
argument count, checking a list/string index, and handling division by zero.
Run `block check <file>` after editing.

Examples:

```text
print(missing_name)  -> define missing_name before reading it
items[99]            -> verify the collection length and index
total / divisor      -> handle divisor == 0
```

## Runtime and build failures

### BLK4001 — Execution timed out

**When it appears:** A native operation or host process exceeds the configured
execution limit.

**How to fix:** Check for an infinite loop, blocked input, or a command waiting
for interactive input. Reduce the input, reproduce the smallest stage, and
review `block config show`.

### BLK4002 — Required runtime not found

**When it appears:** Block cannot find the executable needed by a language
stage.

**How to fix:** Run `block runtimes`, install the runtime from its official
source, confirm the command works directly, and open a new terminal after a
PATH change. Block reports missing runtimes; it does not silently execute a
package manager during a source run.

### BLK4003 — Host runtime failed

**When it appears:** The host process started but returned a non-zero exit
code, or a native tool reported a failed result.

**How to fix:** Read the runtime's detail and reproduce the smallest native
snippet directly. Verify its working directory, input files, exit status, and
language-specific diagnostics.

### BLK4004 — Compilation failed

**When it appears:** A compiler or build tool cannot produce a runnable
artifact.

**How to fix:** Read the compiler detail, confirm the toolchain version and
working directory, then fix the language-specific source error. Keep build
outputs inside the project boundary.

## Internal failures

### BLK9001 — Unexpected internal failure

**When it appears:** The failure is not covered by a more specific public code.

**How to fix:** Run `block doctor --full` and reproduce with the smallest safe
document. Maintainers may set `BLOCK_DEBUG=1` for one reproduction. Report
the exact code, engine version, edition, operating system, command, and
redacted detail; never include credentials or private source.

## Reporting checklist

Before opening an issue, collect:

1. The exact `BLKxxxx` code and engine version.
2. The edition (`Lite`, `Standard`, or `Plus`) and operating system.
3. The command and a minimal, non-sensitive reproduction.
4. The `operation`, `location`, `detail`, and `hint` fields.
5. The output of `block doctor --full` with private paths and secrets removed.

Use the [security policy](../SECURITY.md) for suspected vulnerabilities. Do
not publish credentials, private files, malware samples, or a bypass for a
safety guard in a normal issue.
