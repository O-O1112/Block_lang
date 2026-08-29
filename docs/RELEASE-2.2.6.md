# Block Engine v2.2.6 release manifest

Version 2.2.6 is a stability and trust-boundary release. It removes the
unfinished third-party package subsystem instead of presenting prototypes as a
supported marketplace, and it keeps reviewed local source reuse through
`<import src="..." />` and `block.project.json`.

## Shipped artifacts

- `block.exe`, `block-lite.exe`, and `block-plus.exe`, all reporting `2.2.6`
- `block.zip`, `block-lite.zip`, and `block-plus.zip`
- `BlockSetup-v2.2.6.exe` and the identical stable alias `BlockSetup.exe`
- `block-language-2.2.6.vsix`
- `acode-plugin-block-2.2.6.zip`
- `SHA256SUMS.txt` covering every artifact above

## Core fixes

- Project entry discovery and imports work when Block is launched outside the
  project directory.
- Imports remain inside either the trusted project/script root or the configured
  sandbox, reject reparse-point escapes, restrict source extensions, and enforce
  depth, count, and aggregate-byte limits.
- Process timeout handling waits on the real child process and terminates the
  process tree on timeout.
- Running Block without arguments returns a bounded introduction and help instead
  of entering a long animation loop. Terminal color and cursor state are restored
  even after an exception.
- Configuration writes preserve the previous valid file when atomic replacement
  is unavailable or interrupted.
- Oversized custom-runtime registries are rejected before deserialization.
- The local server reports accept-loop failures and closes its listener cleanly.
- Engines and installer retain the tested x86 .NET Framework target, which runs
  on supported 64-bit Windows through its compatibility layer. The website now
  states this architecture instead of incorrectly claiming AnyCPU.

## Installer and supply-chain behavior

The installer is a verified bootstrapper. It uses TLS 1.2, follows only the
official GitHub API/release redirect chain, accepts the exact GitHub release asset
host, validates the final URI, checks SHA-256, rejects archive traversal and
reparse-point content, enforces size limits, and installs atomically.

The installer and engine never invoke Winget, Chocolatey, PowerShell download
scripts, or another package manager. Optional host runtimes must be installed
from their official publishers and made available on `PATH`.

Checksums and GitHub build-provenance attestations prove artifact consistency;
they are not publisher identity. Official Windows publisher reputation requires
an Authenticode certificate. The release pipeline supports a real certificate
through `-SigningCertificateThumbprint` but never creates or claims a self-signed
certificate as public trust.

## Deliberately removed

- `block pkg` and `block ecosystem`
- the first-party package registry and fixed marketplace page
- automatic third-party package or runtime installation

No package catalog is fetched or executed by v2.2.6. A future ecosystem must have
a separately reviewed threat model, immutable hashes, publisher identity, and a
safe installation contract before it can return.

## Verification

From a Windows PowerShell prompt:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1 -Version 2.2.6 -ReleaseDirectory .\release
powershell -ExecutionPolicy Bypass -File .\verify-release.ps1 -ReleaseDirectory .\release -Version 2.2.6
```

If local Windows Application Control blocks newly compiled unsigned executables,
use `-SkipExecutableExecution` only for local artifact inspection. The GitHub
Actions Windows job must still execute the complete smoke, CLI, path, native,
server, and hardening suites before publication.
