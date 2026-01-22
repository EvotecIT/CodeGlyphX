param(
    [string]$Framework = "net8.0",
    [string]$Configuration = "Release",
    [string]$ArtifactsRoot,
    [switch]$NoBase,
    [switch]$NoCompare,
    [switch]$CompareZXing,
    [switch]$CompareQRCoder,
    [switch]$CompareBarcoder,
    [switch]$Full,
    [string]$BaseFilter = "*",
    [string]$CompareFilter = "*Compare*"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $PSScriptRoot "..\CodeGlyphX.Benchmarks\CodeGlyphX.Benchmarks.csproj"

if ([string]::IsNullOrWhiteSpace($ArtifactsRoot)) {
    $ArtifactsRoot = Join-Path $PSScriptRoot "BenchmarkResults"
}

$os = "unknown"
if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
    $os = "windows"
} elseif ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)) {
    $os = "linux"
} elseif ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
    $os = "macos"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$artifactsPath = Join-Path $ArtifactsRoot "$os-$timestamp"
New-Item -ItemType Directory -Force -Path $artifactsPath | Out-Null

$benchQuick = -not $Full
$runMode = if ($benchQuick) { "quick" } else { "full" }
$quickProps = @()
$quickEnv = @{}
if ($benchQuick) {
    $quickProps += "/p:BenchQuick=true"
    $quickEnv["BENCH_QUICK"] = "true"
}

function Invoke-Benchmark {
    param(
        [string[]]$MsBuildProps,
        [string]$Filter,
        [string]$Label,
        [hashtable]$EnvVars
    )

    Write-Host ""
    Write-Host "== $Label =="
    $previous = @{}
    if ($EnvVars) {
        foreach ($key in $EnvVars.Keys) {
            $previous[$key] = [System.Environment]::GetEnvironmentVariable($key)
            [System.Environment]::SetEnvironmentVariable($key, $EnvVars[$key])
        }
    }
    $args = @(
        "run",
        "-c", $Configuration,
        "--framework", $Framework,
        "--project", $projectPath
    )

    if ($MsBuildProps) {
        $args += $MsBuildProps
    }

    $args += @("--", "--filter", $Filter, "--artifacts", $artifactsPath)
    & dotnet @args
    if ($EnvVars) {
        foreach ($key in $EnvVars.Keys) {
            [System.Environment]::SetEnvironmentVariable($key, $previous[$key])
        }
    }
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run failed: $Label"
    }
}

Push-Location $repoRoot
try {
    if (-not $NoBase) {
        Invoke-Benchmark -MsBuildProps $quickProps -Filter $BaseFilter -Label "Baseline (CodeGlyphX only)" -EnvVars $quickEnv
    }

    if (-not $NoCompare) {
        $props = @()
        $envVars = @{}
        if ($CompareZXing -or $CompareQRCoder -or $CompareBarcoder) {
            if ($CompareZXing) { $props += "/p:CompareZXing=true" }
            if ($CompareQRCoder) { $props += "/p:CompareQRCoder=true" }
            if ($CompareBarcoder) { $props += "/p:CompareBarcoder=true" }
            if ($CompareZXing) { $envVars["COMPARE_ZXING"] = "true" }
            if ($CompareQRCoder) { $envVars["COMPARE_QRCODER"] = "true" }
            if ($CompareBarcoder) { $envVars["COMPARE_BARCODER"] = "true" }
        } else {
            $props += "/p:CompareExternal=true"
            $envVars["COMPARE_EXTERNAL"] = "true"
        }

        $props += $quickProps
        foreach ($key in $quickEnv.Keys) {
            $envVars[$key] = $quickEnv[$key]
        }

        Invoke-Benchmark -MsBuildProps $props -Filter $CompareFilter -Label "External comparisons" -EnvVars $envVars
    }
} finally {
    Pop-Location
}

$reportScript = Join-Path $PSScriptRoot "Generate-BenchmarkReport.ps1"
if (Test-Path $reportScript) {
    & $reportScript -ArtifactsPath $artifactsPath -Framework $Framework -Configuration $Configuration -RunMode $runMode
}
