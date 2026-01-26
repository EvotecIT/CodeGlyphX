param(
    [string]$Configuration = "Release",
    [string]$Framework = "net8.0",
    [string]$ExamplesProject = "CodeGlyphX.Examples/CodeGlyphX.Examples.csproj",
    [string]$WebsiteRoot = "CodeGlyphX.Website/wwwroot",
    [switch]$SkipRun
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$examplesProjectPath = Join-Path $repoRoot $ExamplesProject
$websiteRootPath = Join-Path $repoRoot $WebsiteRoot

if (-not (Test-Path $examplesProjectPath)) { throw "Missing examples project at $examplesProjectPath" }
if (-not (Test-Path $websiteRootPath)) { throw "Missing website wwwroot at $websiteRootPath" }

if (-not $SkipRun) {
    Write-Host "Generating style board assets..." -ForegroundColor Cyan
    & dotnet run -c $Configuration -f $Framework --project $examplesProjectPath
    if ($LASTEXITCODE -ne 0) { throw "Examples run failed with exit code $LASTEXITCODE." }
}

$examplesOut = Join-Path $repoRoot "CodeGlyphX.Examples/bin/$Configuration/$Framework/Examples/qr-style-board"
if (-not (Test-Path $examplesOut)) { throw "Style board output not found at $examplesOut" }

$assetsOut = Join-Path $websiteRootPath "assets/style-board"
$dataOut = Join-Path $websiteRootPath "data/style-board.json"

if (-not (Test-Path $assetsOut)) {
    New-Item -ItemType Directory -Path $assetsOut -Force | Out-Null
}

Write-Host "Copying style board PNGs..." -ForegroundColor Cyan
Get-ChildItem -Path $examplesOut -Filter "*.png" | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $assetsOut -Force
}

Write-Host "Copying style board manifest..." -ForegroundColor Cyan
Copy-Item -Path (Join-Path $examplesOut "style-board.json") -Destination $dataOut -Force

Write-Host "Style board assets updated." -ForegroundColor Green
