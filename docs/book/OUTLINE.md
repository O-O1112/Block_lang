# Book Outline

## Working title

**Block Language in Practice: One File, Every Runtime**

## Reader promise

By the end of the book, a reader should be able to create a small `.blk` workflow,
understand how state crosses runtime boundaries, choose an edition, diagnose a failed
stage, and decide whether Block is appropriate for a local project.

The book must be honest about the boundary: Block coordinates host runtimes; it does
not make untrusted code safe, install every language automatically, or remove the need
to understand the programs being executed.

## Chapter map

### 0. Preface — Why glue code became the project

**Goal:** introduce the motivation without overselling the engine.

**Topics:**

- the repeated cost of temporary files, shell glue, and duplicated serialization;
- why a visible execution document is useful;
- what Block coordinates and what remains native to each host language;
- the security and runtime prerequisites readers should know before installing.

**Source:** [`00-preface.md`](00-preface.md), adapted from the Block introduction draft.

**Done when:** a new reader can explain Block in one sentence and name its two most
important limitations.

### 1. Visible boundaries — Make the pipeline inspectable

**Goal:** teach the mental model before teaching flags or features.

**Topics:**

- preparation, execution, state transfer, and presentation as separate stages;
- why explicit state is an interface rather than an accidental global;
- serializable values versus handles, sockets, functions, and circular references;
- a minimal Python-to-JavaScript example with observable input and output;
- deterministic tests before model calls or other variable services.

**Source:** [`01-visible-boundaries.md`](01-visible-boundaries.md) and
[`docs/devto-drafts/01-ai-state-boundaries.md`](../devto-drafts/01-ai-state-boundaries.md).

**Done when:** the reader can identify which stage changed when an output differs.

### 2. The first Block file — Install, run, and read the output

**Goal:** get from a clean installation to a useful result in under ten minutes.

**Topics:**

- choosing Lite, Standard, or Plus;
- installing the engine and required host runtimes;
- the smallest `.blkl`, `.blk`, and `.blkp` examples;
- reading startup, stage, and failure messages;
- where to find the official downloads, examples, and Wiki.

**Source:** `README.md`, `docs/wiki/Installation.md`, `examples/README.md`.

**Done when:** every command has been run on a clean Windows environment and its
expected output is recorded.

### 3. Native blocks and shared state

**Goal:** show how to compose native syntax without pretending the languages are the
same language.

**Topics:**

- block tags and matching closing tags;
- Python, JavaScript, Lua, PHP, SQL, and output blocks;
- state serialization and downstream visibility;
- naming conventions and avoiding accidental state collisions;
- HTML escaping and JSON output.

**Source:** `README.md`, `docs/wiki/Syntax.md`, `docs/wiki/Polyglot-State.md`,
`examples/hello-polyglot.blk`, `examples/local-data-pipeline.blk`.

**Done when:** examples work with the documented edition and fail clearly when a
required runtime is missing.

### 4. AI workflows that can be debugged

**Goal:** connect Block's visible state model to practical AI engineering.

**Topics:**

- explicit state contracts around inference;
- allowlisted tools, typed arguments, and approval for high-impact actions;
- a transparent retrieval baseline before adding a vector database;
- keeping secrets out of shared state and logs;
- separating retrieval failures from generation failures.

**Source:**
[`01-ai-state-boundaries.md`](../devto-drafts/01-ai-state-boundaries.md),
[`02-ai-tool-boundaries.md`](../devto-drafts/02-ai-tool-boundaries.md), and
[`03-debuggable-rag.md`](../devto-drafts/03-debuggable-rag.md).

**Done when:** every AI example has a deterministic test path that does not require a
provider key.

### 5. Editions, runtimes, and packaging

**Goal:** help readers select the smallest edition that fits their project.

**Topics:**

- Lite (`.blkl`), Standard (`.blk`), and Plus (`.blkp`);
- optional runtimes and why a skipped runtime is not necessarily an engine failure;
- editor extensions and the Acode plugin;
- installer behavior, release archives, and checksums;
- version pinning and upgrade notes.

**Source:** `docs/RELEASE-2.2.0.md`, the `gh-pages` website branch,
`verify-release.ps1`, and the extension READMEs.

**Done when:** a reader can tell whether a failure is caused by the installer, the
engine, or a missing host runtime.

### 6. Security, trust, and limits

**Goal:** make safe adoption part of the normal workflow.

**Topics:**

- Block executes host-language code and should be treated accordingly;
- path checks, import limits, process policy, and malformed block validation;
- what the engine's controls do and do not protect against;
- reviewing dependencies and avoiding untrusted scripts;
- reporting a security issue through `SECURITY.md`.

**Source:** `SECURITY.md`, `src/SecurityLimits.cs`, `src/ProcessSandbox.cs`,
`src/Parser.cs`, and `docs/wiki/Troubleshooting.md`.

**Done when:** the chapter contains a threat table with tested claims only, including
known non-goals.

### 7. Testing and troubleshooting polyglot programs

**Goal:** replace trial-and-error debugging with a repeatable diagnosis loop.

**Topics:**

- parser errors versus runtime errors;
- missing executable, non-zero exit code, and state-shape failures;
- minimal reproduction files;
- logging safe summaries instead of secrets;
- test commands and release verification;
- how to submit an actionable bug report.

**Source:** `tests/`, `verify-release.ps1`, `CONTRIBUTING.md`, and the validation
report template.

**Done when:** each troubleshooting entry includes symptom, likely layer, command to
run, and expected evidence.

### 8. Building the ecosystem

**Goal:** give readers a credible path from first script to contribution.

**Topics:**

- examples that teach one concept at a time;
- editor support and plugin packaging;
- independent validation and reproducible reports;
- documentation improvements, translations, and issue triage;
- roadmap discipline: distinguish implemented, experimental, and planned features.

**Source:** `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `docs/GROWTH.md`, and
`docs/THIRD-PARTY-VALIDATION.md`.

**Done when:** a reader can make a small contribution without private context.

## Appendices

- A. Block tag and file-extension reference
- B. State serialization checklist
- C. Runtime and installer checklist
- D. Minimal security review checklist
- E. Reproducible example index
- F. Glossary

## Editorial rules

1. Every code sample must name its required edition and host runtimes.
2. Every example must state whether it was tested, and on which version.
3. Never present an intention, roadmap item, or local-only observation as a shipped
   feature.
4. Keep security language precise; avoid words such as “safe” or “sandboxed” unless
   the exact boundary is documented and tested.
5. Prefer complete small examples over isolated syntax fragments.
6. Keep commands copyable and avoid machine-specific absolute paths.
