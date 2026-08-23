# Installation

## Supported editions

- **Block Lite (`.blkl`)** — the smallest runtime surface for lightweight scripts.
- **Block Standard (`.blk`)** — the recommended general-purpose edition.
- **Block+ (`.blkp`)** — expanded runtime and tooling support.

The Windows installer is published as [`BlockSetup-v2.2.2.exe`](https://github.com/O-O1112/Block_lang/releases/download/v2.2.2/BlockSetup-v2.2.2.exe)
on the [download page](../../downloads.html). Existing runtimes are detected
before optional runtime installation is attempted.

## Install and verify

1. Run the installer and choose an install directory.
2. Select an engine edition. Standard is the recommended default.
3. Select optional runtimes required by your scripts.
4. Open a new PowerShell or Command Prompt window.
5. Verify the installation:

   ```powershell
   block --version
   ```

The core engine can complete installation even when an optional runtime package
cannot be downloaded. The installer reports those optional failures so they
can be installed later and the installer can be run again.

## Run without changing directories

For a project created with `block ecosystem init`, Block discovers the nearest
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
