# Installation

## Supported editions

- **Block Lite (`.blkl`)** — the smallest runtime surface for lightweight scripts.
- **Block Standard (`.blk`)** — the recommended general-purpose edition.
- **Block+ (`.blkp`)** — expanded runtime and tooling support.

The Windows installer is published as [`BlockSetup-v2.7.0.exe`](https://github.com/O-O1112/Block_lang/releases/download/v2.7.0/BlockSetup-v2.7.0.exe)
on the [official download page](https://o-o1112.github.io/Block_lang/downloads.html). The secure bootstrapper downloads
the selected official GitHub asset, verifies SHA-256, and detects optional
runtimes without invoking Winget or Chocolatey.

## Install and verify

1. Run the installer and choose an install directory.
2. Select an engine edition. Standard is the recommended default.
3. Select optional runtimes required by your scripts.
4. Open a new PowerShell or Command Prompt window.
5. Verify the installation:

   ```powershell
   block --version
   ```

The core engine can complete installation even when an optional runtime is not
present. Install missing runtimes from their official sources; the installer
never executes a package manager or a downloaded script.

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
