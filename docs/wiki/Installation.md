# Installation

## Supported editions

- **Block Lite (`.blkl`)** — the smallest runtime surface for lightweight scripts.
- **Block Standard (`.blk`)** — the recommended general-purpose edition.
- **Block+ (`.blkp`)** — expanded runtime and tooling support.

The Windows installer is published as [`BlockSetup-v2.7.5.exe`](https://github.com/O-O1112/Block_lang/releases/download/v2.7.5/BlockSetup-v2.7.5.exe)
on the [official download page](https://o-o1112.github.io/Block_lang/downloads.html). The secure bootstrapper downloads
the selected official GitHub asset, verifies SHA-256, and can offer selected
optional runtimes through an explicit, fixed WinGet allowlist.

## Install and verify

1. Run the installer and choose an install directory.
2. Select an engine edition. Standard is the recommended default.
3. Select optional runtimes required by your scripts. Missing selections are
   shown for confirmation before WinGet is called; uncheck them for a core-only
   installation.
4. Open a new PowerShell or Command Prompt window.
5. Verify the installation:

   ```powershell
   block --version
   ```

The core engine can complete installation even when an optional runtime is not
present. Runtime installation is limited to the package IDs displayed in the
confirmation dialog. The installer never executes a downloaded script,
Chocolatey command, or command supplied by a Block document.

## Run without changing directories

For a project created with `block project init`, Block discovers the nearest
`block.project.json` and its `entry` file from child directories:

```powershell
block project root
block project run
```

For several projects, configure a workspace once:

```powershell
block workspace set C:\Users\you\BlockProjects
block workspace show
block find my-script
```

The resolver checks explicit paths, the current project, and the configured
workspace. It does not scan the whole drive; ambiguous matches are reported so
you can choose an exact path.

## Runtime requirements

Block delegates language blocks to local runtimes; it does not replace them.
Install and verify the runtimes required by the selected tags, then confirm
that they are available on `PATH`. Never run scripts from an untrusted source:
language blocks execute native programs on the host machine.
