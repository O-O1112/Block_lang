# Block Language Roadmap

This roadmap is a public planning document, not a promise that an item is already
implemented. Shipped behavior belongs in the [README](README.md),
[changelog](CHANGELOG.md), and release notes.

## Current: v2.2.5 ecosystem and safety foundation

The current release line focuses on making the core workflow usable and reviewable:

- Lite, Standard, and Plus engine editions;
- Windows installer and stable download aliases;
- explicit runtime blocks and serializable cross-runtime state;
- native Block control flow;
- local imports and the local-first package layout;
- Plus `fmt`, `check`, `doc`, and local server commands;
- VS Code and Acode integrations;
- smoke tests, release verification, checksums, security guidance, and versioned
  documentation.
- workspace-aware project discovery and deterministic native expression limits.
- digest-pinned package registry with five official starter packages;
- read-only `doctor --full` reports and scheduled creator-side health checks;
- secure GitHub Release bootstrapper with SHA-256 verification and ZIP-slip
  protection; optional host runtimes are detected, never silently installed.

## Next: v2.3.0 ecosystem depth and first-run friction

These are the highest-value improvements for the next patch release:

1. Make native logical expressions, loop limits, process timeouts, and state
   transfer failures deterministic and regression-tested.
2. Let the CLI resolve scripts through project roots and configured workspaces
   without requiring repeated directory changes.
3. Make the first five minutes more reproducible with more copy-ready examples and
   clearer runtime diagnostics.
4. Publish every stable build as a GitHub Release with release notes, checksums,
   installer links, and extension assets.
5. Improve error messages for missing host runtimes, path quoting, state
   serialization, and edition mismatches.
6. Add regression coverage for installer behavior, runtime discovery, imports,
   and cross-runtime state edge cases.
7. Keep the README, visual documentation site, Markdown Wiki, installer screen,
   and release manifest aligned to the same version.
8. Collect independent validation reports from users running real local workflows.

## Explore: community-driven directions

These ideas need design discussion and a clear maintenance owner before they become
committed milestones:

- capability profiles for file, network, process, and custom-runtime access;
- a more discoverable package and example index without silently executing remote
  code;
- richer diagnostics and editor navigation;
- additional host runtimes where the security and maintenance cost is justified;
- documentation translations that preserve the tested commands and limitations;
- a small conformance suite for parser, state, import, and edition behavior.

## Non-goals

Block is not intended to replace the languages it coordinates, bundle every host
runtime, or make arbitrary native code safe merely by placing it inside a Block
document. Security boundaries, runtime ownership, and explicit state transfer are
part of the project's design.

## Ways to help

- Run one of the examples and report the exact version, edition, OS, and runtime
  versions.
- Improve a documentation page with a reproducible command or correction.
- Add a small regression test for a failure you can reproduce.
- Contribute a focused runtime integration or editor improvement.
- Share a real workflow rather than a generic claim or benchmark.

Start with [CONTRIBUTING.md](CONTRIBUTING.md), [SUPPORT.md](SUPPORT.md), or the
[third-party validation process](docs/THIRD-PARTY-VALIDATION.md).
