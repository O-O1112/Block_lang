param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..'),
    [switch]$Check,
    [string]$Generated = ""
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$packagesRoot = Join-Path $RepositoryRoot 'registry\packages'
$indexPath = Join-Path $RepositoryRoot 'registry\index.json'
if (-not (Test-Path -LiteralPath $packagesRoot -PathType Container)) {
    throw "Package registry directory not found: $packagesRoot"
}

function Get-RequiredString([object]$Object, [string]$Name, [string]$Context) {
    $value = [string]$Object.$Name
    if ([string]::IsNullOrWhiteSpace($value)) { throw "$Context is missing '$Name'." }
    return $value.Trim()
}

function Test-PackageName([string]$Name) {
    return $Name -match '^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$'
}

function Get-Sha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-RelativePath([string]$Root, [string]$Path) {
    $rootPrefix = $Root.TrimEnd('\', '/') + '\'
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Package entry escapes its package directory: $Path"
    }
    return $fullPath.Substring($rootPrefix.Length).Replace('\', '/')
}

if ([string]::IsNullOrWhiteSpace($Generated)) { $Generated = (Get-Date).ToString('yyyy-MM-dd') }
if ($Generated -notmatch '^\d{4}-\d{2}-\d{2}$') { throw "Invalid generated date: $Generated" }

$entries = New-Object System.Collections.Generic.List[object]
$directories = Get-ChildItem -LiteralPath $packagesRoot -Directory | Sort-Object Name
foreach ($directory in $directories) {
    if ((Get-Item -LiteralPath $directory.FullName).Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)) {
        throw "Package directory must not be a reparse point: $($directory.FullName)"
    }

    $manifestPath = Join-Path $directory.FullName 'block.package.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Package is missing block.package.json: $($directory.Name)"
    }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $name = Get-RequiredString $manifest 'name' $directory.Name
    if (-not (Test-PackageName $name)) { throw "Invalid package name '$name'." }
    if (-not [string]::Equals($name, $directory.Name, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Package name does not match directory: $($directory.Name) / $name"
    }
    $version = Get-RequiredString $manifest 'version' $name
    $main = Get-RequiredString $manifest 'main' $name
    if ([IO.Path]::IsPathRooted($main) -or $main.Contains('..')) {
        throw "Package main entry must be a relative path without '..': $name"
    }
    $entryPath = [IO.Path]::GetFullPath((Join-Path $directory.FullName $main))
    $relativeEntry = Get-RelativePath $directory.FullName $entryPath
    if (-not (Test-Path -LiteralPath $entryPath -PathType Leaf)) { throw "Package entry is missing: $name / $main" }
    if ((Get-Item -LiteralPath $entryPath).Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)) {
        throw "Package entry must not be a reparse point: $entryPath"
    }
    $license = Get-RequiredString $manifest 'license' $name
    if ($license -ne 'MIT') { throw "Official registry v1 only accepts MIT packages: $name" }
    $repository = Get-RequiredString $manifest 'repository' $name
    if ($repository -ne 'https://github.com/O-O1112/Block_lang') {
        throw "Package repository must be the official Block_lang repository: $name"
    }

    $permissions = @($manifest.permissions | ForEach-Object { [string]$_ })
    $keywords = @($manifest.keywords | ForEach-Object { [string]$_ })
    $manifestUrl = "https://raw.githubusercontent.com/O-O1112/Block_lang/main/registry/packages/$name/block.package.json"
    $entryUrl = "https://raw.githubusercontent.com/O-O1112/Block_lang/main/registry/packages/$name/$relativeEntry"
    $entries.Add([ordered]@{
        name = $name
        version = $version
        description = [string]$manifest.description
        engine = [string]$manifest.engine
        license = $license
        repository = $repository
        permissions = $permissions
        keywords = $keywords
        manifestUrl = $manifestUrl
        manifestSha256 = Get-Sha256 $manifestPath
        entryUrl = $entryUrl
        entrySha256 = Get-Sha256 $entryPath
    })
}

$packageArray = $entries.ToArray()
$index = [ordered]@{
    schema = 'block-registry/v1'
    generated = $Generated
    packages = $packageArray
}
$json = $index | ConvertTo-Json -Depth 8

if ($Check) {
    if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) { throw "Registry index is missing: $indexPath" }
    $existing = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json | ConvertTo-Json -Depth 8
    $expected = $json | ConvertFrom-Json | ConvertTo-Json -Depth 8
    if ($existing -ne $expected) { throw 'registry/index.json is stale; run tools/build-registry-index.ps1.' }
    Write-Host 'Registry index is current.'
    exit 0
}

[IO.File]::WriteAllText($indexPath, $json + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
Write-Host "Wrote $indexPath with $($entries.Count) package(s)."
