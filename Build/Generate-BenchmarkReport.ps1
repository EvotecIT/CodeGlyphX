param(
    [string]$ArtifactsPath,
    [string]$OutputPath,
    [string]$Framework = "net8.0",
    [string]$Configuration = "Release",
    [string]$RunMode,
    [string]$OsName,
    [string]$Commit,
    [string]$Branch,
    [string]$DotnetSdk,
    [string]$RuntimeVersion,
    [switch]$Publish,
    [switch]$NoPublish
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot "..\BENCHMARK.md"
}

if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $resultsRoot = Join-Path $PSScriptRoot "BenchmarkResults"
    if (Test-Path $resultsRoot) {
        $latest = Get-ChildItem -Path $resultsRoot -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latest) {
            $ArtifactsPath = $latest.FullName
        }
    }
}

if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    throw "ArtifactsPath is required. Provide -ArtifactsPath or run from Build/Run-Benchmarks-Compare.ps1 first."
}

$resultsPath = Join-Path $ArtifactsPath "results"
if (-not (Test-Path $resultsPath)) {
    throw "Results folder not found: $resultsPath"
}

$runModeNormalized = $RunMode
if ([string]::IsNullOrWhiteSpace($runModeNormalized)) {
    $runModeNormalized = if ($env:BENCH_QUICK -eq "true") { "quick" } else { "full" }
}
$runModeLabel = if ($runModeNormalized -eq "quick") {
    "Run mode: Quick (warmupCount=1, iterationCount=3, invocationCount=1)."
} else {
    "Run mode: Full (BenchmarkDotNet default job settings)."
}

$publishFlag = if ($Publish) {
    $true
} elseif ($NoPublish) {
    $false
} else {
    $runModeNormalized -eq "full"
}

function Try-Get-DotnetSdk {
    if (-not [string]::IsNullOrWhiteSpace($DotnetSdk)) { return $DotnetSdk }
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        try {
            $version = & dotnet --version
            if (-not [string]::IsNullOrWhiteSpace($version)) { return $version.Trim() }
        } catch {
        }
    }
    return $null
}

$runtimeInfo = [System.Runtime.InteropServices.RuntimeInformation]
$meta = [ordered]@{
    commit = if ($Commit) { $Commit } elseif ($env:GIT_COMMIT) { $env:GIT_COMMIT } elseif ($env:BUILD_SOURCEVERSION) { $env:BUILD_SOURCEVERSION } else { $null }
    branch = if ($Branch) { $Branch } elseif ($env:GIT_BRANCH) { $env:GIT_BRANCH } elseif ($env:BUILD_SOURCEBRANCH) { $env:BUILD_SOURCEBRANCH } else { $null }
    dotnetSdk = Try-Get-DotnetSdk
    runtime = if ($RuntimeVersion) { $RuntimeVersion } else { $runtimeInfo::FrameworkDescription }
    osDescription = $runtimeInfo::OSDescription
    osArchitecture = $runtimeInfo::OSArchitecture.ToString()
    processArchitecture = $runtimeInfo::ProcessArchitecture.ToString()
    machineName = [Environment]::MachineName
    processorCount = [Environment]::ProcessorCount
}

function Normalize-Method([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $value }
    $trimmed = $value.Trim()
    if ($trimmed.StartsWith("'") -and $trimmed.EndsWith("'")) {
        return $trimmed.Substring(1, $trimmed.Length - 2)
    }
    return $trimmed
}

function Normalize-MeanText([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $value }
    $normalized = $value
    $normalized = $normalized -replace "µs", "μs"
    $normalized = $normalized -replace "�s", "μs"
    return $normalized
}

function Parse-AllocatedBytes([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $null }
    $clean = $value.Trim().Replace(",", "")
    if ($clean -eq "NA") { return $null }
    if ($clean -match "^([0-9]+(?:\.[0-9]+)?)\s*(B|KB|MB)$") {
        $number = [double]$Matches[1]
        $unit = $Matches[2]
        if ($unit -eq "B") { return $number }
        if ($unit -eq "KB") { return $number * 1024.0 }
        if ($unit -eq "MB") { return $number * 1024.0 * 1024.0 }
    }
    return $null
}

function Get-Rating([Nullable[double]]$timeRatio, [Nullable[double]]$allocRatio) {
    if (-not $timeRatio) { return "unknown" }
    if ($allocRatio) {
        if ($timeRatio -le 1.1 -and $allocRatio -le 1.25) { return "good" }
        if ($timeRatio -le 1.5 -and $allocRatio -le 2.0) { return "ok" }
        return "bad"
    }
    if ($timeRatio -le 1.1) { return "good" }
    if ($timeRatio -le 1.5) { return "ok" }
    return "bad"
}

function Normalize-CompareScenario([string]$scenario) {
    switch ($scenario) {
        "EAN PNG" { return "EAN-13 PNG" }
        "QR Decode (clean, balanced)" { return "QR Decode (clean)" }
        "QR Decode (noisy, robust)" { return "QR Decode (noisy)" }
        "QR Decode (noisy, try harder)" { return "QR Decode (noisy)" }
        default { return $scenario }
    }
}

function Get-CsvDelimiter([string]$path) {
    $firstLine = Get-Content -Path $path -TotalCount 1 -Encoding UTF8
    if ($firstLine -and $firstLine.Contains(";")) { return ";" }
    return ","
}

function Try-Parse-Mean([string]$value, [ref]$nanoseconds) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $false }
    $clean = (Normalize-MeanText $value).Trim().Replace(",", "")
    if ($clean -eq "NA") { return $false }
    if ($clean -match "^([0-9]+(?:\.[0-9]+)?)\s*(ns|us|μs|µs|ms|s)$") {
        $number = [double]$Matches[1]
        $unit = $Matches[2]
        $scale = 1.0
        if ($unit -eq "ns") { $scale = 1.0 }
        elseif ($unit -eq "us" -or $unit -eq "μs" -or $unit -eq "µs") { $scale = 1000.0 }
        elseif ($unit -eq "ms") { $scale = 1000000.0 }
        elseif ($unit -eq "s") { $scale = 1000000000.0 }
        $nanoseconds.Value = $number * $scale
        return $true
    }
    return $false
}

function Get-OsName {
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) { return "windows" }
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)) { return "linux" }
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) { return "macos" }
    return "unknown"
}

function Resolve-OsName([string]$artifactsPath, [string]$override) {
    if (-not [string]::IsNullOrWhiteSpace($override)) { return $override.ToLowerInvariant() }
    if (-not [string]::IsNullOrWhiteSpace($artifactsPath)) {
        $leaf = (Split-Path -Leaf $artifactsPath).ToLowerInvariant()
        if ($leaf -match "^(windows|linux|macos)-") { return $Matches[1] }
    }
    return Get-OsName
}

$titleMap = @{
    "QrCodeBenchmarks" = "QR (Encode)"
    "QrDecodeBenchmarks" = "QR (Decode)"
    "BarcodeBenchmarks" = "1D Barcodes (Encode)"
    "MatrixCodeBenchmarks" = "2D Matrix Codes (Encode)"
    "QrCompareBenchmarks" = "QR (Encode)"
    "QrDecodeCleanCompareBenchmarks" = "QR Decode (Clean)"
    "QrDecodeNoisyCompareBenchmarks" = "QR Decode (Noisy)"
    "Code128CompareBenchmarks" = "Code 128 (Encode)"
    "Code39CompareBenchmarks" = "Code 39 (Encode)"
    "Code93CompareBenchmarks" = "Code 93 (Encode)"
    "EanCompareBenchmarks" = "EAN-13 (Encode)"
    "UpcACompareBenchmarks" = "UPC-A (Encode)"
    "DataMatrixCompareBenchmarks" = "Data Matrix (Encode)"
    "Pdf417CompareBenchmarks" = "PDF417 (Encode)"
    "AztecCompareBenchmarks" = "Aztec (Encode)"
}

$baselineFiles = Get-ChildItem $resultsPath -Filter "*-report.csv" | Where-Object { $_.Name -notmatch "Compare" }
$compareFiles = Get-ChildItem $resultsPath -Filter "*-report.csv" | Where-Object { $_.Name -match "Compare" }

$lines = New-Object System.Collections.Generic.List[string]
$osName = Resolve-OsName $ArtifactsPath $OsName
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")

$lines.Add("## $($osName.ToUpperInvariant())")
$lines.Add("")
$lines.Add("Updated: $timestamp")
$lines.Add("Framework: $Framework")
$lines.Add("Configuration: $Configuration")
$lines.Add("Artifacts: $ArtifactsPath")
$lines.Add("How to read:")
$lines.Add("- Mean: average time per operation. Lower is better.")
$lines.Add("- Allocated: managed memory allocated per operation. Lower is better.")
$lines.Add("- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x means CodeGlyphX is fastest; 1.5 x means ~50% slower.")
$lines.Add("- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the fastest allocation for that scenario. 1 x means CodeGlyphX allocates the least; higher is more allocations.")
$lines.Add("- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).")
$lines.Add("- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.")
$lines.Add("Notes:")
$lines.Add("- $runModeLabel")
$lines.Add("- Comparisons target PNG output and include encode+render (not encode-only).")
$lines.Add("- Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.")
$lines.Add("- ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).")
$lines.Add("- Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).")
$lines.Add("- QRCoder uses PngByteQRCode (managed PNG output, no external renderer).")
$lines.Add("- QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).")
$lines.Add("- QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).")
$lines.Add("")

if ($compareFiles.Count -gt 0) {
    $summaryRows = New-Object System.Collections.Generic.List[string]
    $summaryItems = New-Object System.Collections.Generic.List[object]
    foreach ($file in $compareFiles | Sort-Object Name) {
        $delimiter = Get-CsvDelimiter $file.FullName
        $rows = Import-Csv -Path $file.FullName -Delimiter $delimiter -Encoding UTF8
        if ($rows.Count -eq 0) { continue }

        $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $className = $baseName -replace "^CodeGlyphX\\.Benchmarks\\.", "" -replace "-report$", ""
        if ($className.StartsWith("CodeGlyphX.Benchmarks.")) {
            $className = $className.Substring("CodeGlyphX.Benchmarks.".Length)
        }
        $title = $titleMap[$className]
        if (-not $title) { $title = $className }

        $scenarioMap = @{}
        foreach ($row in $rows) {
            if ([string]::IsNullOrWhiteSpace($row.Method)) { continue }
            $method = Normalize-Method $row.Method
            $vendor = "Unknown"
            $scenario = $method
            if ($method -match "^(CodeGlyphX|ZXing\.Net|QRCoder|Barcoder)\s+(.*)$") {
                $vendor = $Matches[1]
                $scenario = $Matches[2]
            }
            $scenario = Normalize-CompareScenario $scenario
            if (-not $scenarioMap.ContainsKey($scenario)) {
                $scenarioMap[$scenario] = @{}
            }
            $meanText = Normalize-MeanText $row.Mean
            $meanNs = $null
            [void](Try-Parse-Mean $meanText ([ref]$meanNs))
            $scenarioMap[$scenario][$vendor] = @{
                mean = $meanText
                meanNs = $meanNs
                allocated = $row.Allocated
            }
        }

        foreach ($scenario in ($scenarioMap.Keys | Sort-Object)) {
            $vendors = $scenarioMap[$scenario]
            $fastestVendor = $null
            $fastest = $null
            foreach ($vendor in $vendors.Keys) {
                $entry = $vendors[$vendor]
                if (-not $entry.meanNs) { continue }
                if (-not $fastest -or $entry.meanNs -lt $fastest.meanNs) {
                    $fastest = $entry
                    $fastestVendor = $vendor
                }
            }
            if (-not $fastestVendor) { continue }
            $cgx = $vendors["CodeGlyphX"]
            $ratioText = ""
            $ratioValue = $null
            $allocRatioText = ""
            $allocRatioValue = $null
            $cgxMean = ""
            $cgxAlloc = ""
            if ($cgx -and $cgx.meanNs) {
                $ratioValue = [math]::Round(($cgx.meanNs / $fastest.meanNs), 2)
                $ratioText = "$ratioValue x"
                $cgxMean = $cgx.mean
                $cgxAlloc = $cgx.allocated
                $fastestAllocBytes = Parse-AllocatedBytes $fastest.allocated
                $cgxAllocBytes = Parse-AllocatedBytes $cgx.allocated
                if ($fastestAllocBytes -and $cgxAllocBytes) {
                    $allocRatioValue = [math]::Round(($cgxAllocBytes / $fastestAllocBytes), 2)
                    $allocRatioText = "$allocRatioValue x"
                }
            }
            $fastestText = "$fastestVendor $($fastest.mean)"
            $rating = Get-Rating $ratioValue $allocRatioValue
            $summaryRows.Add("| $title | $scenario | $fastestText | $ratioText | $allocRatioText | $rating | $cgxMean | $cgxAlloc |")
            $summaryItems.Add(@{
                benchmark = $title
                scenario = $scenario
                fastestVendor = $fastestVendor
                fastestMean = $fastest.mean
                codeGlyphXMean = $cgxMean
                codeGlyphXAlloc = $cgxAlloc
                codeGlyphXVsFastest = $ratioValue
                codeGlyphXVsFastestText = $ratioText
                codeGlyphXAllocVsFastest = $allocRatioValue
                codeGlyphXAllocVsFastestText = $allocRatioText
                rating = $rating
            })
        }
    }

    if ($summaryRows.Count -gt 0) {
        $lines.Add("### Summary (Comparisons)")
        $lines.Add("")
        $lines.Add("| Benchmark | Scenario | Fastest | CodeGlyphX vs Fastest | CodeGlyphX Alloc vs Fastest | Rating | CodeGlyphX Mean | CodeGlyphX Alloc |")
        $lines.Add("| --- | --- | --- | --- | --- | --- | --- | --- |")
        foreach ($row in $summaryRows) {
            $lines.Add($row)
        }
        $lines.Add("")
    }
}

if ($baselineFiles.Count -gt 0) {
    $lines.Add("### Baseline")
    $lines.Add("")
    foreach ($file in $baselineFiles | Sort-Object Name) {
        $delimiter = Get-CsvDelimiter $file.FullName
        $rows = Import-Csv -Path $file.FullName -Delimiter $delimiter -Encoding UTF8
        if ($rows.Count -eq 0) { continue }
        $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $className = $baseName -replace "^CodeGlyphX\\.Benchmarks\\.", "" -replace "-report$", ""
        if ($className.StartsWith("CodeGlyphX.Benchmarks.")) {
            $className = $className.Substring("CodeGlyphX.Benchmarks.".Length)
        }
        $title = $titleMap[$className]
        if (-not $title) { $title = $className }
        $lines.Add("#### $title")
        $lines.Add("")
        $lines.Add("| Scenario | Mean | Allocated |")
        $lines.Add("| --- | --- | --- |")
        foreach ($row in $rows) {
            if ([string]::IsNullOrWhiteSpace($row.Method)) { continue }
            $scenario = Normalize-Method $row.Method
            $mean = Normalize-MeanText $row.Mean
            $lines.Add("| $scenario | $mean | $($row.Allocated) |")
        }
        $lines.Add("")
    }
}

if ($compareFiles.Count -gt 0) {
    $lines.Add("### Comparisons")
    $lines.Add("")
    foreach ($file in $compareFiles | Sort-Object Name) {
        $delimiter = Get-CsvDelimiter $file.FullName
        $rows = Import-Csv -Path $file.FullName -Delimiter $delimiter
        if ($rows.Count -eq 0) { continue }

        $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $className = $baseName -replace "^CodeGlyphX\\.Benchmarks\\.", "" -replace "-report$", ""
        if ($className.StartsWith("CodeGlyphX.Benchmarks.")) {
            $className = $className.Substring("CodeGlyphX.Benchmarks.".Length)
        }
        $title = $titleMap[$className]
        if (-not $title) { $title = $className }
        $lines.Add("#### $title")
        $lines.Add("")
        $lines.Add("| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |")
        $lines.Add("| --- | --- | --- | --- | --- |")

        $scenarios = @{}
        foreach ($row in $rows) {
            if ([string]::IsNullOrWhiteSpace($row.Method)) { continue }
            $method = Normalize-Method $row.Method
            $vendor = "Unknown"
            $scenario = $method
            if ($method -match "^(CodeGlyphX|ZXing\.Net|QRCoder|Barcoder)\s+(.*)$") {
                $vendor = $Matches[1]
                $scenario = $Matches[2]
            }
            $scenario = Normalize-CompareScenario $scenario
            if (-not $scenarios.ContainsKey($scenario)) {
                $scenarios[$scenario] = @{}
            }
            $scenarios[$scenario][$vendor] = $row
        }

        foreach ($scenario in ($scenarios.Keys | Sort-Object)) {
            $cgx = $scenarios[$scenario]["CodeGlyphX"]
            $zx = $scenarios[$scenario]["ZXing.Net"]
            $qrc = $scenarios[$scenario]["QRCoder"]
            $bar = $scenarios[$scenario]["Barcoder"]

            $cgxCell = if ($cgx) { "$(Normalize-MeanText $cgx.Mean)<br>$($cgx.Allocated)" } else { "" }
            $zxCell = if ($zx) { "$(Normalize-MeanText $zx.Mean)<br>$($zx.Allocated)" } else { "" }
            $qrcCell = if ($qrc) { "$(Normalize-MeanText $qrc.Mean)<br>$($qrc.Allocated)" } else { "" }
            $barCell = if ($bar) { "$(Normalize-MeanText $bar.Mean)<br>$($bar.Allocated)" } else { "" }
            $lines.Add("| $scenario | $cgxCell | $zxCell | $qrcCell | $barCell |")
        }
        $lines.Add("")
    }
}

$jsonOutput = Join-Path $PSScriptRoot "..\Assets\Data\benchmark.json"
$jsonDir = Split-Path -Parent $jsonOutput
if (-not (Test-Path $jsonDir)) {
    New-Item -ItemType Directory -Force -Path $jsonDir | Out-Null
}

function Read-CsvResults([string]$path) {
    $delimiter = Get-CsvDelimiter $path
    return Import-Csv -Path $path -Delimiter $delimiter -Encoding UTF8
}

$jsonSections = New-Object System.Collections.Generic.List[object]

foreach ($file in $compareFiles | Sort-Object Name) {
    $rows = Read-CsvResults $file.FullName
    if ($rows.Count -eq 0) { continue }
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    $className = $baseName -replace "^CodeGlyphX\\.Benchmarks\\.", "" -replace "-report$", ""
    if ($className.StartsWith("CodeGlyphX.Benchmarks.")) {
        $className = $className.Substring("CodeGlyphX.Benchmarks.".Length)
    }
    $title = $titleMap[$className]
    if (-not $title) { $title = $className }

    $scenarioMap = @{}
    foreach ($row in $rows) {
        if ([string]::IsNullOrWhiteSpace($row.Method)) { continue }
        $method = Normalize-Method $row.Method
        $vendor = "Unknown"
        $scenario = $method
        if ($method -match "^(CodeGlyphX|ZXing\.Net|QRCoder|Barcoder)\s+(.*)$") {
            $vendor = $Matches[1]
            $scenario = $Matches[2]
        }
        $scenario = Normalize-CompareScenario $scenario
        if (-not $scenarioMap.ContainsKey($scenario)) { $scenarioMap[$scenario] = @{} }
        $meanText = Normalize-MeanText $row.Mean
        $meanNs = $null
        [void](Try-Parse-Mean $meanText ([ref]$meanNs))
        $scenarioMap[$scenario][$vendor] = @{
            mean = $meanText
            meanNs = $meanNs
            allocated = $row.Allocated
        }
    }

    $scenarios = @()
    foreach ($scenario in ($scenarioMap.Keys | Sort-Object)) {
        $vendors = $scenarioMap[$scenario]
        $cgx = $vendors["CodeGlyphX"]
        $entry = @{
            name = $scenario
            vendors = $vendors
        }
        if ($cgx -and $cgx.meanNs) {
            $ratios = @{}
            foreach ($key in $vendors.Keys) {
                if ($key -eq "CodeGlyphX") { continue }
                $other = $vendors[$key]
                if ($other.meanNs) {
                    $ratios[$key] = [math]::Round($other.meanNs / $cgx.meanNs, 3)
                }
            }
            $entry["ratios"] = $ratios
        }
        $scenarios += $entry
    }

    $jsonSections.Add(@{
        id = $className
        title = $title
        scenarios = $scenarios
    })
}

$jsonBaseline = New-Object System.Collections.Generic.List[object]
foreach ($file in $baselineFiles | Sort-Object Name) {
    $rows = Read-CsvResults $file.FullName
    if ($rows.Count -eq 0) { continue }
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    $className = $baseName -replace "^CodeGlyphX\\.Benchmarks\\.", "" -replace "-report$", ""
    if ($className.StartsWith("CodeGlyphX.Benchmarks.")) {
        $className = $className.Substring("CodeGlyphX.Benchmarks.".Length)
    }
    $title = $titleMap[$className]
    if (-not $title) { $title = $className }
    $items = @()
    foreach ($row in $rows) {
        if ([string]::IsNullOrWhiteSpace($row.Method)) { continue }
        $meanText = Normalize-MeanText $row.Mean
        $meanNs = $null
        [void](Try-Parse-Mean $meanText ([ref]$meanNs))
        $items += @{
            name = (Normalize-Method $row.Method)
            mean = $meanText
            meanNs = $meanNs
            allocated = $row.Allocated
        }
    }
    $jsonBaseline.Add(@{
        id = $className
        title = $title
        scenarios = $items
    })
}

$jsonDoc = @{
    generatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    schemaVersion = 1
    os = $osName
    framework = $Framework
    configuration = $Configuration
    runMode = $runModeNormalized
    runModeDetails = $runModeLabel
    publish = $publishFlag
    artifacts = $ArtifactsPath
    meta = $meta
    howToRead = @(
        "Mean: average time per operation. Lower is better.",
        "Allocated: managed memory allocated per operation. Lower is better.",
        "CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x means CodeGlyphX is fastest; 1.5 x means ~50% slower.",
        "CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the fastest allocation for that scenario. 1 x means CodeGlyphX allocates the least; higher is more allocations.",
        "Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).",
        "Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing."
    )
    notes = @(
        $runModeLabel,
        "Comparisons target PNG output and include encode+render (not encode-only).",
        "Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.",
        "ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).",
        "Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).",
        "QRCoder uses PngByteQRCode (managed PNG output, no external renderer).",
        "QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).",
        "QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy)."
    )
    summary = $summaryItems
    baseline = $jsonBaseline
    comparisons = $jsonSections
}

$jsonSkeleton = @{
    windows = @{ quick = $null; full = $null }
    linux = @{ quick = $null; full = $null }
    macos = @{ quick = $null; full = $null }
}

if (-not (Test-Path $jsonOutput)) {
    $jsonSkeleton | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonOutput -NoNewline -Encoding UTF8
}

$jsonText = Get-Content -Path $jsonOutput -Raw -Encoding UTF8
$jsonAll = $jsonText | ConvertFrom-Json

foreach ($os in @("windows", "linux", "macos")) {
    if ($jsonAll.$os -and -not ($jsonAll.$os.PSObject.Properties.Name -contains "quick")) {
        $existing = $jsonAll.$os
        $jsonAll.$os = [pscustomobject]@{
            quick = $existing
            full = $null
        }
    } elseif (-not $jsonAll.$os) {
        $jsonAll.$os = [pscustomobject]@{
            quick = $null
            full = $null
        }
    }
}

$jsonAll.$osName.$runModeNormalized = $jsonDoc
$jsonOut = $jsonAll | ConvertTo-Json -Depth 8
Set-Content -Path $jsonOutput -Value $jsonOut -NoNewline -Encoding UTF8

$summaryOutput = Join-Path $PSScriptRoot "..\Assets\Data\benchmark-summary.json"
if (-not (Test-Path $summaryOutput)) {
    $jsonSkeleton | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryOutput -NoNewline -Encoding UTF8
}
$summaryText = Get-Content -Path $summaryOutput -Raw -Encoding UTF8
$summaryAll = $summaryText | ConvertFrom-Json
foreach ($os in @("windows", "linux", "macos")) {
    if ($summaryAll.$os -and -not ($summaryAll.$os.PSObject.Properties.Name -contains "quick")) {
        $existing = $summaryAll.$os
        $summaryAll.$os = [pscustomobject]@{
            quick = $existing
            full = $null
        }
    } elseif (-not $summaryAll.$os) {
        $summaryAll.$os = [pscustomobject]@{
            quick = $null
            full = $null
        }
    }
}
$summaryAll.$osName.$runModeNormalized = [pscustomobject]@{
    generatedUtc = $jsonDoc.generatedUtc
    schemaVersion = $jsonDoc.schemaVersion
    os = $jsonDoc.os
    framework = $jsonDoc.framework
    configuration = $jsonDoc.configuration
    runMode = $jsonDoc.runMode
    runModeDetails = $jsonDoc.runModeDetails
    publish = $jsonDoc.publish
    artifacts = $jsonDoc.artifacts
    meta = $jsonDoc.meta
    howToRead = $jsonDoc.howToRead
    notes = $jsonDoc.notes
    summary = $jsonDoc.summary
}
$summaryOut = $summaryAll | ConvertTo-Json -Depth 8
Set-Content -Path $summaryOutput -Value $summaryOut -NoNewline -Encoding UTF8

# Summary output is already stored under Assets/Data for website ingestion.

$indexOutput = Join-Path $PSScriptRoot "..\Assets\Data\benchmark-index.json"
if (-not (Test-Path $indexOutput)) {
    $indexSkeleton = @{
        schemaVersion = 1
        entries = @()
    } | ConvertTo-Json -Depth 6
    Set-Content -Path $indexOutput -Value $indexSkeleton -NoNewline -Encoding UTF8
}

$indexText = Get-Content -Path $indexOutput -Raw -Encoding UTF8
$indexDoc = $indexText | ConvertFrom-Json
if (-not $indexDoc.schemaVersion) { $indexDoc | Add-Member -NotePropertyName schemaVersion -NotePropertyValue 1 }
if (-not $indexDoc.entries) { $indexDoc.entries = @() }

$newEntry = [pscustomobject]@{
    os = $jsonDoc.os
    runMode = $jsonDoc.runMode
    generatedUtc = $jsonDoc.generatedUtc
    publish = $jsonDoc.publish
    framework = $jsonDoc.framework
    configuration = $jsonDoc.configuration
    artifacts = $jsonDoc.artifacts
    meta = $jsonDoc.meta
}

$entries = @()
foreach ($entry in $indexDoc.entries) {
    if ($entry.os -eq $newEntry.os -and $entry.runMode -eq $newEntry.runMode) { continue }
    $entries += $entry
}
$entries += $newEntry
$indexDoc.entries = $entries

$indexOut = $indexDoc | ConvertTo-Json -Depth 6
Set-Content -Path $indexOutput -Value $indexOut -NoNewline -Encoding UTF8

$sectionContent = ($lines -join "`n").TrimEnd()
$marker = "BENCHMARK:$($osName.ToUpperInvariant()):$($runModeNormalized.ToUpperInvariant())"
$startMarker = "<!-- ${marker}:START -->"
$endMarker = "<!-- ${marker}:END -->"
$sectionBlock = "$startMarker`n$sectionContent`n$endMarker"

function Get-Block([string]$text, [string]$osName, [string]$runMode) {
    $marker = "BENCHMARK:$($osName.ToUpperInvariant()):$($runMode.ToUpperInvariant())"
    $start = "<!-- ${marker}:START -->"
    $end = "<!-- ${marker}:END -->"
    $pattern = [regex]::Escape($start) + "[\s\S]*?" + [regex]::Escape($end)
    if ($text -match $pattern) {
        return [regex]::Match($text, $pattern).Value
    }
    return "$start`n_no results yet_`n$end"
}

$text = if (Test-Path $OutputPath) { Get-Content -Path $OutputPath -Raw -Encoding UTF8 } else { "" }
$blocks = @{
    windowsQuick = Get-Block $text "windows" "quick"
    windowsFull = Get-Block $text "windows" "full"
    linuxQuick = Get-Block $text "linux" "quick"
    linuxFull = Get-Block $text "linux" "full"
    macosQuick = Get-Block $text "macos" "quick"
    macosFull = Get-Block $text "macos" "full"
}

if ($osName -eq "windows" -and $runModeNormalized -eq "quick") { $blocks["windowsQuick"] = $sectionBlock }
elseif ($osName -eq "windows" -and $runModeNormalized -eq "full") { $blocks["windowsFull"] = $sectionBlock }
elseif ($osName -eq "linux" -and $runModeNormalized -eq "quick") { $blocks["linuxQuick"] = $sectionBlock }
elseif ($osName -eq "linux" -and $runModeNormalized -eq "full") { $blocks["linuxFull"] = $sectionBlock }
elseif ($osName -eq "macos" -and $runModeNormalized -eq "quick") { $blocks["macosQuick"] = $sectionBlock }
elseif ($osName -eq "macos" -and $runModeNormalized -eq "full") { $blocks["macosFull"] = $sectionBlock }

$template = @(
    "# Benchmarks",
    "",
    $blocks["windowsQuick"],
    "",
    $blocks["windowsFull"],
    "",
    $blocks["linuxQuick"],
    "",
    $blocks["linuxFull"],
    "",
    $blocks["macosQuick"],
    "",
    $blocks["macosFull"],
    ""
) -join "`n"

Set-Content -Path $OutputPath -Value $template -NoNewline -Encoding UTF8
