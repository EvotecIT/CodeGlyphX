[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $OutputRoot,
    [string] $ManifestPath = (Join-Path $PSScriptRoot '../CodeGlyphX.Benchmarks/Corpus/corpus-manifest.json')
)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -Raw -LiteralPath $ManifestPath | ConvertFrom-Json
$root = [IO.Path]::GetFullPath($OutputRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
foreach ($sample in $manifest.Samples) {
    $path = [IO.Path]::GetFullPath((Join-Path $root $sample.File))
    if (-not $path.StartsWith($root, $comparison)) { throw "Corpus path escapes output root: $($sample.Id)" }
    if (-not (Test-Path -LiteralPath $path)) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
        $source = "https://raw.githubusercontent.com/zxing/zxing/$($manifest.Revision)/core/src/test/resources/blackbox/$($sample.File)"
        Invoke-WebRequest -Uri $source -OutFile $path
    }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $sample.Sha256) {
        throw "Corpus hash mismatch: $($sample.Id)"
    }
}
Write-Output "Verified $($manifest.Samples.Count) corpus images at $OutputRoot"
