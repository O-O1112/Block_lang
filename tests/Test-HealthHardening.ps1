param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin'),
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$engine = Join-Path $EngineDirectory 'block.exe'
if (-not (Test-Path -LiteralPath $engine)) { throw "Missing engine: $engine" }

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-hardening-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Invoke-Probe([string]$Name, [string]$Source) {
    $path = Join-Path $tempRoot ($Name + '.blk')
    [IO.File]::WriteAllText($path, $Source, (New-Object Text.UTF8Encoding($false)))
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = (& $engine $path 2>&1 | Out-String).Trim()
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    return [pscustomobject]@{ Output = $output; ExitCode = $exitCode; Path = $path }
}

try {
    $jsonProbe = Invoke-Probe 'json-values' @'
flag = true
score = 1.5
items = [1, 2]
<json>
{"flag":{{flag}},"score":{{score}},"items":{{items}}}
</json>
'@
    Assert-True ($jsonProbe.ExitCode -eq 0) "JSON probe failed: $($jsonProbe.Output)"
    $json = $jsonProbe.Output | ConvertFrom-Json
    Assert-True ($json.flag -eq $true) 'JSON boolean was not serialized as a JSON boolean.'
    Assert-True ($json.score -eq 1.5) 'JSON number was not serialized invariantly.'
    Assert-True (@($json.items).Count -eq 2) 'JSON list was not serialized as an array.'

    if (Get-Command node.exe -ErrorAction SilentlyContinue) {
        $stateProbe = Invoke-Probe 'javascript-state' @'
payload = {"items": [1, 2, 3]}
<js>
payload.items.push(4);
console.log("js-items=" + payload.items.length);
</js>
print("native-items=" + len(payload.items))
'@
        Assert-True ($stateProbe.ExitCode -eq 0) "JavaScript state probe failed: $($stateProbe.Output)"
        Assert-True ($stateProbe.Output -match 'js-items=4') 'JavaScript did not mutate the shared list.'
        Assert-True ($stateProbe.Output -match 'native-items=4') 'JavaScript shared-list mutation was not returned to Block state.'
    }

    $htmlProbe = Invoke-Probe 'html-context' @'
text_value = "<img src=x onerror=alert(1)>"
attribute_value = "x onmouseover=alert(2)"
<html>
<p>{{text_value}}</p>
<div data-value={{attribute_value}}>safe</div>
</html>
'@
    Assert-True ($htmlProbe.ExitCode -eq 0) "HTML context probe failed: $($htmlProbe.Output)"
    $match = [regex]::Match($htmlProbe.Output, '\[HTML\] Output written to -> (.+)$')
    Assert-True $match.Success 'HTML output path was not reported.'
    $htmlPath = $match.Groups[1].Value.Trim()
    $renderedHtml = Get-Content -LiteralPath $htmlPath -Raw
    Assert-True (-not $renderedHtml.Contains('<img src=x')) 'HTML text substitution remained executable markup.'
    Assert-True (-not $renderedHtml.Contains(' onmouseover=')) 'Unquoted HTML attribute substitution escaped its value.'
    if ($htmlPath.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $htmlPath -Force
    }

    $urlProbe = Invoke-Probe 'html-url' @'
target = "javascript:alert(1)"
<html>
<a href="{{target}}">unsafe</a>
</html>
'@
    Assert-True ($urlProbe.ExitCode -ne 0) 'A javascript: template URL was accepted.'
    Assert-True ($urlProbe.Output -match 'Unsafe URL scheme') 'Unsafe URL failure was not actionable.'

    $eventProbe = Invoke-Probe 'html-event' @'
handler = "alert(1)"
<html>
<button onclick="{{handler}}">unsafe</button>
</html>
'@
    Assert-True ($eventProbe.ExitCode -ne 0) 'Template substitution into an event handler was accepted.'

    $unknownProbe = Invoke-Probe 'unknown-language' @'
<notaruntime>
hello
</notaruntime>
'@
    Assert-True ($unknownProbe.ExitCode -ne 0) 'An unknown language tag was treated as an operating-system command.'
    Assert-True ($unknownProbe.Output -match 'BLK1101|Unknown language tag') 'Unknown language failure was not actionable.'

    if (Get-Command python.exe -ErrorAction SilentlyContinue) {
        $previousNetworkOverride = $env:BLOCK_NETWORK_BLOCKED_OVERRIDE
        $env:BLOCK_NETWORK_BLOCKED_OVERRIDE = '1'
        try {
            $networkProbe = Invoke-Probe 'network-reload' @'
<py>
import importlib
importlib.reload(_block_socket)
</py>
'@
        }
        finally {
            $env:BLOCK_NETWORK_BLOCKED_OVERRIDE = $previousNetworkOverride
        }
        Assert-True ($networkProbe.ExitCode -ne 0) 'The advisory guard allowed socket module reload.'
        $networkMessage = $networkProbe.Output -replace '\s', ''
        Assert-True ($networkMessage -match 'deniedmodulereload') 'Network guard rejection was not actionable.'
    }

    $example = Join-Path $RepositoryRoot 'examples\hello-polyglot.blk'
    $astOutput = (& $engine ast $example 2>&1 | Out-String).Trim()
    Assert-True ($LASTEXITCODE -eq 0) "AST command failed: $astOutput"
    $ast = $astOutput | ConvertFrom-Json
    Assert-True ($ast.SchemaVersion -eq 1) 'AST schema version is missing.'
    Assert-True ($ast.Kind -eq 'Document') 'AST root kind is missing.'
    Assert-True (@($ast.Blocks).Count -ge 3) 'AST did not expose language blocks.'
    Assert-True (@($ast.Diagnostics).Count -eq 0) 'Valid example produced AST diagnostics.'

    $badAstPath = Join-Path $tempRoot 'bad-ast.blk'
    [IO.File]::WriteAllText($badAstPath, "<py>`nprint('x')`n</js>`n", (New-Object Text.UTF8Encoding($false)))
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $badAstOutput = (& $engine ast $badAstPath 2>&1 | Out-String).Trim()
        $badAstExit = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousPreference }
    Assert-True ($badAstExit -ne 0) 'Malformed AST input returned success.'
    $badAst = $badAstOutput | ConvertFrom-Json
    Assert-True (@($badAst.Diagnostics).Count -gt 0) 'Malformed AST input did not include structured diagnostics.'

    $apiSource = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'src\ApiServer.cs') -Raw
    Assert-True ($apiSource -match 'catch \(PlatformNotSupportedException(?:\s+\w+)?\)[\s\S]*?Environment\.ExitCode = 1') 'Unsupported API listener still returns a success exit code.'
    Assert-True ($apiSource -match 'catch \(HttpListenerException ex\)[\s\S]*?Environment\.ExitCode = 1') 'HTTP listener startup failure still returns a success exit code.'

    $programSource = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'src\Program.cs') -Raw
    Assert-True ($programSource -match 'finally[\s\S]*?Console\.CursorVisible = true') 'CLI startup animation can leave the terminal cursor hidden after an error.'
    Assert-True ($programSource -match 'ShowAnimationAndUsage\(bool infinite = false\)') 'No-argument CLI still defaults to an effectively unbounded animation.'

    $executorSource = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'src\Executor.cs') -Raw
    Assert-True ($executorSource -notmatch '(?i)\bwinget\b|choco\.exe') 'Engine source still invokes an automatic package manager.'
    Assert-True ($executorSource -match 'cfg\.AllowCustomDefinitions && CustomLangRegistry\.TryGet') 'Global custom runtimes can bypass AllowCustomDefinitions.'
    Assert-True ($executorSource -match "Unknown language tag") 'Unknown tags are not rejected before process launch.'

    $acodeSource = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'acode-plugin-block\main.js') -Raw
    Assert-True ($acodeSource -match 'JSON\.stringify\(safeProfiles\)') 'Acode profiles are not sanitized before persistent storage.'
    Assert-True ($acodeSource -notmatch "X-Block-Max-Parallel|X-Block-Cache") 'Acode still sends unsupported runtime headers.'

    # The malformed-AST probe is expected to return 1. PowerShell preserves the
    # last native process exit code even after every assertion has passed, which
    # would otherwise make GitHub Actions mark this successful script as failed.
    $global:LASTEXITCODE = 0
    Write-Host 'Health-hardening regression tests passed.'
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
