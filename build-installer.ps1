param(
    [string]$ReleaseDirectory = $PSScriptRoot,
    [string]$OutputDirectory = $ReleaseDirectory,
    [string]$Version = "",
    [string]$SigningCertificateThumbprint = ""
)

$ErrorActionPreference = 'Stop'
$ReleaseDirectory = [IO.Path]::GetFullPath($ReleaseDirectory)
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'VERSION') -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+(?:\.\d+)?$') { throw "Invalid release version: $Version" }
$compilerCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe')
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) { throw 'Microsoft .NET Framework C# compiler (csc.exe) not found.' }

$source = Join-Path $PSScriptRoot 'Installer.cs'
$icon = Join-Path $PSScriptRoot 'icon.ico'
if (-not (Test-Path -LiteralPath $source)) { throw "Installer source not found: $source" }
if (-not (Test-Path -LiteralPath $icon)) { throw "Installer icon not found: $icon" }

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$primary = Join-Path $OutputDirectory "BlockSetup-v$Version.exe"
$generatedDirectory = Join-Path ([IO.Path]::GetTempPath()) ('block-installer-version-' + [Guid]::NewGuid().ToString('N'))
$generatedVersionSource = Join-Path $generatedDirectory 'InstallerBuildVersion.g.cs'
$generatedAssemblySource = Join-Path $generatedDirectory 'InstallerAssemblyInfo.g.cs'
New-Item -ItemType Directory -Force -Path $generatedDirectory | Out-Null
[IO.File]::WriteAllText(
    $generatedVersionSource,
    "namespace BlockInstaller { internal static class InstallerBuildVersion { public const string Value = `"$Version`"; } }",
    (New-Object Text.UTF8Encoding($false))
)
# Windows assembly versions have exactly four numeric components. Keep a supplied
# fourth release component; add a zero revision only for legacy three-part tags.
$assemblyVersion = if (($Version -split '\.').Count -eq 3) { $Version + '.0' } else { $Version }
$assemblyInfo = 'using System.Reflection; ' +
    '[assembly: AssemblyTitle("Block Engine Secure Bootstrapper")] ' +
    '[assembly: AssemblyDescription("Downloads one selected official Block Engine release and verifies its SHA-256 digest before installation")] ' +
    '[assembly: AssemblyCompany("Block Language Project")] ' +
    '[assembly: AssemblyVersion("' + $assemblyVersion + '")] ' +
    '[assembly: AssemblyFileVersion("' + $assemblyVersion + '")] ' +
    '[assembly: AssemblyInformationalVersion("' + $Version + '")]'
[IO.File]::WriteAllText(
    $generatedAssemblySource,
    $assemblyInfo,
    (New-Object Text.UTF8Encoding($false))
)
$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:x86',
    ('/out:' + $primary),
    ('/win32icon:' + $icon),
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    '/reference:System.IO.Compression.dll',
    '/reference:System.IO.Compression.FileSystem.dll',
    '/reference:System.Web.Extensions.dll',
    $source,
    $generatedVersionSource,
    $generatedAssemblySource
)

try {
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
}
finally {
    if (Test-Path -LiteralPath $generatedDirectory) { Remove-Item -LiteralPath $generatedDirectory -Recurse -Force }
}

if (-not [string]::IsNullOrWhiteSpace($SigningCertificateThumbprint)) {
    $signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if (-not $signtool) { throw 'SigningCertificateThumbprint was supplied but signtool.exe was not found.' }
    & $signtool.Source sign /sha1 $SigningCertificateThumbprint /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $primary
    if ($LASTEXITCODE -ne 0) { throw 'Authenticode signing failed for the versioned installer.' }
}
$stableAlias = Join-Path $OutputDirectory 'BlockSetup.exe'
# Copy after signing so both published installer names remain byte-identical and
# users only need to verify one digest and one Authenticode signature.
Copy-Item -LiteralPath $primary -Destination $stableAlias -Force
Write-Host "Created $primary"
Write-Host "Updated $stableAlias"
