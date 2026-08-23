param(
    [string]$SourcePath = (Join-Path $PSScriptRoot '..\src\NativeBlockProgram.cs')
)

$ErrorActionPreference = 'Stop'
$source = [IO.Path]::GetFullPath($SourcePath)
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-native-interpreter-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

try {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) { throw "C# compiler not found: $compiler" }

    $assemblyPath = Join-Path $tempRoot 'BlockEngine.NativeInterpreter.Tests.dll'
    & $compiler /nologo /target:library /out:$assemblyPath /reference:System.dll /reference:System.Core.dll $source
    if ($LASTEXITCODE -ne 0) { throw 'Native interpreter library compilation failed.' }

    $assembly = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($assemblyPath))
    $type = $assembly.GetType('BlockEngine.NativeBlockProgram')
    $method = $type.GetMethod('Execute')
    $code = @'
items = [1, 2, 3, 4]
total = 0
for item in items:
    if item == 2:
        continue
    block
    total = total + item
block

score = 9
if score == 8:
    status = "wrong"
elif score == 9:
    status = "elif-ok"
else:
    status = "wrong"
block

while total < 10:
    total = total + 1
    if total == 10:
        break
    block
block

profile = {"name": "Block", "total": total}
profile["name"] = "Block Language"

func greet(name):
    return "Hello " + name
block

outside = "global"
func local_scope(value):
    local_value = value
    return local_value
block

printer = "not-a-print-call"
short_or = true || missing_variable
short_and = false && missing_variable
logic_precedence = false || true && true
print(status)
print(profile["name"])
print(profile.total)
print(items[1], items.length)
print(sum(items), str(total), type(profile), contains(items, 3))
print(greet("Block"))
print(local_scope("local"), outside)
print(printer)
print(short_or, short_and, logic_precedence)
'@

    $state = [System.Collections.Generic.Dictionary[string, object]]::new()
    $output = [System.Collections.Generic.List[string]]::new()
    $callback = [Action[string]]{ param([string]$value) [void]$output.Add($value) }
    [void]$method.Invoke($null, [object[]]@($code, $state, $callback))
    $text = $output -join ''

$expected = @('elif-ok', 'Block Language', '2 4', '10 10 map true', 'Hello Block', 'local global', 'not-a-print-call', 'true false true')
    foreach ($value in $expected) {
        if ($text -notmatch [regex]::Escape($value)) { throw "Native interpreter output missing '$value': $text" }
    }
    Write-Host 'Native interpreter static-library test passed.'
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
