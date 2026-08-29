# Block CLI workspace and project discovery

Block resolves relative script paths without requiring the terminal to be
opened in the same directory as the script.

## Search order

For a relative path, the engine checks:

1. The current working directory.
2. The nearest parent directory containing `block.project.json`.
3. The configured workspace root.
4. The direct child project folders of that workspace.

Block never performs an unrestricted drive-wide search. If more than one file
matches, the command fails with the candidates so the user can choose safely.

## Project entry points

Initialize a project once:

```powershell
block project init C:\Users\you\BlockProjects\demo demo
```

Then run it from the project root or any child directory:

```powershell
block project root
block project run
block run main.blk
```

The entry is read from `block.project.json`; it defaults to `main.blk`.

## Workspace root

Use a workspace when several projects live under one directory:

```powershell
block workspace set C:\Users\you\BlockProjects
block workspace show
block find demo
```

The setting is stored in the normal Block configuration file. It can be
removed with `block workspace clear`. The `BLOCK_WORKSPACE` environment
variable is also accepted for temporary or CI use and takes effect when no
configured workspace is set.

## Diagnostics

```powershell
block find main
block project root C:\Users\you\BlockProjects\demo
block config show
block doctor
```

Use an absolute path whenever a workspace contains multiple scripts with the
same name.
