#Requires -Version 7.0
<#
.SYNOPSIS
    Snapshots the ImagePlayground QR code and barcode commands for codeglyphx.com.

.DESCRIPTION
    Reads CmdletsToExport from an ImagePlayground module manifest, groups the CodeGlyphX-powered commands into
    families and writes Website/data/imageplayground.json. Point it at the same published module version that
    Render-PowerShellExamples.ps1 runs, so the command list and the examples describe one release.

.EXAMPLE
    pwsh ./Website/build/Sync-ImagePlayground.ps1

.EXAMPLE
    pwsh ./Website/build/Sync-ImagePlayground.ps1 -ModulePath D:\Modules\ImagePlayground\3.2.7\ImagePlayground.psd1
#>
[CmdletBinding()]
param(
    [string] $ModulePath
)

$ErrorActionPreference = 'Stop'
$website = Split-Path -Parent $PSScriptRoot
if (-not $ModulePath) {
    $installed = Get-Module ImagePlayground -ListAvailable | Sort-Object Version -Descending | Select-Object -First 1
    if (-not $installed) { throw 'ImagePlayground is not installed. Run Install-Module ImagePlayground or pass -ModulePath.' }
    $ModulePath = $installed.Path
}

$manifest = Import-PowerShellDataFile -LiteralPath $ModulePath
$cmdlets = @($manifest.CmdletsToExport | Sort-Object -Unique)
$families = @(
    [ordered]@{ id = 'qr'; title = 'QR codes'; pattern = '^New-ImageQR(Code|Contact)$'; description = 'QR codes from text, URLs or contact details, with colours and a logo.' }
    [ordered]@{ id = 'payloads'; title = 'QR payloads'; pattern = '^New-ImageQRCode\w+'; description = 'Wi-Fi, calendar, email, OTP, payments, cryptocurrency, phone, SMS and location payloads.' }
    [ordered]@{ id = 'barcodes'; title = 'Barcodes'; pattern = '^New-ImageBarCode$'; description = 'EAN, UPC, Code 128, Data Matrix, PDF417 and other linear and 2D symbologies.' }
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

$catalog = [ordered]@{
    generatedBy = 'Website/build/Sync-ImagePlayground.ps1'
    module = [ordered]@{
        name = 'ImagePlayground'
        version = $manifest.ModuleVersion
        commandCount = $cmdlets.Count
        codeGlyphXCommandCount = [int](@($familyResults | ForEach-Object { $_.commandCount }) | Measure-Object -Sum).Sum
        install = "Install-Module -Name ImagePlayground -MinimumVersion $($manifest.ModuleVersion) -Scope CurrentUser"
        powerShell = 'Windows PowerShell 5.1 and PowerShell 7+'
    }
    families = @($familyResults)
}
$catalogPath = Join-Path $website 'data/imageplayground.json'
$catalog | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $catalogPath -Encoding utf8NoBOM
Write-Host "ImagePlayground $($manifest.ModuleVersion): $($cmdlets.Count) commands, $($catalog.module.codeGlyphXCommandCount) powered by CodeGlyphX -> $catalogPath"

# Command help for the on-site reference (/powershell/commands/), filtered to the commands above.
$helpSource = Join-Path (Split-Path -Parent $ModulePath) 'en-US/ImagePlayground-help.xml'
if (Test-Path -LiteralPath $helpSource) {
    $keep = @($familyResults | ForEach-Object { $_.commands })
    [xml] $maml = Get-Content -LiteralPath $helpSource -Raw
    foreach ($node in @($maml.helpItems.command)) {
        if ($node.details.name.Trim() -notin $keep) { $null = $maml.helpItems.RemoveChild($node) }
    }
    $helpPath = Join-Path $website 'powershell/imageplayground-help.xml'
    $maml.Save($helpPath)
    Write-Host "Kept help for $(@($maml.helpItems.command).Count) commands -> $helpPath"
} else {
    Write-Warning "No command help at $helpSource; the on-site command reference keeps its current snapshot."
}
