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

For a complete clean release build, use `build-release.ps1`. Before opening a
pull request that changes engine, installer, or release behavior, run the
focused checks and then the complete Windows suite:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\Test-BlockEngine.ps1 -EngineDirectory .\release
powershell -ExecutionPolicy Bypass -File .\tests\Test-BlockCli.ps1 -EngineDirectory .\release
powershell -ExecutionPolicy Bypass -File .\tests\Test-HealthHardening.ps1 -EngineDirectory .\release -RepositoryRoot $PWD
powershell -ExecutionPolicy Bypass -File .\tests\Test-RepositoryIntegrity.ps1 -RepositoryRoot $PWD
powershell -ExecutionPolicy Bypass -File .\tests\Test-VersionConsistency.ps1 -RepositoryRoot $PWD
```

The shorter community harness is available for both testers and contributors:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\Test-CommunityLab.ps1 -EngineDirectory .\bin
```

Changes to the engine belong in `src/`. Changes to the editor integrations
belong in `block-vscode-extension/` or `acode-plugin-block/`. Keep generated
build output out of commits; the repository root contains only the intentionally
published release artifacts used by GitHub Pages.

### Change checklist

- Start from a fresh branch based on `main`; do not develop from generated
  release output.
- Add or update a regression test for every security or behavior change.
- Keep secrets, local absolute paths, downloaded binaries, and private logs out
  of commits and issue reports.
- Run `git diff --check` and review the final file list before pushing.
- Treat a green CI run as a gate, not as proof that untrusted native code is
  safe to execute.

## Pull requests

- Explain the problem and the chosen solution.
- Include reproduction steps for bug fixes.
- Update the relevant documentation or changelog when behavior changes.
- Run the build and release verification scripts when release-facing code is
  changed.
- Do not commit credentials, certificates, private keys, or personal data.

By submitting a contribution, you agree that it may be distributed under the
project's [MIT License](LICENSE).
