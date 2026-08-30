param(
    [string]$OutputDirectory = "",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = $root }
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+(?:\.\d+)?$') { throw "Invalid release version: $Version" }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$stage = Join-Path ([IO.Path]::GetTempPath()) ("block-package-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path (Join-Path $stage "extension") | Out-Null

function Get-VsCodeExtensionVersion([string]$ReleaseVersion) {
    $parts = $ReleaseVersion -split '\.'
    if ($parts.Count -eq 3) { return $ReleaseVersion }

    # VS Code only accepts major.minor.patch extension versions. A four-part
    # engine revision therefore maps to the next patch release for the bundled
    # editor extension, keeping upgrades installable instead of publishing an
    # invalid VSIX manifest.
    return ("{0}.{1}.{2}" -f $parts[0], $parts[1], (([int]$parts[2]) + 1))
}

$vsCodeExtensionVersion = Get-VsCodeExtensionVersion $Version

function Write-JsonNoBom([string]$Path, [object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20
    [IO.File]::WriteAllText($Path, $json + "`n", (New-Object Text.UTF8Encoding($false)))
}

try {
    $vsixSource = Join-Path $root "block-vscode-extension"
    Copy-Item -LiteralPath (Join-Path $vsixSource "extension.vsixmanifest") -Destination $stage
    Copy-Item -LiteralPath (Join-Path $vsixSource "[Content_Types].xml") -Destination $stage
    Get-ChildItem -LiteralPath $vsixSource -Force | Where-Object {
        $_.Name -notin @("extension.vsixmanifest", "[Content_Types].xml", "block-language-$Version.vsix")
    } | Copy-Item -Destination (Join-Path $stage "extension") -Recurse -Force
    $license = Join-Path $root "LICENSE"
    if (Test-Path -LiteralPath $license) {
        Copy-Item -LiteralPath $license -Destination (Join-Path $stage "extension") -Force
    }

    # Release version is injected into staged files so a tag cannot create
    # correctly named packages with stale internal metadata.
    $vsixPackagePath = Join-Path $stage "extension\package.json"
    $vsixPackage = Get-Content -LiteralPath $vsixPackagePath -Raw | ConvertFrom-Json
    $vsixPackage.version = $vsCodeExtensionVersion
    if ($vsixPackage.description) {
        $vsixPackage.description = [regex]::Replace($vsixPackage.description, 'v\d+\.\d+\.\d+(?:\.\d+)?', "v$Version")
    }
    Write-JsonNoBom $vsixPackagePath $vsixPackage

    $vsixManifestPath = Join-Path $stage "extension.vsixmanifest"
    [xml]$vsixManifest = Get-Content -LiteralPath $vsixManifestPath -Raw
    $vsixManifest.PackageManifest.Metadata.Identity.SetAttribute('Version', $vsCodeExtensionVersion)
    $vsixManifest.PackageManifest.Metadata.Description.InnerText = [regex]::Replace(
        $vsixManifest.PackageManifest.Metadata.Description.InnerText,
        'v\d+\.\d+\.\d+(?:\.\d+)?',
        "v$Version"
    )
    $vsixManifest.Save($vsixManifestPath)

    $vsix = Join-Path $OutputDirectory "block-language-$Version.vsix"
    $vsixZip = Join-Path $OutputDirectory "block-language-$Version.package.zip"
    if (Test-Path -LiteralPath $vsix) { Remove-Item -LiteralPath $vsix -Force }
    if (Test-Path -LiteralPath $vsixZip) { Remove-Item -LiteralPath $vsixZip -Force }
    # Compress-Archive only accepts .zip output.  Rename the completed zip
    # afterwards to the VS Code-supported .vsix extension.
    Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $vsixZip -CompressionLevel Optimal
    Move-Item -LiteralPath $vsixZip -Destination $vsix -Force

    $acode = Join-Path $OutputDirectory "acode-plugin-block-$Version.zip"
    if (Test-Path -LiteralPath $acode) { Remove-Item -LiteralPath $acode -Force }
    $acodeStage = Join-Path $stage "acode-plugin-block"
    New-Item -ItemType Directory -Force -Path $acodeStage | Out-Null
    Copy-Item -Path (Join-Path $root "acode-plugin-block\*") -Destination $acodeStage -Recurse -Force
    if (Test-Path -LiteralPath $license) {
        Copy-Item -LiteralPath $license -Destination $acodeStage -Force
    }
    $acodeManifestPath = Join-Path $acodeStage 'plugin.json'
    $acodeManifest = Get-Content -LiteralPath $acodeManifestPath -Raw | ConvertFrom-Json
    $acodeManifest.version = $Version
    Write-JsonNoBom $acodeManifestPath $acodeManifest

    $acodeMainPath = Join-Path $acodeStage 'main.js'
    $acodeMain = Get-Content -LiteralPath $acodeMainPath -Raw
    $acodeMain = [regex]::Replace($acodeMain, "const BLOCK_PLUGIN_VERSION = '[^']+';", "const BLOCK_PLUGIN_VERSION = '$Version';")
    [IO.File]::WriteAllText($acodeMainPath, $acodeMain, (New-Object Text.UTF8Encoding($false)))

    $acodeReadmePath = Join-Path $acodeStage 'readme.md'
    if (Test-Path -LiteralPath $acodeReadmePath) {
        $acodeReadme = Get-Content -LiteralPath $acodeReadmePath -Raw
        $acodeReadme = [regex]::Replace($acodeReadme, 'v\d+\.\d+\.\d+(?:\.\d+)?', "v$Version")
        [IO.File]::WriteAllText($acodeReadmePath, $acodeReadme, (New-Object Text.UTF8Encoding($false)))
    }
    Compress-Archive -Path (Join-Path $acodeStage "*") -DestinationPath $acode -CompressionLevel Optimal
    Write-Output "Created $vsix"
    Write-Output "Created $acode"
}
finally {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
