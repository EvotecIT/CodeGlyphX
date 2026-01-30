# CodeGlyphX - No-deps QR & Barcode Toolkit for .NET

CodeGlyphX is a fast, dependency-free toolkit for QR codes and barcodes, with robust decoding and a minimal API. It targets modern .NET as well as legacy .NET Framework, and includes renderers, payload helpers, and WPF controls.

Status: Actively developed · Stable core · Expanding format support

📦 NuGet Package

[![nuget downloads](https://img.shields.io/nuget/dt/CodeGlyphX?label=nuget%20downloads)](https://www.nuget.org/packages/CodeGlyphX)
[![nuget version](https://img.shields.io/nuget/v/CodeGlyphX)](https://www.nuget.org/packages/CodeGlyphX)

🛠️ Project Information

[![top language](https://img.shields.io/github/languages/top/EvotecIT/CodeGlyphX.svg)](https://github.com/EvotecIT/CodeGlyphX)
[![license](https://img.shields.io/github/license/EvotecIT/CodeGlyphX.svg)](https://github.com/EvotecIT/CodeGlyphX)
[![build](https://github.com/EvotecIT/CodeGlyphX/actions/workflows/ci.yml/badge.svg)](https://github.com/EvotecIT/CodeGlyphX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/EvotecIT/CodeGlyphX/branch/master/graph/badge.svg)](https://codecov.io/gh/EvotecIT/CodeGlyphX)

👨‍💻 Author & Social

[![Twitter follow](https://img.shields.io/twitter/follow/PrzemyslawKlys.svg?label=Twitter%20%40PrzemyslawKlys&style=social)](https://twitter.com/PrzemyslawKlys)
[![Blog](https://img.shields.io/badge/Blog-evotec.xyz-2A6496.svg)](https://evotec.xyz/hub)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-pklys-0077B5.svg?logo=LinkedIn)](https://www.linkedin.com/in/pklys)
[![Threads](https://img.shields.io/badge/Threads-@PrzemyslawKlys-000000.svg?logo=Threads&logoColor=White)](https://www.threads.net/@przemyslaw.klys)
[![Discord](https://img.shields.io/discord/508328927853281280?style=flat-square&label=discord%20chat)](https://evo.yt/discord)

## What it's all about

**CodeGlyphX** is a no-deps QR + barcode toolkit for .NET with:
- Reliable QR decoding (ECI, FNC1/GS1, Kanji, structured append, Micro QR)
- 1D barcode encoding/decoding (Code128/GS1-128, Code39, Code93, Code11, Codabar, MSI, Plessey, EAN/UPC, ITF-14)
- 2D encoding/decoding (Data Matrix, MicroPDF417, PDF417, Aztec)
- Renderers (SVG / SVGZ / HTML / PNG / JPEG / WebP / GIF / TIFF / BMP / PPM / PBM / PGM / PAM / XBM / XPM / TGA / ICO / PDF / EPS / ASCII) and image decoding (PNG/JPEG/WebP/GIF/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA/ICO/TIFF, plus limited PSD/PDF)
- OTP helpers (otpauth://totp + Base32)
- WPF controls + demo apps

## Highlights

- Zero external dependencies (no System.Drawing, no SkiaSharp, no ImageSharp)
- Encode + decode for QR/Micro QR + common 1D/2D symbologies
- Robust pixel decoder for screenshots, gradients, low-contrast, rotation/mirroring
- Payload helpers for QR (WiFi, email/phone/SMS, contacts, calendar, payments, crypto, social, OTP)
- Friendly APIs: one-liners + options + fluent presets

## Roadmap & Website

- Roadmap: `ROADMAP.md`

## Installation

```powershell
dotnet add package CodeGlyphX
```

## Target Framework Feature Matrix

CodeGlyphX targets `netstandard2.0`, `net472`, `net8.0`, and `net10.0`. Most features are available everywhere, but the full QR pixel pipeline and Span-based APIs are net8+ only.

| Feature | net8.0 / net10.0 | net472 / netstandard2.0 |
| --- | --- | --- |
| Encode (QR/Micro QR + 1D/2D symbologies) | ✅ | ✅ |
| Decode from module grids (BitMatrix) | ✅ | ✅ |
| Renderers + image file codecs (PNG/JPEG/SVG/PDF/etc) | ✅ | ✅ |
| 1D/2D pixel decode (Barcode/DataMatrix/PDF417/Aztec) | ✅ | ✅ |
| QR pixel decode from raw pixels / screenshots | ✅ | ⚠️ Best-effort fallback (clean/generated images) |
| QR pixel debug rendering | ✅ | ✖ |
| Span-based overloads | ✅ | ✖ (byte[] only) |

Notes:
- `netstandard2.0` and `net472` require `System.Memory` 4.5.5 (automatically pulled by NuGet).
- net8+ uses the full QR pixel pipeline; `net472`/`netstandard2.0` use a best-effort fallback for QR image decode via `QrImageDecoder` and byte[] overloads.
- Runtime checks are available via `CodeGlyphXFeatures` (e.g., `SupportsQrPixelDecode`, `SupportsQrPixelDecodeFallback`, `SupportsQrPixelDebug`).

net472 capability notes (QR from images):
- ✅ Clean/generated PNG/JPEG QR renders (including large module sizes)
- ⚠️ Multi-code screenshots, heavy styling/art, blur, warp, and low-contrast scenes are best-effort
- ✅ Recommended: run the quick smoke checklist in `Build/Net472-SmokeTest.md`

Recommended pattern for shared code:

```csharp
if (CodeGlyphXFeatures.SupportsQrPixelDecode &&
    QrImageDecoder.TryDecodeImage(bytes, QrPixelDecodeOptions.Screen(), out var decoded))
{
    Console.WriteLine(decoded.Text);
}
else
{
    // net472 fallback: decode from module grids or run QR pixel decode on a net8+ worker.
}
```

Choosing a target:
- Pick `net8.0`/`net10.0` when you need the most robust QR pixel decode from images/screenshots, pixel debug rendering, Span APIs, or maximum throughput.
- Pick `net472`/`netstandard2.0` for legacy apps; QR image decode is available via a best-effort fallback, but it is less robust on heavily styled/artistic inputs.

## Decode (unified)

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

var options = new CodeGlyphDecodeOptions {
    PreferBarcode = false,
    Qr = new QrPixelDecodeOptions {
        Profile = QrDecodeProfile.Robust,
        MaxMilliseconds = 800
    }
};

if (CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, options)) {
    Console.WriteLine($"{decoded.Kind}: {decoded.Text}");
}
```

Diagnostics:

```csharp
if (!CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, out var diagnostics, options)) {
    Console.WriteLine(diagnostics.FailureReason);
    Console.WriteLine(diagnostics.Failure);
}
```

Presets for easy tuning:

```csharp
var fast = CodeGlyphDecodeOptions.Fast();
var robust = CodeGlyphDecodeOptions.Robust();
var stylized = CodeGlyphDecodeOptions.Stylized();
var screen = CodeGlyphDecodeOptions.Screen(maxMilliseconds: 300, maxDimension: 1200);
```

Barcode checksum policy:

```csharp
var options = new CodeGlyphDecodeOptions {
    ExpectedBarcode = BarcodeType.Code39,
    Code39Checksum = Code39ChecksumPolicy.StripIfValid,
    PreferBarcode = true
};
```

Cancellation and time budget:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

var options = new CodeGlyphDecodeOptions {
    Qr = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust },
    CancellationToken = cts.Token
};

if (CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, options)) {
    Console.WriteLine(decoded.Text);
}
```

Screen-friendly preset:

```csharp
var options = CodeGlyphDecodeOptions.Screen(maxMilliseconds: 300, maxDimension: 1200);
if (CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, options)) {
    Console.WriteLine(decoded.Text);
}
```

## Supported .NET Versions and Dependencies

### Core Library (CodeGlyphX)
- **.NET 10 / .NET 8** (Windows, Linux, macOS)
  - No external dependencies
- **.NET Standard 2.0** (Cross-platform compatibility)
  - System.Memory (4.5.5)
- **.NET Framework 4.7.2** (Windows only)
  - System.Memory (4.5.5)

### Examples Project
- **.NET 8.0** only

### WPF Projects
- **.NET 8.0 (windows)** only

## Platform support (at a glance)

Runs wherever .NET runs (Windows, Linux, macOS). WPF controls are Windows-only.

| Feature | Windows | Linux | macOS |
| --- | --- | --- | --- |
| Core encode/decode (QR/1D/2D) | ✅ | ✅ | ✅ |
| Renderers (PNG/SVG/SVGZ/HTML/JPEG/WebP/GIF/TIFF/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA/ICO/PDF/EPS/ASCII) | ✅ | ✅ | ✅ |
| Image decoding (PNG/JPEG/WebP/GIF/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA/ICO/TIFF/PSD/PDF) | ✅ | ✅ | ✅ |
| WPF controls | ✅ | ❌ | ❌ |

## Build Status

**Cross-Platform Testing:** Builds and tests run on Windows, Linux, and macOS. Windows additionally builds WPF and .NET Framework targets.

## Benchmarks (local run)

Latest benchmark tables are generated into `BENCHMARK.md` (and `Assets/Data/benchmark*.json`).
This README intentionally does not mirror benchmark tables to avoid drift. See `BENCHMARK.md` for the latest Windows/Linux/macOS quick and full runs, including timestamps and hardware details.
Quick runs use fewer iterations but include the same scenario list as full runs (for BenchmarkDotNet tables). The QR pack runner uses a smaller quick pack set and adds art/stylized packs in full mode.

### Performance checklist

Environment
- Close heavy apps (indexers, browsers, video calls).
- Use AC power and High Performance plan; avoid battery saver.
- Reboot if the system has been running for a long time.

Build & preflight
- `dotnet --info` (record SDK + runtime versions).
- `dotnet build CodeGlyphX.sln`
- `dotnet run -c Release --project CodeGlyphX.Benchmarks -- --preflight`

Quick run (sanity)
- `bash Build/run-benchmarks-compare.sh --no-compare --base-filter '*QrDecode*'`
- Generate report:
  - `python3 Build/generate-benchmark-report.py --artifacts-path <artifacts> --framework net8.0 --configuration Release --run-mode quick`

Full run (publishable)
- `bash Build/run-benchmarks-compare.sh`
- Generate report:
  - `python3 Build/generate-benchmark-report.py --artifacts-path <artifacts> --framework net8.0 --configuration Release --run-mode full`

Regression triage
- Re-run the same filter twice to confirm regressions.
- Use QR diagnostics knobs if needed:
  - `CODEGLYPHX_DIAG_QR_MAXMS`, `CODEGLYPHX_DIAG_QR_AGG`, `CODEGLYPHX_DIAG_QR_DISABLE_TRANSFORMS`
- Capture before/after benchmark tables in the PR summary.

Docs/artifacts
- Update `BENCHMARK.md` and `Assets/Data/benchmark*.json`.
- If public API docs changed, regenerate website docs: `pwsh Build/Build-Website.ps1`.

### Run benchmarks

```powershell
dotnet run -c Release --framework net8.0 --project CodeGlyphX.Benchmarks/CodeGlyphX.Benchmarks.csproj -- --filter "*"
```

### Run comparison benchmarks (external libraries)

Comparison benchmarks are opt-in so the default benchmark project stays dependency-free. Enable them with MSBuild properties:

```powershell
# all external comparisons
dotnet run -c Release --framework net8.0 --project CodeGlyphX.Benchmarks/CodeGlyphX.Benchmarks.csproj /p:CompareExternal=true -- --filter "*Compare*"

# per-library toggles
dotnet run -c Release --framework net8.0 --project CodeGlyphX.Benchmarks/CodeGlyphX.Benchmarks.csproj /p:CompareZXing=true -- --filter "*Compare*"
dotnet run -c Release --framework net8.0 --project CodeGlyphX.Benchmarks/CodeGlyphX.Benchmarks.csproj /p:CompareQRCoder=true -- --filter "*Compare*"
dotnet run -c Release --framework net8.0 --project CodeGlyphX.Benchmarks/CodeGlyphX.Benchmarks.csproj /p:CompareBarcoder=true -- --filter "*Compare*"
```

Notes:
- Comparisons target PNG output and use each library’s ImageSharp-based renderer where applicable.
- Run the same command on Windows and Linux to compare OS-level differences.

## Supported Symbologies

All symbologies can be rendered to any output format listed below.
Matrix/stacked/4-state symbols use a `BitMatrix` + the `Matrix*` renderers.

### QR family

| Symbology | Encode | Decode | Notes |
| --- | --- | --- | --- |
| QR | ✅ | ✅ | ECI, FNC1/GS1, Kanji, structured append |
| Micro QR | ✅ | ✅ | Versions M1–M4 |

### 2D matrix / stacked / 4-state (BitMatrix)

| Symbology | Encode | Decode | Notes |
| --- | --- | --- | --- |
| Data Matrix | ✅ | ✅ | ASCII/C40/Text/X12/EDIFACT/Base256 |
| PDF417 | ✅ | ✅ | Macro PDF417 metadata |
| MicroPDF417 | ✅ | ✅ | Module matrix encode/decode |
| Aztec | ✅ | ✅ | Pixel decode via `AztecCode.TryDecodeImage` |
| GS1 DataBar-14 Omni / Stacked | ✅ | ✅ | Matrix renderers |
| GS1 DataBar Expanded Stacked | ✅ | ✅ | Matrix renderers |
| Pharmacode (two-track) | ✅ | ✅ | Numeric 4–64570080 |
| KIX | ✅ | ✅ | Headerless 4-state |
| Royal Mail 4-State (RM4SCC) | ✅ | ✅ | Encodes with headers by default |
| POSTNET / PLANET | ✅ | ✅ | 4-state, checksum |
| Australia Post | ✅ | ✅ | Standard + Customer 2/3; decode may be ambiguous for numeric-only Customer 3 |
| Japan Post | ✅ | ✅ | 67-bar 4-state |
| USPS Intelligent Mail (IMB) | ✅ | ✅ | 65-bar 4-state, tracking + routing (5/9/11) |

### 1D linear

| Symbology | Encode | Decode | Notes |
| --- | --- | --- | --- |
| Code128 | ✅ | ✅ | Set A/B/C |
| GS1-128 | ✅ | ✅ | FNC1 + AI helpers |
| Code39 | ✅ | ✅ | Optional checksum |
| Code93 | ✅ | ✅ | Optional checksum |
| Code11 | ✅ | ✅ | Optional checksum |
| Codabar | ✅ | ✅ | A/B/C/D start/stop |
| MSI | ✅ | ✅ | Mod10 / Mod10Mod10 |
| Plessey | ✅ | ✅ | CRC |
| Telepen | ✅ | ✅ | ASCII 0–127 |
| Pharmacode (one-track) | ✅ | ✅ | Numeric 3–131070 |
| Code 32 (Italian Pharmacode) | ✅ | ✅ | 8 digits + checksum |
| EAN-8 / EAN-13 | ✅ | ✅ | +2/+5 add-ons |
| UPC-A / UPC-E | ✅ | ✅ | +2/+5 add-ons |
| ITF-14 | ✅ | ✅ | Checksum validation |
| ITF (Interleaved 2 of 5) | ✅ | ✅ | Even-length digits, optional checksum |
| Industrial 2 of 5 | ✅ | ✅ | Optional checksum |
| Matrix 2 of 5 | ✅ | ✅ | Optional checksum |
| IATA 2 of 5 | ✅ | ✅ | Optional checksum |
| Patch Code | ✅ | ✅ | Single symbol (1,2,3,4,6,T) |
| GS1 DataBar-14 Truncated | ✅ | ✅ | GTIN-13 input (check digit computed) |
| GS1 DataBar Expanded | ✅ | ✅ | GS1 AI strings (linear) |

## Features

- [x] QR encode + robust decode
- [x] Micro QR support
- [x] 1D barcode encode + decode
- [x] Data Matrix + MicroPDF417 + PDF417 encode + decode
- [x] Matrix/stacked/4-state encoding (Data Matrix / PDF417 / MicroPDF417 / Aztec / GS1 DataBar / postal + pharmacode) with dedicated matrix renderers
- [x] SVG / SVGZ / HTML / PNG / JPEG / WebP / GIF / TIFF / BMP / PPM / PBM / PGM / PAM / XBM / XPM / TGA / ICO / PDF / EPS / ASCII renderers
- [x] Image decode: PNG / JPEG / WebP / GIF / BMP / PPM / PBM / PGM / PAM / XBM / XPM / TGA / ICO / TIFF
- [x] Base64 + data URI helpers for rendered outputs
- [x] Payload helpers (URL, WiFi, Email, Phone/SMS/MMS, Contact, Calendar, OTP, payments, crypto, social)
- [x] WPF controls and demo apps
- [x] Aztec encode + decode (module matrix + pixel)
- [x] Matrix render helpers for Aztec/DataMatrix/PDF417 (PNG/SVG/SVGZ/HTML/JPEG/WebP/GIF/TIFF/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA/ICO/PDF/EPS/ASCII)

## AOT & trimming

CodeGlyphX is AOT-friendly (no reflection, no runtime codegen) and ships with trimming/AOT analyzers enabled for .NET 8+ targets.
Recommended publish flags: `PublishAot=true` (native), or `PublishTrimmed=true` (size) for app projects.

## Output formats (Save by extension)

Save(...) chooses the output based on file extension for QR/Barcode/DataMatrix/PDF417/Aztec.
For other matrix barcodes (e.g., KIX/Royal Mail 4‑State), use the `Matrix*` renderers with a `BitMatrix` from `MatrixBarcodeEncoder`.
Use `.txt` for ASCII output and `.ps` as an EPS alias.

| Format | Extensions | Notes |
| --- | --- | --- |
| PNG | `.png` | Raster |
| JPEG | `.jpg`, `.jpeg` | Raster, quality via options |
| WebP | `.webp` | Raster, quality via options (lossless at quality 100) |
| GIF | `.gif` | Raster (indexed, 256 colors max) |
| TIFF | `.tif`, `.tiff` | Raster (baseline RGBA, compression via options: none/PackBits/LZW/Deflate) |
| BMP | `.bmp` | Raster |
| PPM | `.ppm` | Raster (portable pixmap) |
| PBM | `.pbm` | Raster (portable bitmap) |
| PGM | `.pgm` | Raster (portable graymap) |
| PAM | `.pam` | Raster (portable anymap, RGBA) |
| XBM | `.xbm` | Text (1-bit) |
| XPM | `.xpm` | Text (2-color) |
| TGA | `.tga` | Raster |
| ICO | `.ico` | Raster (PNG inside, multi-size by default) |
| SVG | `.svg` | Vector |
| SVGZ | `.svgz`, `.svg.gz` | Vector (gzip-compressed SVG) |
| HTML | `.html`, `.htm` | Table-based output |
| PDF | `.pdf` | Vector by default, raster via RenderMode |
| EPS | `.eps`, `.ps` | Vector by default, raster via RenderMode |
| ASCII | `.txt` | Text output (also via `Render(..., OutputFormat.Ascii)`) |
| Raw RGBA | API only | Use `RenderPixels` methods |

### Render to bytes or text

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

var svg = Barcode.Render(BarcodeType.Code128, "CODE128-12345", OutputFormat.Svg).GetText();
var png = QrCode.Render("https://example.com", OutputFormat.Png).Data;

// HTML title + PDF/EPS vector/raster output
var extras = new RenderExtras { HtmlTitle = "My Code", VectorMode = RenderMode.Raster };
Barcode.Save(BarcodeType.Code128, "CODE128-12345", "barcode.html", extras: extras);
```

Per-format helpers (e.g., `Png`, `Svg`, `SavePng`) remain for convenience, but new code should prefer `Render(..., OutputFormat)` to keep output handling consistent.

### ICO multi-size

```csharp
using CodeGlyphX;

var opts = new QrEasyOptions {
    IcoSizes = new[] { 32, 64, 128 },
    IcoPreserveAspectRatio = true
};
QR.Save("https://example.com", "qr.ico", opts);
```

## Payload helpers

QR payload helpers generate well-known structured strings so scanners can trigger the right action.

| Category | Payloads |
| --- | --- |
| Core | Text, URL, Bookmark, WiFi |
| Communication | Email (Mailto/MATMSG/SMTP), Phone, SMS, MMS, Skype, WhatsApp |
| Location & Calendar | Geo, Calendar (iCal/vEvent) |
| Contacts | vCard / MeCard |
| OTP | TOTP / HOTP (otpauth://) |
| Social & Stores | App Store (Apple/Google), Facebook, X/Twitter, TikTok, LinkedIn |
| Payments | PayPal.Me, UPI, SEPA Girocode (EPC), BezahlCode (contact/payment/debit/periodic), Swiss QR Bill, Slovenian UPN, Russia Payment Order (ST00012) |
| Crypto & Network | Bitcoin / Bitcoin Cash / Litecoin, Monero, ShadowSocks |

Auto-detect helper: `QrPayloads.Detect("...")` builds the best-known payload for mixed inputs.

## Image format support

### Raster formats (encode + decode)

| Format | Encode | Decode | Notes |
| --- | --- | --- | --- |
| PNG | ✅ | ✅ |  |
| JPEG | ✅ | ✅ |  |
| WebP | ✅ | ✅ | Managed VP8/VP8L; optional ICCP/EXIF/XMP on encode; ImageReader returns first frame (use DecodeAnimationFrames/DecodeAnimationCanvasFrames for animations) |
| BMP | ✅ | ✅ |  |
| GIF | ✅ | ✅ | ImageReader returns first frame (use DecodeAnimationFrames/DecodeAnimationCanvasFrames for animations) |
| TIFF | ✅ | ✅ | Baseline strips/tiles, 8/16-bit; compression: none/PackBits/LZW/Deflate (multipage via pageIndex) |
| PSD | ❌ | ✅ | Flattened 8/16-bit grayscale/RGB/CMYK (raw/RLE) |
| PPM/PGM/PAM/PBM | ✅ | ✅ |  |
| TGA | ✅ | ✅ |  |
| ICO | ✅ | ✅ | PNG/BMP payloads (CUR decode supported) |
| XBM/XPM | ✅ | ✅ |  |

### Vector / text outputs (encode only)

| Format | Encode | Notes |
| --- | --- | --- |
| SVG / SVGZ | ✅ | Vector output |
| PDF / EPS | ✅ | Vector by default, raster via RenderMode (PDF decode: image-only JPEG/Flate) |
| HTML | ✅ | Table-based output |
| ASCII | ✅ | `.txt` output or `Render(..., OutputFormat.Ascii)` |
| Raw RGBA | ✅ | Use `RenderPixels` APIs |

### Known gaps / not supported (decode)

- ImageReader.DecodeRgba32 returns the first animation frame only (GIF/WebP); use ImageReader.DecodeAnimationFrames/DecodeAnimationCanvasFrames or GifReader/WebpReader for full animations. Use ImageReader.TryReadAnimationInfo for lightweight frame/loop metadata.
- Managed WebP decode supports VP8/VP8L stills; default size limit is 256 MB (configurable via `WebpReader.MaxWebpBytes`)
- Managed WebP encode is VP8 (lossy intra-only) and VP8L (lossless)
- WebP VP8 interframes in animations are currently treated as repeats of the previous frame (best-effort)
- AVIF, HEIC, JPEG2000 are not supported (format detection only)
- Multi-page / tiled TIFF: use `ImageReader.DecodeRgba32(data, pageIndex, ...)`, `ImageReader.TryReadInfo(..., pageIndex, ...)`, or `TiffReader.DecodeRgba32(data, pageIndex, ...)`
- PSD decode is limited to flattened 8/16-bit grayscale/RGB/CMYK (raw/RLE); no layers/CMYK ICC profiles/other color modes
- PDF decode is limited to embedded image-only JPEG/Flate (with ASCII85/RunLength wrappers), including inline images and Indexed color spaces. PS decode is not supported (rasterize first)

### Format corpus (optional)

- Image format corpus: `CodeGlyphX.Tests/Fixtures/ImageSamples/manifest.json`  
  Download with `pwsh Build/Download-ImageSamples.ps1` (or set `CODEGLYPHX_IMAGE_SAMPLES`).
- External barcode/QR samples: `CodeGlyphX.Tests/Fixtures/ExternalSamples/manifest.json`  
  Download with `pwsh Build/Download-ExternalSamples.ps1` (or set `CODEGLYPHX_EXTERNAL_SAMPLES`).

## Quick usage

```csharp
using CodeGlyphX;

QR.Save("https://example.com", "qr.png");
QR.Save("https://example.com", "qr.svg");
QR.Save("https://example.com", "qr.jpg");
QR.Save("https://example.com", "qr.pdf");
```

```csharp
using CodeGlyphX;

// Simple styling (colored eyes + rounded modules)
var opts = new QrEasyOptions {
    ModuleShape = QrPngModuleShape.Rounded,
    ModuleCornerRadiusPx = 3,
    Eyes = new QrPngEyeOptions {
        UseFrame = true,
        OuterShape = QrPngModuleShape.Circle,
        InnerShape = QrPngModuleShape.Circle,
        OuterColor = new Rgba32(220, 20, 60),
        InnerColor = new Rgba32(220, 20, 60),
    }
};
QR.Save("https://example.com", "qr-styled.png", opts);
```

### High-resolution output (print / large displays)

For large displays or print, render at a higher pixel size using `TargetSizePx` or a larger `ModuleSize`.
Vector output (SVG/PDF) is ideal when your style stays vector-friendly (no gradients/palettes/logos).

```csharp
using CodeGlyphX;

var opts = new QrEasyOptions {
    TargetSizePx = 1200,
    TargetSizeIncludesQuietZone = true,
    BackgroundSupersample = 2 // smoother gradients/patterns
};
QR.Save("https://example.com", "qr-1200.png", opts);
```

Examples: see `CodeGlyphX.Examples` output files (`qr-print-4k.png`, `qr-print-8k.png`, `qr-print-8k.pdf`) for print-ready presets.

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

// PDF/EPS are vector by default. Use Raster when you need pixels.
QR.SavePdf("https://example.com", "qr-raster.pdf", mode: RenderMode.Raster);
```

### Rendering pipeline notes (PNG)

- Layout: compute quiet-zone, module grid, and output pixel size.
- Background: solid/gradient/pattern fill (optional supersample for smoother gradients/patterns).
- Modules: shape + scale map + palette/gradient, then eyes, then logo overlay.
- Canvas/debug: optional sticker canvas + debug overlays as final passes.

Notes:
- Vector PDF/EPS support square/rounded/circle modules and eye shapes.
- Gradients and logos automatically fall back to raster to preserve appearance.
- PDF/EPS are output-only. For decoding, rasterize to PNG/BMP/PPM/PBM/PGM/PAM/TGA and use the image decoders.
- Logo background plates auto-bump the minimum QR version to 8 by default for scan safety (disable with `AutoBumpVersionForLogoBackground` or set `LogoBackgroundMinVersion = 0`).

```csharp
using CodeGlyphX;

Barcode.Save(BarcodeType.Code128, "CODE128-12345", "code128.png");
Barcode.Save(BarcodeType.Code128, "CODE128-12345", "code128.pdf");
Barcode.Save(BarcodeType.Code128, "CODE128-12345", "code128.eps");
```

```csharp
using CodeGlyphX;

// One-liners with defaults
var png = BarcodeEasy.RenderPng(BarcodeType.Code128, "CODE128-12345");
```

```csharp
using CodeGlyphX;

DataMatrixCode.Save("DataMatrix-12345", "datamatrix.png");
DataMatrixCode.Save("DataMatrix-12345", "datamatrix.pdf");
DataMatrixCode.Save("DataMatrix-12345", "datamatrix.eps");
Pdf417Code.Save("PDF417-12345", "pdf417.png");
Pdf417Code.Save("PDF417-12345", "pdf417.pdf");
Pdf417Code.Save("PDF417-12345", "pdf417.eps");
```

## Payload helpers (3 lines each)

```csharp
using CodeGlyphX;
using CodeGlyphX.Payloads;

QR.Save(QrPayloads.Url("https://example.com"), "url.png");
QR.Save(QrPayloads.Wifi("MyWiFi", "p@ssw0rd"), "wifi.png");
QR.Save(QrPayloads.OneTimePassword(OtpAuthType.Totp, "JBSWY3DPEHPK3PXP", label: "user@example.com", issuer: "AuthIMO"), "otp.png");
```

## Decode (pixels or images)

```csharp
using CodeGlyphX;

if (QrImageDecoder.TryDecodeImage(File.ReadAllBytes("code.bmp"), out var decoded)) {
    Console.WriteLine(decoded.Text);
}
```

```csharp
using CodeGlyphX;

var options = QrPixelDecodeOptions.Fast();
if (QrImageDecoder.TryDecodeImage(File.ReadAllBytes("screen.png"), options, out var decoded)) {
    Console.WriteLine(decoded.Text);
}
```

```csharp
using CodeGlyphX;

var options = QrPixelDecodeOptions.Screen(maxMilliseconds: 300, maxDimension: 1200);
if (QrImageDecoder.TryDecodeImage(File.ReadAllBytes("screen.png"), options, out var decoded)) {
    Console.WriteLine(decoded.Text);
}
```

```csharp
using CodeGlyphX;

var bytes = File.ReadAllBytes("screen.png");
if (QR.TryDecodeImage(bytes, QrPixelDecodeOptions.Screen(), out var decoded)) {
    Console.WriteLine(decoded.Text);
}
```

```csharp
using CodeGlyphX;

if (!QrImageDecoder.TryDecodeImage(File.ReadAllBytes("screen.png"), out var decoded, out var info)) {
    QrDiagnosticsDump.WriteText("decode-diagnostics.txt", info, source: "screen.png");
}
```

```csharp
using CodeGlyphX;

if (CodeGlyph.TryDecodeAllPng(File.ReadAllBytes("unknown.png"), out var results)) {
    foreach (var item in results) Console.WriteLine($"{item.Kind}: {item.Text}");
}
```

### Decode (3 lines each)

```csharp
using CodeGlyphX;

if (Barcode.TryDecodeImage(File.ReadAllBytes("code.png"), BarcodeType.Code128, out var barcode))
    Console.WriteLine(barcode.Text);
```

```csharp
using CodeGlyphX;

if (DataMatrixCode.TryDecodeImage(File.ReadAllBytes("dm.png"), out var text))
    Console.WriteLine(text);
```

```csharp
using CodeGlyphX;

if (Pdf417Code.TryDecodeImage(File.ReadAllBytes("pdf417.png"), out var text))
    Console.WriteLine(text);
```

```csharp
using CodeGlyphX;

if (AztecCode.TryDecodeImage(File.ReadAllBytes("aztec.png"), out var text))
    Console.WriteLine(text);
```

```csharp
using CodeGlyphX;

var opts = ImageDecodeOptions.Screen(maxMilliseconds: 300, maxDimension: 1200);
Barcode.TryDecodePng(File.ReadAllBytes("barcode.png"), BarcodeType.Code128, opts, out var barcode);
```

## WPF controls

```xml
xmlns:wpf="clr-namespace:CodeGlyphX.Wpf;assembly=CodeGlyphX.Wpf"
```

```xml
<wpf:QrCodeControl Text="{Binding QrText}" Ecc="M" ModuleSize="6" QuietZone="4" />
<wpf:Barcode128Control Value="{Binding BarcodeValue}" ModuleSize="2" QuietZone="10" />
```

## License

Apache-2.0.
