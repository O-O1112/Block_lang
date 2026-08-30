param(
    [string]$ReleaseDirectory = $PSScriptRoot,
    [switch]$SkipExecutableExecution,
    [string]$Version = "",
    [switch]$RequireSignedRelease
)

$ErrorActionPreference = 'Stop'
$ReleaseDirectory = [IO.Path]::GetFullPath($ReleaseDirectory)
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'VERSION') -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid release version: $Version" }
$bin = Join-Path $ReleaseDirectory 'bin'
if (Test-Path -LiteralPath (Join-Path $ReleaseDirectory 'block.exe')) {
    $bin = $ReleaseDirectory
}
$failures = New-Object System.Collections.Generic.List[string]
$coreNames = @('block.exe', 'block-lite.exe', 'block-plus.exe')
$requiredArtifacts = @(
    'block.exe', 'block-lite.exe', 'block-plus.exe',
    'block.zip', 'block-lite.zip', 'block-plus.zip',
    "BlockSetup-v$Version.exe", 'BlockSetup.exe',
    "block-language-$Version.vsix", "acode-plugin-block-$Version.zip"
)
$expectedVersions = @{
    'block.exe' = "Block Language Engine v$Version (Standard Edition)"
    'block-lite.exe' = "Block Lite Engine v$Version (Lite Edition)"
    'block-plus.exe' = "Block+ Engine v$Version (Flagship Edition)"
}

$installerSource = Join-Path $PSScriptRoot 'Installer.cs'
if (-not (Test-Path -LiteralPath $installerSource)) {
    $failures.Add('missing installer source')
} else {
    $installerText = Get-Content -LiteralPath $installerSource -Raw
    foreach ($marker in @('OfficialApiBase', 'OfficialRepository', 'ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12', 'AllowAutoRedirect = true', 'ValidateReleaseAssetUri', 'ContainsReparsePoint', 'Extracted release executable exceeds', 'SHA-256 verification failed', 'ExtractVerifiedArchive', 'ValidateResponseUri')) {
        if ($installerText -notmatch [regex]::Escape($marker)) { $failures.Add("secure installer marker missing: $marker") }
    }
    foreach ($forbidden in @('winget', 'choco.exe', 'RunRuntimeInstall', 'Process.Kill(', 'GetManifestResourceStream(')) {
        if ($installerText -match [regex]::Escape($forbidden)) { $failures.Add("unsafe installer behavior remains: $forbidden") }
    }
}

foreach ($name in $coreNames) {
    $path = Join-Path $bin $name
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("missing engine: $name")
        continue
    }
    if (-not $SkipExecutableExecution) {
        $actualVersion = (& $path --version 2>&1 | Out-String).Trim()
        if ($actualVersion -notmatch [regex]::Escape($expectedVersions[$name])) {
            $failures.Add("wrong engine version for ${name}: $actualVersion")
        }
    }
}

foreach ($name in $requiredArtifacts) {
    $path = if ($coreNames -contains $name) { Join-Path $bin $name } else { Join-Path $ReleaseDirectory $name }
    if (-not (Test-Path -LiteralPath $path)) { $failures.Add("missing release artifact: $name") }
}

$versionedInstaller = Join-Path $ReleaseDirectory "BlockSetup-v$Version.exe"
$installerPaths = @($versionedInstaller, (Join-Path $ReleaseDirectory 'BlockSetup.exe'))
foreach ($installerPath in $installerPaths) {
    if (-not (Test-Path -LiteralPath $installerPath)) { continue }
    if ($installerPath -eq $versionedInstaller) {
        $installerVersionInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($installerPath)
        if ($installerVersionInfo.FileVersion -ne "$Version.0") {
            $failures.Add("wrong installer file version: $($installerVersionInfo.FileVersion)")
        }
    }
}

if ($RequireSignedRelease) {
    foreach ($name in @('block.exe', 'block-lite.exe', 'block-plus.exe', "BlockSetup-v$Version.exe", 'BlockSetup.exe')) {
        $path = if ($coreNames -contains $name) { Join-Path $bin $name } else { Join-Path $ReleaseDirectory $name }
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $signature = Get-AuthenticodeSignature -LiteralPath $path
        if ($signature.Status -ne 'Valid') {
            $failures.Add("release signature is not valid for ${name}: $($signature.Status)")
        }
    }
}

$hashFile = Join-Path $ReleaseDirectory 'SHA256SUMS.txt'
$hashes = @{}
if (-not (Test-Path -LiteralPath $hashFile)) {
    $failures.Add('missing SHA256SUMS.txt')
} else {
    foreach ($line in Get-Content -LiteralPath $hashFile) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^([0-9a-fA-F]{64})\s+(.+)$') {
            $failures.Add("invalid hash line: $line")
            continue
        }
        $hashes[$Matches[2]] = $Matches[1].ToLowerInvariant()
    }
    foreach ($name in $requiredArtifacts) {
        $path = if ($coreNames -contains $name) { Join-Path $bin $name } else { Join-Path $ReleaseDirectory $name }
        if (-not (Test-Path -LiteralPath $path)) { continue }
        if (-not $hashes.ContainsKey($name)) {
            $failures.Add("hash missing from SHA256SUMS.txt: $name")
            continue
        }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $hashes[$name]) { $failures.Add("hash mismatch: $name") }
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-verify-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
try {
    $bundles = @(
        @{ Archive = 'block-lite.zip'; Executable = 'block-lite.exe' },
        @{ Archive = 'block.zip'; Executable = 'block.exe' },
        @{ Archive = 'block-plus.zip'; Executable = 'block-plus.exe' }
    )
    foreach ($bundle in $bundles) {
        $archive = Join-Path $ReleaseDirectory $bundle.Archive
        $destination = Join-Path $tempRoot ([IO.Path]::GetFileNameWithoutExtension($bundle.Archive))
        if (-not (Test-Path -LiteralPath $archive)) { $failures.Add("missing engine bundle: $($bundle.Archive)"); continue }
        try {
            [IO.Compression.ZipFile]::ExtractToDirectory($archive, $destination)
            $files = @(Get-ChildItem -LiteralPath $destination -Recurse -File)
            $exe = @($files | Where-Object { $_.Name -eq $bundle.Executable })
            if ($exe.Count -ne 1) {
                $failures.Add("bundle does not contain exactly one $($bundle.Executable): $($bundle.Archive)")
                continue
            }
            $rootExe = Join-Path $bin $bundle.Executable
            $bundleHash = (Get-FileHash -LiteralPath $exe[0].FullName -Algorithm SHA256).Hash
            $rootHash = (Get-FileHash -LiteralPath $rootExe -Algorithm SHA256).Hash
            if ($bundleHash -ne $rootHash) { $failures.Add("bundle executable differs from published engine: $($bundle.Archive)") }
            if ($files.Count -ne 1) { $failures.Add("unexpected files in engine bundle: $($bundle.Archive)") }
        } catch {
            $failures.Add("cannot inspect engine bundle $($bundle.Archive): $($_.Exception.Message)")
        }
    }

    $pluginPackages = @(
        @{ Archive = "block-language-$Version.vsix"; Manifest = 'extension/package.json'; License = 'extension/LICENSE'; VsixManifest = 'extension.vsixmanifest' },
        @{ Archive = "acode-plugin-block-$Version.zip"; Manifest = 'plugin.json'; License = 'LICENSE'; Main = 'main.js' }
    )
    foreach ($package in $pluginPackages) {
        $archive = Join-Path $ReleaseDirectory $package.Archive
        $destination = Join-Path $tempRoot ([IO.Path]::GetFileNameWithoutExtension($package.Archive))
        if (-not (Test-Path -LiteralPath $archive)) { $failures.Add("missing plugin package: $($package.Archive)"); continue }
        try {
            [IO.Compression.ZipFile]::ExtractToDirectory($archive, $destination)
            $manifestPath = Join-Path $destination $package.Manifest
            $licensePath = Join-Path $destination $package.License
            if (-not (Test-Path -LiteralPath $manifestPath)) { $failures.Add("missing manifest in $($package.Archive)"); continue }
            if (-not (Test-Path -LiteralPath $licensePath)) { $failures.Add("missing LICENSE in $($package.Archive)") }
            $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            if ($manifest.version -ne $Version) { $failures.Add("wrong package version in $($package.Archive): $($manifest.version)") }
            if ($manifest.license -ne 'MIT') { $failures.Add("missing MIT metadata in $($package.Archive)") }
            if ($package.VsixManifest) {
                $vsixManifestPath = Join-Path $destination $package.VsixManifest
                if (-not (Test-Path -LiteralPath $vsixManifestPath)) {
                    $failures.Add("missing VSIX manifest in $($package.Archive)")
                } else {
                    [xml]$vsixManifest = Get-Content -LiteralPath $vsixManifestPath -Raw
                    if ($vsixManifest.PackageManifest.Metadata.Identity.Version -ne $Version) {
                        $failures.Add("wrong VSIX identity version in $($package.Archive): $($vsixManifest.PackageManifest.Metadata.Identity.Version)")
                    }
                }
            }
            if ($package.Main) {
                $mainPath = Join-Path $destination $package.Main
                if (-not (Test-Path -LiteralPath $mainPath)) {
                    $failures.Add("missing plugin entry point in $($package.Archive)")
                } else {
                    $mainText = Get-Content -LiteralPath $mainPath -Raw
                    if ($mainText -notmatch [regex]::Escape("const BLOCK_PLUGIN_VERSION = '$Version';")) {
                        $failures.Add("wrong runtime UI version in $($package.Archive)")
                    }
                }
            }
        } catch {
            $failures.Add("cannot inspect plugin package $($package.Archive): $($_.Exception.Message)")
        }
    }
} finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}

$setupHash = Join-Path $ReleaseDirectory "BlockSetup-v$Version.exe"
$stableHash = Join-Path $ReleaseDirectory 'BlockSetup.exe'
if ((Test-Path -LiteralPath $setupHash) -and (Test-Path -LiteralPath $stableHash)) {
    if ((Get-FileHash -LiteralPath $setupHash -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $stableHash -Algorithm SHA256).Hash) {
        $failures.Add("BlockSetup.exe is not identical to BlockSetup-v$Version.exe")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Block Engine v$Version release verification passed."
