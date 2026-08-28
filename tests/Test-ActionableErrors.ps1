param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin')
)

$ErrorActionPreference = 'Stop'
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$engine = Join-Path $EngineDirectory 'block.exe'
if (-not (Test-Path -LiteralPath $engine)) { throw "Missing test executable: $engine" }

$failures = New-Object System.Collections.Generic.List[string]
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-error-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

function Invoke-Block([string[]]$Arguments) {
    $previousPreference = $ErrorActionPreference
    try {
        # Windows PowerShell can promote native stderr records to PowerShell
        # errors. Capture them as text so the test can inspect every line.
        $ErrorActionPreference = 'Continue'
        $records = @(& $engine @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
        $output = (($records | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine).Trim()
        [pscustomobject]@{ ExitCode = $exitCode; Output = $output }
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

function Assert-Match($Result, [string]$Pattern, [string]$Label) {
    if ($Result.Output -notmatch $Pattern) {
        $failures.Add("$Label did not match /$Pattern/: $($Result.Output)")
    }
}

try {
    $missingPath = Join-Path $tempRoot 'missing script.blk'
    $missing = Invoke-Block @('run', $missingPath)
    if ($missing.ExitCode -eq 0) { $failures.Add('Missing-file command returned exit code 0.') }
    Assert-Match $missing 'error\[BLK1001\]: File not found' 'Missing-file diagnostic code'
    Assert-Match $missing 'operation\s*:\s*run' 'Missing-file operation'
    Assert-Match $missing 'hint\s*:\s*Run ''block find <name>''' 'Missing-file hint'
    if ($missing.Output -match '\bat BlockEngine\.') { $failures.Add('Default missing-file output leaked a stack trace.') }

    $syntaxPath = Join-Path $tempRoot 'mismatched tags.blk'
    [IO.File]::WriteAllText($syntaxPath, "<py>`nprint('hello')`n</js>`n", (New-Object Text.UTF8Encoding($false)))
    $syntax = Invoke-Block @('check', $syntaxPath)
    if ($syntax.ExitCode -eq 0) { $failures.Add('Syntax-error command returned exit code 0.') }
    Assert-Match $syntax 'error\[BLK1101\]: Mismatched closing tag' 'Syntax diagnostic code'
    Assert-Match $syntax 'location\s*:\s*3:1' 'Syntax location'
    Assert-Match $syntax 'source\s*:\s*3 \| </js>' 'Syntax source excerpt'
    Assert-Match $syntax 'hint\s*:\s*Replace </js> with </py>' 'Syntax repair hint'
    if ($syntax.Output -match '\bat BlockEngine\.') { $failures.Add('Default syntax output leaked a stack trace.') }

    $usage = Invoke-Block @('ast')
    if ($usage.ExitCode -eq 0) { $failures.Add('Invalid-usage command returned exit code 0.') }
    Assert-Match $usage 'error\[BLK0001\]' 'Usage diagnostic code'
    Assert-Match $usage 'usage\s*:\s*block ast <file>' 'Usage correction'
}
catch {
    $failures.Add($_.Exception.Message)
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Actionable error diagnostics passed.'
# Negative-path assertions intentionally run commands that exit with code 1.
# Reset the native exit code so CI records the successful test result while
# allowing callers (including release.yml) to continue running later tests.
$global:LASTEXITCODE = 0
