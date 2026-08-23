# Block Engine v2.2.2 release manifest

Released 2026-08-23 after the merged v2.2.2 stability work passed the Windows
GitHub Actions build and release verification workflow.

## Included

- Lite, Standard, and Plus engine executables.
- Versioned and stable Windows installers.
- Engine ZIP bundles for all three editions.
- VS Code `.vsix` and Acode plugin packages.
- `SHA256SUMS.txt` for every published artifact.
- Workspace-aware project and script discovery.
- Native logical short-circuit parsing that preserves syntax consumption.
- Explicit `range()` size-limit errors instead of silent truncation.

## Verification gate

The release workflow builds the artifacts on `windows-latest`, runs the engine,
CLI, native-language, and native-interpreter tests, verifies package manifests
and licenses, checks archive contents, and compares every published hash.

The release assets are attached to the [GitHub v2.2.2 release](https://github.com/O-O1112/Block_lang/releases/tag/v2.2.2).
