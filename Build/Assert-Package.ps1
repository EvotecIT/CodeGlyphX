[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackageDirectory
)

$ErrorActionPreference = 'Stop'

$resolvedDirectory = (Resolve-Path -LiteralPath $PackageDirectory).Path
$projectPath = Join-Path $PSScriptRoot '..\CodeGlyphX\CodeGlyphX.csproj'
[xml] $project = Get-Content -LiteralPath $projectPath -Raw
$versionNode = $project.SelectSingleNode('/Project/PropertyGroup/VersionPrefix')
$version = if ($null -eq $versionNode) { '' } else { $versionNode.InnerText.Trim() }
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "CodeGlyphX.csproj does not define VersionPrefix."
}

$package = @(Get-ChildItem -LiteralPath $resolvedDirectory -Filter "CodeGlyphX.$version.nupkg" -File)
$symbols = @(Get-ChildItem -LiteralPath $resolvedDirectory -Filter "CodeGlyphX.$version.snupkg" -File)
if ($package.Count -ne 1 -or $symbols.Count -ne 1) {
    throw "Expected one CodeGlyphX $version package and one symbol package; found $($package.Count) and $($symbols.Count)."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Assert-ZipEntries {
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string[]] $ExpectedEntries
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entries = @($archive.Entries.FullName)
        foreach ($entry in $ExpectedEntries) {
            if ($entry -notin $entries) {
                throw "Package '$Path' is missing '$entry'."
            }
        }
    } finally {
        $archive.Dispose()
    }
}

function Assert-DependencyGroups {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.GetEntry('CodeGlyphX.nuspec')
        if ($null -eq $entry) {
            throw "Package '$Path' is missing CodeGlyphX.nuspec."
        }
        $reader = [System.IO.StreamReader]::new($entry.Open())
        try {
            [xml] $nuspec = $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }
    } finally {
        $archive.Dispose()
    }

    $groups = @($nuspec.SelectNodes("//*[local-name()='dependencies']/*[local-name()='group']"))
    $expected = @{
        '.NETStandard2.0' = @('System.Memory', 'System.Text.Encoding.CodePages')
        '.NETFramework4.7.2' = @('System.Memory', 'System.Text.Encoding.CodePages')
        'net8.0' = @()
        'net10.0' = @()
    }
    foreach ($targetFramework in $expected.Keys) {
        $group = @($groups | Where-Object { $_.targetFramework -eq $targetFramework })
        if ($group.Count -ne 1) {
            throw "Package '$Path' must contain exactly one dependency group for '$targetFramework'."
        }
        $actualIds = @($group[0].SelectNodes("*[local-name()='dependency']") | ForEach-Object { $_.id } | Sort-Object)
        $expectedIds = @($expected[$targetFramework] | Sort-Object)
        if (($actualIds -join '|') -ne ($expectedIds -join '|')) {
            throw "Package '$Path' dependency group '$targetFramework' contains '$($actualIds -join ', ')' instead of '$($expectedIds -join ', ')'."
        }
    }
}

$frameworks = @('netstandard2.0', 'net472', 'net8.0', 'net10.0')
$libraryEntries = foreach ($framework in $frameworks) {
    "lib/$framework/CodeGlyphX.dll"
    "lib/$framework/CodeGlyphX.xml"
}

$expectedLibraryEntries = @(
    'CodeGlyphX.nuspec'
    'README.md'
    'LICENSE'
    'THIRD-PARTY-NOTICES.md'
    'codeglyphx-qr-icon.png'
) + $libraryEntries
Assert-ZipEntries -Path $package[0].FullName -ExpectedEntries $expectedLibraryEntries
Assert-DependencyGroups -Path $package[0].FullName

$symbolEntries = $frameworks | ForEach-Object { "lib/$_/CodeGlyphX.pdb" }
$expectedSymbolEntries = @('CodeGlyphX.nuspec') + $symbolEntries
Assert-ZipEntries -Path $symbols[0].FullName -ExpectedEntries $expectedSymbolEntries

Write-Host "Validated CodeGlyphX $version package and symbols for $($frameworks -join ', ')."
