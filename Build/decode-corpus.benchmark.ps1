$assembly = Get-BenchmarkInput AssemblyPath
$manifest = Get-BenchmarkInput ManifestPath
$imageRoot = Get-BenchmarkInput ImageRoot
$outputRoot = Get-BenchmarkInput OutputRoot
$iterations = [int](Get-BenchmarkInput Iterations 12)
$warmup = [int](Get-BenchmarkInput Warmup 3)
$assemblyRoot = Split-Path -Parent $assembly
foreach ($name in 'CodeGlyphX.dll', 'zxing.dll', 'ZXingCpp.dll', 'CodeGlyphX.Benchmarks.dll') { Add-Type -Path (Join-Path $assemblyRoot $name) }
[CodeGlyphX.Benchmarks.CorpusNativeLibrary]::ConfigureFromDirectory($assemblyRoot)
New-BenchmarkSuite 'decode-corpus' -OutputRoot $outputRoot {
    Set-BenchmarkPolicy -Warmup $warmup -Iterations $iterations -Order GroupedRotated -OutlierMode None
    Add-BenchmarkMetadata ManifestSha256 (Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash
    Add-BenchmarkMetadata CoreAssemblySha256 (Get-FileHash -LiteralPath (Join-Path $assemblyRoot 'CodeGlyphX.dll') -Algorithm SHA256).Hash
    Add-BenchmarkMetadata Input 'Same preloaded RGBA32 pixels, explicit format hints, single/multiple APIs selected from oracle count'
    Add-BenchmarkMetadata Accuracy 'Recognition failures are retained in CorrectImages and UnexpectedImages; timing alone is not a quality ranking'
    Add-BenchmarkCases { Add-BenchmarkCase IndependentCorpus @{} }
    Set-BenchmarkSetup {
        param($case, $run)
        $run.Session = [CodeGlyphX.Benchmarks.CorpusSession]::new($manifest, $imageRoot)
    }
    foreach ($name in 'CodeGlyphX', 'ZXing.Net', 'ZXing-C++') {
        Add-BenchmarkEngine $name {
            Add-BenchmarkOperation Decode {
                param($case, $run)
                $run.Output = $run.Session.Decode($case.Engine)
            }
        }
    }
    Add-BenchmarkValidation {
        param($case, $run)
        $run.Outcomes = $run.Session.Evaluate($case.Engine, $run.Output)
        if ($run.Outcomes.Length -ne $run.Session.Count) { throw 'Not every input was processed.' }
    }
    Add-BenchmarkMetric Images { param($case, $run) $run.Session.Count }
    Add-BenchmarkMetric CorrectImages { param($case, $run) @($run.Outcomes | Where-Object Status -eq Correct).Count }
    Add-BenchmarkMetric UnsupportedImages { param($case, $run) @($run.Outcomes | Where-Object Status -eq Unsupported).Count }
    Add-BenchmarkMetric InconclusiveImages { param($case, $run) @($run.Outcomes | Where-Object Status -eq Inconclusive).Count }
    Add-BenchmarkMetric UnexpectedImages { param($case, $run) @($run.Outcomes | Where-Object {$_.Unexpected.Length -gt 0}).Count }
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
