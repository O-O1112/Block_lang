# Block Language canonical knowledge (v2.2.5)

## Product identity

Block Language / Block Engine is a local-first polyglot programming language and
execution engine. One document can contain multiple native-language stages and
pass serializable state from one stage to the next. The engine is primarily
published for Windows 10/11 and does not replace or bundle every host language.

## Editions and extensions

| Edition | File extension | Position |
| --- | --- | --- |
| Block Lite | `.blkl` | Smallest runtime surface |
| Block Standard | `.blk` | Recommended general-purpose edition |
| Block+ | `.blkp` | Expanded runtimes and tooling |

The repository also recognizes `.block`, `.blocklite`, and `.blockplus` aliases
in editor integrations. The VS Code extension and Acode plugin are separate
downloads from the engine.

## Runtime block syntax

```block
<py>
numbers = [1, 2, 3]
total = sum(numbers)
</py>

<js>
console.log(total)
</js>
```

Common documented tags include `<py>`, `<js>`, `<php>`, `<ruby>` / `<rb>`,
`<lua>`, `<ps>`, `<sql>`, `<html>`, `<json>`, `<c>`, `<cpp>`, `<go>`, `<rust>`,
`<ts>`, `<cs>`, `<kotlin>`, `<dart>`, `<zig>`, `<perl>`, and `<r>`. Support
depends on the edition, installed tool, and host environment.

There is no `<block>` tag. The native Block language is written without a tag;
its compound statements close with a standalone `block` line.

## Native language core

The native core supports assignments, expressions, `if` / `elif` / `else`,
`while`, `for name in values:`, `break`, `continue`, `range`, `func`, `return`,
`print`, `pass`, list and dictionary literals, indexing, `.length` / `.count`,
and deterministic built-ins including `len`, `str`, `int`, `float`, `bool`,
`type`, `contains`, `keys`, `values`, and `sum`.

```block
total = 0
for number in [1, 2, 3, 4]:
    total = total + number
block
print(total)
```

The native core intentionally does not expose file, network, process, or package
APIs. A Python, JavaScript, or other runtime block is required for those
capabilities.

## v2.2.5 package registry

Standard and Plus expose the package commands `pkg search`, `pkg info`,
`pkg install <name> --remote`, `pkg verify`, and `pkg remove`. The official
registry is a reviewable `registry/index.json` catalog. The five starter
packages are `octopus`, `block-web`, `gblock-d`, `block-work`, and `drawing`.
Remote installation is explicit and fails closed unless the source is an
official HTTPS raw GitHub file with a matching SHA-256 digest. The installer
stages only a package manifest and entry document; it does not execute package
code while installing.

`block doctor --full --root <dir> --report <file> --strict` performs a read-only
health scan of Block scripts, package manifests, website metadata, and required
repository files. It should be used as a review signal, not as an operating
system sandbox or a substitute for independent testing.

## State boundary

The preceding executable stage produces values, Block serializes them, and the
next stage receives the prepared state. Plain numbers, strings, booleans, lists,
and dictionaries are the safest values. Open handles, sockets, functions,
callbacks, database connections, and circular references should not be shared.
The receiving stage must validate values before using them in paths, commands,
queries, or templates.

## CLI and discovery

Typical commands are:

```powershell
block --version
block --help
block workspace set C:\BlockProjects
block workspace show
block find hello
block project root
block project run
```

The resolver checks explicit paths, the current project, and the configured
workspace. It does not scan the entire drive; ambiguous matches should be
reported rather than silently selecting an arbitrary file.

## Security boundaries

Block language blocks can start native programs on the local machine. A timeout,
import limit, or edition policy is not a complete operating-system sandbox.
Users should review untrusted `.blk`, `.blkl`, and `.blkp` files, install only
required runtimes, and verify release SHA-256 checksums before installation.
