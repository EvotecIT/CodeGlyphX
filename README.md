# CodeGlyphX

CodeGlyphX is a pure-managed .NET toolkit for QR codes, linear barcodes, Data Matrix, PDF417, Aztec, structured QR payloads, and image rendering/decoding. It does not require native graphics libraries such as System.Drawing, SkiaSharp, or ImageSharp.

[![NuGet](https://img.shields.io/nuget/v/CodeGlyphX)](https://www.nuget.org/packages/CodeGlyphX)
[![CI](https://github.com/EvotecIT/CodeGlyphX/actions/workflows/ci.yml/badge.svg)](https://github.com/EvotecIT/CodeGlyphX/actions/workflows/ci.yml)
[![Codecov](https://codecov.io/gh/EvotecIT/CodeGlyphX/branch/master/graph/badge.svg)](https://codecov.io/gh/EvotecIT/CodeGlyphX)
[![License](https://img.shields.io/github/license/EvotecIT/CodeGlyphX.svg)](LICENSE)

## What it covers

- QR and Micro QR encoding, module decoding, and image recognition, including QR ECI, Kanji, FNC1/GS1, and structured append
- Common 1D symbologies including Code 128/GS1-128, Code 39/93/11, EAN/UPC, ITF, Codabar, MSI, Plessey, postal, and GS1 DataBar variants
- Data Matrix ECC 200 and DMRE encoding/decoding, plus PDF417/MicroPDF417 and Aztec
- QR payload builders for Wi-Fi, contacts, calendar events, OTP, payments, social profiles, and app links
- PNG, JPEG, WebP, GIF, TIFF, BMP, Netpbm, TGA, ICO, XBM, XPM, SVG/SVGZ, HTML, PDF, EPS, and ASCII output
- Managed raster decoding with explicit byte, pixel, dimension, animation, cancellation, and recognition-budget controls
- WPF controls and example applications

The exact behavior and known codec limits are documented under [image decoding](https://codeglyphx.com/docs/decoding/) and [output formats](https://codeglyphx.com/docs/renderers/).

## Install

```shell
dotnet add package CodeGlyphX
```

## Primary API

The output format is selected from the file extension:

```csharp
using CodeGlyphX;
using CodeGlyphX.Payloads;

var contact = QrPayload.VCard(
    firstName: "Ava",
    lastName: "Stone",
    phone: "+14155550198",
    email: "ava@example.com",
    organization: "CodeGlyphX");

QR.Save(contact, "contact.png");
Barcode.Save(BarcodeType.EAN, "5901234123457", "product.svg");
DataMatrixCode.Save("LOT-2026-0042", "lot.png");
Pdf417Code.Save("DOCUMENT-2026-0042", "document.pdf");
AztecCode.Save("TICKET-2026-0042", "ticket.svg");
```

These calls are also compiled and executed by the NativeAOT CI smoke test in `CodeGlyphX.Examples/FlagshipApiExample.cs`.

For in-memory output, use a generic renderer and an explicit format:

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

byte[] png = QrCode.Render("Hello", OutputFormat.Png).Data;
string svg = Barcode.Render(BarcodeType.Code128, "PRODUCT-123", OutputFormat.Svg).GetText();

using var stream = File.Create("hello.pdf");
OutputWriter.Write(stream, QrCode.Render("Hello", OutputFormat.Pdf));
```

## QR styling

```csharp
var options = new QrEasyOptions {
    Art = QrArt.Theme(
        QrArtTheme.NeonGlow,
        QrArtVariant.Conservative,
        intensity: 60)
};

QR.Save("https://codeglyphx.com", "styled.png", options);
```

`QrEasy.EvaluateScanHeuristics` reports static contrast, quiet-zone, module-scale, and related concerns before rendering. It does not decode the output or guarantee scanner interoperability; validate final artifacts on the real devices and applications you support.

## Standards-aware QR encoding

`QrCodeEncoder.EncodeText` selects the smallest combination of numeric, alphanumeric, byte, and Kanji segments. UTF-8 ECI is emitted automatically when non-ASCII byte data needs it; `QrEncodingOptions` can force or suppress ECI and segment optimization.

```csharp
QrCode unicode = QrCodeEncoder.EncodeText("Zażółć gęślą jaźń 😀");

QrCode gs1 = QrCodeEncoder.EncodeGs1(
    "010590123412345710ABC123\u001D2112345");

QrCode[] sequence = QrCodeEncoder.EncodeStructuredAppend(new[] {
    "ORDER-2026-",
    "LINE-0001",
    "LOT-ABC123"
});
```

Use `\u001D` between variable-length GS1 element strings. Structured append accepts two through sixteen explicit text or binary parts, computes the shared XOR parity, and exposes one-based `QrStructuredAppend` metadata after decoding. FNC1 second position and its 8-bit application indicator are available through `QrEncodingOptions` for industry-specific applications.

## Standards-aware Data Matrix encoding

The historical square ECC 200 default remains unchanged. `DataMatrixEncodingOptions` can instead select the six original rectangular models, any of the eighteen ISO/IEC 21471 DMRE models, the smallest symbol across all families, or an exact supported size. Automatic encodation plans mixed ASCII, C40, Text, X12, EDIFACT, and Base256 runs rather than forcing the entire payload into one mode.

```csharp
using CodeGlyphX.DataMatrix;

BitMatrix compact = DataMatrixCode.Encode(
    "HELLO-UPPERCASE-lowercase-1234567890",
    new DataMatrixEncodingOptions { Shape = DataMatrixShape.Any });

BitMatrix dmre = DataMatrixCode.Encode(
    "LOT-2026-0042",
    new DataMatrixEncodingOptions { Shape = DataMatrixShape.Dmre });

BitMatrix exact = DataMatrixCode.Encode(
    "A",
    new DataMatrixEncodingOptions { Rows = 12, Columns = 88 });
```

GS1/FNC1, ECI, Macro 05/06, Reader Programming, and Data Matrix structured append are first-class controls. A structured-append file identifier consists of two values in the standard 1..254 range.

```csharp
string elementString = "0109501101020917\u001D10LOT42";
BitMatrix gs1 = DataMatrixCode.EncodeGs1(elementString);

BitMatrix[] sequence = DataMatrixCode.EncodeStructuredAppend(
    new[] { "ORDER-2026", "LINE-0001", "LOT-ABC123" },
    fileId1: 7,
    fileId2: 9);

if (DataMatrixDecoder.TryDecodeDetailed(gs1, out DataMatrixDecoded decoded)) {
    Console.WriteLine($"GS1: {decoded.IsGs1}; model: {decoded.Rows}x{decoded.Columns}");
}
```

`TryDecodeDetailed` is available for module matrices and pixel buffers; `DataMatrixCode.TryDecodePngDetailed` preserves the same control metadata when decoding PNG input. Plain `TryDecode` continues to return only the reconstructed text.

## Official GS1 Application Identifier catalog

`Gs1ApplicationIdentifierCatalog` is generated from the GS1 Barcode Syntax Dictionary release 2026-01-27. It expands all assigned ranges into 541 directly addressable AIs and exposes titles, data components, separator rules, association/exclusion rules, and GS1 Digital Link metadata.

```csharp
using CodeGlyphX;
using CodeGlyphX.Gs1Data;

Gs1ApplicationIdentifier lot =
    Gs1ApplicationIdentifierCatalog.Get("10");

Console.WriteLine($"{lot.Title}: {lot.Format}");
Console.WriteLine($"Dictionary: {Gs1ApplicationIdentifierCatalog.Release}");
```

Use `Gs1.Validate` when conformance matters. It accepts bracketed syntax and raw element strings, returns every parsed element and actionable issue in one pass, and applies all semantic rules referenced by this dictionary release—including check digits, dates and times, code lists, IBAN, AI associations, and coupon formats.

```csharp
const string message =
    "(01)09506000134352(10)ABC123(17)240101";

Gs1ValidationResult validation = Gs1.Validate(message);
if (!validation.IsValid) {
    foreach (Gs1ValidationIssue issue in validation.Issues) {
        Console.WriteLine(issue);
    }
}

string elementString = Gs1Validator.ToElementString(message);
BitMatrix dataMatrix = DataMatrixCode.EncodeGs1(elementString);
```

`Gs1.ElementString` remains the compatibility-oriented separator builder used by existing encoders: it accepts expert-defined and legacy fields. `Gs1.Validate`, `Gs1.TryValidate`, and `Gs1Validator.ToElementString` are the strict entry points for standards-sensitive workflows.

## Decode an image

```csharp
byte[] image = File.ReadAllBytes("qrcode.png");

if (QrImageDecoder.TryDecodeImage(
        image,
        QrPixelDecodeOptions.Screen(budgetMilliseconds: 500, maxDimension: 1600),
        out var result)) {
    Console.WriteLine(result.Text);
}
```

Use `SymbolScanner` when the application needs one coherent result model across QR, Micro QR, barcodes, Data Matrix, PDF417, and Aztec:

```csharp
var scan = SymbolScanner.Scan(image, ScanOptions.Screen(
    timeoutMilliseconds: 500,
    maxDimension: 1600));

foreach (var symbol in scan.Symbols)
{
    Console.WriteLine($"{symbol.Format}: {symbol.Text}");
}
```

`ScanOptions.TimeoutMilliseconds` is a total wall-clock deadline covering compressed-image decoding, pixel conversion, and recognition. Decoder cancellation remains cooperative, so it is not a hard real-time guarantee. Use `ScanOptions.Formats` and `ScanOptions.Region` to avoid work the application does not need.

Micro QR can also be recognized directly from RGBA/BGRA pixels or an encoded image. The detailed overload reports the detected quadrilateral, rotation, polarity, and mirroring state:

```csharp
if (MicroQrDecoder.TryDecodeImage(
        image,
        out MicroQrDecoded micro,
        out MicroQrPixelDecodeInfo recognition)) {
    Console.WriteLine($"{micro.Text} at {recognition.Geometry.Bounds}");
}
```

Raw camera or interop buffers can be passed without an image-codec dependency:

```csharp
var frame = new ImageFrame(
    pixels,
    width,
    height,
    stride,
    PixelFormat.Gray8);

ScanResult scan = SymbolScanner.Scan(frame, new ScanOptions
{
    Formats = new[] { SymbolFormat.QrCode, SymbolFormat.DataMatrix }
});
```

`ImageFrame` accepts RGBA/BGRA, RGB/BGR24, ARGB/ABGR, Gray8/Gray16, and RGB565 buffers, including padded stride and bottom-up row order. The generated [symbol capability table](https://codeglyphx.com/docs/symbol-capabilities/) distinguishes encoding, module decoding, and image recognition for every format.

For untrusted raster inputs, set explicit resource limits:

```csharp
using CodeGlyphX.Rendering;

var limits = ImageDecodeOptions.Strict(
    maxBytes: 8 * 1024 * 1024,
    maxPixels: 8_000_000,
    maxDimension: 1600);

byte[] rgba = ImageReader.DecodeRgba32(image, limits, out int width, out int height);
```

Limit semantics are deliberate:

- `MaxBytes` and `MaxPixels`: `null` uses the corresponding `ImageReader` global; `0` disables that per-call limit.
- `MaxDimension`: codecs validate the original dimensions first, then the single-image RGBA result is resized. It is not a codec-memory limit.
- `RecognitionBudgetMilliseconds`: applies to barcode/matrix recognition after raster decoding. It does not time-box the image codec. Multi-format `CodeGlyph` entry points give each candidate decoder this budget; it is not a wall-clock limit for the complete candidate sequence.
- `ImageReader.LimitViolation`: reports guard failures for telemetry.

See [SECURITY.md](SECURITY.md) for reporting and [FUZZING.md](FUZZING.md) for the bounded decoder harness.

## Targets and dependencies

| Target | Intended use | Package dependencies |
| --- | --- | --- |
| `net8.0` | Current applications, full QR pixel pipeline, trimming/NativeAOT | None |
| `net10.0` | Current applications, full QR pixel pipeline, trimming/NativeAOT | None |
| `netstandard2.0` | Legacy-compatible libraries | `System.Memory` |
| `net472` | .NET Framework applications | `System.Memory` |

The package is pure managed on every target. QR image decoding on `netstandard2.0` and `net472` uses a less capable fallback intended for clean/generated images; use `net8.0` or newer for screenshots, stylized codes, and the full pixel pipeline.

CI builds and tests Windows, Linux, and macOS; builds the complete solution on Windows; packs and inspects the NuGet and symbol packages; and publishes and executes a `net8.0` NativeAOT consumer.

## Codec limits worth knowing

- `ImageReader.DecodeRgba32` returns the first frame for GIF and WebP. Use the animation APIs for multiple frames.
- Managed WebP supports VP8/VP8L still images. Unsupported VP8 animation interframes fail decoding; the library does not fabricate a transparent or repeated frame.
- PDF raster decoding is intentionally limited to supported embedded-image cases; PDF is primarily an output format.
- PSD decoding is limited to flattened 8-bit grayscale/RGB raw or RLE image data.

## Version 2 migration

Version 2 removes the obsolete per-format method explosion and duplicate facades. Use generic `Render(..., OutputFormat)` and extension-based `Save(...)` APIs. It also makes decode-limit inheritance explicit, collapses QR decoding to one cooperative budget, and replaces unprovable “safe” claims with honest heuristic and guardrail terminology.

See the [2.0 migration guide](Website/content/docs/migration-2.md) for mappings and behavioral changes.

## Build and validate

```powershell
dotnet build CodeGlyphX.sln -c Release
dotnet test CodeGlyphX.Tests/CodeGlyphX.Tests.csproj -c Release -f net8.0
dotnet test CodeGlyphX.Tests/CodeGlyphX.Tests.csproj -c Release -f net10.0
dotnet pack CodeGlyphX/CodeGlyphX.csproj -c Release -o artifacts/packages
./Build/Assert-Package.ps1 -PackageDirectory artifacts/packages
```

Run the examples with:

```powershell
dotnet run --project CodeGlyphX.Examples -c Release
```

## License

CodeGlyphX is licensed under the [Apache License 2.0](LICENSE). See [third-party notices](THIRD-PARTY-NOTICES.md) for incorporated standards resources.
