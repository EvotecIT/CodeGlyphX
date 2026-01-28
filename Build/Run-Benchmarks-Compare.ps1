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
    [switch]$AllowPartial,
    [switch]$SkipPreflight,
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
$quickEnv = @{ BENCH_QUICK = if ($benchQuick) { "true" } else { "false" } }
if ($benchQuick) {
    $quickProps += "/p:BenchQuick=true"
}

# Keep the default quick run focused on compare suites so it stays fast.
$baseFilterBound = $PSBoundParameters.ContainsKey("BaseFilter")
if ($benchQuick -and -not $baseFilterBound) {
    $BaseFilter = "*CompareBenchmarks*"
}

$expectedCompare = @(
    "QrCompareBenchmarks",
    "QrDecodeCleanCompareBenchmarks",
    "QrDecodeNoisyCompareBenchmarks",
    "QrDecodeStressCompareBenchmarks",
    "Code128CompareBenchmarks",
    "Code39CompareBenchmarks",
    "Code93CompareBenchmarks",
    "EanCompareBenchmarks",
    "UpcACompareBenchmarks",
    "DataMatrixCompareBenchmarks",
    "Pdf417CompareBenchmarks",
    "AztecCompareBenchmarks"
)

function Test-ResultFileReady([string]$path) {
    if (-not (Test-Path $path)) { return $false }
    try {
        $lines = Get-Content -Path $path -TotalCount 2 -Encoding UTF8
        if (-not $lines -or $lines.Count -lt 2) { return $false }
        return $true
    } catch {
        return $false
    }
}

function Get-MissingCompareClasses([string]$resultsPath, [string[]]$expectedClasses) {
    $missing = @()
    foreach ($className in $expectedClasses) {
        $fileName = "CodeGlyphX.Benchmarks.$className-report.csv"
        $fullPath = Join-Path $resultsPath $fileName
        if (-not (Test-ResultFileReady $fullPath)) {
            $missing += $className
        }
    }
    return $missing
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

function Invoke-Preflight {
    param(
        [string[]]$MsBuildProps,
        [hashtable]$EnvVars
    )

    Write-Host ""
    Write-Host "== Preflight (Compare dependencies) =="
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

    $args += @("--", "--preflight")
    & dotnet @args

    if ($EnvVars) {
        foreach ($key in $EnvVars.Keys) {
            [System.Environment]::SetEnvironmentVariable($key, $previous[$key])
        }
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Preflight failed."
    }
}

function Invoke-PackRunner {
    param(
        [string[]]$MsBuildProps,
        [hashtable]$EnvVars,
        [string]$Label
    )

    Write-Host ""
    Write-Host "== $Label =="

    $reportsDir = Join-Path $artifactsPath "pack-runner"
    New-Item -ItemType Directory -Force -Path $reportsDir | Out-Null

    $packEnv = @{}
    if ($EnvVars) {
        foreach ($key in $EnvVars.Keys) {
            $packEnv[$key] = $EnvVars[$key]
        }
    }
    $packEnv["CODEGLYPHX_PACK_REPORTS_DIR"] = $reportsDir
    $packEnv["BENCH_QUICK"] = if ($benchQuick) { "true" } else { "false" }

    $previous = @{}
    foreach ($key in $packEnv.Keys) {
        $previous[$key] = [System.Environment]::GetEnvironmentVariable($key)
        [System.Environment]::SetEnvironmentVariable($key, $packEnv[$key])
    }

    try {
        $args = @(
            "run",
            "-c", $Configuration,
            "--framework", $Framework,
            "--project", $projectPath
        )
        if ($MsBuildProps) {
            $args += $MsBuildProps
        }
        $args += @("--", "--pack-runner", "--mode", $runMode, "--format", "json", "--reports-dir", $reportsDir)
        & dotnet @args
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet run failed: $Label"
        }
    } finally {
        foreach ($key in $packEnv.Keys) {
            [System.Environment]::SetEnvironmentVariable($key, $previous[$key])
        }
    }
}

Push-Location $repoRoot
try {
    $packProps = @()
    $packEnvVars = @{}
    if (-not $NoBase) {
        Invoke-Benchmark -MsBuildProps $quickProps -Filter $BaseFilter -Label "Baseline (CodeGlyphX only)" -EnvVars $quickEnv
        $packProps = @($quickProps)
        foreach ($key in $quickEnv.Keys) { $packEnvVars[$key] = $quickEnv[$key] }
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

        if (-not $SkipPreflight) {
            Invoke-Preflight -MsBuildProps $props -EnvVars $envVars
        }

        Invoke-Benchmark -MsBuildProps $props -Filter $CompareFilter -Label "External comparisons" -EnvVars $envVars
        $packProps = @($props)
        $packEnvVars = @{}
        foreach ($key in $envVars.Keys) { $packEnvVars[$key] = $envVars[$key] }

        $resultsPath = Join-Path $artifactsPath "results"
        $shouldEnforceCompare = -not $AllowPartial -and $CompareFilter -eq "*Compare*"
        if ($shouldEnforceCompare) {
            $missing = Get-MissingCompareClasses -resultsPath $resultsPath -expectedClasses $expectedCompare
            if ($missing.Count -gt 0) {
                Write-Warning "Missing compare results after initial run: $($missing -join ', '). Re-running missing benchmarks..."
                foreach ($className in $missing) {
                    Invoke-Benchmark -MsBuildProps $props -Filter "*$className*" -Label "Compare (retry $className)" -EnvVars $envVars
                }
                $missing = Get-MissingCompareClasses -resultsPath $resultsPath -expectedClasses $expectedCompare
                if ($missing.Count -gt 0) {
                    throw "Missing compare results after retry: $($missing -join ', ')."
                }
            }
        }
    }
    Invoke-PackRunner -MsBuildProps $packProps -EnvVars $packEnvVars -Label "QR decode pack runner"
} finally {
    Pop-Location
}

$reportScript = Join-Path $PSScriptRoot "Generate-BenchmarkReport.ps1"
if (Test-Path $reportScript) {
    if ($AllowPartial) {
        & $reportScript -ArtifactsPath $artifactsPath -Framework $Framework -Configuration $Configuration -RunMode $runMode -AllowPartial
    } else {
        & $reportScript -ArtifactsPath $artifactsPath -Framework $Framework -Configuration $Configuration -RunMode $runMode -FailOnMissingCompare
    }
}
