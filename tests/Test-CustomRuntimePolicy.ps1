param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin')
)

$ErrorActionPreference = 'Stop'
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$engine = Join-Path $EngineDirectory 'block-plus.exe'
if (-not (Test-Path -LiteralPath $engine)) { throw "Missing Block+ engine: $engine" }

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-custom-policy-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
$scriptPath = Join-Path $tempRoot 'reserved-language.blkp'

try {
    @'
<define lang="python" cmd="python" />
<python>
print("custom override must not run")
</python>
'@ | Set-Content -LiteralPath $scriptPath -Encoding UTF8

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = (& $engine $scriptPath 2>&1 | Out-String).Trim()
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousPreference }

    if ($exitCode -eq 0) { throw 'Reserved built-in language override was accepted.' }
    if ($output -match 'custom override must not run') { throw 'Reserved language override reached a runtime.' }
    if ($output -notmatch 'built-in|shadow|definition') { throw "Custom runtime rejection was not actionable: $output" }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}

$global:LASTEXITCODE = 0
Write-Host 'Custom runtime policy tests passed.'
