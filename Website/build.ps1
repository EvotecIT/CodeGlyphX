#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and serve the CodeGlyphX website.

.PARAMETER Serve
    Build the full pipeline and start the development server.

.PARAMETER Port
    Server port (default: 8080).

.PARAMETER PowerForgeRoot
    Root folder of PSPublishModule (overrides $env:POWERFORGE_ROOT).

.EXAMPLE
    ./build.ps1
    ./build.ps1 -Serve
    ./build.ps1 -Serve -Port 3000
#>

param(
    [switch]$Serve,
    [int]$Port = 8080,
    [switch]$SkipBuildTool,
    [string]$PowerForgeRoot = $env:POWERFORGE_ROOT
)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot

# Resolve PowerForge.Web.Cli executable (prefer local fresh build)
$PowerForge = $null
$PowerForgeArgs = @()
if ([string]::IsNullOrWhiteSpace($PowerForgeRoot)) {
    $candidate = Join-Path $PSScriptRoot '..\PSPublishModule'
    if (Test-Path $candidate) {
        $PowerForgeRoot = (Resolve-Path $candidate).Path
    } else {
        $scriptRoot = (Resolve-Path $PSScriptRoot).Path
        $parent = Split-Path -Parent $scriptRoot
        if ($parent) {
            $grandParent = Split-Path -Parent $parent
            if ($grandParent) {
                $candidate = Join-Path $grandParent 'PSPublishModule'
                if (Test-Path $candidate) {
                    $PowerForgeRoot = (Resolve-Path $candidate).Path
                }
            }
        }
    }
}
if (-not [string]::IsNullOrWhiteSpace($PowerForgeRoot)) {
    $PowerForgeCliProject = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\PowerForge.Web.Cli.csproj'
    $PowerForgeReleaseExe = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\bin\Release\net10.0\PowerForge.Web.Cli.exe'
    $PowerForgeReleaseAppHost = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\bin\Release\net10.0\PowerForge.Web.Cli'
    $PowerForgeReleaseDll = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\bin\Release\net10.0\PowerForge.Web.Cli.dll'
    $PowerForgeDebugExe = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\bin\Debug\net10.0\PowerForge.Web.Cli.exe'
    $PowerForgeDebugAppHost = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\bin\Debug\net10.0\PowerForge.Web.Cli'
    $PowerForgeDebugDll = Join-Path $PowerForgeRoot 'PowerForge.Web.Cli\bin\Debug\net10.0\PowerForge.Web.Cli.dll'
}

if (-not $SkipBuildTool -and $PowerForgeCliProject -and (Test-Path $PowerForgeCliProject)) {
    Write-Host "Building PowerForge.Web.Cli..." -ForegroundColor Cyan
    dotnet build $PowerForgeCliProject -c Release | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "PowerForge.Web.Cli build failed (exit code $LASTEXITCODE)" }
}

if ($PowerForgeReleaseExe -and (Test-Path $PowerForgeReleaseExe)) {
    $PowerForge = $PowerForgeReleaseExe
} elseif ($PowerForgeReleaseAppHost -and (Test-Path $PowerForgeReleaseAppHost)) {
    $PowerForge = $PowerForgeReleaseAppHost
} elseif ($PowerForgeReleaseDll -and (Test-Path $PowerForgeReleaseDll)) {
    $PowerForge = 'dotnet'
    $PowerForgeArgs = @($PowerForgeReleaseDll)
} elseif ($PowerForgeDebugExe -and (Test-Path $PowerForgeDebugExe)) {
    $PowerForge = $PowerForgeDebugExe
} elseif ($PowerForgeDebugAppHost -and (Test-Path $PowerForgeDebugAppHost)) {
    $PowerForge = $PowerForgeDebugAppHost
} elseif ($PowerForgeDebugDll -and (Test-Path $PowerForgeDebugDll)) {
    $PowerForge = 'dotnet'
    $PowerForgeArgs = @($PowerForgeDebugDll)
} else {
    $PowerForge = Get-Command powerforge-web -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
}

if (-not $PowerForge) {
    Write-Error 'PowerForge.Web.Cli not found. Build PSPublishModule first, install the global tool, or pass -PowerForgeRoot.'
    exit 1
}

Write-Host "Using: $PowerForge $($PowerForgeArgs -join ' ')" -ForegroundColor DarkGray

try {
    if ($Serve) {
        Write-Host 'Building website...' -ForegroundColor Cyan
        & $PowerForge @PowerForgeArgs pipeline --config pipeline.json
        if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)" }
        Write-Host "Starting dev server on http://localhost:$Port ..." -ForegroundColor Cyan
        & $PowerForge @PowerForgeArgs serve --path _site --port $Port
    } else {
        Write-Host 'Building website...' -ForegroundColor Cyan
        & $PowerForge @PowerForgeArgs pipeline --config pipeline.json
        if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)" }
        Write-Host 'Build complete -> _site/' -ForegroundColor Green
    }
} finally {
    Pop-Location
}
