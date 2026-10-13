$assembly = Get-BenchmarkInput AssemblyPath
$manifest = Get-BenchmarkInput ManifestPath
$imageRoot = Get-BenchmarkInput ImageRoot
$outputRoot = Get-BenchmarkInput OutputRoot
$iterations = [int](Get-BenchmarkInput Iterations 12)
$warmup = [int](Get-BenchmarkInput Warmup 3)

# Dependencies are loaded only into the dedicated comparison host.
$assemblyRoot = Split-Path -Parent $assembly
$dependencies = Get-Content -LiteralPath ([IO.Path]::ChangeExtension($assembly, '.deps.json')) -Raw | ConvertFrom-Json
$engineVersions = @{}
foreach ($name in $dependencies.libraries.PSObject.Properties.Name) {
    $parts = $name.Split('/')
    $engineVersions[$parts[0]] = $parts[1]
}
foreach ($name in 'CodeGlyphX.dll', 'zxing.dll', 'ZXingCpp.dll', 'QRCoder.dll', 'CodeGlyphX.Benchmarks.dll') {
    Add-Type -Path (Join-Path $assemblyRoot $name)
}
[CodeGlyphX.Benchmarks.CorpusNativeLibrary]::ConfigureFromDirectory($assemblyRoot)

New-BenchmarkSuite 'barcode-comparisons' -OutputRoot $outputRoot {
    Set-BenchmarkPolicy -Warmup $warmup -Iterations $iterations -Order GroupedRotated -OutlierMode None
    Add-BenchmarkMetadata Workload 'fixed-version-m-utf8-v1'
    Add-BenchmarkMetadata CoreAssemblySha256 (Get-FileHash -LiteralPath (Join-Path $assemblyRoot 'CodeGlyphX.dll') -Algorithm SHA256).Hash
    Add-BenchmarkMetadata ComparisonAssemblySha256 (Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash
    Add-BenchmarkMetadata NativeEngine 'ZXing-C++ 3.1.1 / ZXingCpp 0.5.3'
    Add-BenchmarkMetadata ManagedEngines ('ZXing.Net {0} / QRCoder {1}' -f $engineVersions['ZXing.Net'], $engineVersions['QRCoder'])
    Add-BenchmarkCases {
        Add-BenchmarkCase Url @{ Payload = 'https://github.com/EvotecIT/CodeGlyphX'; Version = 4 }
        Add-BenchmarkCase Numeric @{ Payload = '01234567890123456789012345678901'; Version = 2 }
        Add-BenchmarkCase Unicode @{ Payload = ('Zażółć gęślą jaźń — 日本語 😀 ' * 8); Version = 16 }
    }
    Add-BenchmarkAxis Operation Modules, Png
    Set-BenchmarkSetup {
        param($case, $run)
        $run.Generator = [CodeGlyphX.Benchmarks.FairQrGeneration]::new($case.Payload, [int]$case.Version)
    }
    foreach ($name in 'CodeGlyphX', 'ZXing.Net', 'ZXing-C++', 'QRCoder') {
        Add-BenchmarkEngine $name {
            foreach ($operation in 'Modules', 'Png') {
                Add-BenchmarkOperation $operation {
                    param($case, $run)
                    $run.Output = $run.Generator.Generate($case.Engine, $case.Operation)
                }
            }
        }
    }
    Add-BenchmarkValidation {
        param($case, $run)
        $run.Generator.Validate($run.Output, $case.Operation)
    }
    Add-BenchmarkMetric ManagedAllocatedBytes {
        param($case, $run)
        $run.Output.AllocatedBytes
    }
    Add-BenchmarkMetric Outputs {
        param($case, $run)
        $run.Output.Outputs.Length
    }
    Add-BenchmarkComparison Engine -Baseline CodeGlyphX -Metric MedianMs -TieTolerance 0.05
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
