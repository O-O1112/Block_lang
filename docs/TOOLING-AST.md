# Block tooling syntax tree

Block exposes an execution-free document view for editors, linters, review
tools, and AI integrations:

```powershell
block ast .\examples\hello-polyglot.blk
```

The command writes one JSON document to standard output. It does not execute
language blocks, resolve imports, load packages, or invoke custom definitions.
Malformed boundaries are returned as structured diagnostics and cause exit code
`1`; a valid document returns exit code `0`.

## Schema version 1

The root object contains:

- `SchemaVersion`: currently `1`;
- `Kind`: `Document`;
- `Blocks`: top-level language boundaries with `Language`, `StartLine`,
  `EndLine`, and source `Code`;
- `Diagnostics`: objects containing `Severity`, `Code`, `Message`, `Line`, and
  `Column`.

The same model is public in the engine assembly through `BlockSyntax.Parse`,
`BlockSyntaxTree`, `BlockSyntaxNode`, and `BlockSyntaxDiagnostic`. Tooling should
check `SchemaVersion` before consuming fields and should not treat this syntax
tree as proof that a host language is installed or safe to execute.

This API describes Block document boundaries. Host-language ASTs remain the
responsibility of the corresponding Python, JavaScript, PHP, Lua, or other
language tooling.

## Execution plan in v2.7.5

For a preflight view that is easier to consume than the full source-bearing AST,
use:

```powershell
block plan --json .\examples\hello-polyglot.blk
```

The command emits one JSON object with `kind: "ExecutionPlan"` and
`schemaVersion: 1`. It contains the absolute `script` path, engine `edition` and
`engineVersion`, a `blocks` array, normalized `requiredRuntimes`, and any
structural `diagnostics`. Each block entry includes `index`, `language`,
`runtime`, `requiresHostRuntime`, `startLine`, `endLine`, and `characters`.

Unlike `block check`, the plan deliberately uses only the structural parser. It
does not load imports, read package metadata, resolve custom commands, start
host processes, or modify configuration. A non-zero exit code means the plan
contains an error diagnostic; a zero exit code only means the document has valid
top-level boundaries, not that optional runtimes are installed or that the code
is safe to execute.
