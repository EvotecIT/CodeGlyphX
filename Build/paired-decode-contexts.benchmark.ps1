# Invoked by Invoke-PairedDecodeBenchmark.ps1, whose closure owns both sessions.
$outputRoot=Get-BenchmarkInput OutputRoot
$manifest=Get-BenchmarkInput ManifestPath
$iterations=[int](Get-BenchmarkInput Iterations 12)
$warmup=[int](Get-BenchmarkInput Warmup 3)
$source=Get-BenchmarkInput Source
New-BenchmarkSuite 'paired-whole-image-decoding' -OutputRoot $outputRoot {
    Set-BenchmarkProfile Current
    Set-BenchmarkPolicy -Warmup $warmup -Iterations $iterations -Order GroupedRotated -OutlierMode None
    Add-BenchmarkMetadata ManifestSha256 (Get-FileHash -LiteralPath $manifest).Hash
    Add-BenchmarkMetadata BaselineCoreSha256 $sessions.Baseline.CoreSha256
    Add-BenchmarkMetadata CandidateCoreSha256 $sessions.Candidate.CoreSha256
    Add-BenchmarkMetadata BaselineHostSha256 $sessions.Baseline.HostSha256
    Add-BenchmarkMetadata CandidateHostSha256 $sessions.Candidate.HostSha256
    Add-BenchmarkMetadata BaselineCoreInformationalVersion $sessions.Baseline.CoreInformationalVersion
    Add-BenchmarkMetadata CandidateCoreInformationalVersion $sessions.Candidate.CoreInformationalVersion
    Add-BenchmarkMetadata CandidateSource $source
    Add-BenchmarkMetadata CandidateWorktreeClean 'true'
    Add-BenchmarkMetadata ContextLifetime 'RetainedEngineContextsV1: one separately owned collectible context per engine, reused by all warmups and measurements; the wrapper disposes both in finally.'
    Add-BenchmarkMetadata Scope 'Same preloaded RGBA32 requests and robust public adapter; loading, outcome evaluation, persistence and context disposal are outside operation timing. All measured and priming outcomes are retained. Managed allocation excludes native allocation.'
    Add-BenchmarkCases { Add-BenchmarkCase FixedImageCorpus @{} }
    Set-BenchmarkSetup {
        param($case,$run)
        $run.Session=$sessions[$case.Engine]
        # The runner does not validate successful warmups. Capture the preceding
        # priming call here, before the next timed operation, including misses.
        if($run.Session.HasDecodedResult -and $lastIterations.ContainsKey($case.Engine) -and $lastIterations[$case.Engine] -lt 0){
            $warmupOutput=Join-Path (Join-Path $run.OutputRoot $run.RunId) 'warmup-outcomes'
            [IO.Directory]::CreateDirectory($warmupOutput) | Out-Null
            $path=Join-Path $warmupOutput ("{0}-{1}.json" -f $case.Engine,$lastIterations[$case.Engine])
            [IO.File]::WriteAllText($path,(ConvertTo-Json -InputObject @($run.Session.Evaluate()) -Depth 20))
        }
        $lastIterations[$case.Engine]=$run.Iteration
    }
    foreach($engineName in @('Baseline','Candidate')){
        Add-BenchmarkEngine $engineName {
            Add-BenchmarkOperation Decode { param($case,$run) $run.Session.Decode() }
        }
    }
    Add-BenchmarkValidation {
        param($case,$run)
        $run.Outcomes=$run.Session.Evaluate()
        if($run.Outcomes.Length -ne $run.Session.Count){throw 'Some requests were not processed.'}
        [IO.File]::WriteAllText((Join-Path $run.OutputDirectory 'outcomes.json'),(ConvertTo-Json -InputObject @($run.Outcomes) -Depth 20))
    }
    Add-BenchmarkMetric ContextIdentity { param($case,$run) $run.Session.Identity }
    Add-BenchmarkMetric Requests { param($case,$run) $run.Session.Count }
    Add-BenchmarkMetric CorrectImages { param($case,$run) @($run.Outcomes|Where-Object Status -eq Correct).Count }
    Add-BenchmarkMetric UnexpectedImages { param($case,$run) @($run.Outcomes|Where-Object {$_.Unexpected.Length -gt 0}).Count }
    Add-BenchmarkMetric InconclusiveImages { param($case,$run) @($run.Outcomes|Where-Object Status -eq Inconclusive).Count }
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
