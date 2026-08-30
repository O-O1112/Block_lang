param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin')
)

$ErrorActionPreference = 'Stop'
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$failures = New-Object System.Collections.Generic.List[string]
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-native-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

function Invoke-Block([string]$Executable, [string[]]$Arguments) {
    $path = Join-Path $EngineDirectory $Executable
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing test executable: $path" }
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
    $scriptPath = Join-Path $tempRoot 'native-language.blk'
    @'
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

list_eq_same = [1, 2] == [1, 2]
list_eq_diff = [1, 2] == [3, 4]
map_eq_same = {"a": 1} == {"a": 1}
map_eq_diff = {"a": 1} == {"b": 2}
nested_contains_pass = contains([[1, 2], [3, 4]], [1, 2])
nested_contains_fail = contains([[1, 2], [3, 4]], [5, 6])

nested_matrix = [[1, 2], [3, 4]]
nested_matrix[0][1] = 99
profile.meta = {"role": "admin"}
profile.meta.role = "lead"

print(status)
print(profile["name"])
print(profile.total)
print(profile.meta.role)
print(nested_matrix[0][1])
print(items[1], items.length)
print(sum(items), str(total), type(profile), contains(items, 3))
print(greet("Block"))
print(local_scope("local"), outside)
print(printer)
print(short_or, short_and, logic_precedence)
print(items)
print(list_eq_same, list_eq_diff, map_eq_same, map_eq_diff, nested_contains_pass, nested_contains_fail)
'@ | Set-Content -LiteralPath $scriptPath -Encoding UTF8

    $result = Invoke-Block 'block.exe' @('run', $scriptPath)
    Assert-Condition ($result.ExitCode -eq 0) "Native language program failed: $($result.Output)"
    Assert-Condition ($result.Output -match 'elif-ok') "elif did not execute: $($result.Output)"
    Assert-Condition ($result.Output -match 'Block Language') "map assignment/member access failed: $($result.Output)"
    Assert-Condition ($result.Output -match 'lead') "nested member property assignment failed: $($result.Output)"
    Assert-Condition ($result.Output -match '99') "nested list index assignment failed: $($result.Output)"
    Assert-Condition ($result.Output -match '10') "loop control failed: $($result.Output)"
    Assert-Condition ($result.Output -match '2 4') "list index/length failed: $($result.Output)"
    Assert-Condition ($result.Output -match '10 10 map true') "built-in functions failed: $($result.Output)"
    Assert-Condition ($result.Output -match 'Hello Block') "function return failed: $($result.Output)"
    Assert-Condition ($result.Output -match 'local global') "function scope/global lookup failed: $($result.Output)"
    Assert-Condition ($result.Output -match 'not-a-print-call') "identifier beginning with print was misparsed: $($result.Output)"
    Assert-Condition ($result.Output -match 'true false true') "logical short-circuit or precedence failed: $($result.Output)"
    Assert-Condition ($result.Output -match '\[1, 2, 3, 4\]') "list collection formatting failed: $($result.Output)"
    Assert-Condition ($result.Output -match 'true false true false true false') "structural equality comparison failed: $($result.Output)"

    $limitPath = Join-Path $tempRoot 'range-limit.blk'
    @'
for item in range(0, 10001):
    pass
block
'@ | Set-Content -LiteralPath $limitPath -Encoding UTF8
    $limit = Invoke-Block 'block.exe' @('run', $limitPath)
    Assert-Condition ($limit.ExitCode -ne 0) "range limit was not enforced: $($limit.Output)"
    Assert-Condition ($limit.Output -match 'range exceeded the 10,000 item limit') "range limit error was unclear: $($limit.Output)"
    # The non-zero exit code is intentional for this negative test. Reset it so
    # PowerShell does not propagate the expected failure as the script result.
    $global:LASTEXITCODE = 0
} catch {
    $failures.Add($_.Exception.Message)
} finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Native Block language tests passed.'
