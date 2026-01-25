param(
    [string]$IndexPath = "CodeGlyphX.Website/wwwroot/index.html"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$fullPath = if ([IO.Path]::IsPathRooted($IndexPath)) { $IndexPath } else { Join-Path $repoRoot $IndexPath }

if (-not (Test-Path $fullPath)) {
    throw "Blazor host index.html not found at $fullPath"
}

$content = Get-Content -Path $fullPath -Raw
$errors = @()

if ($content -notmatch 'blazor\.webassembly\.js') {
    $errors += "Missing blazor.webassembly.js reference."
}

if ($content -notmatch '<base href="/"\s*/?>') {
    $errors += 'Missing <base href="/">.'
}

if ($content -notmatch 'id="app"') {
    $errors += "Missing #app root element."
}

if ($errors.Count -gt 0) {
    throw ("Blazor host index.html validation failed:`n - " + ($errors -join "`n - "))
}

Write-Host "Blazor host index.html validation passed." -ForegroundColor Green
