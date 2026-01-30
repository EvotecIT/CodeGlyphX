param(
    [string]$RepoRoot,
    [switch]$Strict
)

$ErrorActionPreference = "Stop"

if (-not $RepoRoot) {
    $RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
}

$assetsPath = Join-Path $RepoRoot "Assets" "Data"
$apiPath = Join-Path $RepoRoot "CodeGlyphX.Website" "wwwroot" "api"

$files = @("faq.json", "showcase.json")
$hasMismatch = $false

foreach ($file in $files) {
    $assetFile = Join-Path $assetsPath $file
    $apiFile = Join-Path $apiPath $file

    if (-not (Test-Path $assetFile)) {
        Write-Warning "Missing source data: $assetFile"
        $hasMismatch = $true
        continue
    }

    if (-not (Test-Path $apiFile)) {
        Write-Warning "Missing API data: $apiFile (run Build/Build-Website.ps1 to sync)"
        $hasMismatch = $true
        continue
    }

    $assetHash = (Get-FileHash -Path $assetFile -Algorithm SHA256).Hash
    $apiHash = (Get-FileHash -Path $apiFile -Algorithm SHA256).Hash

    if ($assetHash -ne $apiHash) {
        Write-Warning "API data out of sync for $file. Source of truth is Assets/Data/$file."
        $hasMismatch = $true
    }
}

if ($Strict -and $hasMismatch) {
    throw "API data sources are out of sync."
}
