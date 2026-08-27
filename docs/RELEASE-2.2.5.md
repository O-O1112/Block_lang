# Block Engine v2.2.5 release manifest

This release strengthens the local-first engine and establishes the first
reviewable package marketplace. The release workflow builds Lite, Standard and
Plus from the same source revision, packages each executable, generates
`SHA256SUMS.txt`, and verifies the bundle before publication.

## Included

- `block.exe`, `block-lite.exe`, and `block-plus.exe` with version `2.2.5`.
- A secure Windows bootstrapper that downloads only the matching official
  GitHub Release asset, verifies SHA-256, rejects unsafe archive paths, and
  installs atomically.
- No automatic Winget, Chocolatey, PowerShell, or downloaded-script execution
  in the installer. Optional runtimes are detected and reported only.
- `block pkg search`, `info`, `install --remote`, `verify`, and `remove` for the
  official digest-pinned registry.
- A reproducible `tools/build-registry-index.ps1` generator and a registry CI
  workflow that fail when package metadata or SHA-256 entries are stale.
- Five starter packages: `octopus`, `block-web`, `gblock-d`, `block-work`, and
  `drawing`.
- `block doctor --full` with a read-only script, package, website and repository
  scan, plus a scheduled GitHub Actions health check.
- Updated website, package marketplace, documentation, editor package metadata,
  release workflow and regression coverage.

## Trust and compatibility

The installer is a bootstrapper, so it needs an existing `v2.2.5` GitHub Release
to install. It fails closed when the release, selected asset, checksum, or
archive layout cannot be verified. The release workflow publishes the ZIPs and
`SHA256SUMS.txt` together.

The installer can be Authenticode-signed by passing
`-SigningCertificateThumbprint` to `build-installer.ps1` or
`build-release.ps1`. A signing certificate is not included in this repository;
an unsigned build must not be described as publisher-trusted merely because its
checksum is valid.

## Verification

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1 -Version 2.2.5 -ReleaseDirectory .\release
powershell -ExecutionPolicy Bypass -File .\verify-release.ps1 -ReleaseDirectory .\release -Version 2.2.5
block doctor --full --root . --report .blocklang\health\release.json --strict
```

The installer itself is not executed by CI. Installation testing should use a
disposable Windows account or VM after the Release assets and their checksums
are available.
