# Block Language Roadmap

This roadmap is a public planning document, not a promise that an item is already
implemented. Shipped behavior belongs in the [README](README.md),
[changelog](CHANGELOG.md), and release notes.

## Current: v2.7.0 stability and security foundation

The current release line focuses on making the core workflow usable and reviewable:

- Lite, Standard, and Plus engine editions;
- Windows installer and stable download aliases;
- explicit runtime blocks and serializable cross-runtime state;
- native Block control flow;
- explicit local imports and project discovery;
- Plus `fmt`, `check`, `doc`, and local server commands;
- VS Code and Acode integrations;
- smoke tests, release verification, checksums, security guidance, and versioned
  documentation.
- workspace-aware project discovery and deterministic native expression limits.
- read-only `doctor --full` reports and scheduled creator-side health checks;
- secure GitHub Release bootstrapper with SHA-256 verification and ZIP-slip
  protection; optional host runtimes are detected, never silently installed.

## Next: v2.8.0 developer workflow and observability

These are candidate improvements for the next minor release:

1. Add structured execution traces that redact secrets and local absolute paths.
2. Improve CLI diagnostics for runtime discovery, path quoting, state
   serialization, and edition mismatches.
3. Add focused conformance tests for parser, native control flow, imports, and
   cross-runtime state edge cases.
4. Add explicit capability profiles for file, network, process, and custom-runtime
   access without claiming to create an OS sandbox.
5. Keep the README, GitHub Pages site, Markdown Wiki, installer screen, and
   release manifest aligned to the same version.
6. Collect independent validation reports from users running real local workflows.

## Explore: community-driven directions

These ideas need design discussion and a clear maintenance owner before they become
committed milestones:

- capability profiles for file, network, process, and custom-runtime access;
- a more discoverable example index without silently executing remote code;
- richer diagnostics and editor navigation;
- additional host runtimes where the security and maintenance cost is justified;
- documentation translations that preserve the tested commands and limitations;
- a small conformance suite for parser, state, import, and edition behavior.

## Non-goals

Block is not intended to replace the languages it coordinates, bundle every host
runtime, or make arbitrary native code safe merely by placing it inside a Block
document. Security boundaries, runtime ownership, and explicit state transfer are
part of the project's design.

The 2.7.0 release does not include a third-party package loader or marketplace.
Reusable Block source remains explicit through reviewed local imports.

## Ways to help

- Run one of the examples and report the exact version, edition, OS, and runtime
  versions.
- Improve a documentation page with a reproducible command or correction.
- Add a small regression test for a failure you can reproduce.
- Contribute a focused runtime integration or editor improvement.
- Share a real workflow rather than a generic claim or benchmark.

Start with [CONTRIBUTING.md](CONTRIBUTING.md), [SUPPORT.md](SUPPORT.md), or the
[third-party validation process](docs/THIRD-PARTY-VALIDATION.md).
