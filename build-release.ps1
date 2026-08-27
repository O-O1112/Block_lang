param(
    [string]$ReleaseDirectory = $PSScriptRoot,
    [switch]$SkipEngineBuild,
    [switch]$SkipExecutableExecution,
    [string]$Version = "",
    [string]$SigningCertificateThumbprint = ""
)

$ErrorActionPreference = 'Stop'
$ReleaseDirectory = [IO.Path]::GetFullPath($ReleaseDirectory)
$root = [IO.Path]::GetFullPath($PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid release version: $Version" }
$powershell = (Get-Command powershell.exe -ErrorAction Stop).Source
New-Item -ItemType Directory -Force -Path $ReleaseDirectory | Out-Null

function Invoke-ReleaseStep([string]$Script, [string[]]$Arguments) {
    & $powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root $Script) @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Release step failed: $Script" }
}

if (-not $SkipEngineBuild) {
    Invoke-ReleaseStep 'build.ps1' @('-OutputDirectory', $ReleaseDirectory, '-SkipHash', '-Version', $Version)
}
Invoke-ReleaseStep 'package-engine.ps1' @('-EngineDirectory', $ReleaseDirectory, '-OutputDirectory', $ReleaseDirectory)
Invoke-ReleaseStep 'package-extensions.ps1' @('-OutputDirectory', $ReleaseDirectory, '-Version', $Version)
$installerArguments = @('-ReleaseDirectory', $ReleaseDirectory, '-OutputDirectory', $ReleaseDirectory, '-Version', $Version)
if (-not [string]::IsNullOrWhiteSpace($SigningCertificateThumbprint)) {
    $installerArguments += @('-SigningCertificateThumbprint', $SigningCertificateThumbprint)
}
Invoke-ReleaseStep 'build-installer.ps1' $installerArguments

$artifacts = @(
    'block.exe', 'block-lite.exe', 'block-plus.exe',
    'block.zip', 'block-lite.zip', 'block-plus.zip',
    "BlockSetup-v$Version.exe", 'BlockSetup.exe',
    "block-language-$Version.vsix", "acode-plugin-block-$Version.zip"
)
$hashFile = Join-Path $ReleaseDirectory 'SHA256SUMS.txt'
$lines = foreach ($name in $artifacts) {
    $path = Join-Path $ReleaseDirectory $name
    if (-not (Test-Path -LiteralPath $path)) { throw "Release artifact missing: $path" }
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash $name"
}
$hashText = ($lines -join "`n") + "`n"
[IO.File]::WriteAllText($hashFile, $hashText, [Text.Encoding]::ASCII)
$verifyArguments = @('-ReleaseDirectory', $ReleaseDirectory, '-Version', $Version)
if ($SkipExecutableExecution) { $verifyArguments += '-SkipExecutableExecution' }
if (-not [string]::IsNullOrWhiteSpace($SigningCertificateThumbprint)) { $verifyArguments += '-RequireSignedInstaller' }
Invoke-ReleaseStep 'verify-release.ps1' $verifyArguments
Write-Host "Block Engine v$Version release completed in $ReleaseDirectory"
