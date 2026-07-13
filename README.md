# CodeGlyphX

CodeGlyphX is a pure-managed .NET toolkit for QR codes, linear barcodes, Data Matrix, PDF417, Aztec, structured QR payloads, and image rendering/decoding. It does not require native graphics libraries such as System.Drawing, SkiaSharp, or ImageSharp.

[![NuGet](https://img.shields.io/nuget/v/CodeGlyphX)](https://www.nuget.org/packages/CodeGlyphX)
[![CI](https://github.com/EvotecIT/CodeGlyphX/actions/workflows/ci.yml/badge.svg)](https://github.com/EvotecIT/CodeGlyphX/actions/workflows/ci.yml)
[![Codecov](https://codecov.io/gh/EvotecIT/CodeGlyphX/branch/master/graph/badge.svg)](https://codecov.io/gh/EvotecIT/CodeGlyphX)
[![License](https://img.shields.io/github/license/EvotecIT/CodeGlyphX.svg)](LICENSE)

## What it covers

- QR and Micro QR encoding/decoding, including QR ECI, Kanji, FNC1/GS1, and structured append
- Common 1D symbologies including Code 128/GS1-128, Code 39/93/11, EAN/UPC, ITF, Codabar, MSI, Plessey, postal, and GS1 DataBar variants
- Data Matrix, PDF417/MicroPDF417, and Aztec encoding/decoding
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

CodeGlyphX is licensed under the [Apache License 2.0](LICENSE).
