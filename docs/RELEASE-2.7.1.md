# Block Engine v2.7.1

Version 2.7.1 is the 2.7 maintenance and security-hardening update. It keeps
the local-first execution model and the Lite, Standard, and Plus editions while
making the trust boundary more explicit and the development workflow easier to
repeat.

## Highlights

- Windows child runtimes now fail closed if the Job Object resource boundary
  cannot be created or attached. A process is never presented as sandboxed when
  the required lifetime and memory controls were not installed.
- Custom runtime definitions are validated before process launch. Reserved
  built-in language names cannot be shadowed, malformed language identifiers are
  rejected, and command, argument, and extension values have bounded formats.
- The local Block server uses the same no-store, no-sniff, and referrer policy
  headers as the API server. Repository health scans skip reparse-point files
  so a linked file cannot be mistaken for an ordinary project file.
- Execution timeout and configuration limits are centralized and documented.
- The CI workflow now has an explicit timeout and cancels superseded runs. The
  daily health workflow and release checklist are documented as repeatable
  commands instead of informal maintainer knowledge.
- The README, SECURITY.md, CONTRIBUTING.md, AI knowledge pack, tester material,
  and editor metadata are synchronized to v2.7.1.

## Security boundary

Block launches host language runtimes. `NetworkBlocked` remains an advisory
runtime guard, not an operating-system sandbox. Do not execute hostile or
unreviewed documents merely because these checks pass. Use a container, virtual
machine, Windows Sandbox, AppContainer-equivalent, or separately managed
firewall policy for untrusted code.

The API and language server remain loopback-only and require the session token
for non-OPTIONS requests. They are intended for local tooling, not public
internet exposure.

## Verification contract

Before publishing, maintainers must run the clean release build and the complete
Windows regression suite, including engine, CLI, path, native language,
installer transport, health-hardening, API, repository-integrity, and version
consistency checks. Release assets are generated from the tag, verified with
`SHA256SUMS.txt`, and published through GitHub Releases.

## Upgrade notes

The update preserves existing configuration files. If a Windows runtime cannot
be attached to the required Job Object, that execution now stops with an
actionable error instead of continuing without the resource boundary. Custom
runtime definitions must use a new non-built-in language identifier and remain
explicitly enabled through configuration.
