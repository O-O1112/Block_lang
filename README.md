# Block Language — Block Engine

[![Build and test](https://github.com/O-O1112/Block_lang/actions/workflows/ci.yml/badge.svg)](https://github.com/O-O1112/Block_lang/actions/workflows/ci.yml)
[![Website](https://github.com/O-O1112/Block_lang/actions/workflows/site-ci.yml/badge.svg?branch=gh-pages)](https://github.com/O-O1112/Block_lang/actions/workflows/site-ci.yml?query=branch%3Agh-pages)
[![Latest tag](https://img.shields.io/github/v/tag/O-O1112/Block_lang?sort=semver&label=latest%20tag)](https://github.com/O-O1112/Block_lang/tags)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**One file. Every runtime.** Block is a local-first polyglot programming language and execution engine for composing Python, JavaScript, Lua, PHP, SQLite, and more in one readable program with a shared state pipeline.

If you are looking for a polyglot programming language, a multi-language scripting workflow, a Python-to-JavaScript bridge, or a local-first automation engine built around readable `.blk` files, Block is designed for that use case.

**Start here:** [download the engine](https://o-o1112.github.io/Block_lang/downloads.html) · [read the documentation](https://o-o1112.github.io/Block_lang/wiki.html) · [run the examples](examples/) · [join the Community Lab](docs/COMMUNITY-LAB.md) · [read the book source](docs/book/) · [report an external result](docs/THIRD-PARTY-VALIDATION.md) · [browse the source on GitHub](https://github.com/O-O1112/Block_lang)

Maintainers can use the [organic growth playbook](docs/GROWTH.md) to turn demos, releases, and user feedback into a repeatable discovery-to-install funnel.

## Code signing policy

Free code signing provided by SignPath.io, certificate by SignPath Foundation.
The application is pending review; current Windows releases remain unsigned
until approval. Read the complete [Code signing policy](docs/CODE-SIGNING-POLICY.md)
before downloading or verifying a release.

## At a glance

| Item | Details |
| --- | --- |
| Current release | `2.2.6.5` |
| Primary platform | Windows 10/11 release workflow |
| Execution model | Parse one document, run native stages in order, transfer serializable state |
| Editions | Lite (`.blkl`), Standard (`.blk`), Plus (`.blkp`) |
| Host runtimes | Python, Node.js, PHP, Lua, Ruby, PowerShell, SQLite, and optional custom runtimes |
| Editor integrations | VS Code extension and Acode plugin |
| License | MIT |

## Table of contents

- [Why Block?](#why-block)
- [How Block works](#how-block-works)
- [Installation](#installation)
- [Getting started](#getting-started-in-1-minute)
- [Editions and file formats](#file-formats-and-editions)
- [CLI reference](#cli-reference)
- [Runtime blocks and syntax](#core-syntax-language-blocks)
- [Native Block control flow](#native-block-control-flow)
- [Shared state](#shared-state-the-core-power-of-block)
- [Imports](#importing-external-block-files)
- [HTML, JSON, and local servers](#html-and-json-output)
- [Editor extensions](#editor-extensions)
- [Security model and limits](#security-model)
- [Troubleshooting](#troubleshooting)
- [Build, test, and release](#repository-layout-and-release-verification)
- [Roadmap](#roadmap)
- [Community and discovery](#community-and-discovery)
- [Documentation and contribution](#documentation-contribution-and-license)
- [A longer first tour](#a-longer-first-tour)
- [Core concepts](#core-concepts)
- [A practical learning path](#a-practical-learning-path)
- [Complete CLI cookbook](#complete-cli-cookbook)
- [Syntax reference by concern](#syntax-reference-by-concern)
- [State design patterns](#state-design-patterns)
- [Project layout guide](#project-layout-guide)
- [Runtime and failure ownership](#runtime-and-failure-ownership)
- [Testing strategy](#testing-strategy)
- [Security operations guide](#security-operations-guide)
- [Release and supply-chain notes](#release-and-supply-chain-notes)
- [Migration notes](#migration-notes)
- [Frequently asked questions](#frequently-asked-questions)
- [Glossary](#glossary)
- [Support checklist](#support-checklist)
- [How to cite and share](#how-to-cite-and-share)
- [Maintainer quick reference](#maintainer-quick-reference)

## What Block is — and is not

Block is an orchestration language and execution engine. It gives one document a
clear structure while allowing each stage to remain in the language that is best
suited to the task. A Python stage can prepare data, a JavaScript stage can call a
Node package, a SQL stage can query local data, and an HTML or JSON stage can
present the result.

Block is not a replacement for Python, JavaScript, Lua, PHP, PowerShell, Rust, or
another mature language. It does not emulate their syntax or bundle every host
runtime. It coordinates local runtimes and makes the data boundary visible.

That distinction matters when diagnosing failures: a parser error belongs to
Block, a missing `python.exe` belongs to the host environment, and a package error
belongs to the runtime that owns the package.

## Why Block?

Block is for developers who already have useful code in more than one ecosystem and want a small, inspectable entry point instead of a collection of glue scripts. It does not replace Python, JavaScript, PowerShell, Rust, or any other mature language; it coordinates them.

<p align="center">
  <img src="demo.gif" alt="Block Engine Polyglot State Pipeline Demo" width="780"/>
</p>

Previously, when you wanted a Python snippet to feed directly into JavaScript, you usually had to:

* Create multiple files;
* Handle inputs and outputs across different languages;
* Manually serialize data;
* Stitch them together using shell scripts, temporary files, or extra import workflows.

Block simplifies this entire process into a single file:

```block
<py>
total = 40 + 2
print("Python produced:", total)
</py>

<js>
console.log("JavaScript received:", total)
</js>

<html>
<p>Total: {{total}}</p>
</html>
```

Each language block retains its native syntax; Block manages block separation, sequential execution, serializable state passing, and handing results off to subsequent blocks.

For a five-minute tour, start with [`examples/README.md`](examples/README.md). It contains copy-ready programs for a first polyglot pipeline, a local data workflow, and native Block control flow.

## How Block works

Every Block document follows the same high-level pipeline:

```text
source file
    |
    v
parse tags, imports, and native statements
    |
    v
validate edition, paths, limits, and runtime policy
    |
    v
run each language stage as a host process
    |
    v
capture output and serializable state
    |
    v
prepare the next stage and continue in source order
```

The important consequence is that a stage does not share the Python, Node.js, or
other runtime's memory directly. Block passes a prepared representation of the
state across the process boundary. This makes the workflow inspectable and
portable, but it also means that open handles, callbacks, sockets, and live
database connections cannot be passed to the next stage.

Use Block when you want:

- one readable entry point for a multi-language workflow;
- explicit data flow instead of hidden environment variables or temporary files;
- native access to the libraries already installed for each language;
- local-first automation that can be inspected before execution;
- a small document that can later be packaged, documented, or served locally.

---

## Installation

### Windows installer

The versioned installer is [`BlockSetup-v2.2.6.5.exe`](https://github.com/O-O1112/Block_lang/releases/download/v2.2.6.5/BlockSetup-v2.2.6.5.exe).
The stable download alias is [`BlockSetup.exe`](https://github.com/O-O1112/Block_lang/releases/download/v2.2.6.5/BlockSetup.exe).
The same files are also linked from the [official download page](https://o-o1112.github.io/Block_lang/downloads.html).

1. Run the installer.
2. Choose the installation directory.
3. Select Lite, Standard, or Plus. Standard is the recommended general-purpose
   edition.
4. Select the runtimes you want the installer to check.
5. Open a new PowerShell or Command Prompt window so the updated `PATH` is
   loaded.
6. Verify the installed engine:

   ```powershell
   block --version
   block --help
   ```

The secure installer downloads only the selected official GitHub Release asset,
checks its SHA-256 value against `SHA256SUMS.txt`, rejects unsafe ZIP paths, and
then installs the executable atomically. It never invokes Winget, Chocolatey,
PowerShell, or a downloaded script. Optional runtimes are detected only; install
them from their official sources and reopen the terminal afterward.

### Runtime prerequisites

Block delegates execution to the host tools. Install only the runtimes required by
your scripts and verify them directly before debugging a Block file:

```powershell
python --version
node --version
php --version
lua -v
ruby --version
```

The exact command may differ by distribution. Block cannot make a missing host
runtime available, and a runtime's own modules or packages remain managed by that
runtime.

### Build from source

The v2.2.6.5 Windows build uses the .NET Framework C# compiler available at
`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This produces `block.exe`, `block-lite.exe`, and `block-plus.exe` in `bin/` and
creates SHA-256 hashes unless `-SkipHash` is supplied. See the release section
below for the complete packaging and verification flow.

---

## Getting Started in 1 Minute

Create a file named `hello.blk`:

```block
<py>
name = "Block"
number = 6 * 7
print(f"Hello, {name}!")
print("Answer:", number)
</py>
```

Run it:

```powershell
block hello.blk
```

Block detects the `<py>` block and delegates execution to your local Python environment. Please note that the corresponding runtime must be pre-installed on your system.

---

## File Formats and Editions

| Edition | Extension | Best For |
| --- | --- | --- |
| Lite | `.blkl` | Lightweight, local polyglot scripts |
| Standard | `.blk` | General development, modules, and local projects |
| Plus | `.blkp` | Standard features plus extra runtimes, servers, formatting, linting, and documentation tools |

Executable commands typically correspond to:

```text
block-lite  example.blkl
block        example.blk
block-plus   example.blkp
```

Available languages and capabilities vary depending on your installed edition, operating system, and local runtimes.

### Choosing an edition

| Edition | Use it when | Advanced features |
| --- | --- | --- |
| Lite | You need the smallest local polyglot runner | Basic language blocks and local configuration |
| Standard | You want the recommended daily-use engine | Imports, project discovery, native control flow, and local server support |
| Plus | You need the broadest runtime and tooling surface | Custom runtime definitions, `fmt`, `check`, `doc`, and extended integrations |

The editions use the same project idea but have different compile-time feature
surfaces. A script written for Plus may use tags or commands that are not present
in Lite. When sharing an example, state its required edition and host runtimes.

The editor integrations also recognize the aliases `.block`, `.blocklite`, and
`.blockplus`, but the command-line edition extensions remain the clearest way to
communicate which engine is expected.

---

## CLI reference

The executable name depends on the edition. The following commands are available
in the v2.2.6.5 Windows build:

| Command | Lite | Standard | Plus | Purpose |
| --- |:---:|:---:|:---:| --- |
| `<engine> <file>` | ✓ | ✓ | ✓ | Execute a `.blkl`, `.blk`, or `.blkp` document |
| `<engine> --version` | ✓ | ✓ | ✓ | Print the engine and edition version |
| `<engine> --help` | ✓ | ✓ | ✓ | Show the edition-specific usage text |
| `<engine> config` | ✓ | ✓ | ✓ | Edit runtime, network, timeout, and sandbox settings |
| `<engine> config show` | ✓ | ✓ | ✓ | Print the active security and sandbox settings without editing them |
| `<engine> config path` | ✓ | ✓ | ✓ | Print the user configuration file path |
| `<engine> run <file>` | ✓ | ✓ | ✓ | Explicitly execute a document, including paths containing spaces |
| `<engine> check <file>` | ✓ | ✓ | ✓ | Parse a document without executing its stages |
| `<engine> ast <file>` | ✓ | ✓ | ✓ | Emit a stable JSON syntax tree and structured diagnostics without executing code |
| `<engine> info [file]` / `capabilities` | ✓ | ✓ | ✓ | Show engine settings and optionally inspect a document's blocks |
| `<engine> runtimes` | ✓ | ✓ | ✓ | Detect optional runtimes on the current `PATH` |
| `<engine> doctor` | ✓ | ✓ | ✓ | Run read-only environment and configuration diagnostics |
| `<engine> workspace show\|set\|clear` | ✓ | ✓ | ✓ | Configure and inspect a safe workspace root for script discovery |
| `<engine> find [name]` | ✓ | ✓ | ✓ | Find scripts in the current project and workspace without scanning the drive |
| `<engine> project root\|run [path]` | ✓ | ✓ | ✓ | Discover a project manifest or run its configured entry file |
| `<engine> project init\|list [path]` | — | ✓ | ✓ | Create or inspect a local Block project |
| `<engine> serve [port]` | — | ✓ | ✓ | Start a local HTTP server document |
| `block-plus fmt <file>` | — | — | ✓ | Format a Plus document and keep a `.bak` backup |
| `block-plus doc <file>` | — | — | ✓ | Generate a `.doc.md` block summary |

`run` is an explicit execution form; the original `<engine> <file>` form remains supported. Relative
script paths are resolved from the current directory, the nearest
`block.project.json`, and the configured workspace. The resolver never scans an
entire drive and reports ambiguous matches instead of choosing randomly.

For a project created with `block project init`, run its entry file from any
child directory:

```powershell
block project root
block project run
block run main.blk
```

Set a workspace once when you keep several Block projects together:

```powershell
block workspace set C:\Users\you\BlockProjects
block workspace show
block find hello
```

Paths containing spaces should be quoted:

```powershell
block-plus "C:\Projects\My Block\main.blkp"
```

Running an engine without a file displays its animated banner and usage text. In
automation, prefer an explicit file path and check the process exit code.

---

## Core Syntax: Language Blocks

### Basic Format

```block
<language>
Native code for the language
</language>
```

Python example:

```block
<py>
message = "hello"
print(message)
</py>
```

JavaScript example:

```block
<js>
const message = "hello from JavaScript";
console.log(message);
</js>
```

Opening and closing tags must match. Always use explicit closing tags (e.g., `</py>`, `</js>`) without omitting the slash.

### Common Language Tags

| Tag | Language |
| --- | --- |
| `<py>` | Python |
| `<js>` | JavaScript / Node.js |
| `<php>` | PHP |
| `<ruby>` or `<rb>` | Ruby |
| `<lua>` | Lua |
| `<ps>` | PowerShell |
| `<sql>` | SQL |
| `<html>` | HTML output |
| `<json>` | JSON output |
| `<c>`, `<cpp>` | C / C++ |
| `<go>` | Go |
| `<rust>` | Rust |
| `<ts>` | TypeScript |
| `<cs>` | C# |
| `<kotlin>` | Kotlin |
| `<dart>` | Dart |
| `<zig>` | Zig |
| `<perl>` | Perl |
| `<r>` | R |

Supported tags differ across editions. Plus supports custom runtime definitions, though the underlying runtime must be installed.

### Runtime ownership

| Block tag | Executed by | What you must provide |
| --- | --- | --- |
| `<py>` / `<python>` | Python | A compatible `python` or `python.exe` on `PATH` |
| `<js>` / `<javascript>` | Node.js | `node` on `PATH` |
| `<php>` | PHP | `php` on `PATH` |
| `<lua>` | Lua | A compatible Lua interpreter |
| `<ruby>` / `<rb>` | Ruby | Ruby on `PATH` |
| `<ps>` / `<powershell>` | PowerShell | A permitted PowerShell host; disabled by default in new configurations |
| `<sql>` | SQLite integration | SQLite support enabled by the selected edition |
| `<html>` | Block output renderer | No external language runtime; template values must be safe and serializable |
| `<json>` | Block output renderer | No external language runtime; the rendered document must be valid JSON |
| `<c>`, `<cpp>`, `<go>`, `<rust>`, `<zig>`, and others | Host compiler/tool | The corresponding compiler and project layout |

The table describes the execution ownership, not a promise that every tag is
enabled in every edition. A missing executable, incompatible version, or missing
language package is reported at the host-runtime boundary.

---

## Native Block control flow

In addition to native-language blocks, all editions can execute a small
Block-native language core. Compound statements end with a standalone `block`
line.
Indentation is encouraged for readability, but the terminator defines the scope.

```block
score = 80

if score >= 60:
    result = "pass"
else:
    result = "retry"
block

print(result)
```

The native language core includes:

- assignments and expressions;
- `if` / `elif` / `else` conditions;
- `while` loops;
- `for name in values:` loops;
- `break` and `continue` loop control;
- `range(start, end, step)`;
- `func name(args):` functions;
- function-local variables with read-only fallback lookup into shared state;
- list and dictionary literals;
- list, string, and dictionary indexing such as `items[0]` and `profile["name"]`;
- `.length` / `.count` members;
- `len`, `str`, `int`, `float`, `bool`, `type`, `contains`, `keys`, `values`,
  and `sum` built-ins;
- `return`, `print`, `pass`, and common arithmetic/comparison operators.

This core is intentionally deterministic and sandboxed: it does not expose
file, network, process, or package APIs. Use a runtime block when you need the
standard library, package ecosystem, concurrency model, or advanced syntax of
Python, JavaScript, Lua, PHP, or another host language.

---

## Shared State: The Core Power of Block

Block transfers serializable values between blocks. The most reliably supported types across languages are:

* Integers and floating-point numbers
* Strings
* Booleans
* Arrays / Lists
* Objects / Dictionaries

```block
<py>
user = {
    "name": "Ada",
    "score": 98,
    "tags": ["math", "logic"]
}
</py>

<js>
console.log(user.name);
console.log(user.tags.join(", "));
user.score = user.score + 1;
</js>

<json>
{
  "user": {{user}}
}
</json>
```

State-sharing operates on these principles:

1. The preceding executable block produces values.
2. Block serializes and stores these values into the active execution state.
3. The next block receives these values upon startup.
4. Any modifications made by the block propagate to downstream blocks.

Avoid placing open file handles, sockets, database connections, functions, or circular references into the shared state, as they cannot be serialized across process boundaries.

### State boundary checklist

Before adding a new value to a cross-runtime workflow, ask:

1. Is the value made only of plain data?
2. Does the next runtime have an equivalent representation?
3. Is the value small enough for the configured JSON and request limits?
4. Does it contain a secret that should stay inside one stage?
5. Will the receiving language validate the value before using it in a path,
   command, query, or template?

Prefer a small explicit result object over exporting every local variable. For
example:

```block
<py>
rows = [10, 20, 30]
result = {
    "count": len(rows),
    "total": sum(rows)
}
</py>

<js>
console.log(`count=${result.count}, total=${result.total}`)
</js>
```

State transfer is a copy-and-validate operation, not shared memory. Each runtime
keeps its own types, libraries, error behavior, and security model.

---

## HTML and JSON Output

### HTML Templating

Use `{{variable}}` inside `<html>` to inject state values:

```block
<py>
title = "Block Dashboard"
count = 42
</py>

<html>
<!doctype html>
<html lang="en">
  <body>
    <h1>{{title}}</h1>
    <p>Items: {{count}}</p>
  </body>
</html>
</html>
```

Standard variables are automatically HTML-escaped during insertion to prevent arbitrary markup injection.

### JSON Output

```block
<py>
status = "ok"
items = ["python", "javascript", "html"]
</py>

<json>
{
  "status": "{{status}}",
  "items": {{items}}
}
</json>
```

`<html>` produces HTML output, whereas `<json>` yields structured JSON. Ensure the final interpolated output represents valid JSON when injecting raw objects or arrays.

---

## Importing External Block Files

Standard and Plus editions support local module imports:

```block
<import src="modules/common.blk" />
```

`src` specifies a relative path from the current script. Imported files can contain arbitrary language blocks and are evaluated inline at the point of import.

Directory layout:

```text
my-project/
├─ main.blk
└─ modules/
   └─ common.blk
```

`modules/common.blk`:

```block
<py>
shared_message = "loaded from common module"
</py>
```

`main.blk`:

```block
<import src="modules/common.blk" />

<py>
print(shared_message)
</py>
```

Imports are governed by sandboxed directories, file count limits, file size caps, and recursion depth limits. Directory traversal via `../` outside the project root and circular imports are blocked.

---

## Block projects

Initialize a project:

```powershell
block project init . my-project
```

The generated `block.project.json` makes `main.blk` discoverable from the
project's child directories. You can inspect or run that entry without changing
the current directory:

```powershell
block project root
block project run
```

Directory structure created:

```text
my-project/
├─ block.project.json
└─ main.blk
```

Third-party package loading and the Block package marketplace have been removed.
To reuse reviewed local Block code, keep it inside the project and use an
explicit relative import such as `<import src="modules/common.blk" />`.

---

## Local Server

Standard and Plus editions can declare local HTTP servers using `<server>`:

```block
<server port="8080">
  <route path="/hello">
    <py>
    message = "hello from Block server"
    </py>
    <json>
    {
      "message": "{{message}}"
    }
    </json>
  </route>
</server>
```

Start the server:

```powershell
block server.blk
```

The server listens on:

```text
http://localhost:8080/
```

Route requests require an `X-Api-Token` header by default, printed to the console at startup. This built-in server is designed for local development and should not be exposed to public networks without additional security controls.

Serving static directories in Plus:

```block
<server port="8080">
  <static path="/assets" dir="public" />
</server>
```

---

## Custom Runtimes in Plus

Plus allows defining custom runtime tags:

```block
<define lang="deno" cmd="deno run" ext=".ts" />

<deno>
console.log("Hello from a custom runtime")
</deno>
```

Because this spawns external processes, it is subject to security policy checks. Enable it only when you trust the script contents, command definition, and host environment.

---

## Editor extensions

### VS Code

The repository publishes [`block-language-2.2.6.5.vsix`](https://github.com/O-O1112/Block_lang/releases/download/v2.2.6.5/block-language-2.2.6.5.vsix).
In VS Code, open **Extensions**, choose **Install from VSIX...**, select the
package, and reload the window if prompted.

The extension provides Block language recognition, syntax highlighting, snippets,
folding markers, state interpolation highlighting, and commands that invoke the
local Block executable. It does not bundle Python, Node.js, PHP, Lua, or any other
host runtime.

### Acode

The mobile editor package is
[`acode-plugin-block-2.2.6.5.zip`](https://github.com/O-O1112/Block_lang/releases/download/v2.2.6.5/acode-plugin-block-2.2.6.5.zip). Install it through
Acode's plugin workflow, then configure the local execution command if the device
or terminal environment uses a non-default path.

Both integrations inherit the same trust boundary as the command-line engine:
they can launch the local Block executable, which can launch host runtimes.

---

## Security Model

Block adopts a local-first, conservative security design:

* File imports are strictly restricted to designated sandbox directories.
* Circular imports and deeply nested import chains are rejected.
* APIs and local servers bind to `localhost` by default.
* API endpoints require `X-Api-Token` verification.
* An optional best-effort network guard blocks common runtime networking APIs.
* Each block enforces strict execution timeouts.
* Request concurrency, input sizes, and output payloads are capped by upper limits.
* Certificates (`.pfx`), passwords, and private keys should never be committed into Block projects.

Security configurations do not replace rigorous code review. Never execute untrusted `.blk`, `.blkl`, or `.blkp` files, as language blocks invoke host runtimes directly.

### Default configuration in v2.2.6.5

New configurations use conservative defaults:

| Setting | Default | Meaning |
| --- | --- | --- |
| Python / JavaScript / PHP / Ruby / Lua / SQLite | Enabled | These runtimes may be used when installed and supported by the edition |
| PowerShell | Disabled | Enable only when the script and host environment are trusted |
| Advisory network guard | On | Common runtime networking APIs are patched by default; this is not isolation |
| Custom `<define>` tags | Disabled | Arbitrary custom process definitions require explicit opt-in |
| Execution timeout | 15 seconds | Can be changed through `config`, within the engine's allowed range |
| Import depth | 16 levels | Prevents excessively nested or circular import chains |
| Imported files | 256 files / 32 MiB | Limits import fan-out and aggregate import size |
| Script size | 32 MiB | Rejects oversized source documents |
| Captured output | 1 MiB per stage | Excess output is truncated with a marker |

The process guard tracks child-process trees and terminates descendants after a
timeout where the host platform supports it. This is process-lifecycle control,
not a complete operating-system sandbox: a trusted host runtime can still access
the permissions available to the user running Block.

The advisory network guard has the same trust boundary. It reduces accidental
network access through common Python and Node.js APIs, but code running with the
user's permissions may bypass language-level patches. Use an OS sandbox,
container, virtual machine, or firewall policy for untrusted code.

Tagged releases also generate GitHub build-provenance attestations. After
installing GitHub CLI, a downloaded artifact can be checked with
`gh attestation verify <artifact> -R O-O1112/Block_lang`; continue to compare
the artifact against `SHA256SUMS.txt` as well.

### Security boundaries to review

- Treat every language block as executable native code.
- Review imported files before invoking them.
- Keep API servers on `localhost` unless you have added an appropriate external
  authentication and network policy.
- Do not commit passwords, API tokens, certificates, private keys, or personal
  data to a Block project.
- Treat `<define>` commands and PowerShell stages as high-risk capabilities.
- Validate values again after a runtime boundary; serialization does not make
  untrusted input safe.

For a vulnerability report, follow [`SECURITY.md`](SECURITY.md) and do not publish
an exploitable proof of concept in a public issue.

---

## Common Pitfalls

### 1. Mismatched Tags

Incorrect:

```block
<py>
print("hello")
</js>
```

Correct:

```block
<py>
print("hello")
</py>
```

### 2. Missing Host Runtimes

Block acts as an orchestrator and does not bundle language runtimes. Ensure Python is available in your shell for `<py>`, Node.js for `<js>`, and so forth.

### 3. Non-Serializable State

Pass primitive strings, numbers, booleans, arrays, or objects across blocks. Avoid passing open file pointers, live connections, or function handles.

### 4. Code Outside Blocks

Executable logic must reside inside explicit tags like `<py>...</py>` or `<js>...</js>`. Raw text outside tags will not be evaluated as executable code.

---

## Troubleshooting

Block errors now use stable diagnostic codes and show the operation, file,
source location, and a suggested next action when that information is
available:

```text
error[BLK1101]: Mismatched closing tag
  operation: check
  file     : C:\Projects\demo.blk
  location : 3:1
  source   : 3 | </js>
               | ^
  detail   : Expected </py>, but found </js>.
  hint     : Replace </js> with </py>.
```

Use the `BLKxxxx` code when searching the documentation or opening an issue.
Normal output intentionally hides internal stack traces. Maintainers can set
`BLOCK_DEBUG=1` before reproducing a failure to include the stack trace; remove
private paths and secrets before sharing it publicly.

| Symptom | First checks |
| --- | --- |
| `block` is not recognized | Open a new terminal, check the selected install directory, and run the executable by its full path |
| `Script file not found` | Run `block find <name>`; check the project root/workspace, quote paths containing spaces, and confirm the real extension is not hidden by Explorer |
| Optional runtime is missing | Install the runtime manually, verify its command on `PATH`, then reopen the terminal |
| A language block cannot start | Run the host runtime directly, confirm the selected edition supports the tag, and check both opening and closing tags |
| State is missing in the next stage | Return plain serializable values and check stage order; handles and functions cannot cross processes |
| Import is rejected | Confirm the file is inside the configured sandbox, the path is correct, and the import is not circular or too deep |
| A stage times out | Reduce the input, check for an infinite loop or blocked host command, and review the configured timeout |
| Output ends with `[output truncated]` | Reduce stage output or write a bounded artifact instead of printing a large stream |

For a support request, include the Block version and edition, Windows version,
host runtime versions, the smallest safe reproduction, and output with secrets
and private paths removed. Use the [support guide](SUPPORT.md) for the correct
issue type. Do not place an exploitable security report in a public issue.

---

## CLI Diagnostics and Block+ Tooling

All editions include read-only diagnostics:

```powershell
block doctor
block runtimes
block config show
block workspace show
block find hello-polyglot
block info examples\hello-polyglot.blk
block check examples\hello-polyglot.blk
block run examples\hello-polyglot.blk
```

`doctor` checks the current edition, Windows environment, optional runtime
locations, sandbox directory, timeout, and network policy. It does not run a
Block document or change configuration. `runtimes` only searches `PATH` and
reads executable metadata; it does not install or launch runtimes.

The Plus edition also includes document tooling:

```powershell
block-plus fmt main.blkp
block-plus check main.blkp
block-plus doc main.blkp
```

Command overview:

* `fmt`: Formats Block structures, retaining a `.bak` backup before rewriting.
* `check`: Parses the file and lists all identified blocks.
* `doc`: Generates structured documentation summarizing all blocks in the script.

---

## Full Example

```block
<import src="modules/config.blk" />

<py>
numbers = [1, 2, 3, 4, 5]
total = sum(numbers)
average = total / len(numbers)
</py>

<js>
const label = `total=${total}, average=${average}`;
console.log(label);
</js>

<html>
<!doctype html>
<html lang="en">
  <body>
    <main>
      <h1>Block result</h1>
      <p>Total: {{total}}</p>
      <p>Average: {{average}}</p>
    </main>
  </body>
</html>
</html>
```

This reflects the core philosophy of Block: write each task in the language best suited for it, while maintaining a single entry point, unified script, and transparent data flow.

---

## Repository Layout and Release Verification

The default branch contains the Block Engine, editor integrations, tests, and
maintainer documentation. The public website is maintained separately on the
[`gh-pages`](https://github.com/O-O1112/Block_lang/tree/gh-pages) branch so
website implementation files do not obscure the engine's language statistics.

For the maintainer build and release checks, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
powershell -ExecutionPolicy Bypass -File .\package-extensions.ps1
powershell -ExecutionPolicy Bypass -File .\verify-release.ps1
```

See [`docs/REPOSITORY_LAYOUT.md`](docs/REPOSITORY_LAYOUT.md) for the complete
directory map and the compatibility rules for published files.

### Test the local build

After building into `bin/`, run the Windows smoke suite:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\Test-BlockEngine.ps1 -EngineDirectory .\bin
```

The smoke suite checks all three engine versions, native control flow, Plus
syntax checking, malformed tag rejection, and the Python-to-Node state bridge
when both host runtimes are available.

### Package and verify a release

The complete v2.2.6.5 release flow builds the three engines, creates matching ZIP
bundles, packages the VS Code and Acode extensions, builds the installer, and
verifies the published artifacts and hashes:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1
```

The release directory should contain the versioned installer, stable installer
alias, three engine ZIPs, two extension packages, and `SHA256SUMS.txt`. To verify
an already prepared release without rebuilding it:

```powershell
powershell -ExecutionPolicy Bypass -File .\verify-release.ps1
```

Do not replace a versioned artifact with a hand-built file after verification.
Re-run the verification step whenever an artifact, manifest, installer, or
published checksum changes.

---

## v2.2.6.5 status and known boundaries

Version 2.2.6.5 is the current documented release line. It includes the Lite,
Standard, and Plus engines, the Windows installer, the VS Code extension, the
Acode plugin, native control flow, cross-runtime state synchronization, local
local imports, project discovery, the Plus
formatting/check/documentation commands, safe
workspace/project discovery, deterministic logical short-circuit parsing, and
explicit range limits.

The following are deliberate boundaries or known limitations rather than hidden
promises:

- Block still requires the host runtimes used by a document.
- State must remain serializable; live handles cannot cross process boundaries.
- Circular imports are rejected for safety.
- Very large state objects can create memory pressure.
- Custom runtime definitions may need extra care around Windows symlinks.
- GUI behavior, compiler availability, and runtime command names depend on the
  host operating system and installed toolchain.
- Process timeouts and import limits reduce accidental resource abuse but do not
  turn arbitrary native code into a security sandbox.

See the [changelog](CHANGELOG.md) and [v2.2.6.5 release notes](docs/RELEASE-2.2.6.5.md)
for the tested changes and release artifact contract. Planned behavior should not
be read as shipped behavior.

---

## Roadmap

The [public roadmap](ROADMAP.md) separates the v2.2.6.5 foundation, proposed next
steps, and longer-term ideas. If you want to help Block grow, a reproducible
example, documentation fix, regression test, or real workflow is more useful than
an unverified benchmark.

If Block saves you from maintaining a fragile chain of glue scripts, consider
starring the repository, sharing the example that helped you, or opening a focused
issue with the version and runtime details. Those signals help prioritize work
without pretending that every use case is already supported.

If you want to help validate the current release, start with the [external tester packet](docs/TESTER-PACKET.md). Maintainers should keep external validation separate from their own smoke tests; the [validation guide](docs/THIRD-PARTY-VALIDATION.md) explains how to record independent evidence.

---

## Community and discovery

Block grows through reproducible examples and honest technical feedback. If you
are evaluating the project, the best first steps are:

- [Run a copy-ready example](examples/README.md) and share what worked.
- Ask a usage question in [GitHub Discussions](https://github.com/O-O1112/Block_lang/discussions).
- Report a focused bug through the [issue templates](https://github.com/O-O1112/Block_lang/issues/new/choose).
- Submit an independent workflow, tutorial, or compatibility result using the
  [validation guide](docs/THIRD-PARTY-VALIDATION.md).

When sharing Block, link to the specific example, release, or documentation
page that supports the claim. This keeps discovery useful for new users and
makes project growth easy to verify.

---

## Documentation, contribution, and license

- [Documentation index](docs/README.md)
- [Markdown Wiki](docs/wiki/README.md)
- [Book source](docs/book/README.md)
- [AI knowledge pack](docs/ai/README.md)
- [Contributing guide](CONTRIBUTING.md)
- [Support guide](SUPPORT.md)
- [Governance](GOVERNANCE.md)
- [Security policy](SECURITY.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Citation metadata](CITATION.cff)
- [MIT License](LICENSE)
- [v2.2.6.5 release manifest](docs/RELEASE-2.2.6.5.md)

The visual documentation site is available from the [Block documentation site](https://o-o1112.github.io/Block_lang/wiki.html). The
Markdown Wiki is the reviewable source for the same installation, syntax,
architecture, and troubleshooting topics.

---

## A Note for Newcomers

**Leave the language to the language; let Block handle the flow.**

There is no need to abandon familiar tools to bridge multi-language workflows. Place each task into its respective block and let your data flow forward naturally.

---

## A Longer First Tour

The shortest description of Block is “a readable document that coordinates
several language runtimes.” The longer description is more useful when you are
deciding whether Block belongs in a real project.

A Block document has five important properties:

1. It is still a normal text file that can be reviewed, diffed, copied, and
   stored in Git.
2. Its language sections are explicit. A reader can see whether a piece of
   code belongs to Python, JavaScript, Lua, PHP, or the Block-native core.
3. Values can move from one stage to the next through a serializable state
   boundary.
4. The engine owns the order, diagnostics, import policy, and local process
   lifecycle.
5. The host operating system still owns the native runtimes and their
   permissions.

Consider a small data workflow:

```block
<py>
numbers = [4, 8, 15, 16, 23, 42]
summary = {
    "count": len(numbers),
    "total": sum(numbers)
}
</py>

<js>
summary.average = summary.total / summary.count;
console.log(`average=${summary.average}`);
</js>

<json>
{
  "ok": true,
  "summary": {{summary}}
}
</json>
```

The Python stage creates plain data. Block serializes that data before starting
the JavaScript stage. JavaScript adds another field. The JSON stage renders the
final state. Nothing in this example requires the Python and JavaScript
processes to share memory, and neither stage needs to know how the other stage
was implemented.

That boundary is the central design decision. It is also the reason a Block
workflow is not simply “a shell script with nicer tags.” Shell scripts often
accumulate untyped strings, implicit working directories, and hidden process
side effects. Block makes the stage boundary visible and can validate the
document before any runtime is launched.

### What happens when a document runs

At a high level, the engine performs the following sequence:

1. Resolve the command, document path, project root, and workspace rules.
2. Read the document using the selected edition's parser.
3. Match opening and closing tags and validate the structural grammar.
4. Resolve imports inside the permitted project boundary.
5. Build the execution plan and identify the required host runtimes.
6. Create the initial state and execute stages in document order.
7. Serialize the values that cross each runtime boundary.
8. Render HTML or JSON output when an output block is present.
9. Return a useful exit code and a diagnostic if a stage fails.

The exact implementation may evolve, but these responsibilities are the mental
model to use when reading a failure. A problem in step 3 is a Block syntax
problem. A problem in step 5 may be a missing runtime. A problem in step 6 may
come from the native language or from the host language. A problem in step 8
is usually an output validity or escaping issue.

### What Block does not hide

Block intentionally does not claim to make native code safe merely by placing
it in a document. A Python block can still use the capabilities allowed to the
Python process. A JavaScript block can still load Node.js modules that are
available to it. A local server can still expose an HTTP listener. Timeouts,
import restrictions, and configuration controls reduce accidental abuse, but
they are not a complete security sandbox.

For untrusted code, use a separately isolated environment with operating-system
or container controls. Do not ask a downloaded Block file to be your security
boundary.

---

## Core Concepts

The following vocabulary is used throughout the documentation. Keeping these
terms separate prevents many design and troubleshooting mistakes.

### Document

A document is the text file passed to an engine. Depending on the edition and
the project, the usual extensions are:

| Extension | Intended role | Typical engine |
| --- | --- | --- |
| `.blkl` | Lightweight Block document | Block Lite |
| `.blk` | Standard Block document | Block |
| `.blkp` | Plus document with extended tooling | Block+ |

The extension helps people and tools understand the intended edition. It is
not a replacement for checking the actual contents or engine capabilities.

### Stage

A stage is one executable or output section in a document. A stage is selected
by its tag, such as `<py>`, `<js>`, `<lua>`, `<html>`, or `<json>`. Stages run in
document order unless a native control-flow construct changes that order.

A stage should have a clear responsibility. Good stages commonly:

- load or normalize input data;
- calculate one meaningful result;
- call a host-language library;
- validate a boundary payload;
- render a final document or response.

Huge stages are legal in many host languages, but they make state ownership,
diagnostics, and review harder. Splitting by responsibility is usually more
valuable than splitting by line count.

### Host runtime

The host runtime is the executable that interprets or compiles a stage. Block
does not bundle every host runtime. For example, a `<py>` block normally needs
`python` or `python.exe` on `PATH`, while a `<js>` block needs `node`.

The runtime's own version, package installation, current environment, and
permissions can affect behavior. Record those details when reporting a bug.

### State boundary

The state boundary is the serialized data passed from one stage to another. It
is deliberately narrower than shared memory. The most portable values are
numbers, strings, booleans, arrays, lists, objects, and dictionaries made from
those values.

State is data, not a live object graph. File handles, sockets, callbacks,
database connections, class instances, circular references, and process
objects should stay inside one stage.

### Edition

An edition is a distribution of the engine with a defined capability set:

- **Lite** is intended for small, lightweight documents.
- **Standard** is the normal local multi-runtime workflow.
- **Plus** adds extended project and developer tooling and supports custom
  runtime definitions where the host tools are installed.

Use the edition that matches the workflow rather than assuming that every tag
is available everywhere. The edition is part of the compatibility contract.

### Project root and workspace

The project root is the directory controlled by a `block.project.json` file.
The workspace is an optional, explicitly configured parent location for finding
Block projects. These concepts make script discovery predictable without
scanning an entire drive.

When a command appears to “lose” a file after you change directories, configure
the workspace or use a project manifest instead of copying files into an
arbitrary folder.

### Artifact

An artifact is a file produced for distribution or installation: an executable,
VS Code extension, Acode plugin archive, checksum file, or release manifest.
An artifact is not automatically trustworthy because it is hosted on a public
repository. Verify its source, checksum, release context, and signature status.

---

## A Practical Learning Path

New users often try to learn the syntax, install every runtime, and build a
large workflow all at once. The following sequence gives smaller checkpoints.

### Step 0: Confirm what you installed

Open a new PowerShell window after installation and run:

```powershell
block --version
block --help
block doctor
```

If you installed another edition, replace `block` with the executable name
shown by that package. The new terminal matters because an installer or a
runtime manager may have changed `PATH` for future processes only.

### Step 1: Run one native example

Create `hello.blk`:

```block
name = "Block"
print("Hello, " + name)
```

Check it before running it:

```powershell
block check hello.blk
block run hello.blk
```

This isolates the engine's native parser and evaluator from Python, Node.js,
and other optional runtimes.

### Step 2: Run one host-language stage

Create `python-hello.blk`:

```block
<py>
message = "hello from Python"
print(message)
</py>
```

Then compare the document check with the execution:

```powershell
block check python-hello.blk
block runtimes
block run python-hello.blk
```

If `check` passes and `run` reports a missing executable, the document is
probably valid and the host environment needs attention.

### Step 3: Add a second stage through state

Use a small explicit payload rather than exporting every temporary variable:

```block
<py>
source = [3, 5, 8]
result = {
    "count": len(source),
    "total": sum(source)
}
</py>

<js>
result.average = result.total / result.count;
console.log(JSON.stringify(result));
</js>
```

The `result` object is the contract. If the JavaScript stage later needs a new
field, update the contract and its validation instead of relying on accidental
variables from the Python implementation.

### Step 4: Add output last

Once the state is correct, add `<json>` or `<html>`. This keeps data bugs
separate from rendering bugs. For HTML, remember that interpolated values are
escaped by the renderer, but a string that is later inserted by native
JavaScript is still the responsibility of that JavaScript code.

### Step 5: Move repeated code into a local import

When a document becomes repetitive, create a project and import a reviewed
local module:

```powershell
block project init . sample-project
cd sample-project
block project root
```

Use an explicit import such as:

```block
<import src="modules/common.blk" />
```

Keep import paths short, relative, and easy to review. Avoid building import
paths from uncontrolled input.

### Step 6: Make the workflow reproducible

Before sharing a project, record:

- the Block edition and version;
- the operating system;
- the host runtime names and versions;
- the entry document;
- the expected output;
- the command used to check and run it;
- any required environment variables, without committing secrets.

The resulting README or issue is far easier for another person to reproduce.

---

## Complete CLI Cookbook

The commands below use PowerShell syntax and assume that the engine is on
`PATH`. Quote a path whenever a directory or filename contains spaces.

### Discover the installed engine

```powershell
block --version
block --help
block info
block capabilities
```

`--version` is useful in bug reports. `--help` describes the selected build.
`info` and `capabilities` help you confirm what an edition exposes before
writing a document around it.

### Inspect runtime availability

```powershell
block runtimes
block doctor
block config show
block config path
```

These commands are intended for diagnostics. Detection is not installation:
the presence of a runtime on `PATH` does not mean its packages, compiler
toolchain, or project dependencies are ready.

### Validate without executing

```powershell
block check .\examples\hello-polyglot.blk
block ast .\examples\hello-polyglot.blk
```

Use `check` when you only need a pass/fail structural validation. Use `ast` when
you need to inspect the parsed structure or capture structured diagnostics for
tooling. Neither command should be treated as proof that native code is safe
or that every runtime dependency is installed.

### Run a document explicitly

```powershell
block run .\examples\hello-polyglot.blk
block ".\projects\My Block\main.blk"
```

The explicit `run` form is usually easiest to read in scripts. The direct file
form remains supported for interactive use.

### Use project discovery

```powershell
block project init . my-project
block project root
block project list
block project run
```

The project manifest makes the entry document discoverable from child
directories. This is preferable to asking every user to edit a command path.

### Configure a workspace

```powershell
block workspace set "C:\Users\you\BlockProjects"
block workspace show
block workspace clear
block find hello
block find hello-polyglot
```

The resolver checks the current directory, project metadata, and configured
workspace according to its documented order. It does not search the entire
drive, and it reports ambiguous matches instead of guessing.

### Use Standard and Plus server commands

```powershell
block serve .\server.blk
block serve .\server.blk 8080
```

The built-in server is for local development. Keep it bound to a controlled
local environment and retain the API-token requirement. Do not expose it to a
public interface without adding an appropriate reverse proxy, authentication,
rate limiting, logging, and network policy.

### Use Plus tooling

```powershell
block-plus fmt .\main.blkp
block-plus check .\main.blkp
block-plus doc .\main.blkp
```

The formatter keeps a `.bak` backup according to the current Plus behavior.
Review the diff after formatting. The documentation command produces a
`.doc.md` summary; it does not replace a human-written project guide.

### Capture diagnostics in automation

```powershell
block check .\main.blk
if ($LASTEXITCODE -ne 0) {
    throw "Block validation failed"
}

block run .\main.blk
if ($LASTEXITCODE -ne 0) {
    throw "Block execution failed"
}
```

Always check `$LASTEXITCODE` in CI or release scripts. A console line that
looks successful is not a substitute for a process exit code.

---

## Syntax Reference by Concern

This section is organized around questions that arise while writing a document.
Use the formal examples earlier in the README and the examples directory as
the compatibility source for a specific release.

### How do tags work?

An opening tag identifies the stage language. A closing tag must use the same
language name and a slash:

```block
<py>
value = 1
</py>
```

Do not omit the closing slash, change the tag spelling, or nest a different
language inside an unclosed stage. If a host language contains markup-like
text, keep it inside a properly delimited stage or a string literal supported
by that host language.

### How do comments work?

Use the comment syntax accepted by the language that owns the stage. For
example, Python uses `#`, JavaScript commonly uses `//` or `/* ... */`, and
PowerShell uses `#`. Native Block comments and future syntax should be checked
against the edition's parser rather than inferred from another language.

```block
<py>
# This comment belongs to Python.
answer = 42
</py>

<js>
// This comment belongs to JavaScript.
console.log(answer);
</js>
```

### How do native assignments work?

Native assignments create or update values in the active Block state:

```block
name = "Ada"
score = 98
tags = ["math", "logic"]
profile = {"name": name, "score": score}
```

Prefer descriptive names and stable shapes. A downstream stage is easier to
maintain when it receives `profile.name` and `profile.score` than when it must
guess which temporary variable happens to be present.

### How do conditions work?

Native compound statements close with a standalone `block` line:

```block
score = 80

if score >= 60:
    result = "pass"
else:
    result = "retry"
block
```

Indentation improves readability, but the terminator is what defines the
compound statement. Keep branches explicit and avoid changing a value in many
branches unless the resulting state is obvious.

### How do loops work?

The native core supports `while`, `for name in values:`, `break`, `continue`,
and `range(start, end, step)`:

```block
total = 0
for value in range(1, 4, 1):
    total = total + value
block
print(total)
```

Every loop needs a clear termination condition. The execution limit is a
protective boundary, not a loop-control strategy. If a test times out, inspect
the loop first instead of simply increasing the limit.

### How do functions work?

The native core supports functions with `func`, parameters, local variables,
and `return`:

```block
func label(value):
    result = "value=" + str(value)
    return result
block

print(label(7))
```

Function-local variables should be treated as local implementation details.
Use the return value to make the function's output explicit. The native core's
read-only fallback lookup into shared state is convenient, but explicit
parameters are clearer for reusable logic.

### Which values can be indexed?

Lists, strings, and dictionaries can be indexed using the supported forms:

```block
items = ["first", "second"]
profile = {"name": "Ada"}

print(items[0])
print(profile["name"])
```

Use `.length` or `.count` where supported, or use `len` for a value whose type
may vary. Validate indexes and keys when input is not fully controlled.

### What built-ins exist in the native core?

The current documented core includes `len`, `str`, `int`, `float`, `bool`,
`type`, `contains`, `keys`, `values`, and `sum`, as well as `print`, `pass`, and
`return`. Built-ins are intentionally small. The native core does not expose
file, network, process, or package APIs.

When a workflow needs a host library or an operating-system capability, place
that work in an appropriate runtime stage and document the trust boundary.

### How does HTML interpolation work?

Use `{{variable}}` in an `<html>` stage:

```block
<py>
title = "A report"
</py>

<html>
<h1>{{title}}</h1>
</html>
```

The renderer escapes standard interpolated values. Treat this as a useful
default, not permission to concatenate untrusted HTML in JavaScript or to
disable output validation.

### How does JSON interpolation work?

Raw object and array values must result in valid JSON after interpolation:

```block
<py>
items = ["one", "two"]
</py>

<json>
{
  "items": {{items}}
}
</json>
```

If you interpolate a string inside JSON quotes, make sure the renderer's
escaping rules produce valid JSON for quotes, newlines, and backslashes. For
complex payloads, prefer passing a structured object rather than manually
assembling JSON text.

### How do imports work?

Use a relative import:

```block
<import src="modules/common.blk" />
```

Imports are evaluated inline at the import location. They are not a remote
package manager, and the current release does not provide the removed
third-party package marketplace. Keep reusable code under the project root,
review it as source, and test it as part of the project.

---

## State Design Patterns

Cross-language state is powerful when it is small and intentional. The
following patterns make a workflow easier to debug and safer to evolve.

### Producer, contract, consumer

Treat a stage as a producer and the next stage as a consumer of a named
payload:

```block
<py>
records = [10, 20, 30]
report = {
    "count": len(records),
    "total": sum(records),
    "version": 1
}
</py>

<js>
if (report.version !== 1) {
  throw new Error("Unsupported report version");
}
console.log(report.total);
</js>
```

The `version` field is inexpensive and makes future changes visible. It is
especially useful when a project is shared between people who update stages at
different times.

### Normalize once

Normalize input at the first trusted boundary:

```block
<py>
raw_name = "  Ada  "
user = {
    "name": raw_name.strip(),
    "name_lower": raw_name.strip().lower()
}
</py>
```

Downstream stages can then rely on a documented shape. Do not make every stage
guess whether a field is trimmed, optional, numeric, or a string.

### Keep secrets out of shared state

Shared state is designed for workflow data, not secret storage. Keep tokens,
private keys, and passwords inside the smallest stage that needs them. Do not
print them, put them in output objects, or copy them into an HTML template.

If a failure report contains state, redact credentials before sharing it.

### Return an envelope

For workflows with multiple possible outcomes, use a small result envelope:

```block
<py>
result = {
    "ok": True,
    "code": "READY",
    "data": {"count": 3},
    "errors": []
}
</py>

<json>
{{result}}
</json>
```

An envelope gives later stages a stable place for status and diagnostics. Do
not use a successful-looking payload to hide an exception; fail the stage when
the operation did not complete as promised.

### Keep live resources local

Open files, sockets, database connections, subprocess handles, and library
objects should be created and closed inside one runtime stage. Export the
resulting data, not the resource itself.

### Bound large payloads

Serialization copies data and consumes memory. For large inputs, reduce the
payload to the fields the next stage actually needs. If the workflow truly
needs streaming or a database, document that capability and keep the resource
lifecycle within the owning stage.

### Validate before sensitive use

Before using state in a path, command, SQL query, URL, or template, validate
its type, length, allowed characters, and intended meaning. A value being
serializable does not make it safe for every sink.

---

## Project Layout Guide

A small project can start with one file. Once it grows, a predictable layout
helps reviewers and tools understand what belongs in the release.

```text
my-block-project/
├─ block.project.json
├─ main.blk
├─ modules/
│  ├─ normalize.blk
│  └─ render.blk
├─ examples/
│  └─ quickstart.blk
├─ tests/
│  ├─ valid-input.blk
│  └─ invalid-input.blk
├─ docs/
│  └─ workflow.md
└─ README.md
```

### `main.blk`

Use `main.blk` as the documented entry point. Keep its stage order easy to
follow and avoid hidden dependence on the caller's current directory.

### `modules/`

Keep imported code in a clearly named directory. Prefer a module per cohesive
responsibility. An import should be easy to review without opening unrelated
files.

### `examples/`

Examples should be copy-ready and small. Each example should say which edition
and host runtimes it needs. A good example contains the expected output and a
check command.

### `tests/`

Keep positive and negative cases separate. A negative case should explain what
must fail and, when relevant, which actionable diagnostic is expected.

### `block.project.json`

Treat the project manifest as part of the execution contract. Review changes
to its entry path and root settings as carefully as changes to source code.

### Naming and path rules

Use stable ASCII paths for automation when possible. Quoted paths with spaces
are supported, but scripts are easier to move when they do not depend on a
user-specific directory. Never assume that a project lives in
`C:\Users\<name>` on another machine.

---

## Runtime and Failure Ownership

When a workflow fails, identify the first owner before changing code. This
prevents random edits to the wrong layer.

| Symptom | Likely owner | First check |
| --- | --- | --- |
| Opening and closing tags do not match | Block parser | `block check file.blk` |
| A supported command is unknown | Installed edition/build | `block --version`, `block --help` |
| `python` or `node` cannot be found | Host environment | `block runtimes`, `Get-Command python,node` |
| Native stage loops forever | Document logic | Inspect loop condition and `range` bounds |
| State cannot be serialized | Stage contract | Remove handles, functions, cycles, and oversized values |
| Imported file is rejected | Project/import policy | Check root, relative path, size, and recursion |
| HTML looks wrong | Output stage | Inspect escaped values and generated markup |
| JSON is invalid | Output payload | Check interpolation and raw object structure |
| Local server cannot start | Port/host/runtime config | Check port, token, and process ownership |
| A native runtime returns a package error | Host language/toolchain | Run the smallest host-language reproduction |
| A release asset is blocked by security software | Trust/reputation/signing | Verify checksum and signature status |

### Parse failures

Start with `check`, reduce the document to the smallest failing section, and
confirm tag spelling. Do not start by installing more runtimes: parsing happens
before most runtime execution.

### Runtime resolution failures

Use `runtimes` and `doctor`, then run the host executable directly. A runtime
can be installed but unavailable to the process because a new `PATH` has not
reached the current terminal. Open a new terminal before retesting.

### Stage failures

Run the host-language code independently when possible. Confirm the stage's
input shape and log only non-sensitive, structured facts such as a count or
field name. Avoid dumping the full state by default.

### Timeout failures

A timeout can indicate an infinite loop, a blocked child process, a slow native
package, or an overly small test limit. Reproduce with the smallest document,
inspect the first stage that stops responding, and record the configured limit.
Increasing a timeout without understanding the cause can hide a regression.

### Import failures

Check the import path from the importing file, not from the current shell
directory. Confirm the file is inside the permitted project root and that the
import graph is not circular. Avoid changing the root to make an unknown file
load without reviewing the security effect.

---

## Testing Strategy

Block has several layers of behavior, so one successful example is not a full
test suite. A practical project tests the following layers.

### Parser and syntax tests

Cover valid tags, mismatched tags, empty stages where legal, malformed
attributes, native compound terminators, and invalid expressions. These tests
should run without launching optional host runtimes.

### Native evaluator tests

Cover assignments, conditions, loops, functions, indexing, built-ins,
short-circuit behavior, return values, and explicit error cases. Include empty
lists, zero values, missing keys, and boundary ranges.

### State boundary tests

Cover strings, numbers, booleans, arrays, nested objects, empty values, and
rejection of functions, circular references, live handles, and oversized
payloads. Test both producer and consumer expectations.

### Host-runtime integration tests

Run the smallest useful Python, Node.js, Lua, PHP, or other supported stage
when that runtime is available. Keep host dependencies explicit so a missing
optional runtime produces a controlled skip or documented failure rather than
a misleading pass.

### Import and project tests

Test relative imports, nested imports, project discovery from child
directories, path-with-spaces cases, missing files, traversal attempts, and
circular imports.

### Server tests

Test a local route, invalid method or path behavior, missing token behavior,
valid token behavior, JSON validity, static path boundaries, and clean process
shutdown. Never use a public production endpoint as the only server test.

### Diagnostic tests

Assert that failures identify the operation, file, stage or command, and a
next action when one is available. Avoid brittle tests that require exact
punctuation unless the diagnostic is a documented interface.

### Release tests

For each release candidate:

1. Build each intended engine edition.
2. Run the engine smoke suite.
3. Run CLI, native-language, import, and package/extension tests that apply.
4. Verify version consistency across source, manifests, docs, and artifacts.
5. Verify checksums from a clean staging directory.
6. Inspect the release asset list and filenames.
7. Record any missing optional runtime as a clear, non-fatal condition only
   when the core installation actually completed.

### Third-party validation

Keep independent tester results separate from maintainer smoke tests. Record
the tester's operating system, download source, release version, commands,
expected output, actual output, and whether the artifact was signed at the
time of testing.

---

## Security Operations Guide

Block is a local execution engine. The security posture therefore depends on
both the engine and the host operating system.

### Trust model

The safe default assumption is:

- source files may be changed before you receive them;
- release artifacts may be unsigned or have limited reputation;
- native runtimes can access more than the Block parser can see;
- a local server is not automatically a public-service security boundary;
- a third-party imported file is code, not merely configuration.

Review the source and release metadata before running a file you did not write.

### Download verification

For a release asset, keep these facts together:

1. the repository and release URL;
2. the exact filename;
3. the SHA-256 checksum from the release record;
4. the expected version and edition;
5. the Authenticode signature state, if applicable;
6. the source commit or release tag;
7. the result of your local security scan.

Example checksum verification on Windows:

```powershell
Get-FileHash .\BlockSetup-v2.2.6.5.exe -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

A matching hash proves that the bytes match the published checksum. It does
not prove who created the checksum. Use the release context and repository
provenance as well.

### Current signing status

The current release documentation records a pending application for free
SignPath Foundation code signing. Until a signed artifact is actually issued
and published, treat the Windows installer and executable artifacts as
unsigned. Do not describe the application as a completed certificate.

If Windows Defender, Chrome, or another product reports a Trojan or blocks a
download, do not tell users to disable protection as a normal installation
step. Preserve the exact filename, detection name, hash, source URL, and
timestamp. Submit the artifact to the security vendor's official review path
when appropriate, and publish a transparent status update.

### Native runtime permissions

The engine's parser does not remove the capabilities of Python, Node.js, Lua,
PHP, PowerShell, or a compiler. Use least privilege at the operating-system
level. Avoid running unknown documents from an administrator terminal.

### Local server controls

The local server should remain local during development. Keep the token
requirement enabled, avoid binding to all interfaces without a reason, and do
not place secrets in URLs or logs. If the server must be deployed, add an
independent authentication and network layer.

### Reporting a security issue

Do not publish credentials, private keys, exploit details, or a weaponized
sample in a public issue. Follow the repository's [security policy](SECURITY.md)
and include a minimal non-sensitive reproduction.

---

## Release and Supply-Chain Notes

A release is more than a version string. Users need a coherent set of source,
documentation, artifacts, and verification information.

### Version alignment

Before publishing, compare:

- engine `--version` output;
- installer display version;
- VS Code extension version and package metadata;
- Acode plugin metadata;
- release tag and title;
- changelog and release manifest;
- documentation examples and known-boundary notes.

If one artifact deliberately uses a different internal packaging version, say
so in the release notes. Do not silently make users infer the mapping.

### Artifact naming

Stable filenames are part of the user interface. Keep existing release names
unless a breaking change is intentional and documented. Update download links,
checksums, website buttons, and installation instructions together.

### Release manifest

The release manifest should state what was built, what was tested, what was not
available on the build host, and what trust metadata exists. “Optional runtime
not installed” is different from “core engine failed to install.” Keep those
outcomes visually and textually separate in the installer.

### Clean staging

Build artifacts in a clean staging directory. Do not accidentally package
temporary files, credentials, source backups, debug logs, or unrelated
executables. Inspect the archive contents before upload.

### Checksums and signatures

Generate checksums after the final bytes are produced. If signing changes the
bytes, generate the checksum for the signed artifact, not the pre-signing
intermediate. Publish signature information only after verification succeeds.

### Website synchronization

After an artifact changes, verify every public download surface:

- the main download page;
- edition cards and installer controls;
- release links;
- installation guide;
- checksum and security explanation;
- GitHub release assets.

The website must not advertise a file that is missing, and a visible download
button must point to an actual release asset rather than a placeholder URL.

---

## Migration Notes

Migration should be treated as a compatibility exercise, not just a file copy.

### From an older Block 2.x document

Start by copying the document to a new branch or backup, then run:

```powershell
block --version
block check .\legacy.blk
block ast .\legacy.blk
```

Fix parser errors first. Then run the smallest valid workflow and compare its
state and output with the older engine. Only after that should you add imports,
servers, or Plus tooling.

### Native control flow

Use the current standalone `block` terminator for native compound statements.
Do not assume that indentation alone defines scope. Keep old examples that use
ambiguous or experimental syntax out of the main README until they are tested
against the target release.

### Imports and packages

The current release uses explicit local imports and does not provide the
removed third-party package marketplace. Move reusable code into a reviewed
project module rather than relying on a remote package reference.

### Runtime availability

An older machine may have depended on a runtime that is no longer on `PATH`.
Run `block runtimes` and test the host executable directly. Installing a
runtime from its official source is a separate operation from installing Block.

### Installer and trust changes

If Windows displays a warning for an unsigned artifact, verify the release
source and checksum first. Do not assume that a browser warning identifies a
defect in the engine, but do not dismiss it either. Preserve the evidence and
wait for a signed artifact or an official vendor review when available.

### Preserve filenames in scripts

Existing automation may refer to `BlockSetup.exe`, extension archives, or
release URLs by exact name. If a filename must change, update scripts, docs,
website links, and checksums in the same change and leave a migration note.

---

## Frequently Asked Questions

### Is Block a real programming language?

Yes, with a precise qualification: Block is a programming and orchestration
language with a native core and a multi-runtime execution engine. It is not a
replacement implementation of Python, JavaScript, or every other language in
its tag table. Its distinct value is the document format, stage ordering,
serializable state pipeline, project model, and diagnostics.

### Does Block replace Python or Node.js?

No. Block coordinates them. You still need the host runtime for the stage you
want to execute, and the host language keeps its own syntax, libraries,
versions, and security behavior.

### Can I run Block in a browser?

The documented engine is local-first and Windows-oriented. A website can
document or distribute Block, but a browser page is not automatically a safe
host for arbitrary native Python, Node.js, Lua, or PHP execution. A hosted
execution service would require an independently designed sandbox and is not
implied by the current local engine.

### Does the installer install every optional runtime?

No promise should be inferred from the component list. The installer can detect
optional runtimes, while availability may depend on the installed edition,
configuration, permissions, and official runtime setup. Read the installer's
result and run `block runtimes` afterward.

### Why did a script work in one folder but not another?

The file resolver uses the current directory, project metadata, and optionally
the configured workspace. Use `block project root`, `block workspace show`, or
an explicit quoted path instead of depending on a particular shell location.

### Why did my state disappear between stages?

Only values that are produced into the active serializable state can cross the
boundary. Local variables, unassigned values, live handles, and unsupported
objects do not become portable state automatically. Export a small explicit
result object and inspect it with a minimal next stage.

### Why is my JSON invalid?

The final rendered text must be valid JSON. Check whether strings are being
inserted with correct escaping and whether arrays or objects are being treated
as quoted strings. Prefer structured state interpolation for complex values.

### Is the local server a production server?

It is documented as a local development server. Treat it as such unless you
add independent production controls, including authentication, TLS, network
policy, rate limiting, observability, and a process-management strategy.

### Is the installer signed?

Only claim a signature when the exact artifact has a verifiable signature. The
current documentation records a pending SignPath Foundation application; that
is not the same as a completed certificate or signed release.

### What should I do if security software reports a Trojan?

Keep protection enabled, isolate the file, record the detection name and hash,
verify the official release URL and checksum, and submit the sample through the
security vendor's official false-positive process if appropriate. Do not tell
other users to disable protection as a workaround.

### Is the third-party package marketplace available?

Not in the current documented release. Use explicit local imports inside a
reviewed project. A future registry must have its own trust, versioning,
signature, and dependency policy before it is described as supported.

### How do I ask for help effectively?

Include the smallest document that reproduces the issue, the exact command,
engine edition and version, operating system, relevant runtime versions, exit
code, and the first actionable diagnostic. Remove credentials and private data.

---

## Glossary

| Term | Meaning |
| --- | --- |
| Block document | A text file containing native or host-language stages |
| Stage | One executable or output section delimited by a language tag |
| Native core | The deterministic Block syntax for assignments, control flow, functions, and built-ins |
| Host runtime | The external interpreter, compiler, or renderer that owns a stage |
| State pipeline | The ordered transfer of serializable values between stages |
| State boundary | The serialization and validation point between runtimes |
| Edition | Lite, Standard, or Plus capability distribution |
| Project root | The directory controlled by `block.project.json` |
| Workspace | An optional configured parent used for safe script discovery |
| Import | An explicit local inclusion of another Block file |
| Artifact | A distributable executable, archive, extension, checksum, or manifest |
| Release asset | A file attached to a GitHub release or equivalent release record |
| Checksum | A digest used to compare downloaded bytes with published bytes |
| Authenticode | Windows executable signature metadata |
| Attestation | Evidence describing how an artifact was built or verified |
| Sandbox | An isolation boundary; the current local engine is not a complete one |
| Actionable diagnostic | An error that identifies context and suggests a next check |
| Smoke test | A small test proving that the main path can start and finish |
| Regression test | A test that protects a previously fixed behavior |
| Third-party validation | Independent evidence from an external tester or project |

---

## Support Checklist

Before opening an issue, collect the following information:

### Environment

```powershell
block --version
block doctor
block runtimes
block config show
```

Also record the Windows version, shell, and whether the terminal was opened
after the latest runtime or installer change.

### Reproduction

Provide:

- the smallest `.blkl`, `.blk`, or `.blkp` file that fails;
- the exact command, including the working directory;
- the expected result;
- the actual result;
- the exit code;
- the first error and any operation/file/stage context;
- whether `check` succeeds while `run` fails.

### Security hygiene

Remove API keys, passwords, access tokens, private URLs, personal data, and
large proprietary inputs. Replace secrets with clearly marked placeholders and
say that the original value was redacted.

### Installer or download problems

For a download or Defender/Chrome warning, also include the exact filename,
release URL, SHA-256 hash, detection name, and timestamp. Do not attach a
private or sensitive executable to a public issue without reviewing the
security policy first.

---

## How to Cite and Share

When introducing Block to another developer, prefer a specific, verifiable
claim:

> Block is a local-first multi-runtime execution engine that lets a single
> document coordinate native Block logic and host-language stages through an
> explicit serializable state boundary.

Then link to:

- the exact release or commit;
- the matching example;
- the relevant syntax or installation guide;
- the security and signing status;
- the [citation metadata](CITATION.cff) when writing academic or formal work.

Avoid claims that the current release is a universal language replacement, a
browser sandbox, a production server, or a completed signed distribution unless
the referenced artifact and documentation explicitly prove that claim.

For tutorials, include the command output users should expect and identify
which runtimes are optional. For benchmarks, publish the machine, runtime
versions, input sizes, warm-up method, and complete command. A small honest
reproduction is more useful than an impressive number without context.

---

## Maintainer Quick Reference

Use this checklist before merging a change:

- [ ] The change has a focused scope and preserves existing filenames unless a
      migration is documented.
- [ ] Parser, native evaluator, runtime bridge, or website behavior has a
      corresponding test when applicable.
- [ ] New errors identify the operation and provide a next action where one is
      known.
- [ ] Paths are resolved from the documented project/workspace rules.
- [ ] No credentials, tokens, private keys, local absolute paths, or temporary
      files entered the repository.
- [ ] The README, changelog, release manifest, or migration note reflects user-visible behavior.
- [ ] Security boundaries are stated honestly; local process execution is not
      described as a complete sandbox.
- [ ] Download links point to real assets and checksums describe the final bytes.

Use this checklist before cutting a release:

1. Confirm the version in source, binaries, extensions, docs, and tags.
2. Build in a clean staging directory.
3. Run syntax, native, runtime, import, project, server, and diagnostic tests
   that apply.
4. Run the smallest supported smoke example for Lite, Standard, and Plus.
5. Inspect artifact contents and remove unrelated files.
6. Generate and verify SHA-256 checksums.
7. Verify signature status for each exact artifact; do not copy a status from a
   different file.
8. Update release notes, installation instructions, and website download
   controls together.
9. Publish known limitations and optional-runtime results.
10. Ask an independent tester to repeat the documented path from a clean
    machine when possible.

Use this checklist after release:

- [ ] Download each public asset from the public link.
- [ ] Verify the checksum from a second clean directory.
- [ ] Run the installer and confirm the selected edition and default path.
- [ ] Open a new terminal and run `--version`, `doctor`, and `runtimes`.
- [ ] Run one native example and one applicable host-runtime example.
- [ ] Check that the website does not advertise unavailable files or completed
      signing that has not happened.
- [ ] Record tester evidence separately from maintainer verification.

Block is strongest when its language model, runtime boundaries, installer
behavior, and public documentation tell the same story. This expanded guide is
intended to make that story practical: start small, keep the state explicit,
test the boundary, verify the artifact, and report what the release actually
does.
