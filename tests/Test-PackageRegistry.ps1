param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin')
)

$ErrorActionPreference = 'Stop'
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$engine = Join-Path $EngineDirectory 'block.exe'
if (-not (Test-Path -LiteralPath $engine)) { throw "Engine not found: $engine" }

$root = [IO.Path]::GetFullPath((Join-Path (Get-Location) ('.blocklang-package-test-' + [Guid]::NewGuid().ToString('N'))))
New-Item -ItemType Directory -Force -Path $root | Out-Null
try {
    & $engine ecosystem init $root package-test | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'ecosystem init failed' }

    $package = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\registry\packages\drawing'))
    & $engine ecosystem add $package $root | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'local package add failed' }

    & $engine pkg verify $root | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'package verification failed' }

    $script = Join-Path $root 'use-drawing.blk'
    [IO.File]::WriteAllText($script, '<use package="drawing" />' + [Environment]::NewLine + 'print(drawing_point_count([1, 2, 3]))' + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
    $output = (& $engine run $script 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0 -or $output -notmatch '(?m)^3\s*$') { throw "package import execution failed: $output" }
    Write-Host 'Package registry tests passed.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
