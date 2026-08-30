param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$version = (Get-Content -LiteralPath (Join-Path $RepositoryRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(?:\.\d+)?$') { throw "Invalid VERSION value: $version" }
$versionParts = $version -split '\.'
$vsCodeExtensionVersion = if ($versionParts.Count -eq 3) { $version } else { "{0}.{1}.{2}" -f $versionParts[0], $versionParts[1], (([int]$versionParts[2]) + 1) }

function Assert-Contains([string]$Path, [string]$Text) {
    $full = Join-Path $RepositoryRoot $Path
    if (-not (Test-Path -LiteralPath $full)) { throw "Required versioned file missing: $Path" }
    $content = Get-Content -LiteralPath $full -Raw
    if (-not $content.Contains($Text)) { throw "$Path does not contain the current version marker '$Text'." }
}

Assert-Contains 'src\BlockVersion.cs' ('Value = "' + $version + '"')
$citation = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'CITATION.cff') -Raw
if ($citation -notmatch ('(?m)^version:\s*["'']?' + [regex]::Escape($version) + '["'']?\s*$')) {
    throw "CITATION.cff does not declare version $version."
}
Assert-Contains 'README.md' ('v' + $version)
Assert-Contains 'downloads.html' ('BlockSetup-v' + $version + '.exe')
Assert-Contains 'index.html' ('v' + $version)
Assert-Contains 'block-vscode-extension\extension.vsixmanifest' ('Version="' + $vsCodeExtensionVersion + '"')
Assert-Contains 'acode-plugin-block\main.js' ("BLOCK_PLUGIN_VERSION = '$version'")

$vscode = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'block-vscode-extension\package.json') -Raw | ConvertFrom-Json
if ($vscode.version -ne $vsCodeExtensionVersion) { throw "VS Code package version is $($vscode.version), expected $vsCodeExtensionVersion." }
$acode = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'acode-plugin-block\plugin.json') -Raw | ConvertFrom-Json
if ($acode.version -ne $version) { throw "Acode package version is $($acode.version), expected $version." }

$releaseNotes = Join-Path $RepositoryRoot ("docs\RELEASE-$version.md")
if (-not (Test-Path -LiteralPath $releaseNotes)) { throw "Current release notes are missing: $releaseNotes" }

$workflow = Get-Content -LiteralPath (Join-Path $RepositoryRoot '.github\workflows\release.yml') -Raw
if (-not $workflow.Contains("default: v$version")) { throw 'Release workflow default tag is stale.' }

Write-Host "Version consistency checks passed for v$version."
