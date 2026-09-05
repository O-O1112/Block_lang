# Security policy

## Supported versions

Security fixes are currently developed against the `main` branch and the
latest published 2.7.0 release. Older 2.2.x binaries are not supported for
security fixes; upgrade to the latest release before reporting a regression.

## Reporting a vulnerability

Please do not disclose an exploitable vulnerability in a public issue. Use a
private GitHub Security Advisory or contact the repository maintainer through
the [Block Language GitHub profile](https://github.com/O-O1112).

Include, when safe to share:

- affected version, edition, operating system, and runtime;
- a minimal reproduction or proof of concept;
- impact and the attacker capabilities required; and
- relevant logs with secrets and personal data removed.

The engine executes local language runtimes, so reports involving command
execution, path traversal, process isolation, state injection, installer
behavior, or secret exposure should include the exact invocation and file
layout used.

`NetworkBlocked` is a best-effort language-runtime guard, not an operating-
system security boundary. Never use it as the only isolation layer for
untrusted code; use a container, virtual machine, AppContainer-equivalent, or
firewall policy outside Block.

Please allow time for assessment and a fix before public disclosure. Do not
upload credentials, certificates, private keys, or malicious samples containing
real user data.
