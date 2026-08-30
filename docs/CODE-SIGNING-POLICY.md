# Code signing policy

This policy defines how Block Language publishes Windows executables.

## Scope

The policy applies to the versioned installer, the stable installer alias, and
the Lite, Standard, and Block+ engine executables published in a GitHub Release.
ZIP packages are valid only when their contained executable matches the release
checksum manifest.

## Release requirements

1. Source changes are reviewed in the public `O-O1112/Block_lang` repository.
2. The release is built by the protected GitHub Actions release workflow from a
   version tag.
3. Required test suites, SHA-256 checksums, and GitHub build attestations must
   be produced before assets are published.
4. When a publicly trusted code-signing service is configured, all Windows
   executables in the release must have a valid Authenticode signature and
   timestamp. The signed versioned installer is copied to `BlockSetup.exe` so
   the two names are byte-identical.
5. Private keys, certificate files, access tokens, and signing passwords are
   never committed to the repository or bundled in a release.

## Installer behaviour

Block Setup downloads only the package matching its own version tag from the
official GitHub Release and verifies its SHA-256 digest before extraction. It
does not run package managers, command shells, or runtime installers. Adding
the Block command to the user `PATH` and changing file associations are
explicit user choices. It never terminates running programs.

## Verification

Users should download only from the official release page, compare the file
with `SHA256SUMS.txt`, and verify its GitHub attestation. Once signed releases
are available, users can additionally run:

```powershell
Get-AuthenticodeSignature .\BlockSetup-vX.Y.Z.exe
```

Unsigned builds are development artifacts and must never be presented as a
trusted publisher release.
