# Block Community Lab

The Community Lab is the public entry point for people who want to test,
review, document, or improve Block. It separates two kinds of participation:

- **Testers** use an official release and report what happened in a real
  environment. No source build is required.
- **Contributors** work from a fork, make a focused change, and submit a pull
  request. GitHub Actions builds and tests the change on Windows.

The goal is a reproducible feedback loop, not a collection of unverified
endorsements. A failure, limitation, or confusing step is useful evidence.

## Start here

| If you want to... | Use this path |
| --- | --- |
| Try Block without compiling | [External tester packet](TESTER-PACKET.md) |
| Submit a test result or independent review | [Validation guide](THIRD-PARTY-VALIDATION.md) and the [validation issue form](https://github.com/O-O1112/Block_lang/issues/new?template=validation_report.yml) |
| Volunteer for a test round | [Tester recruitment guide](TESTER-RECRUITMENT.md) and the [tester form](https://github.com/O-O1112/Block_lang/issues/new?template=tester_application.yml) |
| Fix code, documentation, or tooling | [Contributing guide](../CONTRIBUTING.md) |
| Ask a usage question | [GitHub Discussions](https://github.com/O-O1112/Block_lang/discussions) |
| Report a security vulnerability | [Security policy](../SECURITY.md); do not use a public issue |

## Tester lane: release-first, no build required

1. Download a release from the [official download page](../downloads.html) or
   the [GitHub Releases page](https://github.com/O-O1112/Block_lang/releases).
2. Read the [security policy](../SECURITY.md) and run only scripts you trust.
3. Start with `examples/native-control-flow.blk`, then choose one deeper test
   from the tester packet.
4. Record the Block edition, Windows version, host runtime versions, exact
   command, expected result, and observed result.
5. Submit the report through the validation form. Remove tokens, passwords,
   private paths, and personal data before posting.

For a repository checkout or a locally extracted release directory, the
standardized harness can be run with:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\Test-CommunityLab.ps1 `
  -EngineDirectory .\bin `
  -ReportPath .\community-test-report.json
```

The harness runs native checks on every machine. Python-to-Node coverage is
reported as `skipped` when either optional host runtime is unavailable; that is
an environment fact, not an engine failure.

## Contributor lane: fork, branch, pull request

Use this path for code, parser, runtime, installer, documentation, or editor
changes:

```powershell
git clone https://github.com/O-O1112/Block_lang.git
Set-Location Block_lang
git switch -c fix/short-description

# Make one focused change, then run the smallest relevant checks.
powershell -ExecutionPolicy Bypass -File .\tests\Test-CommunityLab.ps1 -EngineDirectory .\bin
git diff --check
git status --short
```

If engine code changed, run `build.ps1` before the smoke tests. If release
files, installer logic, or extensions changed, follow the full verification
steps in [`CONTRIBUTING.md`](../CONTRIBUTING.md). Open a pull request against
`main`; the Windows CI workflow is the shared verification environment.

Contributors should not include credentials, certificates, private keys,
unredacted logs, generated build output, or third-party code without a license.
Security issues must use the private reporting route in `SECURITY.md`.

## What the test report means

The harness writes a small JSON report when `-ReportPath` is supplied. It
contains test names, pass/fail/skip status, version output, and a short output
excerpt. It does not collect environment secrets or upload anything.

Keep maintainer smoke tests separate from external evidence:

- **Maintainer / CI result:** proves that the project build passed in a known
  environment.
- **External validation:** records what another person observed on their
  machine, including limitations.

Do not turn a successful local run into a claim of community adoption. The
project may quote or summarize an external report only with the permission
recorded in the validation form.

## Suggested test rotation

Each release test round should cover different risks instead of asking every
person to repeat the same command:

| Role | Focus |
| --- | --- |
| Native tester | Built-in syntax, errors, output, and file paths |
| Polyglot tester | Python/Node.js state transfer and runtime discovery |
| Installer tester | Clean install, path with spaces, edition selection, uninstall |
| Workflow tester | A small real automation, data, teaching, or AI-tooling task |
| Documentation tester | Follow the README from a clean directory without private help |

Three independent reports across at least two Windows/runtime environments are
enough for a first round. Repeated failures should become a regression test or
documentation issue before the next release.

## Community rules

- Give specific, reproducible feedback instead of promotional wording.
- Never require a star, follow, quote, or positive review in exchange for help.
- Do not mass-post the same message to unrelated communities.
- Disclose relevant human–AI collaboration when authorship is asked about.
- Respect the privacy of testers and do not publish their identity or logs
  without permission.
