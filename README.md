# Block Language — Block Engine

[![Build and test](https://github.com/O-O1112/Block_lang/actions/workflows/ci.yml/badge.svg)](https://github.com/O-O1112/Block_lang/actions/workflows/ci.yml)
[![Latest tag](https://img.shields.io/github/v/tag/O-O1112/Block_lang?sort=semver&label=latest%20tag)](https://github.com/O-O1112/Block_lang/tags)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**One file. Every runtime.** Block is a local-first polyglot programming language and execution engine for composing Python, JavaScript, Lua, PHP, SQLite, and more in one readable program with a shared state pipeline.

If you are looking for a polyglot programming language, a multi-language scripting workflow, a Python-to-JavaScript bridge, or a local-first automation engine built around readable `.blk` files, Block is designed for that use case.

**Start here:** [download the engine](downloads.html) · [read the documentation](wiki.html) · [run the examples](examples/) · [join the Community Lab](docs/COMMUNITY-LAB.md) · [read the book source](docs/book/) · [report an external result](docs/THIRD-PARTY-VALIDATION.md) · [browse the source on GitHub](https://github.com/O-O1112/Block_lang)

Maintainers can use the [organic growth playbook](docs/GROWTH.md) to turn demos, releases, and user feedback into a repeatable discovery-to-install funnel.

## At a glance

| Item | Details |
| --- | --- |
| Current release | `2.2.5` |
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
- [Imports and packages](#importing-external-block-files)
- [HTML, JSON, and local servers](#html-and-json-output)
- [Editor extensions](#editor-extensions)
- [Security model and limits](#security-model)
- [Troubleshooting](#troubleshooting)
- [Build, test, and release](#repository-layout-and-release-verification)
- [Roadmap](#roadmap)
- [Community and discovery](#community-and-discovery)
- [Documentation and contribution](#documentation-contribution-and-license)

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

The versioned installer is [`BlockSetup-v2.2.5.exe`](https://github.com/O-O1112/Block_lang/releases/download/v2.2.5/BlockSetup-v2.2.5.exe).
The stable download alias is [`BlockSetup.exe`](https://github.com/O-O1112/Block_lang/releases/download/v2.2.5/BlockSetup.exe).
The same files are also linked from the [download page](downloads.html).

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

The v2.2.5 Windows build uses the .NET Framework C# compiler available at
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
| Standard | `.blk` | General development, modules, and local packages |
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
| Standard | You want the recommended daily-use engine | Imports, local packages, native control flow, and local server support |
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
in the v2.2.5 Windows build:

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
| `<engine> serve [port]` | — | ✓ | ✓ | Start a local HTTP server document |
| `<engine> ecosystem ...` / `project ...` | — | ✓ | ✓ | Create, add, and list local packages |
| `<engine> pkg search|info|install|verify|remove ...` | — | ✓ | ✓ | Discover the signed-by-digest registry and manage packages |
| `block-plus fmt <file>` | — | — | ✓ | Format a Plus document and keep a `.bak` backup |
| `block-plus doc <file>` | — | — | ✓ | Generate a `.doc.md` block summary |

Aliases `eco` and `pkg` are accepted for `ecosystem`. `run` is an explicit
execution form; the original `<engine> <file>` form remains supported. Relative
script paths are resolved from the current directory, the nearest
`block.project.json`, and the configured workspace. The resolver never scans an
entire drive and reports ambiguous matches instead of choosing randomly.

For a project created with `block ecosystem init`, run its entry file from any
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

## Block Ecosystem and Packages

Initialize a project:

```powershell
block ecosystem init . my-project
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
├─ main.blk
└─ packages/
```

Add a local package:

```powershell
block ecosystem add .\hello-block .
block ecosystem list .
```

Use the package:

```block
<use package="hello-block" />
```

Specify a custom entry point:

```block
<use package="hello-block" entry="src/main.blk" />
```

Basic `block.package.json`:

```json
{
  "name": "hello-block",
  "version": "1.0.0",
  "main": "main.blk",
  "description": "A reusable Block package"
}
```

Block ecosystem commands are local-first: adding a package reorganizes local directories without downloading or executing untrusted code. Package contents enter the execution pipeline only when explicitly invoked via script tags.

### Official package registry

The v2.2.5 registry is a reviewable `registry/index.json` file in this
repository. It currently lists the starter packages `octopus`, `block-web`,
`gblock-d`, `block-work`, and `drawing`. Each entry declares its license,
permissions, source files, and SHA-256 digests. Search and inspect before an
explicit remote install:

```powershell
block pkg search
block pkg info drawing
block pkg install drawing --remote
block pkg verify .
```

Remote package installation is restricted to official HTTPS raw GitHub files,
requires a digest for every file, stages only the manifest and entry document,
and never executes package code during installation. See the [package
marketplace](marketplace.html) and [`registry/`](registry/) for the current
catalog and source.

Maintainers can rebuild the catalog after reviewing a package change. The
generator hashes each manifest and entry file, rejects reparse points and
unsafe paths, and the registry workflow fails if the committed index is stale:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build-registry-index.ps1
powershell -ExecutionPolicy Bypass -File .\tools\build-registry-index.ps1 -Check -Generated (Get-Content .\registry\index.json -Raw | ConvertFrom-Json).generated
```

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

The repository publishes [`block-language-2.2.5.vsix`](https://github.com/O-O1112/Block_lang/releases/download/v2.2.5/block-language-2.2.5.vsix).
In VS Code, open **Extensions**, choose **Install from VSIX...**, select the
package, and reload the window if prompted.

The extension provides Block language recognition, syntax highlighting, snippets,
folding markers, state interpolation highlighting, and commands that invoke the
local Block executable. It does not bundle Python, Node.js, PHP, Lua, or any other
host runtime.

### Acode

The mobile editor package is
[`acode-plugin-block-2.2.5.zip`](https://github.com/O-O1112/Block_lang/releases/download/v2.2.5/acode-plugin-block-2.2.5.zip). Install it through
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

### Default configuration in v2.2.5

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
- Review imported files and local packages before invoking them.
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

This GitHub repository also hosts the Block Pages download site. The root
website files and published download artifacts intentionally keep their stable
paths; source code and maintainer notes are separated into `src/`, the two
extension directories, and `docs/`.

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

The complete v2.2.5 release flow builds the three engines, creates matching ZIP
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

## v2.2.5 status and known boundaries

Version 2.2.5 is the current documented release line. It includes the Lite,
Standard, and Plus engines, the Windows installer, the VS Code extension, the
Acode plugin, native control flow, cross-runtime state synchronization, local
imports and packages, the verified package registry, the Plus
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

See the [changelog](CHANGELOG.md) and [v2.2.5 release notes](docs/RELEASE-2.2.5.md)
for the tested changes and release artifact contract. Planned behavior should not
be read as shipped behavior.

---

## Roadmap

The [public roadmap](ROADMAP.md) separates the v2.2.5 foundation, proposed next
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
- [v2.2.5 release manifest](docs/RELEASE-2.2.5.md)

The visual documentation site is available from [`wiki.html`](wiki.html). The
Markdown Wiki is the reviewable source for the same installation, syntax,
architecture, and troubleshooting topics.

---

## A Note for Newcomers

**Leave the language to the language; let Block handle the flow.**

There is no need to abandon familiar tools to bridge multi-language workflows. Place each task into its respective block and let your data flow forward naturally.
