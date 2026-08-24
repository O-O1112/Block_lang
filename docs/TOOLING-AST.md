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
