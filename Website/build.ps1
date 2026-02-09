#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and serve the CodeGlyphX website.

.PARAMETER Serve
    Build the full pipeline and start the development server.

.PARAMETER Fast
    Run the pipeline with `--fast` (recommended for local iteration).
    When `-Serve` is used, fast mode is enabled by default unless `-NoFast` is set.

.PARAMETER Dev
    Run the pipeline with `--dev` (implies fast mode and enables pipeline mode 'dev').
    When `-Serve` is used, dev mode is enabled by default unless `-NoDev` is set.

.PARAMETER NoFast
    Disable fast mode when `-Serve` is used.

.PARAMETER NoDev
    Disable dev mode when `-Serve` is used.

.PARAMETER Only
    Run only the specified pipeline tasks (comma/semicolon separated), for example: build,verify.

.PARAMETER Skip
    Skip the specified pipeline tasks (comma/semicolon separated), for example: optimize,doctor.

.PARAMETER Watch
    Run the pipeline in watch mode (rebuild on file changes).
    When combined with `-Serve`, starts the static server and keeps rebuilding in the foreground.

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
    [switch]$Watch,
    [switch]$Dev,
    [switch]$Fast,
    [switch]$NoDev,
    [switch]$NoFast,
    [int]$Port = 8080,
    [string[]]$Only = @(),
    [string[]]$Skip = @(),
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

function Assert-SiteOutput {
    param(
        [Parameter(Mandatory)]
        [string]$SiteRoot
    )

    $notFoundPage = Join-Path $SiteRoot '404.html'
    if (-not (Test-Path -LiteralPath $notFoundPage -PathType Leaf)) {
        throw "Build validation failed: expected '$notFoundPage' for GitHub Pages 404 handling."
    }
}

try {
    $UseDev = ($Dev -or ($Serve -and -not $NoDev))
    $UseFast = ($Fast -or ($Serve -and -not $NoFast))
    $IsCI = ($env:CI -and $env:CI.ToString().ToLowerInvariant() -eq 'true') -or
            ($env:GITHUB_ACTIONS -and $env:GITHUB_ACTIONS.ToString().ToLowerInvariant() -eq 'true') -or
            ($env:TF_BUILD -and $env:TF_BUILD.ToString().ToLowerInvariant() -eq 'true')

    $pipelineArgsBase = @('pipeline', '--config', 'pipeline.json', '--profile')
    if ($UseDev) {
        $pipelineArgsBase += '--dev'
    } elseif ($UseFast) {
        $pipelineArgsBase += '--fast'
    }
    if ($IsCI -and -not $UseDev) {
        # CI contract: run any steps gated behind modes:["ci"] (strict verify, baselines, budgets).
        $pipelineArgsBase += @('--mode', 'ci')
    }
    if ($Only -and $Only.Count -gt 0) {
        $pipelineArgsBase += @('--only', ($Only -join ','))
    }
    if ($Skip -and $Skip.Count -gt 0) {
        $pipelineArgsBase += @('--skip', ($Skip -join ','))
    }

    $pipelineArgs = @($pipelineArgsBase)
    if ($Watch) {
        $pipelineArgs += '--watch'
    }

    if ($Serve) {
        Write-Host 'Building website...' -ForegroundColor Cyan
        if ($Watch) {
            # Ensure _site exists before we start the server.
            & $PowerForge @PowerForgeArgs @pipelineArgsBase
        } else {
            & $PowerForge @PowerForgeArgs @pipelineArgs
        }
        if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)" }
        Assert-SiteOutput -SiteRoot (Join-Path $PSScriptRoot '_site')
        Write-Host "Starting dev server on http://localhost:$Port ..." -ForegroundColor Cyan

        $serveArgs = @($PowerForgeArgs + @('serve', '--path', '_site', '--port', $Port))
        $serveProcess = Start-Process -FilePath $PowerForge -ArgumentList $serveArgs -NoNewWindow -PassThru
        try {
            if ($Watch) {
                Write-Host 'Watching for changes...' -ForegroundColor Cyan
                & $PowerForge @PowerForgeArgs @pipelineArgs
            } else {
                Wait-Process -Id $serveProcess.Id
            }
        } finally {
            if ($serveProcess -and -not $serveProcess.HasExited) {
                Stop-Process -Id $serveProcess.Id -Force -ErrorAction SilentlyContinue
            }
        }
    } else {
        Write-Host 'Building website...' -ForegroundColor Cyan
        & $PowerForge @PowerForgeArgs @pipelineArgs
        if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)" }
        Assert-SiteOutput -SiteRoot (Join-Path $PSScriptRoot '_site')
        Write-Host 'Build complete -> _site/' -ForegroundColor Green
    }
} finally {
    Pop-Location
}
