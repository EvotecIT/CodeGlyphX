#Requires -Version 7.0
<#
.SYNOPSIS
    Snapshots the ImagePlayground QR code and barcode commands and samples for codeglyphx.com.

.DESCRIPTION
    Reads CmdletsToExport from ImagePlayground.psd1, groups the CodeGlyphX-powered commands into families and writes
    Website/data/imageplayground.json. When -SamplesPath is supplied, copies the selected example outputs into
    Website/static/powershell/. Produce the samples by running the ImagePlayground example scripts against a current
    build, for example:

        $env:IMAGEPLAYGROUND_DEVELOPMENT = '1'
        ./Examples/QrCode.ps1

.EXAMPLE
    pwsh ./Website/build/Sync-ImagePlayground.ps1 -ImagePlaygroundRoot C:\Support\GitHub\ImagePlayground -SamplesPath D:\Scratch\ip-samples
#>
[CmdletBinding()]
param(
    [string] $ImagePlaygroundRoot = (Join-Path ($env:EVOTEC_GITHUB_ROOT ?? 'C:\Support\GitHub') 'ImagePlayground'),
    [string] $SamplesPath = (Join-Path $ImagePlaygroundRoot 'Examples/Samples')
)

$ErrorActionPreference = 'Stop'
$website = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $ImagePlaygroundRoot 'ImagePlayground.psd1'
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "ImagePlayground.psd1 was not found under '$ImagePlaygroundRoot'. Pass -ImagePlaygroundRoot."
}

$manifest = Import-PowerShellDataFile -LiteralPath $manifestPath
$cmdlets = @($manifest.CmdletsToExport | Sort-Object -Unique)
$families = @(
    [ordered]@{ id = 'qr'; title = 'QR codes'; pattern = '^New-ImageQR(Code|Contact)$'; description = 'QR codes from text, URLs or contact details, with styling options.' }
    [ordered]@{ id = 'payloads'; title = 'QR payloads'; pattern = '^New-ImageQRCode\w+'; description = 'Wi-Fi, calendar, email, OTP, payments, cryptocurrency, phone, SMS and location payloads.' }
    [ordered]@{ id = 'barcodes'; title = 'Barcodes'; pattern = '^New-ImageBarCode$'; description = 'EAN, UPC, Code 128, Data Matrix, PDF417 and the other CodeGlyphX symbologies.' }
    [ordered]@{ id = 'decode'; title = 'Decoding'; pattern = '^Get-Image(QRCode|BarCode)$'; description = 'Read QR codes and barcodes back from image files.' }
)

$familyResults = foreach ($family in $families) {
    $names = @($cmdlets | Where-Object { $_ -match $family.pattern })
    [ordered]@{
        id = $family.id
        title = $family.title
        description = $family.description
        commandCount = $names.Count
        commands = $names
    }
}

$commit = (git -C $ImagePlaygroundRoot rev-parse --short HEAD 2>$null) ?? ''
$catalog = [ordered]@{
    generatedBy = 'Website/build/Sync-ImagePlayground.ps1'
    module = [ordered]@{
        name = 'ImagePlayground'
        version = $manifest.ModuleVersion
        commit = $commit
        commandCount = $cmdlets.Count
        codeGlyphXCommandCount = [int](@($familyResults | ForEach-Object { $_.commandCount }) | Measure-Object -Sum).Sum
        install = 'Install-Module -Name ImagePlayground -Scope CurrentUser'
        powerShell = 'Windows PowerShell 5.1 and PowerShell 7+'
    }
    families = @($familyResults)
}
$catalogPath = Join-Path $website 'data/imageplayground.json'
$catalog | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $catalogPath -Encoding utf8NoBOM
Write-Host "ImagePlayground $($manifest.ModuleVersion): $($cmdlets.Count) commands, $($catalog.module.codeGlyphXCommandCount) powered by CodeGlyphX -> $catalogPath"

if ($SamplesPath) {
    $target = Join-Path $website 'static/powershell'
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    $samples = 'QRCode.png', 'QRCodeContact.png', 'QRCodeWiFi.png', 'BarcodeEAN13.png'
    foreach ($sample in $samples) {
        $source = Join-Path $SamplesPath $sample
        if (-not (Test-Path -LiteralPath $source)) { throw "Missing sample '$source'. Run the ImagePlayground QR and barcode examples first." }
        Copy-Item -LiteralPath $source -Destination (Join-Path $target $sample) -Force
    }
    Write-Host "Copied $($samples.Count) samples to $target"
}
