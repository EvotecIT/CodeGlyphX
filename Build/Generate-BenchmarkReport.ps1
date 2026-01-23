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
    [switch]$AllowPartial,
    [switch]$Publish,
    [switch]$NoPublish,
    [switch]$FailOnMissingCompare
)

$ErrorActionPreference = "Stop"

function Write-TextUtf8NoBom([string]$path, [string]$value) {
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $value, $encoding)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot "..\BENCHMARK.md"
}

$artifactsPathProvided = -not [string]::IsNullOrWhiteSpace($ArtifactsPath)

function Format-RunModeLabel([string]$runMode, [string]$source, [string]$requested) {
    $label = if ($runMode -eq "quick") {
        "Run mode: Quick (warmupCount=1, iterationCount=3, invocationCount=1)."
    } else {
        "Run mode: Full (BenchmarkDotNet default job settings)."
    }
    if ($source -eq "inferred" -or $source -eq "inferred-mismatch") {
        if (-not [string]::IsNullOrWhiteSpace($requested) -and $requested -ne $runMode) {
            return "$label (inferred from artifacts; requested $requested)."
        }
        return "$label (inferred from artifacts)."
    }
    return $label
}

function Get-RunModeFromReports([string]$resultsPath) {
    if (-not (Test-Path $resultsPath)) { return $null }
    $candidates = Get-ChildItem -Path $resultsPath -Filter "*-report-github.md" -ErrorAction SilentlyContinue
    if (-not $candidates -or $candidates.Count -eq 0) {
        $candidates = Get-ChildItem -Path $resultsPath -Filter "*-report.md" -ErrorAction SilentlyContinue
    }
    foreach ($file in $candidates | Sort-Object Name) {
        $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
        if ([string]::IsNullOrWhiteSpace($content)) { continue }
        $iteration = [regex]::Match($content, "IterationCount\\s*=\\s*(\\d+)")
        $warmup = [regex]::Match($content, "WarmupCount\\s*=\\s*(\\d+)")
        $invocation = [regex]::Match($content, "InvocationCount\\s*=\\s*(\\d+)")
        if (-not $iteration.Success -or -not $warmup.Success) { continue }
        $iterationCount = [int]$iteration.Groups[1].Value
        $warmupCount = [int]$warmup.Groups[1].Value
        $invocationCount = if ($invocation.Success) { [int]$invocation.Groups[1].Value } else { $null }
        if ($iterationCount -eq 3 -and $warmupCount -eq 1 -and ($invocationCount -eq $null -or $invocationCount -eq 1)) {
            return "quick"
        }
        return "full"
    }
    return $null
}

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

function Wait-For-CompareResults([string]$resultsPath, [string[]]$expectedFiles, [int]$timeoutSeconds = 15, [int]$pollMs = 250) {
    if (-not $expectedFiles -or $expectedFiles.Count -eq 0) { return $true }
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    $lastMissing = @()
    $lastEmpty = @()
    while ($true) {
        $missing = @()
        $empty = @()
        foreach ($file in $expectedFiles) {
            $fullPath = Join-Path $resultsPath $file
            if (-not (Test-ResultFileReady $fullPath)) {
                if (-not (Test-Path $fullPath)) {
                    $missing += $file
                } else {
                    $empty += $file
                }
            }
        }
        if ($missing.Count -eq 0 -and $empty.Count -eq 0) { return $true }
        $lastMissing = $missing
        $lastEmpty = $empty
        if ((Get-Date) -ge $deadline) { break }
        Start-Sleep -Milliseconds $pollMs
    }
    if ($lastMissing.Count -gt 0) { Write-Warning "Compare results still missing after wait: $($lastMissing -join ', ')." }
    if ($lastEmpty.Count -gt 0) { Write-Warning "Compare results still empty after wait: $($lastEmpty -join ', ')." }
    return $false
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

$metaRuntime = if ($RuntimeVersion) { $RuntimeVersion } else { $null }
if (-not $metaRuntime) {
    try { $metaRuntime = [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription } catch { $metaRuntime = $null }
}
$metaOsDescription = $null
$metaOsArch = $null
$metaProcessArch = $null
try { $metaOsDescription = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription } catch { $metaOsDescription = $null }
try { $metaOsArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() } catch { $metaOsArch = $null }
try { $metaProcessArch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString() } catch { $metaProcessArch = $null }

$meta = [ordered]@{
    commit = if ($Commit) { $Commit } elseif ($env:GIT_COMMIT) { $env:GIT_COMMIT } elseif ($env:BUILD_SOURCEVERSION) { $env:BUILD_SOURCEVERSION } else { $null }
    branch = if ($Branch) { $Branch } elseif ($env:GIT_BRANCH) { $env:GIT_BRANCH } elseif ($env:BUILD_SOURCEBRANCH) { $env:BUILD_SOURCEBRANCH } else { $null }
    dotnetSdk = Try-Get-DotnetSdk
    runtime = $metaRuntime
    osDescription = $metaOsDescription
    osArchitecture = $metaOsArch
    processArchitecture = $metaProcessArch
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
    $normalized = $normalized -replace "Âµs", "Î¼s"
    $normalized = $normalized -replace "ï¿½s", "Î¼s"
    $normalized = $normalized -replace "Ã‚Âµs", "Î¼s"
    $normalized = $normalized -replace "Ã‚Î¼s", "Î¼s"
    return $normalized
}

function Get-RowValue([object]$row, [string]$name) {
    if (-not $row) { return $null }
    foreach ($prop in $row.PSObject.Properties.Name) {
        $clean = $prop -replace "^\uFEFF", ""
        if ($clean -eq $name) { return $row.$prop }
    }
    return $null
}

function Import-BenchmarkCsv([string]$path) {
    $delimiter = Get-CsvDelimiter $path
    $rows = Import-Csv -Path $path -Delimiter $delimiter -Encoding UTF8
    if ($rows.Count -gt 0 -and -not (Get-RowValue $rows[0] "Method")) {
        $alt = if ($delimiter -eq ";") { "," } else { ";" }
        $rows = Import-Csv -Path $path -Delimiter $alt -Encoding UTF8
    }
    return $rows
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

function Get-ClassName([string]$fileName) {
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($fileName)
    $className = $baseName -replace "^CodeGlyphX\\.Benchmarks\\.", "" -replace "-report$", ""
    if ($className.StartsWith("CodeGlyphX.Benchmarks.")) {
        $className = $className.Substring("CodeGlyphX.Benchmarks.".Length)
    }
    return $className
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
    if ($clean -match "^([0-9]+(?:\.[0-9]+)?)\s*(ns|us|Î¼s|Âµs|ms|s)$") {
        $number = [double]$Matches[1]
        $unit = $Matches[2]
        $scale = 1.0
        if ($unit -eq "ns") { $scale = 1.0 }
        elseif ($unit -eq "us" -or $unit -eq "Î¼s" -or $unit -eq "Âµs") { $scale = 1000.0 }
        elseif ($unit -eq "ms") { $scale = 1000000.0 }
        elseif ($unit -eq "s") { $scale = 1000000000.0 }
        $nanoseconds.Value = $number * $scale
        return $true
    }
    return $false
}

function Format-MeanAllocCell([object]$entry, [string]$deltaText = $null) {
    if (-not $entry) { return "" }
    $mean = Normalize-MeanText $entry["mean"]
    $alloc = $entry["allocated"]
    if ([string]::IsNullOrWhiteSpace($mean) -and [string]::IsNullOrWhiteSpace($alloc)) { return "" }
    if ([string]::IsNullOrWhiteSpace($mean)) {
        return if ([string]::IsNullOrWhiteSpace($deltaText)) { "$alloc" } else { "$alloc<br>$deltaText" }
    }
    if ([string]::IsNullOrWhiteSpace($alloc)) {
        return if ([string]::IsNullOrWhiteSpace($deltaText)) { "$mean" } else { "$mean<br>$deltaText" }
    }
    if ([string]::IsNullOrWhiteSpace($deltaText)) { return "$mean<br>$alloc" }
    return "$mean<br>$alloc<br>$deltaText"
}

function Format-DeltaText([object]$vendorRow, [object]$cgxRow) {
    if (-not $vendorRow -or -not $cgxRow) { return "" }
    $vendorMean = Normalize-MeanText (Get-RowValue $vendorRow "Mean")
    $cgxMean = Normalize-MeanText (Get-RowValue $cgxRow "Mean")
    $vendorNs = $null
    $cgxNs = $null
    [void](Try-Parse-Mean $vendorMean ([ref]$vendorNs))
    [void](Try-Parse-Mean $cgxMean ([ref]$cgxNs))
    $timeRatio = $null
    if ($vendorNs -and $cgxNs) {
        $timeRatio = [math]::Round(($vendorNs / $cgxNs), 2)
    }
    $vendorAlloc = Parse-AllocatedBytes (Get-RowValue $vendorRow "Allocated")
    $cgxAlloc = Parse-AllocatedBytes (Get-RowValue $cgxRow "Allocated")
    $allocRatio = $null
    if ($vendorAlloc -and $cgxAlloc) {
        $allocRatio = [math]::Round(($vendorAlloc / $cgxAlloc), 2)
    }
    if ($timeRatio -and $allocRatio) { return "Δ $timeRatio x / $allocRatio x" }
    if ($timeRatio) { return "Δ $timeRatio x" }
    if ($allocRatio) { return "Δ $allocRatio x alloc" }
    return ""
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

$expectedCompare = $titleMap.Keys | Where-Object { $_ -match "CompareBenchmarks$" } | Sort-Object

if (-not $artifactsPathProvided) {
    $resultsRoot = Join-Path $PSScriptRoot "BenchmarkResults"
    if (Test-Path $resultsRoot) {
        $candidates = Get-ChildItem -Path $resultsRoot -Directory | Sort-Object LastWriteTime -Descending
        $preferred = $null
        $firstCandidate = $null
        foreach ($candidate in $candidates) {
            if (-not $firstCandidate) { $firstCandidate = $candidate }
            $resultsCandidate = Join-Path $candidate.FullName "results"
            if (-not (Test-Path $resultsCandidate)) { continue }
            $compareFiles = Get-ChildItem -Path $resultsCandidate -Filter "*Compare*-report.csv" -ErrorAction SilentlyContinue
            if (-not $compareFiles -or $compareFiles.Count -eq 0) { continue }
            if (-not $AllowPartial) {
                $actualCompare = @()
                foreach ($file in $compareFiles) {
                    $actualCompare += (Get-ClassName $file.Name)
                }
                $missing = @()
                foreach ($expected in $expectedCompare) {
                    if ($actualCompare -notcontains $expected) { $missing += $expected }
                }
                if ($missing.Count -gt 0) { continue }
            }
            $preferred = $candidate
            break
        }
        if (-not $preferred) {
            if ($AllowPartial -and $firstCandidate) {
                $preferred = $firstCandidate
            } else {
                throw "No artifacts folder with a full compare set was found. Run the full benchmark suite or pass -AllowPartial or -ArtifactsPath."
            }
        }
        if ($preferred) {
            $ArtifactsPath = $preferred.FullName
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

$enforceMissingCompare = $FailOnMissingCompare -or (-not $AllowPartial)
$expectedCompareFiles = @()
foreach ($expected in $expectedCompare) {
    $expectedCompareFiles += "CodeGlyphX.Benchmarks.$expected-report.csv"
}
if ($enforceMissingCompare) {
    [void](Wait-For-CompareResults -resultsPath $resultsPath -expectedFiles $expectedCompareFiles)
}

$requestedRunMode = $RunMode
$runModeNormalized = $requestedRunMode
$runModeSource = if ([string]::IsNullOrWhiteSpace($runModeNormalized)) { $null } else { "explicit" }
$runModeWarning = $null

$inferredRunMode = Get-RunModeFromReports $resultsPath
if ($inferredRunMode) {
    if (-not [string]::IsNullOrWhiteSpace($runModeNormalized) -and $runModeNormalized -ne $inferredRunMode) {
        $runModeWarning = "Run mode mismatch: requested $runModeNormalized, inferred $inferredRunMode from artifacts."
        $runModeNormalized = $inferredRunMode
        $runModeSource = "inferred-mismatch"
    } elseif ([string]::IsNullOrWhiteSpace($runModeNormalized)) {
        $runModeNormalized = $inferredRunMode
        $runModeSource = "inferred"
    }
}

if ([string]::IsNullOrWhiteSpace($runModeNormalized)) {
    $runModeNormalized = if ($env:BENCH_QUICK -eq "true") { "quick" } else { "full" }
    $runModeSource = "env-default"
}

if (-not [string]::IsNullOrWhiteSpace($runModeWarning)) {
    Write-Warning $runModeWarning
}

$runModeLabel = Format-RunModeLabel $runModeNormalized $runModeSource $requestedRunMode

$publishFlag = if ($Publish) {
    $true
} elseif ($NoPublish) {
    $false
} else {
    $runModeNormalized -eq "full"
}

$baselineFiles = Get-ChildItem $resultsPath -Filter "*-report.csv" | Where-Object { $_.Name -notmatch "Compare" }
$compareFiles = Get-ChildItem $resultsPath -Filter "*-report.csv" | Where-Object { $_.Name -match "Compare" }
$actualCompare = @()
foreach ($file in $compareFiles) {
    $actualCompare += (Get-ClassName $file.Name)
}
$missingCompare = @()
$missingCompareIds = @()
foreach ($expected in $expectedCompare) {
    if ($actualCompare -notcontains $expected) {
        $title = $titleMap[$expected]
        if (-not $title) { $title = $expected }
        $missingCompare += $title
        $missingCompareIds += $expected
    }
}

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
$lines.Add("- Δ lines in comparison tables show vendor ratios vs CodeGlyphX (time / alloc).")
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
$warnings = New-Object System.Collections.Generic.List[string]
if (-not [string]::IsNullOrWhiteSpace($runModeWarning)) { $warnings.Add($runModeWarning) }
if ($missingCompare.Count -gt 0) { $warnings.Add("Missing compare results: $($missingCompare -join ', ').") }
if ($warnings.Count -gt 0) {
    $lines.Add("Warnings:")
    foreach ($warning in $warnings) { $lines.Add("- $warning") }
}
$lines.Add("")

$compareParseFailures = @()
if ($compareFiles.Count -gt 0) {
    $summaryRows = New-Object System.Collections.Generic.List[string]
    $summaryItems = New-Object System.Collections.Generic.List[object]
    foreach ($file in $compareFiles | Sort-Object Name) {
        $rows = Import-BenchmarkCsv $file.FullName
        if ($rows.Count -eq 0) { continue }

        $className = Get-ClassName $file.Name
        $title = $titleMap[$className]
        if (-not $title) { $title = $className }

        $scenarioMap = @{}
        foreach ($row in $rows) {
            $method = Get-RowValue $row "Method"
            if ([string]::IsNullOrWhiteSpace($method)) { continue }
            $method = Normalize-Method $method
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
            $meanText = Normalize-MeanText (Get-RowValue $row "Mean")
            $meanNs = $null
            [void](Try-Parse-Mean $meanText ([ref]$meanNs))
            $scenarioMap[$scenario][$vendor] = @{
                mean = $meanText
                meanNs = $meanNs
                allocated = (Get-RowValue $row "Allocated")
            }
        }

        if ($scenarioMap.Count -eq 0) {
            $compareParseFailures += $title
            if ($missingCompare -notcontains $title) { $missingCompare += $title }
            if ($missingCompareIds -notcontains $className) { $missingCompareIds += $className }
            continue
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
            $cgxCell = Format-MeanAllocCell $vendors["CodeGlyphX"]
            $zxDelta = Format-DeltaText $vendors["ZXing.Net"] $vendors["CodeGlyphX"]
            $qrcDelta = Format-DeltaText $vendors["QRCoder"] $vendors["CodeGlyphX"]
            $barDelta = Format-DeltaText $vendors["Barcoder"] $vendors["CodeGlyphX"]
            $zxCell = Format-MeanAllocCell $vendors["ZXing.Net"] $zxDelta
            $qrcCell = Format-MeanAllocCell $vendors["QRCoder"] $qrcDelta
            $barCell = Format-MeanAllocCell $vendors["Barcoder"] $barDelta
            $summaryRows.Add("| $title | $scenario | $fastestText | $cgxCell | $zxCell | $qrcCell | $barCell | $ratioText | $allocRatioText | $rating |")
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
                vendors = $vendors
                deltas = @{
                    "ZXing.Net" = $zxDelta
                    "QRCoder" = $qrcDelta
                    "Barcoder" = $barDelta
                }
            })
        }
    }

    if ($summaryRows.Count -gt 0) {
        $lines.Add("### Summary (Comparisons)")
        $lines.Add("")
        $lines.Add("| Benchmark | Scenario | Fastest | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) | CodeGlyphX vs Fastest | CodeGlyphX Alloc vs Fastest | Rating |")
        $lines.Add("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |")
        foreach ($row in $summaryRows) {
            $lines.Add($row)
        }
        $lines.Add("")
    }
}

if ($compareParseFailures.Count -gt 0) {
    $lines.Add("Warnings:")
    $lines.Add("- Compare results could not be parsed: $($compareParseFailures -join ', ').")
    $lines.Add("")
}

if ($enforceMissingCompare -and $missingCompare.Count -gt 0) {
    throw "Missing compare results: $($missingCompare -join ', ')."
}

if ($baselineFiles.Count -gt 0) {
    $lines.Add("### Baseline")
    $lines.Add("")
    foreach ($file in $baselineFiles | Sort-Object Name) {
        $rows = Import-BenchmarkCsv $file.FullName
        if ($rows.Count -eq 0) { continue }
        $className = Get-ClassName $file.Name
        $title = $titleMap[$className]
        if (-not $title) { $title = $className }
        $lines.Add("#### $title")
        $lines.Add("")
        $lines.Add("| Scenario | Mean | Allocated |")
        $lines.Add("| --- | --- | --- |")
        foreach ($row in $rows) {
            $method = Get-RowValue $row "Method"
            if ([string]::IsNullOrWhiteSpace($method)) { continue }
            $scenario = Normalize-Method $method
            $mean = Normalize-MeanText (Get-RowValue $row "Mean")
            $allocated = Get-RowValue $row "Allocated"
            $lines.Add("| $scenario | $mean | $allocated |")
        }
        $lines.Add("")
    }
}

if ($compareFiles.Count -gt 0) {
    $lines.Add("### Comparisons")
    $lines.Add("")
    foreach ($file in $compareFiles | Sort-Object Name) {
        $rows = Import-BenchmarkCsv $file.FullName
        if ($rows.Count -eq 0) { continue }

        $className = Get-ClassName $file.Name
        $title = $titleMap[$className]
        if (-not $title) { $title = $className }
        $lines.Add("#### $title")
        $lines.Add("")
        $lines.Add("| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |")
        $lines.Add("| --- | --- | --- | --- | --- |")

        $scenarios = @{}
        foreach ($row in $rows) {
            $method = Get-RowValue $row "Method"
            if ([string]::IsNullOrWhiteSpace($method)) { continue }
            $method = Normalize-Method $method
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

            $cgxCell = if ($cgx) {
                Format-MeanAllocCell @{ mean = (Normalize-MeanText (Get-RowValue $cgx 'Mean')); allocated = (Get-RowValue $cgx 'Allocated') }
            } else { "" }
            $zxDelta = Format-DeltaText $zx $cgx
            $qrcDelta = Format-DeltaText $qrc $cgx
            $barDelta = Format-DeltaText $bar $cgx
            $zxCell = if ($zx) {
                Format-MeanAllocCell @{ mean = (Normalize-MeanText (Get-RowValue $zx 'Mean')); allocated = (Get-RowValue $zx 'Allocated') } $zxDelta
            } else { "" }
            $qrcCell = if ($qrc) {
                Format-MeanAllocCell @{ mean = (Normalize-MeanText (Get-RowValue $qrc 'Mean')); allocated = (Get-RowValue $qrc 'Allocated') } $qrcDelta
            } else { "" }
            $barCell = if ($bar) {
                Format-MeanAllocCell @{ mean = (Normalize-MeanText (Get-RowValue $bar 'Mean')); allocated = (Get-RowValue $bar 'Allocated') } $barDelta
            } else { "" }
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
    return Import-BenchmarkCsv $path
}

$jsonSections = New-Object System.Collections.Generic.List[object]

foreach ($file in $compareFiles | Sort-Object Name) {
    $rows = Read-CsvResults $file.FullName
    if ($rows.Count -eq 0) { continue }
    $className = Get-ClassName $file.Name
    $title = $titleMap[$className]
    if (-not $title) { $title = $className }

    $scenarioMap = @{}
    foreach ($row in $rows) {
        $method = Get-RowValue $row "Method"
        if ([string]::IsNullOrWhiteSpace($method)) { continue }
        $method = Normalize-Method $method
        $vendor = "Unknown"
        $scenario = $method
        if ($method -match "^(CodeGlyphX|ZXing\.Net|QRCoder|Barcoder)\s+(.*)$") {
            $vendor = $Matches[1]
            $scenario = $Matches[2]
        }
        $scenario = Normalize-CompareScenario $scenario
        if (-not $scenarioMap.ContainsKey($scenario)) { $scenarioMap[$scenario] = @{} }
        $meanText = Normalize-MeanText (Get-RowValue $row "Mean")
        $meanNs = $null
        [void](Try-Parse-Mean $meanText ([ref]$meanNs))
        $scenarioMap[$scenario][$vendor] = @{
            mean = $meanText
            meanNs = $meanNs
            allocated = (Get-RowValue $row "Allocated")
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
    $className = Get-ClassName $file.Name
    $title = $titleMap[$className]
    if (-not $title) { $title = $className }
    $items = @()
    foreach ($row in $rows) {
        $method = Get-RowValue $row "Method"
        if ([string]::IsNullOrWhiteSpace($method)) { continue }
        $meanText = Normalize-MeanText (Get-RowValue $row "Mean")
        $meanNs = $null
        [void](Try-Parse-Mean $meanText ([ref]$meanNs))
        $items += @{
            name = (Normalize-Method $method)
            mean = $meanText
            meanNs = $meanNs
            allocated = (Get-RowValue $row "Allocated")
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
    runModeSource = $runModeSource
    publish = $publishFlag
    artifacts = $ArtifactsPath
    meta = $meta
    missingComparisons = $missingCompare
    missingComparisonIds = $missingCompareIds
    howToRead = @(
        "Mean: average time per operation. Lower is better.",
        "Allocated: managed memory allocated per operation. Lower is better.",
        "CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x means CodeGlyphX is fastest; 1.5 x means ~50% slower.",
        "CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the fastest allocation for that scenario. 1 x means CodeGlyphX allocates the least; higher is more allocations.",
        "Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).",
        "Δ lines in comparison tables show vendor ratios vs CodeGlyphX (time / alloc).",
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
    Write-TextUtf8NoBom $jsonOutput ($jsonSkeleton | ConvertTo-Json -Depth 8)
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
Write-TextUtf8NoBom $jsonOutput $jsonOut

$summaryOutput = Join-Path $PSScriptRoot "..\Assets\Data\benchmark-summary.json"
if (-not (Test-Path $summaryOutput)) {
    Write-TextUtf8NoBom $summaryOutput ($jsonSkeleton | ConvertTo-Json -Depth 8)
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
    runModeSource = $jsonDoc.runModeSource
    publish = $jsonDoc.publish
    artifacts = $jsonDoc.artifacts
    meta = $jsonDoc.meta
    missingComparisons = $jsonDoc.missingComparisons
    missingComparisonIds = $jsonDoc.missingComparisonIds
    howToRead = $jsonDoc.howToRead
    notes = $jsonDoc.notes
    summary = $jsonDoc.summary
}
$summaryOut = $summaryAll | ConvertTo-Json -Depth 8
Write-TextUtf8NoBom $summaryOutput $summaryOut

# Summary output is already stored under Assets/Data for website ingestion.

$indexOutput = Join-Path $PSScriptRoot "..\Assets\Data\benchmark-index.json"
if (-not (Test-Path $indexOutput)) {
    $indexSkeleton = @{
        schemaVersion = 1
        entries = @()
    } | ConvertTo-Json -Depth 6
    Write-TextUtf8NoBom $indexOutput $indexSkeleton
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
    runModeSource = $jsonDoc.runModeSource
}

$entries = @()
foreach ($entry in $indexDoc.entries) {
    if ($entry.os -eq $newEntry.os -and $entry.runMode -eq $newEntry.runMode) { continue }
    $entries += $entry
}
$entries += $newEntry
$indexDoc.entries = $entries

$indexOut = $indexDoc | ConvertTo-Json -Depth 6
Write-TextUtf8NoBom $indexOutput $indexOut

$websiteDataPath = Join-Path $PSScriptRoot "..\CodeGlyphX.Website\wwwroot\data"
if (Test-Path (Join-Path $PSScriptRoot "..\CodeGlyphX.Website")) {
    if (-not (Test-Path $websiteDataPath)) {
        New-Item -ItemType Directory -Force -Path $websiteDataPath | Out-Null
    }
    Copy-Item -Path $jsonOutput -Destination $websiteDataPath -Force
    Copy-Item -Path $summaryOutput -Destination $websiteDataPath -Force
    Copy-Item -Path $indexOutput -Destination $websiteDataPath -Force
}

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
    "**Data locations**",
    "- Generated files are overwritten on each run (do not edit by hand).",
    '- Human-readable report: `BENCHMARK.md`',
    '- Website JSON: `Assets/Data/benchmark.json`',
    '- Summary JSON: `Assets/Data/benchmark-summary.json`',
    '- Index JSON: `Assets/Data/benchmark-index.json`',
    "",
    "**Publish flag**",
    "- Quick runs default to `publish=false` (draft).",
    "- Full runs default to `publish=true`.",
    "- Override with `-Publish` or `-NoPublish` on the report generator.",
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

Write-TextUtf8NoBom $OutputPath $template
