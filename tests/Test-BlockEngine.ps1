param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin')
)

$ErrorActionPreference = 'Stop'
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$failures = New-Object System.Collections.Generic.List[string]
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

function Invoke-Block([string]$Executable, [string[]]$Arguments) {
    $path = Join-Path $EngineDirectory $Executable
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing test executable: $path"
    }
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = (& $path @Arguments 2>&1 | Out-String).Trim()
        [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    } finally {
        $ErrorActionPreference = $previousPreference
    }
}

function Assert-Condition([bool]$Condition, [string]$Message) {
    if (-not $Condition) { $failures.Add($Message) }
}

try {
    $version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\VERSION') -Raw).Trim()
    $versions = @{
        'block.exe' = "Block Language Engine v$version (Standard Edition)"
        'block-lite.exe' = "Block Lite Engine v$version (Lite Edition)"
        'block-plus.exe' = "Block+ Engine v$version (Flagship Edition)"
    }
    foreach ($item in $versions.GetEnumerator()) {
        $result = Invoke-Block $item.Key @('--version')
        Assert-Condition ($result.ExitCode -eq 0) "$($item.Key) --version exited with $($result.ExitCode)"
        Assert-Condition ($result.Output -match [regex]::Escape($item.Value)) "$($item.Key) returned unexpected version: $($result.Output)"
    }

    $nativePath = Join-Path $tempRoot 'native.blk'
    @'
score = 80
if score >= 60:
    result = "pass"
else:
    result = "retry"
block
print(result)
'@ | Set-Content -LiteralPath $nativePath -Encoding UTF8
    $native = Invoke-Block 'block.exe' @($nativePath)
    Assert-Condition ($native.ExitCode -eq 0) "native control-flow smoke test failed: $($native.Output)"
    Assert-Condition ($native.Output -match 'pass') "native control-flow smoke test did not print pass: $($native.Output)"

    $checkPath = Join-Path $tempRoot 'check.blkp'
    @'
<py>
print("syntax")
</py>
<js>
console.log("check")
</js>
'@ | Set-Content -LiteralPath $checkPath -Encoding UTF8
    $check = Invoke-Block 'block-plus.exe' @('check', $checkPath)
    Assert-Condition ($check.ExitCode -eq 0) "block-plus check failed: $($check.Output)"
    Assert-Condition ($check.Output -match 'Syntax Check Passed') "block-plus check returned unexpected output: $($check.Output)"

    $invalidPath = Join-Path $tempRoot 'invalid.blk'
    @'
<py>
print("broken")
</js>
'@ | Set-Content -LiteralPath $invalidPath -Encoding UTF8
    $invalid = Invoke-Block 'block.exe' @($invalidPath)
    Assert-Condition ($invalid.ExitCode -ne 0) 'mismatched tags were accepted unexpectedly'

    $python = Get-Command python.exe -ErrorAction SilentlyContinue
    $node = Get-Command node.exe -ErrorAction SilentlyContinue
    if ($python -and $node) {
        $polyglotPath = Join-Path $tempRoot 'polyglot.blk'
        @'
<py>
answer = 41
</py>
<js>
console.log(answer + 1)
</js>
'@ | Set-Content -LiteralPath $polyglotPath -Encoding UTF8
        $polyglot = Invoke-Block 'block.exe' @($polyglotPath)
        Assert-Condition ($polyglot.ExitCode -eq 0) "polyglot state smoke test failed: $($polyglot.Output)"
        Assert-Condition ($polyglot.Output -match '42') "polyglot state smoke test did not print 42: $($polyglot.Output)"
    } else {
        Write-Warning 'Skipping Python-to-Node state smoke test because Python or Node.js is unavailable.'
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

Write-Host 'Block Engine smoke tests passed.'
