[CmdletBinding()]
param(
    [string] $FuzzAssembly = (Join-Path $PSScriptRoot '../CodeGlyphX.Fuzz/bin/Release/net8.0/CodeGlyphX.Fuzz.dll'),
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '../artifacts/fuzz-replay'),
    [ValidateRange(1, 128)]
    [int] $MaxCases = 32
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$assembly = (Resolve-Path -LiteralPath $FuzzAssembly).Path
$seedPaths = @(
    'CodeGlyphX.Tests/Fixtures/ImageSamples/pngsuite-basn0g01.png',
    'CodeGlyphX.Tests/Fixtures/ImageSamples/pngsuite-basn6a16.png',
    'CodeGlyphX.Tests/Fixtures/Webp/independent-vp8-odd-color.webp',
    'CodeGlyphX.Tests/Fixtures/Webp/independent-vp8-pattern.webp',
    'CodeGlyphX.Tests/Fixtures/Jpeg/progressive.jpg'
)
$seeds = [Collections.Generic.List[byte[]]]::new()
foreach ($seedPath in $seedPaths) {
    $seeds.Add([IO.File]::ReadAllBytes((Join-Path $repositoryRoot $seedPath)))
}
$seeds.Add([Convert]::FromBase64String('R0lGODlhAQABAPAAAAAAAAAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw=='))

[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
for ($case = 0; $case -lt $MaxCases; $case++) {
    $seedIndex = $case % $seeds.Count
    [byte[]] $data = $seeds[$seedIndex].Clone()
    $mutation = [int][Math]::Floor($case / $seeds.Count)
    switch ($mutation) {
        0 { }
        1 { $data = $data[0..([Math]::Min(8, $data.Length) - 1)] }
        2 { $data = $data[0..([int][Math]::Max(1, [Math]::Floor($data.Length / 2)) - 1)] }
        default {
            $offset = ($case * 104729) % $data.Length
            $data[$offset] = $data[$offset] -bxor (1 -shl ($case % 8))
        }
    }
    $input = Join-Path $output ('case-{0:D3}-seed-{1}-mutation-{2}.bin' -f $case, $seedIndex, $mutation)
    [IO.File]::WriteAllBytes($input, $data)
    & dotnet $assembly $input
    if ($LASTEXITCODE -ne 0) { throw "Fuzz replay failed for $input (exit code $LASTEXITCODE)." }
}
Write-Host "Passed $MaxCases deterministic codec replay cases. Inputs are retained in $output."
