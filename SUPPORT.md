# Support

## Before asking for help

Please check the [README](README.md), the [examples](examples/README.md), and the
[Wiki](docs/wiki/README.md). Include the Block version and edition, Windows version,
host runtime versions, the smallest safe reproduction, and the relevant output with
secrets and private paths removed.

If you are joining a test round or want to contribute a fix, use the
[Community Lab](docs/COMMUNITY-LAB.md) first. It provides the standard test
commands and separates external validation from maintainer results.

## Where to ask

- **Bug in the engine, installer, website, or extensions:**
  [open a bug report](https://github.com/O-O1112/Block_lang/issues/new?template=bug_report.yml).
- **Feature or documentation idea:**
  [open a feature request](https://github.com/O-O1112/Block_lang/issues/new?template=feature_request.yml).
- **Independent test, integration, tutorial, or review:** follow the
  [third-party validation process](docs/THIRD-PARTY-VALIDATION.md).
- **Security vulnerability:** do not open a public issue. Follow
  [SECURITY.md](SECURITY.md).

## Common boundaries

Block invokes host language runtimes. A missing Python, Node.js, Lua, PHP, or other
runtime is normally an environment or installation issue, not evidence that the Block
parser is broken. The [troubleshooting guide](docs/wiki/Troubleshooting.md) explains
how to separate those layers.

## Response expectations

This is a maintainer-led open-source project. Response time is not guaranteed, and
support is provided on a best-effort basis. A clear, minimal reproduction gives an
issue the best chance of being understood and fixed.
