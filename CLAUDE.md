# CodeGlyphX assistant context

Use this file as a compact contract when suggesting CodeGlyphX APIs. The README, compiled examples, source XML comments, and generated API reference remain authoritative.

## Product boundary

CodeGlyphX is a pure-managed .NET toolkit for QR codes, linear barcodes, Data Matrix, PDF417, Aztec, QR payloads, and image rendering/decoding. It does not use System.Drawing, SkiaSharp, ImageSharp, or a native codec library.

Targets:

- `net8.0` and `net10.0`: full QR pixel pipeline, trim/AOT analyzers, no runtime package dependency
- `netstandard2.0` and `net472`: managed `System.Memory` dependency and a less capable QR image fallback

Do not describe every symbology or codec path as equally robust. Use the documented format matrix and known limits.

## Primary rendering APIs

Use extension-based `Save` for files:

```csharp
using CodeGlyphX;

QR.Save("https://example.com", "qr.png");
Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.svg");
DataMatrixCode.Save("LOT-42", "lot.png");
Pdf417Code.Save("DOCUMENT-42", "document.pdf");
AztecCode.Save("TICKET-42", "ticket.svg");
```

Use an explicit `OutputFormat` for memory or stream output:

```csharp
using CodeGlyphX.Rendering;

byte[] png = QrCode.Render("Hello", OutputFormat.Png).Data;
string svg = Barcode.Render(BarcodeType.Code128, "PRODUCT-123", OutputFormat.Svg).GetText();

using var stream = File.Create("hello.pdf");
OutputWriter.Write(stream, QrCode.Render("Hello", OutputFormat.Pdf));
```

Do not suggest removed per-format facade or builder methods such as `RenderPng`, `ToPng`, `.Png()`, or `SaveSvg`. Builders terminate with `Render(format)`, `Save(path)`, or `Save(stream, format)`.

## Options

```csharp
using CodeGlyphX.Rendering.Png;

var options = new QrEasyOptions {
    ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
    ModuleShape = QrPngModuleShape.Rounded,
    Art = QrArt.Theme(QrArtTheme.NeonGlow, QrArtVariant.Conservative, intensity: 60)
};

QR.Save("https://example.com", "styled.png", options);
```

Use `QrEasy.EvaluateScanHeuristics` for static checks, but never describe its score as proof that output will scan. Validate final artifacts on target scanners and devices.

## Payloads

```csharp
using CodeGlyphX.Payloads;

QR.Save(QrPayloads.Wifi("NetworkName", "Password123"), "wifi.png");

QR.Save(QrPayload.VCard(
    firstName: "Ava",
    lastName: "Stone",
    phone: "+14155550198",
    email: "ava@example.com",
    organization: "Example"), "contact.png");

QR.Save(QrPayloads.OneTimePassword(
    OtpAuthType.Totp,
    secretBase32: "JBSWY3DPEHPK3PXP",
    label: "user@example.com",
    issuer: "Example"), "otp.png");

QR.Save(QrPayloads.Girocode(
    iban: "DE89370400440532013000",
    bic: "COBADEFFXXX",
    name: "Example",
    amount: 99.99m,
    remittanceInformation: "Invoice-42"), "payment.png");
```

## Decoding

Use `QrImageDecoder` for QR-only input and `CodeGlyph` for unified recognition:

```csharp
byte[] image = File.ReadAllBytes("code.png");
var options = new CodeGlyphDecodeOptions {
    Qr = QrPixelDecodeOptions.Screen(budgetMilliseconds: 500, maxDimension: 1600),
    Image = ImageDecodeOptions.Strict(
        maxBytes: 8 * 1024 * 1024,
        maxPixels: 8_000_000,
        maxDimension: 1600)
        .WithRecognitionBudget(500)
};

if (CodeGlyph.TryDecodeImage(image, out var decoded, options)) {
    Console.WriteLine($"{decoded.Kind}: {decoded.Text}");
}
```

Decode limits:

- `MaxBytes`/`MaxPixels`: `null` inherits the `ImageReader` global, `0` disables that per-call limit, positive values are explicit limits.
- `MaxDimension` resizes after original-image validation and is not a codec-memory limit.
- `RecognitionBudgetMilliseconds` applies after raster decode during symbol recognition.
- `QrPixelDecodeOptions.BudgetMilliseconds` is a cooperative budget for one public call. It does not silently change the selected decode profile and is not a hard real-time deadline.
- Unsupported WebP VP8 animation interframes fail rather than returning fabricated pixels.

## Release evidence

Before claiming a release is ready, require:

- complete solution build, including fuzz and examples
- `net8.0`, `net10.0`, and Windows `net472` tests
- inspected `.nupkg` and `.snupkg` contents for all four targets
- a published and executed NativeAOT consumer
- generated website/API docs built from current XML output

## Links

- [NuGet](https://www.nuget.org/packages/CodeGlyphX)
- [Documentation](https://codeglyphx.com/docs/)
- [API reference](https://codeglyphx.com/api/)
- [2.0 migration](Website/content/docs/migration-2.md)
