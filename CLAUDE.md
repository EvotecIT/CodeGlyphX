# CodeGlyphX - AI Assistant Context

This file provides comprehensive context for AI coding assistants (Claude, Copilot, ChatGPT, etc.) to help users with CodeGlyphX.

## Quick Reference

**What it is:** Zero-dependency .NET library for QR codes, barcodes, and 2D matrix codes
**Install:** `dotnet add package CodeGlyphX`
**License:** Apache-2.0
**Targets:** .NET 8+, .NET 10+, .NET Standard 2.0, .NET Framework 4.7.2

## Core Patterns (Copy-Paste Ready)

### QR Code Generation

```csharp
using CodeGlyphX;

// One-liner - format auto-detected from extension
QR.Save("https://example.com", "qr.png");
QR.Save("https://example.com", "qr.svg");
QR.Save("https://example.com", "qr.pdf");

// Get bytes instead of file
byte[] png = QR.ToPng("https://example.com");
string svg = QR.ToSvg("https://example.com");

// With error correction
QR.Save("data", "qr.png", QrErrorCorrectionLevel.H);

// Styled QR
var opts = new QrEasyOptions {
    ModuleShape = QrPngModuleShape.Rounded,
    ModuleCornerRadiusPx = 3,
    Eyes = new QrPngEyeOptions {
        OuterShape = QrPngModuleShape.Circle,
        InnerShape = QrPngModuleShape.Circle,
        OuterColor = new Rgba32(220, 20, 60),
        InnerColor = new Rgba32(220, 20, 60)
    }
};
QR.Save("https://example.com", "styled.png", opts);
```

### Barcode Generation

```csharp
using CodeGlyphX;

// 1D Barcodes
Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.png");
Barcode.Save(BarcodeType.Ean13, "5901234123457", "ean.png");
Barcode.Save(BarcodeType.Code39, "ABC123", "code39.png");
Barcode.Save(BarcodeType.UpcA, "012345678905", "upca.png");

// Get bytes
byte[] png = Barcode.Png(BarcodeType.Code128, "data");
```

### 2D Matrix Codes

```csharp
using CodeGlyphX;

DataMatrixCode.Save("Serial: ABC123", "datamatrix.png");
Pdf417Code.Save("Document ID: 98765", "pdf417.png");
AztecCode.Save("Ticket: CONF-2024", "aztec.png");

// Get bytes
byte[] dm = DataMatrixCode.ToPng("data");
byte[] pdf = Pdf417Code.ToPng("data");
byte[] az = AztecCode.ToPng("data");
```

### Decoding (Reading Barcodes)

```csharp
using CodeGlyphX;

// QR decode
if (QrImageDecoder.TryDecodeImage(File.ReadAllBytes("qr.png"), out var result))
    Console.WriteLine(result.Text);

// Barcode decode
if (Barcode.TryDecodeImage(File.ReadAllBytes("barcode.png"), BarcodeType.Code128, out var barcode))
    Console.WriteLine(barcode.Text);

// Universal decode (tries all formats)
if (CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded))
    Console.WriteLine($"{decoded.Kind}: {decoded.Text}");

// Decode with options
var opts = QrPixelDecodeOptions.Screen(maxMilliseconds: 300);
QrImageDecoder.TryDecodeImage(bytes, opts, out var decoded);
```

### Payload Helpers (Structured QR Data)

```csharp
using CodeGlyphX;
using CodeGlyphX.Payloads;

// WiFi
QR.Save(QrPayloads.Wifi("NetworkName", "Password123"), "wifi.png");

// Contact (vCard)
QR.Save(QrPayloads.VCard(
    firstName: "John",
    lastName: "Doe",
    email: "john@example.com",
    phone: "+1234567890"
), "contact.png");

// OTP/2FA (Google Authenticator compatible)
QR.Save(QrPayloads.OneTimePassword(
    OtpAuthType.Totp,
    secret: "JBSWY3DPEHPK3PXP",
    label: "user@example.com",
    issuer: "MyApp"
), "otp.png");

// SEPA Payment
QR.Save(QrPayloads.Girocode(
    iban: "DE89370400440532013000",
    bic: "COBADEFFXXX",
    recipientName: "Company",
    amount: 99.99m,
    reference: "Invoice-001"
), "payment.png");

// URL, Email, Phone, SMS
QR.Save(QrPayloads.Url("https://example.com"), "url.png");
QR.Save(QrPayloads.Email("to@example.com", "Subject", "Body"), "email.png");
QR.Save(QrPayloads.Phone("+1234567890"), "phone.png");
QR.Save(QrPayloads.Sms("+1234567890", "Message"), "sms.png");
```

## Supported Formats

### Barcode Types (BarcodeType enum)
- `Code128`, `Gs1128` - High-density alphanumeric
- `Code39`, `Code93` - Alphanumeric (A-Z, 0-9, symbols)
- `Code11` - Numeric with optional checksum
- `Codabar` - Numeric with start/stop chars
- `Ean13`, `Ean8` - Retail (international)
- `UpcA`, `UpcE` - Retail (North America)
- `Itf14`, `Itf` - Interleaved 2 of 5
- `Msi`, `Plessey` - Numeric with checksum
- `Telepen` - Full ASCII
- `Pharmacode`, `PharmacodeTwoTrack` - Pharmaceutical
- `Code32` - Italian Pharmacode

### Output Formats (by extension)
- Raster: `.png`, `.jpg`, `.bmp`, `.tga`, `.ico`
- Vector: `.svg`, `.svgz`, `.pdf`, `.eps`
- Text: `.html`, `.htm`
- API only: ASCII, raw RGBA pixels

### 2D Matrix Codes
- `DataMatrixCode` - Industrial/healthcare marking
- `Pdf417Code` - ID cards, boarding passes
- `AztecCode` - Tickets, curved surfaces

## Platform Integration Examples

### ASP.NET Core

```csharp
// Minimal API
app.MapGet("/qr/{text}", (string text) =>
{
    var png = QR.ToPng(text);
    return Results.File(png, "image/png", "qr.png");
});

// Controller
[HttpGet("barcode/{text}")]
public IActionResult GetBarcode(string text)
{
    var png = Barcode.Png(BarcodeType.Code128, text);
    return File(png, "image/png");
}
```

### Blazor

```csharp
@using CodeGlyphX

<img src="@_qrDataUri" alt="QR Code" />

@code {
    private string _qrDataUri = "";

    protected override void OnInitialized()
    {
        var png = QR.ToPng("https://example.com");
        _qrDataUri = $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }
}
```

### .NET MAUI

```csharp
using CodeGlyphX;

var png = QR.ToPng("https://example.com");
QrImage.Source = ImageSource.FromStream(() => new MemoryStream(png));
```

## Key Classes Reference

| Class | Purpose | Main Methods |
|-------|---------|--------------|
| `QR` | QR code generation | `Save()`, `ToPng()`, `ToSvg()`, `ToPdf()` |
| `Barcode` | 1D barcode generation | `Save()`, `Png()`, `Svg()` |
| `DataMatrixCode` | Data Matrix generation | `Save()`, `ToPng()`, `ToSvg()` |
| `Pdf417Code` | PDF417 generation | `Save()`, `ToPng()`, `ToSvg()` |
| `AztecCode` | Aztec code generation | `Save()`, `ToPng()`, `ToSvg()` |
| `QrImageDecoder` | QR decoding from images | `TryDecodeImage()`, `DecodeImage()` |
| `CodeGlyph` | Universal decode | `TryDecode()`, `TryDecodeAllPng()` |
| `QrPayloads` | Structured payloads | `Wifi()`, `VCard()`, `OneTimePassword()`, etc. |

## Error Correction Levels (QR)

| Level | Recovery | Use Case |
|-------|----------|----------|
| `L` | ~7% | Maximum data capacity |
| `M` | ~15% | Default, balanced |
| `Q` | ~25% | Higher reliability |
| `H` | ~30% | Maximum error correction |

## Common Patterns

### Generate + Display in Web
```csharp
var base64 = Convert.ToBase64String(QR.ToPng("data"));
var dataUri = $"data:image/png;base64,{base64}";
```

### Batch Generation
```csharp
foreach (var item in items)
{
    QR.Save(item.Data, $"qr_{item.Id}.png");
}
```

### With Custom Colors
```csharp
var opts = new QrEasyOptions {
    ForegroundColor = new Rgba32(0, 100, 150),
    BackgroundColor = new Rgba32(255, 255, 255)
};
QR.Save("data", "colored.png", opts);
```

## Dependencies

- **.NET 8+/10+**: Zero dependencies
- **.NET Standard 2.0 / .NET Framework 4.7.2**: Requires `System.Memory` (4.5.5)

## AOT & Trimming

Fully compatible with Native AOT and trimming:
```xml
<PublishAot>true</PublishAot>
<PublishTrimmed>true</PublishTrimmed>
```

## Links

- NuGet: https://www.nuget.org/packages/CodeGlyphX
- GitHub: https://github.com/EvotecIT/CodeGlyphX
- Docs: https://codeglyphx.com/docs/
- API Reference: https://codeglyphx.com/api/
- Playground: https://codeglyphx.com/playground/
