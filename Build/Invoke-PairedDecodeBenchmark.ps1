#requires -Version 7.4
#requires -PSEdition Core
#requires -Modules PSPublishModule
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$BaselineRoot,
    [Parameter(Mandatory)][string]$CandidateRoot,
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$ImageRoot,
    [Parameter(Mandatory)][string]$OutputRoot,
    [string]$RepositoryRoot=(Split-Path -Parent $PSScriptRoot),
    [string]$ExpectedSource,
    [ValidateRange(1,1000)][int]$Iterations=12,
    [ValidateRange(0,100)][int]$Warmup=3,
    [string]$ProcessorAffinityMask
)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../CodeGlyphX.Benchmarks/Compare/IsolatedCorpusSession.cs')
$baselinePath=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($BaselineRoot)
$candidatePath=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($CandidateRoot)
$manifestPath=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ManifestPath)
$imagePath=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ImageRoot)
$output=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputRoot)
$repositoryPath=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RepositoryRoot)
$sessions=@{}
$lastIterations=@{}
Push-Location -LiteralPath $repositoryPath
try {
    $source=(git rev-parse HEAD).Trim()
    if($LASTEXITCODE -ne 0 -or (git status --porcelain)){throw 'Use a clean source snapshot for paired measurements.'}
    if($ExpectedSource -and $source -ne $ExpectedSource){throw 'Source differs from the expected snapshot.'}
    $sessions.Baseline=[CodeGlyphX.Benchmarks.Tools.IsolatedCorpusSession]::new($baselinePath,$manifestPath,$imagePath)
    $sessions.Candidate=[CodeGlyphX.Benchmarks.Tools.IsolatedCorpusSession]::new($candidatePath,$manifestPath,$imagePath)
    $variables=@{OutputRoot=$output;ManifestPath=$manifestPath;Iterations="$Iterations";Warmup="$Warmup";Source=$source}
    $settings=[ScriptBlock]::Create([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'paired-decode-contexts.benchmark.ps1'))).GetNewClosure()
    $options=@{Settings=$settings;Variable=$variables;ProcessPriority='Normal'}
    if($ProcessorAffinityMask){$options.ProcessorAffinityMask=$ProcessorAffinityMask}
    $result=Invoke-BenchmarkSuite @options
    if((git rev-parse HEAD).Trim() -ne $source -or (git status --porcelain)){throw 'Source changed during paired measurements.'}
    $result
} finally {
    foreach($session in $sessions.Values){$session.Dispose()}
    Pop-Location
}
