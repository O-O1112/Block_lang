param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$pagePath = Join-Path $RepositoryRoot 'marketplace.html'
$scriptPath = Join-Path $RepositoryRoot 'marketplace.js'
$stylePath = Join-Path $RepositoryRoot 'styles.css'
$registryPath = Join-Path $RepositoryRoot 'registry\index.json'

foreach ($path in @($pagePath, $scriptPath, $stylePath, $registryPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Marketplace dependency is missing: $path" }
}

$page = Get-Content -LiteralPath $pagePath -Raw
$script = Get-Content -LiteralPath $scriptPath -Raw
$styles = Get-Content -LiteralPath $stylePath -Raw
$registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json

if (-not $page.Contains('data-registry-url="registry/index.json"')) {
    throw 'Marketplace does not read the same-origin registry index.'
}
if (-not $page.Contains('<template id="market-card-template">')) {
    throw 'Marketplace card template is missing.'
}
if (-not $page.Contains('<script src="marketplace.js?v=225-adaptive"></script>')) {
    throw 'Marketplace renderer is not loaded.'
}
if (-not $page.Contains('id="market-package-count"')) {
    throw 'Marketplace package counter is not data-driven.'
}
if (($page | Select-String -Pattern '<article class="market-card' -AllMatches).Matches.Count -ne 1) {
    throw 'Marketplace contains hard-coded package cards outside the reusable template.'
}
if ($script -match '\.innerHTML\s*=|insertAdjacentHTML') {
    throw 'Marketplace must create registry content with safe DOM APIs, not HTML injection.'
}
if (-not $script.Contains("fetch(registryUrl, { cache: 'no-store', credentials: 'same-origin' })")) {
    throw 'Marketplace registry fetch must remain same-origin and bypass stale caches.'
}

foreach ($layout in @('empty', 'solo', 'duo', 'trio', 'quad', 'catalog')) {
    if (-not $styles.Contains("data-layout=`"$layout`"")) {
        throw "Missing adaptive marketplace CSS for layout: $layout"
    }
}

if ($registry.schema -ne 'block-registry/v1' -or $null -eq $registry.packages) {
    throw 'Marketplace registry fixture is invalid.'
}

$node = Get-Command node -ErrorAction Stop
$nodeTest = @'
const market = require(process.argv[1]);
const expected = new Map([[0, 'empty'], [1, 'solo'], [2, 'duo'], [3, 'trio'], [4, 'quad'], [5, 'catalog'], [25, 'catalog']]);
for (const [count, layout] of expected) {
  if (market.layoutForCount(count) !== layout) throw new Error(`count ${count} did not select ${layout}`);
}
const safe = market.normalizePackage({name: 'demo-package', version: '1.0.0', description: 'Demo', permissions: ['native'], keywords: ['testing']});
if (!safe || safe.name !== 'demo-package') throw new Error('valid package was rejected');
if (market.normalizePackage({name: '../unsafe', description: 'bad'})) throw new Error('unsafe package name was accepted');
'@
& $node.Source -e $nodeTest $scriptPath
if ($LASTEXITCODE -ne 0) { throw 'Adaptive marketplace JavaScript contract failed.' }

Write-Host "Adaptive marketplace verified for $($registry.packages.Count) current package(s)."
$global:LASTEXITCODE = 0
