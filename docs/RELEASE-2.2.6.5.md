# Block Engine v2.2.6.5

Released 2026-08-30.

## Trust and installer hardening

- The installer never terminates a running process. A locked target is reported
  with a clear retry instruction instead.
- Adding Block to `PATH` and registering `.blk`, `.blkl`, and `.blkp` file
  associations are explicit, opt-in installer choices.
- The installer records only the state it owns and removes only that state during
  uninstall, while retaining compatibility with existing installations.
- Release downloads are linked directly to versioned GitHub Release assets.

## Release integrity

- The release pipeline accepts a four-part Windows version and uses it in the
  engine, installer, Acode package, checksums, and tag workflow. The VSIX file
  keeps the release-tag filename while its internal version is `2.2.7`, the
  next valid three-part VS Code extension version.
- A signed build can require valid Authenticode signatures on every Windows
  executable. This release is accompanied by GitHub build provenance and
  SHA-256 checksums; publisher signing depends on an approved signing service.

## Upgrade guidance

Download `BlockSetup-v2.2.6.5.exe` from the matching GitHub Release. If Windows
or a browser warns about a newly published executable, verify the published
SHA-256 value and consult [the signing policy](CODE-SIGNING-POLICY.md). Do not
disable endpoint protection or bypass a security prompt.
