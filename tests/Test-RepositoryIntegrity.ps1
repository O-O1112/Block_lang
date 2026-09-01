param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$required = @(
    'VERSION', 'LICENSE', 'README.md', 'CHANGELOG.md', 'SECURITY.md',
    'CONTRIBUTING.md', 'CODE_OF_CONDUCT.md', 'GOVERNANCE.md', 'SUPPORT.md',
    'CITATION.cff', 'Installer.cs', 'build.ps1', 'build-installer.ps1',
    'build-release.ps1', 'verify-release.ps1', 'src\Program.cs',
    'src\Parser.cs', 'src\Executor.cs', 'src\ProjectWorkspace.cs',
    'docs\DEPLOYMENT-SECURITY.md', 'docs\RELEASE-SIGNING.md'
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot $path))) {
        throw "Required repository file is missing: $path"
    }
}

$removed = @(
    'marketplace.html', 'marketplace.js', 'registry',
    'src\BlockRegistryClient.cs', '.github\workflows\registry.yml',
    'tests\Test-PackageRegistry.ps1', 'tools\build-registry-index.ps1'
)
foreach ($path in $removed) {
    $full = Join-Path $RepositoryRoot $path
    $remaining = if (Test-Path -LiteralPath $full -PathType Container) {
        @(Get-ChildItem -LiteralPath $full -Recurse -File -ErrorAction SilentlyContinue).Count -gt 0
    } else {
        Test-Path -LiteralPath $full
    }
    if ($remaining) {
        throw "Removed third-party package subsystem unexpectedly remains: $path"
    }
}

$source = Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'src') -Filter '*.cs' -File |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw } | Out-String
if ($source -match '(?i)\bwinget\b|choco\.exe|BlockRegistryClient') {
    throw 'Automatic package-manager or removed package-registry code remains in engine source.'
}

Write-Host 'Repository integrity checks passed.'
