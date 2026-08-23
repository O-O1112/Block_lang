param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin')
)

$ErrorActionPreference = 'Stop'
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$failures = New-Object System.Collections.Generic.List[string]
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-path-tests-' + [Guid]::NewGuid().ToString('N'))
$oldWorkspace = $env:BLOCK_WORKSPACE
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

function Invoke-Block([string]$Executable, [string[]]$Arguments, [string]$WorkingDirectory) {
    $path = Join-Path $EngineDirectory $Executable
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing test executable: $path" }
    Push-Location -LiteralPath $WorkingDirectory
    try {
        $output = (& $path @Arguments 2>&1 | Out-String).Trim()
        [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    } finally {
        Pop-Location
    }
}

function Assert-Condition([bool]$Condition, [string]$Message) {
    if (-not $Condition) { $failures.Add($Message) }
}

try {
    $workspace = Join-Path $tempRoot 'workspace'
    $project = Join-Path $workspace 'demo-project'
    $nested = Join-Path $project 'src\nested'
    New-Item -ItemType Directory -Force -Path $nested | Out-Null
    @'
{
  "name": "path-test-project",
  "version": "0.1.0",
  "engine": "standard",
  "entry": "main.blk",
  "dependencies": {}
}
'@ | Set-Content -LiteralPath (Join-Path $project 'block.project.json') -Encoding UTF8
    @'
message = "path-ok"
print(message)
'@ | Set-Content -LiteralPath (Join-Path $project 'main.blk') -Encoding UTF8

    $env:BLOCK_WORKSPACE = $workspace

    $root = Invoke-Block 'block.exe' @('project', 'root') $nested
    Assert-Condition ($root.ExitCode -eq 0) "project root failed: $($root.Output)"
    Assert-Condition ($root.Output -match [regex]::Escape($project)) "project root resolved incorrectly: $($root.Output)"

    $check = Invoke-Block 'block.exe' @('check', 'main.blk') $nested
    Assert-Condition ($check.ExitCode -eq 0) "cross-directory check failed: $($check.Output)"

    $run = Invoke-Block 'block.exe' @('project', 'run') $nested
    Assert-Condition ($run.ExitCode -eq 0) "project run failed: $($run.Output)"
    Assert-Condition ($run.Output -match 'path-ok') "project entry was not executed: $($run.Output)"

    $direct = Invoke-Block 'block.exe' @('run', 'main.blk') $nested
    Assert-Condition ($direct.ExitCode -eq 0) "smart relative run failed: $($direct.Output)"
    Assert-Condition ($direct.Output -match 'path-ok') "smart relative run did not find the project file: $($direct.Output)"

    $find = Invoke-Block 'block.exe' @('find', 'main') $workspace
    Assert-Condition ($find.ExitCode -eq 0) "find failed: $($find.Output)"
    Assert-Condition ($find.Output -match [regex]::Escape((Join-Path $project 'main.blk'))) "find did not list main.blk: $($find.Output)"
} catch {
    $failures.Add($_.Exception.Message)
} finally {
    if ($null -eq $oldWorkspace) { Remove-Item Env:BLOCK_WORKSPACE -ErrorAction SilentlyContinue }
    else { $env:BLOCK_WORKSPACE = $oldWorkspace }
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Block path resolution tests passed.'
