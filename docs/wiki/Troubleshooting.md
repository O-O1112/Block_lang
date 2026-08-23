# Troubleshooting

## `block` is not recognized

Open a new terminal after installation so the updated `PATH` is loaded. If it
still fails, run the installed executable using its full path and check the
install directory selected in the installer.

## The installer reports optional runtime failures

The engine and optional runtimes are separate. A failed Winget or Chocolatey
operation does not necessarily mean the core engine failed. Install the missing
runtime manually, ensure its command is on `PATH`, and run the installer again
if you want the runtime checklist refreshed.

## `Script file not found`

First inspect the safe search roots and candidates:

```powershell
block workspace show
block find main
```

For a project with `block.project.json`, run its entry from any child directory:

```powershell
block project root
block project run
```

If multiple scripts have the same name, use an absolute path. Quote paths
containing spaces:

```powershell
block-plus "C:\Projects\My Block\main.blkp"
```

Confirm the extension and filename are correct; Windows Explorer may hide the
real extension. Block does not scan the entire drive automatically.

## A language block cannot start

Check that the corresponding host runtime is installed and callable directly
from the same terminal. Then verify the opening and closing Block tags and the
edition-specific extension.

## State is missing in the next stage

Return only serializable values and keep the stage order explicit. Live handles
such as open files, sockets, and functions cannot cross the process boundary.

## Security concerns

Do not execute an untrusted `.blk`, `.blkl`, or `.blkp` file. For a suspected
security vulnerability, follow [SECURITY.md](../../SECURITY.md) rather than
publishing a proof of concept in an issue.
