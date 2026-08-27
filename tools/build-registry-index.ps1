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

function Get-NormalizedTextSha256([string]$Path) {
    # Registry sources are UTF-8 text. Normalize checkout line endings so the
    # digest describes the repository content consistently on Windows and Unix.
    $text = [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8)
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [Text.Encoding]::UTF8.GetBytes($text)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
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
        manifestSha256 = Get-NormalizedTextSha256 $manifestPath
        entryUrl = $entryUrl
        entrySha256 = Get-NormalizedTextSha256 $entryPath
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
    $existing = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json
    $expected = $json | ConvertFrom-Json

    # Compare the catalog as data rather than comparing ConvertTo-Json output.
    # Windows PowerShell and PowerShell 7 can serialize the same object with
    # different whitespace, escaping, or line endings. Those formatting-only
    # differences must not make a digest-pinned catalog fail CI.
    if ([string]$existing.schema -ne [string]$expected.schema) {
        throw 'registry/index.json has an unexpected schema.'
    }
    if ([string]$existing.generated -ne [string]$expected.generated) {
        throw 'registry/index.json has an unexpected generated date.'
    }

    $existingPackages = @($existing.packages)
    $expectedPackages = @($expected.packages)
    if ($existingPackages.Count -ne $expectedPackages.Count) {
        throw 'registry/index.json has the wrong package count; run tools/build-registry-index.ps1.'
    }

    $scalarFields = @('name', 'version', 'description', 'engine', 'license', 'repository', 'manifestUrl', 'manifestSha256', 'entryUrl', 'entrySha256')
    $arrayFields = @('permissions', 'keywords')
    for ($index = 0; $index -lt $expectedPackages.Count; $index++) {
        $expectedPackage = $expectedPackages[$index]
        $existingPackage = $existingPackages[$index]
        $packageName = [string]$expectedPackage.name
        foreach ($field in $scalarFields) {
            if ([string]$existingPackage.$field -ne [string]$expectedPackage.$field) {
                throw "registry/index.json is stale for package '$packageName' field '$field'; run tools/build-registry-index.ps1."
            }
        }
        foreach ($field in $arrayFields) {
            $expectedValues = @($expectedPackage.$field | ForEach-Object { [string]$_ })
            $existingValues = @($existingPackage.$field | ForEach-Object { [string]$_ })
            if ($existingValues.Count -ne $expectedValues.Count) {
                throw "registry/index.json is stale for package '$packageName' field '$field'; run tools/build-registry-index.ps1."
            }
            for ($valueIndex = 0; $valueIndex -lt $expectedValues.Count; $valueIndex++) {
                if ($existingValues[$valueIndex] -ne $expectedValues[$valueIndex]) {
                    throw "registry/index.json is stale for package '$packageName' field '$field'; run tools/build-registry-index.ps1."
                }
            }
        }
    }
    Write-Host 'Registry index is current.'
    exit 0
}

[IO.File]::WriteAllText($indexPath, $json + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
Write-Host "Wrote $indexPath with $($entries.Count) package(s)."
