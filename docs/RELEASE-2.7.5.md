# Block Engine v2.7.5

Version 2.7.5 is a stability and diagnostics release for the 2.7 maintenance
line. It keeps Block local-first and preserves the Lite, Standard, and Plus
editions while making source inspection safer and execution failures easier to
understand.

## Highlights

- Added `block plan <file>` as a read-only preflight command. It reports the
  document stages, source line ranges, estimated host runtimes, and structural
  diagnostics without loading imports, runtimes, custom commands, or packages.
- Added `block plan --json <file>` for editor integrations and CI. The output
  uses a versioned `ExecutionPlan` schema and returns exit code 1 when the
  structural diagnostics contain an error.
- Fixed nested language tags being passed into a host runtime as ordinary source
  text. The execution parser now rejects them at the tag location with a repair
  hint, matching the existing AST diagnostics contract.
- Added regression coverage for the human-readable plan, JSON plan, paths with
  spaces, and nested-tag rejection across all three engine editions.
- Synchronized engine, extension, Acode, citation, workflow, release, and
  documentation metadata to v2.7.5.

## Safe preflight example

```powershell
block plan .\examples\hello-polyglot.blk
block plan --json .\examples\hello-polyglot.blk | Set-Content plan.json
```

The plan is intentionally structural-only. A successful plan does not execute
the document and does not prove that a host runtime is installed. Use
`block runtimes` to inspect PATH discovery, then use `block check <file>` and
`block run <file>` when you are ready to validate and execute the workflow.

## Compatibility and security

Top-level language blocks remain the supported composition model. Put each
language in its own top-level block and close it before opening the next one:

```block
<py>
print("prepare")
</py>

<js>
console.log("continue")
</js>
```

The new plan command does not weaken the existing import, workspace, runtime,
process, or local-server boundaries. Block still launches host runtimes; its
network guard is advisory and is not an operating-system sandbox. Do not run
unreviewed code solely because a plan or check succeeds.

## Verification contract

Before publishing, run the clean release build and the complete Windows
regression suite. Verify the generated ZIPs, installer, extension packages, and
`SHA256SUMS.txt` from the same source tree and tag. Keep v2.7.5 artifacts bound
to the v2.7.5 GitHub Release; do not replace a verified versioned artifact with
an untracked local build.
