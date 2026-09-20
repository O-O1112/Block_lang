# Block examples

These examples are intentionally small and copy-ready. They are useful for evaluating Block, sharing the project with a teammate, or checking that the local runtimes are available.

## Run one in three steps

1. Install [Block Engine](https://o-o1112.github.io/Block_lang/downloads.html) and the runtimes used by the example.
2. Open a PowerShell prompt anywhere after installation.
3. Run the matching command; Block can resolve files from the configured
   workspace or the current project:

```powershell
block .\examples\hello-polyglot.blk
block .\examples\local-data-pipeline.blk
block .\examples\native-control-flow.blk
block .\examples\native-language-core.blk
block-plus .\examples\polyglot-15-languages.blk
```

To avoid changing directories, set the repository as a temporary workspace:

```powershell
$env:BLOCK_WORKSPACE = (Resolve-Path .).Path
block find native-control-flow
block run native-control-flow.blk
```

The first two examples use Python and Node.js. `native-control-flow.blk` uses the built-in Block syntax and does not need another runtime.

## Examples at a glance

| File | What it demonstrates | Edition |
| --- | --- | --- |
| [`hello-polyglot.blk`](hello-polyglot.blk) | Python creates state, JavaScript consumes it, and HTML presents the result | Standard |
| [`local-data-pipeline.blk`](local-data-pipeline.blk) | A small local data workflow with Python preparation and JavaScript reporting | Standard |
| [`native-control-flow.blk`](native-control-flow.blk) | Variables, conditions, and output using Block-native syntax | All editions |
| [`native-language-core.blk`](native-language-core.blk) | Collections, indexing, functions, built-ins, and loop control | All editions |
| [`polyglot-15-languages.blk`](polyglot-15-languages.blk) | One document coordinating fifteen host-language stages and an HTML report | Block+ |

Block delegates each language block to the corresponding local runtime. Install Python or Node.js separately when an example needs it, and never run scripts you do not trust.

## 15-language showcase requirements

`polyglot-15-languages.blk` uses Python, JavaScript, PHP, Ruby, Lua, PowerShell,
SQLite, C++, Go, Rust, Dart, Zig, Perl, Bash, and R. Block+ does not bundle
these host runtimes. Install only the runtimes you need from their official
publishers, confirm they are available on `PATH`, and then run:

```powershell
block-plus .\examples\polyglot-15-languages.blk
```

To validate the complete document without launching any host runtime, use:

```powershell
block-plus check .\examples\polyglot-15-languages.blk
```
