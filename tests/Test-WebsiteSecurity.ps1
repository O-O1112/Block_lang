param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)

$requiredFiles = @(
    '_headers',
    '.assetsignore',
    '.well-known/security.txt',
    'privacy.html',
    'terms.html',
    'contact.html',
    'security.html'
)

foreach ($relativePath in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot $relativePath))) {
        throw "Missing website security file: $relativePath"
    }
}

$headers = Get-Content -LiteralPath (Join-Path $RepositoryRoot '_headers') -Raw
$requiredDirectives = @(
    'Strict-Transport-Security:',
    'X-Content-Type-Options: nosniff',
    'X-Frame-Options: DENY',
    'Cross-Origin-Opener-Policy: same-origin',
    'Cross-Origin-Resource-Policy: same-origin',
    'frame-ancestors ''none''',
    'object-src ''none''',
    'form-action ''self'''
)

foreach ($directive in $requiredDirectives) {
    if (-not $headers.Contains($directive)) {
        throw "Missing security header directive: $directive"
    }
}

if ($headers -match '(?i)script-src[^\r\n]*unsafe-inline') {
    throw 'CSP must not allow unsafe inline scripts.'
}

if ($headers -match '(?i)img-src[^;\r\n]*https:') {
    throw 'CSP image sources must not allow every HTTPS origin.'
}

$htmlFiles = Get-ChildItem -LiteralPath $RepositoryRoot -Filter '*.html' -File |
    Where-Object { $_.Name -notmatch '^google[0-9a-f]+\.html$' }
foreach ($file in $htmlFiles) {
    $html = Get-Content -LiteralPath $file.FullName -Raw

    if ($html -notmatch '(?i)<meta\b[^>]+http-equiv\s*=\s*["'']Content-Security-Policy["'']') {
        throw "HTML page is missing its GitHub-Pages-compatible CSP meta policy: $($file.Name)."
    }

    foreach ($meta in [regex]::Matches($html, '<meta\b[^>]+http-equiv\s*=\s*["'']Content-Security-Policy["''][^>]*>', [Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        if ($meta.Value -match '(?i)unsafe-inline|unsafe-eval') {
            throw "CSP meta policy must not allow unsafe inline code or eval in $($file.Name)."
        }
    }

    if ($html -match '(?i)<style\b|\sstyle\s*=' -and $html -match '(?i)style-src' -and $html -notmatch '(?i)style-src[^;>]*unsafe-inline') {
        throw "Inline CSS would be blocked by the CSP meta policy in $($file.Name)."
    }

    foreach ($tag in [regex]::Matches($html, '<script\b[^>]*>([\s\S]*?)</script>', [Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        $isInline = $tag.Value -notmatch '\bsrc\s*='
        $isJsonLd = $tag.Value -match 'application/ld\+json'
        if ($isInline -and -not $isJsonLd) {
            throw "Executable inline script found in $($file.Name)."
        }
    }

    if ($html -match '(?i)\bon[a-z]+\s*=|href\s*=\s*["'']javascript:') {
        throw "Inline event handler or javascript: URL found in $($file.Name)."
    }

    foreach ($tag in [regex]::Matches($html, '<a\b[^>]*target\s*=\s*["'']_blank["''][^>]*>', [Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        if ($tag.Value -notmatch '(?i)noopener') {
            throw "External target=_blank link without noopener in $($file.Name)."
        }
    }
}

Write-Host 'Website security checks passed.'
