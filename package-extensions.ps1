param(
    [string]$OutputDirectory = "",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = $root }
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid release version: $Version" }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$stage = Join-Path ([IO.Path]::GetTempPath()) ("block-package-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path (Join-Path $stage "extension") | Out-Null

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
    Compress-Archive -Path (Join-Path $acodeStage "*") -DestinationPath $acode -CompressionLevel Optimal
    Write-Output "Created $vsix"
    Write-Output "Created $acode"
}
finally {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
