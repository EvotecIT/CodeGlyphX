param(
    [string]$SiteRoot,
    [string]$ApiIndex,
    [string]$ProjectFile,
    [string]$ApiBase = "/api",
    [string]$PackageId = "CodeGlyphX"
)

$ErrorActionPreference = "Stop"

if (-not $SiteRoot) {
    $SiteRoot = [IO.Path]::Combine($PSScriptRoot, "..", "CodeGlyphX.Website", "wwwroot")
}
if (-not $ApiIndex) {
    $ApiIndex = [IO.Path]::Combine($SiteRoot, "api", "index.json")
}
if (-not $ProjectFile) {
    $ProjectFile = [IO.Path]::Combine($PSScriptRoot, "..", "CodeGlyphX", "CodeGlyphX.csproj")
}

$version = "unknown"
if (Test-Path $ProjectFile) {
    $content = Get-Content $ProjectFile -Raw
    $match = [regex]::Match($content, "<VersionPrefix>([^<]+)</VersionPrefix>")
    if ($match.Success) { $version = $match.Groups[1].Value }
}

$typeCount = $null
if (Test-Path $ApiIndex) {
    try {
        $index = Get-Content $ApiIndex -Raw | ConvertFrom-Json
        $typeCount = $index.typeCount
    } catch { }
}

if (-not (Test-Path $SiteRoot)) {
    throw "Site root not found: $SiteRoot"
}

$llmsTxtPath = Join-Path $SiteRoot "llms.txt"
$llmsJsonPath = Join-Path $SiteRoot "llms.json"

$quickstart = @"
using CodeGlyphX;

// Generate a QR code
QR.Save("https://example.com", "qr.png");

// Generate a barcode
Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.png");

// Decode an image
if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine(result.Text);
}
"@

$apiIndexRef = "$ApiBase/index.json"
$apiSearchRef = "$ApiBase/search.json"
$apiTypeRef = "$ApiBase/types/{slug}.json"

$lines = @()
$lines += "# CodeGlyphX"
$lines += "Version: $version"
$lines += "Package: $PackageId"
if ($typeCount) { $lines += "API types: $typeCount" }
$lines += ""
$lines += "Install:"
$lines += "- dotnet add package $PackageId"
$lines += ""
$lines += "Quickstart (C#):"
$lines += '```csharp'
$lines += $quickstart.TrimEnd()
$lines += '```'
$lines += ""
$lines += "Machine-friendly API data (no HTML parsing required):"
$lines += "- $apiIndexRef"
$lines += "- $apiSearchRef"
$lines += "- $apiTypeRef"
$lines += ""
$lines += "Slug rule: lower-case, dots and symbols become dashes. Example: CodeGlyphX.QR -> codeglyphx-qr"
$lines += "Generated from XML docs during the website build (Build/Build-Website.ps1)."
$lines += ""
$lines += "For comprehensive AI context including FAQ and examples, see: /llms-full.txt"

$lines -join [Environment]::NewLine | Set-Content -Path $llmsTxtPath -Encoding UTF8

$llmsJson = [ordered]@{
    name = "CodeGlyphX"
    version = $version
    package = $PackageId
    install = @(
        "dotnet add package $PackageId"
    )
    quickstart = @(
        "using CodeGlyphX;",
        'QR.Save("https://example.com", "qr.png");',
        'Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.png");',
        "if (QrImageDecoder.TryDecodeImage(imageBytes, out var result)) { Console.WriteLine(result.Text); }"
    )
    api = [ordered]@{
        index = $apiIndexRef
        search = $apiSearchRef
        type = $apiTypeRef
        slugRule = "lower-case, dots/symbols -> dashes"
    }
}

$llmsJson | ConvertTo-Json -Depth 6 | Set-Content -Path $llmsJsonPath -Encoding UTF8

# ============================================================================
# GENERATE llms-full.txt (Comprehensive AI context)
# ============================================================================
$llmsFullPath = Join-Path $SiteRoot "llms-full.txt"
$faqJsonPath = [IO.Path]::Combine($PSScriptRoot, "..", "Assets", "Data", "faq.json")

$fullLines = @()
$fullLines += "# CodeGlyphX - Complete AI Context"
$fullLines += "# This file provides comprehensive context for AI assistants"
$fullLines += ""
$fullLines += "## Overview"
$fullLines += "CodeGlyphX is a zero-dependency .NET library for generating and decoding QR codes, barcodes, and 2D matrix codes."
$fullLines += "- Package: $PackageId"
$fullLines += "- Version: $version"
if ($typeCount) { $fullLines += "- API types: $typeCount" }
$fullLines += "- License: Apache-2.0"
$fullLines += "- Targets: .NET 8+, .NET 10+, .NET Standard 2.0, .NET Framework 4.7.2"
$fullLines += ""
$fullLines += "## Installation"
$fullLines += '```'
$fullLines += "dotnet add package $PackageId"
$fullLines += '```'
$fullLines += ""
$fullLines += "## Quick Reference"
$fullLines += ""
$fullLines += "### QR Code Generation"
$fullLines += '```csharp'
$fullLines += "using CodeGlyphX;"
$fullLines += 'QR.Save("https://example.com", "qr.png");  // PNG file'
$fullLines += 'QR.Save("https://example.com", "qr.svg");  // SVG file'
$fullLines += 'byte[] png = QR.ToPng("data");             // PNG bytes'
$fullLines += 'string svg = QR.ToSvg("data");             // SVG string'
$fullLines += '```'
$fullLines += ""
$fullLines += "### Barcode Generation"
$fullLines += '```csharp'
$fullLines += "using CodeGlyphX;"
$fullLines += 'Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.png");'
$fullLines += 'Barcode.Save(BarcodeType.Ean13, "5901234123457", "ean.png");'
$fullLines += 'byte[] png = Barcode.Png(BarcodeType.Code128, "data");'
$fullLines += '```'
$fullLines += ""
$fullLines += "### 2D Matrix Codes"
$fullLines += '```csharp'
$fullLines += "using CodeGlyphX;"
$fullLines += 'DataMatrixCode.Save("Serial: ABC123", "datamatrix.png");'
$fullLines += 'Pdf417Code.Save("Document ID: 98765", "pdf417.png");'
$fullLines += 'AztecCode.Save("Ticket: CONF-2024", "aztec.png");'
$fullLines += '```'
$fullLines += ""
$fullLines += "### Decoding (Reading)"
$fullLines += '```csharp'
$fullLines += "using CodeGlyphX;"
$fullLines += 'if (QrImageDecoder.TryDecodeImage(File.ReadAllBytes("qr.png"), out var result))'
$fullLines += '    Console.WriteLine(result.Text);'
$fullLines += ""
$fullLines += 'if (Barcode.TryDecodeImage(bytes, BarcodeType.Code128, out var barcode))'
$fullLines += '    Console.WriteLine(barcode.Text);'
$fullLines += '```'
$fullLines += ""
$fullLines += "### Payload Helpers"
$fullLines += '```csharp'
$fullLines += "using CodeGlyphX;"
$fullLines += "using CodeGlyphX.Payloads;"
$fullLines += 'QR.Save(QrPayloads.Wifi("NetworkName", "Password"), "wifi.png");'
$fullLines += 'QR.Save(QrPayloads.VCard(firstName: "John", lastName: "Doe", email: "john@example.com"), "contact.png");'
$fullLines += 'QR.Save(QrPayloads.OneTimePassword(OtpAuthType.Totp, "SECRET", label: "user@example.com", issuer: "MyApp"), "otp.png");'
$fullLines += 'QR.Save(QrPayloads.Girocode(iban: "DE89...", bic: "COBADEFFXXX", recipientName: "Company", amount: 99.99m), "payment.png");'
$fullLines += '```'
$fullLines += ""
$fullLines += "## Barcode Types (BarcodeType enum)"
$fullLines += "Code128, Gs1128, Code39, Code93, Code11, Codabar, Ean13, Ean8, UpcA, UpcE, Itf14, Itf, Msi, Plessey, Telepen, Pharmacode"
$fullLines += ""
$fullLines += "## Output Formats"
$fullLines += "Auto-detected by file extension: .png, .jpg, .bmp, .svg, .svgz, .pdf, .eps, .html, .tga, .ico"
$fullLines += ""
$fullLines += "## Key Classes"
$fullLines += "- QR: QR code generation (Save, ToPng, ToSvg, ToPdf)"
$fullLines += "- Barcode: 1D barcode generation (Save, Png, Svg)"
$fullLines += "- DataMatrixCode: Data Matrix generation"
$fullLines += "- Pdf417Code: PDF417 generation"
$fullLines += "- AztecCode: Aztec code generation"
$fullLines += "- QrImageDecoder: QR decoding from images"
$fullLines += "- CodeGlyph: Universal decode for any barcode type"
$fullLines += "- QrPayloads: Structured payload generators"
$fullLines += ""
$fullLines += "## Platform Examples"
$fullLines += ""
$fullLines += "### ASP.NET Core"
$fullLines += '```csharp'
$fullLines += 'app.MapGet("/qr/{text}", (string text) => {'
$fullLines += '    var png = QR.ToPng(text);'
$fullLines += '    return Results.File(png, "image/png");'
$fullLines += '});'
$fullLines += '```'
$fullLines += ""
$fullLines += "### Blazor"
$fullLines += '```csharp'
$fullLines += 'var png = QR.ToPng("data");'
$fullLines += 'var dataUri = $"data:image/png;base64,{Convert.ToBase64String(png)}";'
$fullLines += '```'
$fullLines += ""
$fullLines += "### MAUI"
$fullLines += '```csharp'
$fullLines += 'var png = QR.ToPng("data");'
$fullLines += 'QrImage.Source = ImageSource.FromStream(() => new MemoryStream(png));'
$fullLines += '```'
$fullLines += ""

# Add FAQ if available
if (Test-Path $faqJsonPath) {
    $faqData = Get-Content $faqJsonPath -Raw | ConvertFrom-Json
    $fullLines += "## FAQ"
    $fullLines += ""
    foreach ($section in $faqData.sections) {
        $fullLines += "### $($section.title)"
        foreach ($item in $section.items) {
            $plainAnswer = $item.answer -replace '<[^>]+>', ' ' -replace '\s+', ' '
            $plainAnswer = $plainAnswer.Trim()
            $fullLines += ""
            $fullLines += "Q: $($item.question)"
            $fullLines += "A: $plainAnswer"
        }
        $fullLines += ""
    }
}

$fullLines += "## Machine-Readable API Data"
$fullLines += "- Index: $apiIndexRef"
$fullLines += "- Search: $apiSearchRef"
$fullLines += "- Types: $apiTypeRef"
$fullLines += ""
$fullLines += "## Links"
$fullLines += "- NuGet: https://www.nuget.org/packages/CodeGlyphX"
$fullLines += "- GitHub: https://github.com/EvotecIT/CodeGlyphX"
$fullLines += "- Documentation: https://codeglyphx.com/docs/"
$fullLines += "- API Reference: https://codeglyphx.com/api/"
$fullLines += "- Playground: https://codeglyphx.com/playground/"

$fullLines -join [Environment]::NewLine | Set-Content -Path $llmsFullPath -Encoding UTF8

Write-Host "Generated llms.txt, llms.json, and llms-full.txt" -ForegroundColor Green
