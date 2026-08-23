param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin'),
    [string]$ReportPath = '',
    [switch]$SkipPolyglot
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$results = New-Object System.Collections.Generic.List[object]

function Add-TestResult(
    [string]$Name,
    [string]$Status,
    [int]$ExitCode,
    [string]$Output,
    [bool]$Required
) {
    $excerpt = if ($Output.Length -gt 1200) { $Output.Substring(0, 1200) + '...' } else { $Output }
    $results.Add([pscustomobject]@{
        name = $Name
        status = $Status
        required = $Required
        exitCode = $ExitCode
        output = $excerpt
    })
}

function Invoke-CommunityTest(
    [string]$Name,
    [string]$Executable,
    [string[]]$Arguments,
    [string]$ExpectedPattern,
    [bool]$Required = $true
) {
    $path = Join-Path $EngineDirectory $Executable
    if (-not (Test-Path -LiteralPath $path)) {
        Add-TestResult $Name 'fail' -1 "Missing executable: $path" $Required
        return
    }

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = (& $path @Arguments 2>&1 | Out-String).Trim()
        $exitCode = $LASTEXITCODE
    } catch {
        $output = $_.Exception.Message
        $exitCode = 1
    } finally {
        $ErrorActionPreference = $previousPreference
    }

    $passed = ($exitCode -eq 0)
    if ($ExpectedPattern) { $passed = $passed -and ($output -match $ExpectedPattern) }
    Add-TestResult $Name ($(if ($passed) { 'pass' } else { 'fail' })) $exitCode $output $Required
}

$versionPath = Join-Path $root 'VERSION'
$version = if (Test-Path -LiteralPath $versionPath) {
    (Get-Content -LiteralPath $versionPath -Raw).Trim()
} else {
    'unknown'
}

Invoke-CommunityTest 'standard-version' 'block.exe' @('--version') 'Block Language Engine v'
Invoke-CommunityTest 'native-control-flow' 'block.exe' @((Join-Path $root 'examples\native-control-flow.blk')) 'pass'
Invoke-CommunityTest 'native-language-core' 'block.exe' @((Join-Path $root 'examples\native-language-core.blk')) 'Hello Block'

$python = Get-Command python.exe -ErrorAction SilentlyContinue
$node = Get-Command node.exe -ErrorAction SilentlyContinue
if (-not $SkipPolyglot -and $python -and $node) {
    Invoke-CommunityTest 'python-node-state-bridge' 'block.exe' @((Join-Path $root 'examples\hello-polyglot.blk')) 'Total=10'
} else {
    $reason = if ($SkipPolyglot) {
        'Skipped by parameter.'
    } elseif (-not $python -or -not $node) {
        'Skipped because both python.exe and node.exe are not available on PATH.'
    } else {
        'Skipped by environment.'
    }
    Add-TestResult 'python-node-state-bridge' 'skipped' 0 $reason $false
}

$failedCount = @($results | Where-Object status -eq 'fail').Count
$failed = @($results | Where-Object { $_.status -eq 'fail' -and $_.required }).Count -gt 0
$passedCount = @($results | Where-Object status -eq 'pass').Count
$skippedCount = @($results | Where-Object status -eq 'skipped').Count
$report = [pscustomobject][ordered]@{
    schemaVersion = 1
    harness = 'Block Community Lab'
    blockVersion = $version
    generatedAtUtc = ([DateTime]::UtcNow).ToString('o')
    engineDirectory = $EngineDirectory
    tests = $results.ToArray()
    summary = [pscustomobject][ordered]@{
        passed = $passedCount
        failed = $failedCount
        skipped = $skippedCount
    }
}

if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = [IO.Path]::GetFullPath($ReportPath)
    $parent = Split-Path -Parent $ReportPath
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding UTF8
    Write-Host "Community Lab report written to $ReportPath"
}

$report.summary | Format-List
if ($failed) {
    Write-Error 'Community Lab required tests failed.'
    exit 1
}

Write-Host 'Block Community Lab checks passed; optional tests may be skipped by environment.'
