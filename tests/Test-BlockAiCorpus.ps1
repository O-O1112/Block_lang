param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$aiRoot = Join-Path $RepositoryRoot 'docs\ai'

$requiredFiles = @(
    'README.md',
    'SYSTEM-PROMPT.md',
    'block-knowledge.md',
    'training.jsonl',
    'eval-cases.jsonl'
)

foreach ($relativePath in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $aiRoot $relativePath))) {
        throw "Missing AI corpus file: $relativePath"
    }
}

function Read-JsonLines([string]$Path) {
    $items = @()
    foreach ($line in (Get-Content -LiteralPath $Path)) {
        if (-not [string]::IsNullOrWhiteSpace($line)) {
            $items += ($line | ConvertFrom-Json)
        }
    }
    return $items
}

$training = Read-JsonLines (Join-Path $aiRoot 'training.jsonl')
$evalCases = Read-JsonLines (Join-Path $aiRoot 'eval-cases.jsonl')
if ($training.Count -lt 10) { throw 'AI training corpus must contain at least 10 examples.' }
if ($evalCases.Count -lt 8) { throw 'AI evaluation corpus must contain at least 8 cases.' }

foreach ($example in $training) {
    if (-not $example.messages -or $example.messages.Count -lt 2) {
        throw 'Each training example must contain a user prompt and an assistant answer.'
    }
}

foreach ($case in $evalCases) {
    foreach ($field in @('id', 'prompt', 'must_include', 'must_not_include')) {
        if ($null -eq $case.$field) { throw "Evaluation case is missing: $field" }
    }
}

$policy = Get-Content -LiteralPath (Join-Path $aiRoot 'SYSTEM-PROMPT.md') -Raw
foreach ($fact in @('There is no `<block>` tag', 'v2.2.2', 'serializable', 'local-first')) {
    if (-not $policy.Contains($fact)) { throw "AI policy is missing canonical fact: $fact" }
}

Write-Host "AI corpus verified: $($training.Count) training examples, $($evalCases.Count) evaluation cases."
