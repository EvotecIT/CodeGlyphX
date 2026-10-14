#Requires -Version 7.0
<#
.SYNOPSIS
    Runs the website's ImagePlayground examples and records their scripts, console output and generated files.

.DESCRIPTION
    Reads Website/powershell/examples.json, runs each Website/powershell/<id>.ps1 in a fresh pwsh process with
    ImagePlayground imported, and writes:
      - Website/static/powershell/examples/<id>/*  (the files each script produced)
      - Website/data/powershell-examples.json       (script text, console output and output files for the pages)
    The page shows each script exactly as it was run, next to what it produced. Run it against a published module
    version so the examples match what users install.

.EXAMPLE
    pwsh ./Website/build/Render-PowerShellExamples.ps1

.EXAMPLE
    pwsh ./Website/build/Render-PowerShellExamples.ps1 -ModulePath D:\Modules\ImagePlayground\3.2.7\ImagePlayground.psd1 -Id wifi, barcodes
#>
[CmdletBinding()]
param(
    [string] $ModulePath,
    [string[]] $Id
)

$ErrorActionPreference = 'Stop'
# pwsh -File passes "-Id a,b" as one string.
$Id = @($Id | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$website = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $website 'powershell'
$outputRoot = Join-Path $website 'static/powershell/examples'
$dataPath = Join-Path $website 'data/powershell-examples.json'
$manifest = Get-Content -LiteralPath (Join-Path $sourceRoot 'examples.json') -Raw | ConvertFrom-Json

if (-not $ModulePath) {
    $installed = Get-Module ImagePlayground -ListAvailable | Sort-Object Version -Descending | Select-Object -First 1
    if (-not $installed) { throw 'ImagePlayground is not installed. Run Install-Module ImagePlayground or pass -ModulePath.' }
    $ModulePath = $installed.Path
}
$ModulePath = (Resolve-Path -LiteralPath $ModulePath).Path
$moduleVersion = (Import-PowerShellDataFile -LiteralPath $ModulePath).ModuleVersion

function Get-ImageSize([string] $path) {
    $bytes = [IO.File]::ReadAllBytes($path)
    if ($bytes.Length -ge 24 -and $bytes[0] -eq 0x89 -and $bytes[1] -eq 0x50) {
        $width = [Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes, 16))
        $height = [Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes, 20))
        return @($width, $height)
    }
    if ($bytes.Length -ge 10 -and $bytes[0] -eq 0x47 -and $bytes[1] -eq 0x49) {
        return @([BitConverter]::ToUInt16($bytes, 6), [BitConverter]::ToUInt16($bytes, 8))
    }
    return @($null, $null)
}

$previous = @{}
if (Test-Path -LiteralPath $dataPath) {
    foreach ($entry in (Get-Content -LiteralPath $dataPath -Raw | ConvertFrom-Json).examples) { $previous[$entry.id] = $entry }
}

$results = foreach ($example in $manifest.examples) {
    if ($Id -and $example.id -notin $Id) {
        if ($previous.ContainsKey($example.id)) { $previous[$example.id] }
        continue
    }

    $scriptPath = Join-Path $sourceRoot "$($example.id).ps1"
    $outputDirectory = Join-Path $outputRoot $example.id
    if (Test-Path -LiteralPath $outputDirectory) { [IO.Directory]::Delete($outputDirectory, $true) }
    [void][IO.Directory]::CreateDirectory($outputDirectory)

    # ImagePlayground resolves relative paths against the process directory, so pin both locations.
    $command = @"
`$ErrorActionPreference = 'Stop'
Import-Module '$($ModulePath.Replace("'", "''"))'
Set-Location -LiteralPath '$($outputDirectory.Replace("'", "''"))'
[Environment]::CurrentDirectory = (Get-Location).Path
& '$($scriptPath.Replace("'", "''"))' | Out-String -Width 100
"@
    $console = pwsh -NoProfile -NonInteractive -Command $command 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "Example '$($example.id)' failed:`n$console" }

    $outputs = foreach ($file in Get-ChildItem -LiteralPath $outputDirectory -File | Sort-Object Name) {
        $size = Get-ImageSize $file.FullName
        [ordered]@{ file = $file.Name; url = "/powershell/examples/$($example.id)/$($file.Name)"; width = $size[0]; height = $size[1] }
    }
    $lines = ($console -replace "`r", '') -split "`n" | ForEach-Object { $_.TrimEnd() }
    $text = (($lines -join "`n") -replace "^\n+", '' -replace "\n{3,}", "`n`n").TrimEnd()

    Write-Host "$($example.id): $(@($outputs).Count) file(s)"
    [ordered]@{
        id      = $example.id
        family  = $example.family
        title   = $example.title
        body    = $example.body
        show    = $example.show
        script  = (Get-Content -LiteralPath $scriptPath -Raw).Replace("`r", '').TrimEnd()
        console = $text
        outputs = @($outputs)
    }
}

$document = [ordered]@{
    generatedBy = 'Website/build/Render-PowerShellExamples.ps1'
    module      = [ordered]@{ name = 'ImagePlayground'; version = "$moduleVersion" }
    examples    = @($results)
}
[IO.File]::WriteAllText($dataPath, ($document | ConvertTo-Json -Depth 8) + "`n", [Text.UTF8Encoding]::new($false))
Write-Host "Wrote $dataPath with $(@($results).Count) examples (ImagePlayground $moduleVersion)."
