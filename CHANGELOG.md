# Block Engine - Changelog

All notable changes to the Block execution engine will be documented in this file.

## [Unreleased] - v2.2.2

### CLI workspace and project discovery
- Added safe relative script resolution from the current directory, nearest
  `block.project.json`, and a configured workspace.
- Added `workspace show|set|clear`, `find`, and `project root|run` commands.
- Added ambiguity reporting instead of selecting an arbitrary same-named script.
- Added cross-directory path regression coverage and synchronized CLI/site/wiki
  documentation.

Core language and process-stability fixes remain part of the v2.2.2 gate and
must pass before a release tag is created.

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
