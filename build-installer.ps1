param(
    [string]$ReleaseDirectory = $PSScriptRoot,
    [string]$OutputDirectory = $ReleaseDirectory,
    [string]$Version = ""
)

$ErrorActionPreference = 'Stop'
$ReleaseDirectory = [IO.Path]::GetFullPath($ReleaseDirectory)
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'VERSION') -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid release version: $Version" }
$compilerCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe')
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) { throw 'Microsoft .NET Framework C# compiler (csc.exe) not found.' }

$source = Join-Path $PSScriptRoot 'Installer.cs'
$icon = Join-Path $PSScriptRoot 'icon.ico'
$required = @('block.zip', 'block-lite.zip', 'block-plus.zip')
foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $ReleaseDirectory $name))) {
        throw "Missing installer resource: $(Join-Path $ReleaseDirectory $name)"
    }
}
if (-not (Test-Path -LiteralPath $source)) { throw "Installer source not found: $source" }
if (-not (Test-Path -LiteralPath $icon)) { throw "Installer icon not found: $icon" }

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$primary = Join-Path $OutputDirectory "BlockSetup-v$Version.exe"
$generatedDirectory = Join-Path ([IO.Path]::GetTempPath()) ('block-installer-version-' + [Guid]::NewGuid().ToString('N'))
$generatedVersionSource = Join-Path $generatedDirectory 'InstallerBuildVersion.g.cs'
New-Item -ItemType Directory -Force -Path $generatedDirectory | Out-Null
[IO.File]::WriteAllText(
    $generatedVersionSource,
    "namespace BlockInstaller { internal static class InstallerBuildVersion { public const string Value = `"$Version`"; } }",
    (New-Object Text.UTF8Encoding($false))
)
$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:x86',
    ('/out:' + $primary),
    ('/win32icon:' + $icon),
    ('/resource:' + (Join-Path $ReleaseDirectory 'block.zip') + ',block.zip'),
    ('/resource:' + (Join-Path $ReleaseDirectory 'block-lite.zip') + ',block-lite.zip'),
    ('/resource:' + (Join-Path $ReleaseDirectory 'block-plus.zip') + ',block-plus.zip'),
    ('/resource:' + $icon + ',icon.ico'),
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    '/reference:System.IO.Compression.dll',
    '/reference:System.IO.Compression.FileSystem.dll',
    $source,
    $generatedVersionSource
)

try {
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
}
finally {
    if (Test-Path -LiteralPath $generatedDirectory) { Remove-Item -LiteralPath $generatedDirectory -Recurse -Force }
}

$stableAlias = Join-Path $OutputDirectory 'BlockSetup.exe'
Copy-Item -LiteralPath $primary -Destination $stableAlias -Force
Write-Host "Created $primary"
Write-Host "Updated $stableAlias"
