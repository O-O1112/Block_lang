param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin'),
    [switch]$SkipHash,
    [string]$Version = ""
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'VERSION') -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid build version: $Version" }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }

$sourceDirectory = Join-Path $PSScriptRoot 'src'
$sources = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.cs' -File |
    Where-Object Name -ne 'BlockVersion.cs' |
    Sort-Object Name |
    ForEach-Object FullName)
if ($sources.Count -eq 0) { throw "No C# sources found in $sourceDirectory" }

$generatedDirectory = Join-Path ([IO.Path]::GetTempPath()) ('block-version-' + [Guid]::NewGuid().ToString('N'))
$generatedVersionSource = Join-Path $generatedDirectory 'BlockVersion.g.cs'
New-Item -ItemType Directory -Force -Path $generatedDirectory | Out-Null
[IO.File]::WriteAllText(
    $generatedVersionSource,
    "namespace BlockEngine { internal static class BlockVersion { public const string Value = `"$Version`"; } }",
    (New-Object Text.UTF8Encoding($false))
)
$sources += $generatedVersionSource

$references = @(
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Web.dll',
    '/reference:System.Web.Extensions.dll',
    '/reference:System.Configuration.dll',
    '/reference:System.Drawing.dll'
)

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$editions = @(
    @{ Name = 'block.exe'; Define = $null },
    @{ Name = 'block-lite.exe'; Define = 'BLOCK_LITE' },
    @{ Name = 'block-plus.exe'; Define = 'BLOCK_PLUS' }
)

try {
    foreach ($edition in $editions) {
        $outputPath = Join-Path $OutputDirectory $edition.Name
        $arguments = @('/nologo', '/target:exe', '/platform:x86', ('/out:' + $outputPath))
        if ($edition.Define) { $arguments += '/define:' + $edition.Define }
        $arguments += $references
        $arguments += $sources
        & $compiler @arguments
        if ($LASTEXITCODE -ne 0) { throw "Build failed for $($edition.Name)" }
    }
}
finally {
    if (Test-Path -LiteralPath $generatedDirectory) { Remove-Item -LiteralPath $generatedDirectory -Recurse -Force }
}

if (-not $SkipHash) {
    $hashPath = Join-Path $OutputDirectory 'SHA256SUMS.txt'
    Get-ChildItem -LiteralPath $OutputDirectory -File -Filter '*.exe' |
        Sort-Object Name |
        ForEach-Object {
            $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            "$hash  $($_.Name)"
        } | Set-Content -LiteralPath $hashPath -Encoding ascii
}

Write-Host "Block Engine v$Version build completed."
