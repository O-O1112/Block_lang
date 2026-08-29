param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$releaseVersion = (Get-Content -LiteralPath (Join-Path $RepositoryRoot 'VERSION') -Raw).Trim()
if ($releaseVersion -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid repository version: $releaseVersion" }
$sourcePath = Join-Path $RepositoryRoot 'Installer.cs'
$buildScript = Join-Path $RepositoryRoot 'build-installer.ps1'

if (-not (Test-Path -LiteralPath $sourcePath)) { throw 'Installer.cs is missing.' }
if (-not (Test-Path -LiteralPath $buildScript)) { throw 'build-installer.ps1 is missing.' }

$source = Get-Content -LiteralPath $sourcePath -Raw
$requiredMarkers = @(
    'ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12',
    'CreateGitHubRequest',
    'MaximumAutomaticRedirections = 5',
    'AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate',
    'release-assets.githubusercontent.com',
    'WebExceptionStatus.SecureChannelFailure',
    'WebExceptionStatus.TrustFailure',
    'Secure TLS 1.2 connection to '
)
foreach ($marker in $requiredMarkers) {
    if (-not $source.Contains($marker)) { throw "Installer transport marker is missing: $marker" }
}

foreach ($forbidden in @('SecurityProtocolType.Ssl3', 'ServerCertificateValidationCallback', 'TrustAllCert', 'Tls | SecurityProtocolType.Tls11')) {
    if ($source.Contains($forbidden)) { throw "Installer contains an unsafe TLS fallback: $forbidden" }
}

$requestFactoryUses = ([regex]::Matches($source, 'CreateGitHubRequest\(')).Count
if ($requestFactoryUses -lt 3) { throw 'Not every GitHub download path uses the hardened request factory.' }

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-installer-transport-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
try {
    & $buildScript -ReleaseDirectory $tempRoot -OutputDirectory $tempRoot -Version $releaseVersion
    if ($LASTEXITCODE -ne 0) { throw 'TLS-hardened installer failed to compile.' }
    $installer = Join-Path $tempRoot "BlockSetup-v$releaseVersion.exe"
    if (-not (Test-Path -LiteralPath $installer)) { throw 'TLS-hardened installer artifact is missing.' }
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($installer).FileVersion
    if ($version -ne "$releaseVersion.0") { throw "TLS-hardened installer has the wrong file version: $version" }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}

Write-Host 'Installer TLS transport checks passed.'
$global:LASTEXITCODE = 0
