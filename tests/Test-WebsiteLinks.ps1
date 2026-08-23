param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$version = (Get-Content -LiteralPath (Join-Path $RepositoryRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Invalid repository version: $version"
}

$releaseBase = "https://github.com/O-O1112/Block_lang/releases/download/v$version/"
$requiredLinks = @{
    'index.html' = @(
        "$releaseBase" + "BlockSetup-v$version.exe"
    )
    'downloads.html' = @(
        "$releaseBase" + "BlockSetup-v$version.exe"
        "$releaseBase" + 'block-lite.zip'
        "$releaseBase" + 'block.zip'
        "$releaseBase" + 'block-plus.zip'
        "$releaseBase" + "block-language-$version.vsix"
        "$releaseBase" + "acode-plugin-block-$version.zip"
    )
}

foreach ($relativePath in $requiredLinks.Keys) {
    $path = Join-Path $RepositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Website file not found: $relativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    foreach ($link in $requiredLinks[$relativePath]) {
        if (-not $content.Contains($link)) {
            throw "Missing v$version release link in ${relativePath}: $link"
        }
    }
}

$legacyRelativeLinks = @(
    'href="BlockSetup.exe"',
    'href="block-lite.zip"',
    'href="block.zip"',
    'href="block-plus.zip"'
)
foreach ($relativePath in @('index.html', 'downloads.html')) {
    $content = Get-Content -LiteralPath (Join-Path $RepositoryRoot $relativePath) -Raw
    foreach ($legacyLink in $legacyRelativeLinks) {
        if ($content.Contains($legacyLink)) {
            throw "Legacy root download link remains in ${relativePath}: $legacyLink"
        }
    }
}

Write-Host "Website release links verified for v$version."
