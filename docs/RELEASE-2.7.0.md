# Block Engine v2.7.0

Version 2.7.0 is the stability and security-hardening release for the local-first
Block engine. It keeps the existing three-edition model and focuses on making
failure behavior bounded, reviewable, and easier to diagnose.

## What changed

- Added Windows Job Object memory limits for child runtimes: 512 MiB per child
  process and 1 GiB per execution job, in addition to the existing process-tree
  lifetime and active-process limits.
- Moved API request admission control ahead of thread-pool scheduling. Excess
  requests now receive HTTP 429 immediately instead of accumulating unbounded
  pending work.
- Added constant-work session-token comparison for the local API and regenerated
  weak configured tokens that are shorter than 32 characters.
- Added response hardening headers (`nosniff`, `no-store`, and `no-referrer`) to
  local API responses.
- Synchronized the engine, installer, VS Code extension, Acode package, release
  workflow, citation metadata, README, security policy, AI knowledge pack, and
  release manifest to v2.7.0.
- Expanded regression assertions for process limits, API admission control,
  token handling, and version consistency.
- Kept the website on GitHub Pages and the legacy Cloudflare endpoint as a
  redirect only; website source is not reintroduced into the engine branch.

## Security boundary

Block still launches host language runtimes. Network blocking is an advisory
runtime guard, not an operating-system sandbox. Do not execute untrusted Block
documents merely because these limits exist; use a container, virtual machine,
Windows sandbox, or a separately managed firewall policy for hostile input.

The API server remains loopback-only and requires the `X-Api-Token` header for
all non-OPTIONS endpoints. It is intended for local tooling, not public
internet exposure.

## Verification contract

The release must pass the Windows build, engine smoke, CLI, native-language,
native-interpreter, API, installer transport, health-hardening, repository
integrity, and version-consistency checks before publication. Release assets are
generated from the tag and verified with `SHA256SUMS.txt`.

## Upgrade notes

Existing configuration files are preserved. New installations retain the safe
defaults: PowerShell and custom language definitions are disabled, and the
advisory network guard is enabled. A configured API token shorter than 32
characters is replaced with a fresh session token when a local server starts.

The host runtimes remain separately managed. Block does not install Python,
Node.js, PHP, Ruby, Lua, SQL, or other runtimes automatically.
