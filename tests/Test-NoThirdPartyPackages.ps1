param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin'),
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$failures = New-Object System.Collections.Generic.List[string]
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-no-packages-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

function Assert-Condition([bool]$Condition, [string]$Message) {
    if (-not $Condition) { $failures.Add($Message) }
}

function Invoke-Block([string[]]$Arguments) {
    $path = Join-Path $EngineDirectory 'block.exe'
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing test executable: $path" }
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = (& $path @Arguments 2>&1 | Out-String).Trim()
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    [pscustomobject]@{ ExitCode = $exitCode; Output = $output }
}

try {
    foreach ($relativePath in @(
        'marketplace.html',
        'marketplace.js',
        'registry',
        'src\BlockRegistryClient.cs',
        'tests\Test-PackageRegistry.ps1',
        'tests\Test-MarketplaceLayout.ps1',
        'tools\build-registry-index.ps1'
    )) {
        $full = Join-Path $RepositoryRoot $relativePath
        $remaining = if (Test-Path -LiteralPath $full -PathType Container) {
            @(Get-ChildItem -LiteralPath $full -Recurse -File -ErrorAction SilentlyContinue).Count -gt 0
        } else {
            Test-Path -LiteralPath $full
        }
        Assert-Condition (-not $remaining) "Removed package component still exists: $relativePath"
    }

    $help = Invoke-Block @('help')
    Assert-Condition ($help.ExitCode -eq 0) "block help failed: $($help.Output)"
    Assert-Condition ($help.Output -notmatch '(?im)^\s*(pkg|ecosystem)\b') "Removed package command is still advertised: $($help.Output)"

    $projectPath = Join-Path $tempRoot 'project'
    $project = Invoke-Block @('project', 'init', $projectPath, 'no-packages')
    Assert-Condition ($project.ExitCode -eq 0) "block project init failed: $($project.Output)"
    Assert-Condition (-not (Test-Path -LiteralPath (Join-Path $projectPath 'packages'))) 'Project initialization recreated a packages directory.'

    $manifestPath = Join-Path $projectPath 'block.project.json'
    $manifestText = if (Test-Path -LiteralPath $manifestPath) { Get-Content -LiteralPath $manifestPath -Raw } else { '' }
    Assert-Condition (-not [string]::IsNullOrWhiteSpace($manifestText)) 'Project manifest was not created.'
    Assert-Condition ($manifestText -notmatch '"dependencies"') 'Project manifest still contains package dependencies.'

    $removedSyntaxPath = Join-Path $tempRoot 'removed-package-syntax.blk'
    '<use package="example" />' | Set-Content -LiteralPath $removedSyntaxPath -Encoding UTF8
    $removedSyntax = Invoke-Block @('check', $removedSyntaxPath)
    Assert-Condition ($removedSyntax.ExitCode -ne 0) 'Removed <use package> syntax was unexpectedly accepted.'
    Assert-Condition ($removedSyntax.Output -match 'BLK1301') "Removed package syntax did not produce BLK1301: $($removedSyntax.Output)"

    $sourceRoots = @(
        'index.html', 'features.html', 'downloads.html', 'wiki.html', 'wiki-install.html',
        'block-vscode-extension\extension.js',
        'block-vscode-extension\package.json',
        'acode-plugin-block\main.js'
    )
    foreach ($relativePath in $sourceRoots) {
        $path = Join-Path $RepositoryRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $content = Get-Content -LiteralPath $path -Raw
        Assert-Condition ($content -notmatch '(?i)marketplace\.html|block pkg|<use package') "Removed package UI or command remains in ${relativePath}."
    }
} catch {
    $failures.Add($_.Exception.Message)
} finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Third-party package removal tests passed.'
# The BLK1301 rejection above intentionally leaves the last native process at
# exit code 1. Reset it after the assertion so CI reports this test as passed.
$global:LASTEXITCODE = 0
