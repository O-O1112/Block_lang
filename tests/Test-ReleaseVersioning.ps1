param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..'),
    [string]$ProbeVersion = '9.9.9.9',
    [switch]$SkipExecutableExecution
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
if ($ProbeVersion -notmatch '^\d+\.\d+\.\d+(?:\.\d+)?$') { throw "Invalid probe version: $ProbeVersion" }
$release = Join-Path ([IO.Path]::GetTempPath()) ('block-release-version-test-' + [Guid]::NewGuid().ToString('N'))

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

try {
    if ($SkipExecutableExecution) {
        & (Join-Path $RepositoryRoot 'build-release.ps1') -ReleaseDirectory $release -Version $ProbeVersion -SkipExecutableExecution
    }
    else {
        & (Join-Path $RepositoryRoot 'build-release.ps1') -ReleaseDirectory $release -Version $ProbeVersion
    }
    if ($LASTEXITCODE -ne 0) { throw 'Custom-version release build failed.' }

    if (-not $SkipExecutableExecution) {
        $expected = @{
            'block.exe' = "Block Language Engine v$ProbeVersion"
            'block-lite.exe' = "Block Lite Engine v$ProbeVersion"
            'block-plus.exe' = "Block+ Engine v$ProbeVersion"
        }
        foreach ($name in $expected.Keys) {
            $actual = (& (Join-Path $release $name) --version 2>&1 | Out-String).Trim()
            Assert-True ($actual -match [regex]::Escape($expected[$name])) "Wrong generated engine version for ${name}: $actual"
        }
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $inspect = Join-Path $release 'inspect'
    [IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $release "block-language-$ProbeVersion.vsix"), (Join-Path $inspect 'vsix'))
    [IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $release "acode-plugin-block-$ProbeVersion.zip"), (Join-Path $inspect 'acode'))

    $vsixPackage = Get-Content -LiteralPath (Join-Path $inspect 'vsix\extension\package.json') -Raw | ConvertFrom-Json
    $acodePackage = Get-Content -LiteralPath (Join-Path $inspect 'acode\plugin.json') -Raw | ConvertFrom-Json
    [xml]$vsixManifest = Get-Content -LiteralPath (Join-Path $inspect 'vsix\extension.vsixmanifest') -Raw
    $probeParts = $ProbeVersion -split '\.'
    $expectedVsixVersion = if ($probeParts.Count -eq 3) { $ProbeVersion } else { "{0}.{1}.{2}" -f $probeParts[0], $probeParts[1], (([int]$probeParts[2]) + 1) }
    Assert-True ($vsixPackage.version -eq $expectedVsixVersion) 'VS Code package.json retained a stale or invalid version.'
    Assert-True ($vsixManifest.PackageManifest.Metadata.Identity.Version -eq $expectedVsixVersion) 'VSIX identity retained a stale or invalid version.'
    Assert-True ($acodePackage.version -eq $ProbeVersion) 'Acode plugin manifest retained a stale version.'

    if ($SkipExecutableExecution) {
        $verifyOutput = (& (Join-Path $RepositoryRoot 'verify-release.ps1') -ReleaseDirectory $release -Version $ProbeVersion -SkipExecutableExecution 2>&1 | Out-String).Trim()
    }
    else {
        $verifyOutput = (& (Join-Path $RepositoryRoot 'verify-release.ps1') -ReleaseDirectory $release -Version $ProbeVersion 2>&1 | Out-String).Trim()
    }
    Assert-True ($LASTEXITCODE -eq 0) "Release verifier failed: $verifyOutput"
    Assert-True ($verifyOutput -match "Block Engine v$([regex]::Escape($ProbeVersion)) release verification passed") 'Release verifier version was overwritten by executable output.'
    Assert-True ($verifyOutput -notmatch 'vBlock') 'Release verifier emitted the old corrupted success message.'

    Write-Host "Release versioning regression test passed for v$ProbeVersion."
}
finally {
    if (Test-Path -LiteralPath $release) { Remove-Item -LiteralPath $release -Recurse -Force }
}
