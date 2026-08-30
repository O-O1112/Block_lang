# Block Engine - Changelog

All notable changes to the Block execution engine will be documented in this file.

## [2.2.6.5] - 2026-08-30

- Added four-component release-version support across the engine build, installer,
  release verifier, extension packages, GitHub Release workflow, and regression
  tests.
- Hardened the Windows installer trust boundary: it no longer terminates running
  processes, embeds an unused icon resource, changes `PATH`, or registers file
  associations unless the user explicitly selects the corresponding option.
- Made install and uninstall state ownership explicit under
  `HKCU\Software\BlockLanguage\BlockEngine`, with a compatibility read of the
  previous location for existing users.
- Added optional Authenticode verification for every Windows executable in a
  signed release, plus a published code-signing policy and clearer trust guidance.
- Kept official download links release-bound so web downloads resolve to the
  exact GitHub Release artifact rather than a mutable site copy.

## [2.2.6] - 2026-08-29

- Removed the third-party package loader, remote registry, package CLI, starter
  packages, and website marketplace. Project discovery and explicit local file
  imports remain supported.
- Fixed Windows interpreter stages being falsely reported as timed out when the
  .NET Framework `Process.Exited` event races with a short-lived Node.js process.
- Fixed GitHub downloads on older .NET Framework installations by requiring
  TLS 1.2 and reporting actionable TLS, DNS, proxy, and timeout failures.
- Accepted GitHub's exact `release-assets.githubusercontent.com` redirect host
  while continuing to reject non-GitHub and non-HTTPS download endpoints.
- Made project-relative imports work when a project is launched from another
  directory, while retaining project/sandbox boundaries, reparse-point checks,
  file-count limits, byte limits, circular-import checks, and Block-only source
  extensions.
- Guaranteed restoration of the terminal cursor and colors when the startup
  animation encounters an unsupported or redirected console.
- Removed all automatic Winget runtime installation from Block+; missing host
  runtimes now produce an actionable error and must come from their official
  publishers.
- Rejected unknown runtime tags before process launch and required
  `AllowCustomDefinitions=true` for both inline and global custom runtimes.
- Added size limits for configuration, project manifests, custom-runtime
  registries, and runtime state files; invalid state output now fails explicitly
  instead of silently leaking stale values into later stages.
- Made configuration and project-manifest fallbacks preserve the previous valid
  file if an atomic replacement is unavailable or interrupted.
- Bounded the no-argument startup animation, improved local-server shutdown and
  accept-loop diagnostics, and restricted localhost CORS to HTTP(S) origins.
- Corrected the published Windows architecture metadata and added version,
  repository-integrity, deployment-security, and signing checks.
- Standardized CLI failures as stable `BLKxxxx` diagnostics with operation,
  source location, context, and actionable repair hints while hiding internal
  stack traces unless `BLOCK_DEBUG=1` is explicitly enabled.
- Added context-aware HTML template escaping and rejection of executable
  attribute, script/style, and unsafe URL substitutions.
- Corrected JSON rendering for booleans, numbers, lists, maps, and null values.
- Preserved nested JavaScript mutations when returning shared state to later
  Block stages.
- Hardened the advisory Python/Node.js network guard and documented that it is
  not an operating-system sandbox.
- Added the stable `ast` JSON tooling API with source spans and structured
  diagnostics.
- Made API startup failures return a non-zero exit code and made requested
  engine-edition mismatches explicit.
- Removed unsupported Acode runtime controls and stopped persisting API tokens
  in localStorage.
- Made release versions flow into engines, installer, VSIX, and Acode packages
  from one build parameter; strengthened archive verification and added GitHub
  build-provenance attestations.
- Added security, API, Community Lab, and arbitrary-version release regression
  tests to CI.

## [2.2.5] - 2026-08-27

### Package registry
- Added the official digest-pinned `registry/index.json` catalog.
- Added `pkg search`, `pkg info`, `pkg install <name> --remote`, `pkg verify`,
  and `pkg remove` for Standard and Plus.
- Added five reviewable starter packages: Octopus, Block Web, Gblock:D, Block
  Work, and Drawing.

### Health and installer safety
- Added read-only `doctor --full` repository health reports and a scheduled
  GitHub Actions workflow for creator-side daily checks.
- Rebuilt the Windows installer as an official GitHub Release bootstrapper with
  HTTPS host allowlisting, SHA-256 verification, ZIP-slip protection, atomic
  deployment, version metadata, and no package-manager or downloaded-script
  execution.
- Optional runtimes are now detected and reported only; they are not silently
  installed by the setup program.
- Added release and package-registry regression coverage and updated the website
  marketplace, docs, AI knowledge pack, and extension metadata.

## [2.2.2] - 2026-08-23

### CLI workspace and project discovery
- Added safe relative script resolution from the current directory, nearest
  `block.project.json`, and a configured workspace.
- Added `workspace show|set|clear`, `find`, and `project root|run` commands.
- Added ambiguity reporting instead of selecting an arbitrary same-named script.
- Added cross-directory path regression coverage and synchronized CLI/site/wiki
  documentation.

### Core stability
- Fixed native logical short-circuit parsing while still consuming skipped
  expressions safely.
- Replaced silent `range()` truncation with an explicit 10,000-item limit error.
- Added a Windows CI path-resolution gate and expected-negative-test handling.

The v2.2.2 release is built and verified by the GitHub Windows runner before
the release tag is published.

## [2.2.0] - 2026-08-18

### 🚀 Features & Enhancements

#### Cross-Runtime State Synchronization Fixes
- **Fixed** state bridge protocol serialization issues when passing complex objects between Python and JavaScript
- **Improved** null/undefined handling across language boundaries
- **Added** automatic type coercion for numeric edge cases (NaN, Infinity)

#### Parser Robustness Enhancement
- **Fixed** edge case where nested block tags were incorrectly parsed
- **Improved** error recovery in malformed code blocks
- **Enhanced** line number tracking for more accurate error reporting

#### Native Block Language Core
- **Added** `elif`, `break`, and `continue` to native Block control flow
- **Added** list and dictionary literals with safe indexing and assignment
- **Added** `.length` / `.count` members and deterministic collection/string built-ins
- **Added** conversion, inspection, membership, key/value, and sum built-ins
- **Fixed** function-local assignments leaking into the shared state
- **Fixed** identifiers such as `printer` being misread as malformed `print` calls
- **Added** regression coverage for native programs that run without another language runtime

#### IDE Plugin Compatibility Update
- **Updated** VS Code extension syntax highlighting for v2.2 block syntax
- **Fixed** Acode mobile editor serialization issues
- **Added** IntelliSense support for state variable completions

#### Security Patch & Runtime Isolation
- **Patched** potential sandbox escape via circular import loops
- **Hardened** file access restrictions in import statements
- **Fixed** timeout enforcement in long-running blocks

#### Performance Optimization - State Bridge
- **Optimized** serialization performance by 35% for large arrays and objects
- **Reduced** memory overhead during cross-runtime state transfers
- **Fixed** memory leak in state cache eviction logic

### 📦 Edition Updates

#### Block Lite v2.2.0 - Reduced Footprint & Improved Module Resolution
- Installer footprint reduced to 17 MB
- **Fixed** Python module resolution for relative imports
- **Improved** HTML output rendering performance
- Better error messages for missing runtime dependencies

#### Block Standard v2.2.0 - Serialization Protocol Enhancement
- Serialization protocol upgraded to v2.2
- **Added** PHP 8.3 runtime support
- **Added** Lua 5.4 integration improvements
- **Fixed** SQLite transaction handling across state boundaries
- Enhanced documentation for module imports

#### Block Plus v2.2.0 - Extended Runtime Matrix
- **Added** Rust native compilation integration
- **Added** Go module imports support
- **Added** Zig language experimental support
- **Improved** custom runtime definition validation
- **Fixed** WinGet assisted setup configuration

### 🔧 Tooling & CLI

#### CLI Diagnostics and Explicit Execution
- **Added** `run <file>` as an explicit execution form while preserving the original file-only form
- **Added** `check <file>` to Lite, Standard, and Plus editions
- **Added** read-only `info`, `capabilities`, `runtimes`, and `doctor` commands
- **Added** `config show` and `config path` for non-interactive configuration inspection
- **Added** `project` as an alias for the Standard and Plus local ecosystem commands

#### Installer: Fixed PATH Environment Propagation
- **Fixed** Windows PATH environment variable not being set correctly
- **Improved** installation detection on fresh systems
- Added system restart recommendation check

#### VS Code Extension: Enhanced Syntax Highlighting
- **Added** highlighting for state variable interpolation
- **Improved** color theme compatibility
- **Fixed** folding markers for nested blocks
- Enhanced debugging integration

#### Acode Plugin: Mobile Editor Support
- **Added** real-time execution feedback
- **Fixed** touch keyboard interaction issues
- Improved syntax highlighting for mobile screens
- Added execution timeout warnings

### 📚 Documentation & Guides

#### Documentation Structure Consolidation
- Consolidated README variants into single comprehensive guide
- **Improved** architecture documentation clarity
- Added quick-start guides for each edition
- Enhanced polyglot workflow examples
- Better error message reference documentation

### 🐛 Bug Fixes

- Fixed crash when importing large `.blk` files (>10MB)
- Fixed race condition in concurrent state access
- Fixed incorrect error line numbers in nested imports
- Fixed HTML templating with special characters
- Fixed JSON output with circular references
- Fixed module caching not respecting file changes

### ⚠️ Known Issues

- Custom runtime definitions with symlinks may not work on Windows
- Very large state objects (>100MB) may cause memory pressure
- Circular cross-imports still rejected (by design, for safety)

### 🔄 Deprecations

- Block v1.x installation no longer supported
- Legacy `.blkx` format no longer recognized

---

## [2.1.0] - Previous Release

See git history for details on previous versions.

---

## Contributing

To report issues or suggest improvements, visit:
https://github.com/O-O1112/Block_lang/issues

See [CONTRIBUTING.md](CONTRIBUTING.md) for the development workflow and
[SECURITY.md](SECURITY.md) for private vulnerability reports.
