param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepositoryRoot '.blocklang\health'
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$engine = Join-Path $OutputDirectory 'bin\block.exe'
$version = (Get-Content -LiteralPath (Join-Path $RepositoryRoot 'VERSION') -Raw).Trim()
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

if (-not (Test-Path -LiteralPath $engine)) {
    & (Join-Path $RepositoryRoot 'build.ps1') -OutputDirectory (Join-Path $OutputDirectory 'bin') -SkipHash -Version $version
    if ($LASTEXITCODE -ne 0) { throw 'Block engine build failed before health check.' }
}

$report = Join-Path $OutputDirectory ('health-' + (Get-Date -Format 'yyyyMMdd') + '.json')
& $engine doctor --full --root $RepositoryRoot --report $report --strict
if ($LASTEXITCODE -ne 0) { throw 'Block daily health check reported errors or strict warnings.' }

$checks = @(
    @('tests\Test-BlockEngine.ps1', '-EngineDirectory', (Join-Path $OutputDirectory 'bin')),
    @('tests\Test-BlockCli.ps1', '-EngineDirectory', (Join-Path $OutputDirectory 'bin')),
    @('tests\Test-BlockPaths.ps1', '-EngineDirectory', (Join-Path $OutputDirectory 'bin')),
    @('tests\Test-HealthHardening.ps1', '-EngineDirectory', (Join-Path $OutputDirectory 'bin'), '-RepositoryRoot', $RepositoryRoot),
    @('tests\Test-VersionConsistency.ps1', '-RepositoryRoot', $RepositoryRoot),
    @('tests\Test-RepositoryIntegrity.ps1', '-RepositoryRoot', $RepositoryRoot),
    @('tests\Test-WebsiteLinks.ps1', '-RepositoryRoot', $RepositoryRoot),
    @('tests\Test-WebsiteSecurity.ps1', '-RepositoryRoot', $RepositoryRoot),
    @('tests\Test-InstallerTransport.ps1', '-RepositoryRoot', $RepositoryRoot)
)

foreach ($check in $checks) {
    $script = Join-Path $RepositoryRoot $check[0]
    $arguments = @($check[1..($check.Count - 1)])
    & $script @arguments
    if ($LASTEXITCODE -ne 0) { throw "Daily regression check failed: $($check[0])" }
}
Write-Output "Health check passed: $report"
