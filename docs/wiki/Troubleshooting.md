# Troubleshooting

## Reading Block diagnostics

Command failures use stable `BLKxxxx` codes. A diagnostic can include the
operation, file, line and column, source excerpt, technical detail, and a
specific repair hint. For example:

```text
error[BLK1001]: File not found
  operation: run
  file     : C:\Projects\missing.blk
  detail   : Could not find the requested Block document.
  hint     : Run 'block find <name>', quote paths that contain spaces, or provide an absolute path.
```

Include the diagnostic code when searching or reporting a problem. Internal
stack traces are hidden by default. Maintainers may set `BLOCK_DEBUG=1` for one
reproduction to expose the stack trace, but should remove private paths and
secrets before sharing the output.

Common groups are:

| Code range | Meaning |
| --- | --- |
| `BLK0001`–`BLK0002` | Invalid command usage or input |
| `BLK1001`–`BLK1301` | Files, syntax, imports, or package references |
| `BLK2001`–`BLK2101` | Safety-policy rejection |
| `BLK4001`–`BLK4002` | Timeout or missing host runtime |
| `BLK9001` | Unexpected internal failure |

## `block` is not recognized

Open a new terminal after installation so the updated `PATH` is loaded. If it
still fails, run the installed executable using its full path and check the
install directory selected in the installer.

## The installer reports optional runtime failures

The engine and optional runtimes are separate. The v2.2.6.5 secure installer only
detects optional runtimes; it never runs Winget, Chocolatey, PowerShell, or a
downloaded script. Install a missing runtime from its official source, ensure
its command is on `PATH`, and reopen the terminal.

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
