#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and serve the CodeGlyphX PowerForge website.

.PARAMETER Serve
    Build the full pipeline and start the development server.

.PARAMETER Port
    Server port (default: 8080).

.EXAMPLE
    ./build.ps1
    ./build.ps1 -Serve
    ./build.ps1 -Serve -Port 3000
#>

param(
    [switch]$Serve,
    [int]$Port = 8080,
    [switch]$SkipBuildTool
)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot

$PowerForge = $null
$PowerForgeRoot = 'C:\Support\GitHub\PSPublishModule'
$PowerForgeCliProject = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\PowerForge.Web.Cli.csproj'
$PowerForgeReleaseExe = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\bin\Release\net10.0\PowerForge.Web.Cli.exe'
$PowerForgeDebugExe = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\bin\Debug\net10.0\PowerForge.Web.Cli.exe'

if (-not $SkipBuildTool -and (Test-Path $PowerForgeCliProject)) {
    Write-Host "Building PowerForge.Web.Cli..." -ForegroundColor Cyan
    dotnet build $PowerForgeCliProject -c Release | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "PowerForge.Web.Cli build failed (exit code $LASTEXITCODE)" }
}

if (Test-Path $PowerForgeReleaseExe) {
    $PowerForge = $PowerForgeReleaseExe
} elseif (Test-Path $PowerForgeDebugExe) {
    $PowerForge = $PowerForgeDebugExe
} else {
    $PowerForge = Get-Command powerforge-web -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
}

if (-not $PowerForge) {
    Write-Error 'PowerForge.Web.Cli not found. Build PSPublishModule first or install the global tool.'
    exit 1
}

Write-Host "Using: $PowerForge" -ForegroundColor DarkGray

try {
    if ($Serve) {
        Write-Host 'Building website...' -ForegroundColor Cyan
        & $PowerForge pipeline --config pipeline.json
        if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)" }
        Write-Host "Starting dev server on http://localhost:$Port ..." -ForegroundColor Cyan
        & $PowerForge serve --path _site --port $Port
    } else {
        Write-Host 'Building website...' -ForegroundColor Cyan
        & $PowerForge pipeline --config pipeline.json
        if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)" }
        Write-Host 'Build complete -> _site/' -ForegroundColor Green
    }
} finally {
    Pop-Location
}
