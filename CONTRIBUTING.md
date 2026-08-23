# Contributing to Block Language

Thank you for helping improve Block Language. Bug fixes, documentation
improvements, tests, runtime integrations, and editor tooling are welcome.

For the complete public collaboration flow, start with the
[Community Lab](docs/COMMUNITY-LAB.md). It separates release-based testing
from source contributions and explains how to produce a reproducible report
without sharing private data.

## Before opening a change

1. Search existing issues and pull requests.
2. For a security problem, follow [SECURITY.md](SECURITY.md) instead of
   opening a public issue.
3. Keep changes focused and describe the user-visible behavior they change.

## Development workflow

The supported maintainer workflow is Windows-based:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
powershell -ExecutionPolicy Bypass -File .\package-engine.ps1
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
powershell -ExecutionPolicy Bypass -File .\package-extensions.ps1
powershell -ExecutionPolicy Bypass -File .\verify-release.ps1
```

For a complete clean release build, use `build-release.ps1`. The smoke tests
can be run against its output with `tests\Test-BlockEngine.ps1`. The shorter
community harness is available for both testers and contributors:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\Test-CommunityLab.ps1 -EngineDirectory .\bin
```

Changes to the engine belong in `src/`. Changes to the editor integrations
belong in `block-vscode-extension/` or `acode-plugin-block/`. Keep generated
build output out of commits; the repository root contains only the intentionally
published release artifacts used by GitHub Pages.

## Pull requests

- Explain the problem and the chosen solution.
- Include reproduction steps for bug fixes.
- Update the relevant documentation or changelog when behavior changes.
- Run the build and release verification scripts when release-facing code is
  changed.
- Do not commit credentials, certificates, private keys, or personal data.

By submitting a contribution, you agree that it may be distributed under the
project's [MIT License](LICENSE).
