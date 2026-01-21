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
- Renderers (SVG / SVGZ / HTML / PNG / JPEG / BMP / PPM / PBM / PGM / PAM / XBM / XPM / TGA / ICO / PDF / EPS / ASCII) and image decoding (PNG/JPEG/GIF/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA)
- OTP helpers (otpauth://totp + Base32)
- WPF controls + demo apps

## Highlights

- Zero external dependencies (no System.Drawing, no SkiaSharp, no ImageSharp)
- Encode + decode for QR/Micro QR + common 1D/2D symbologies
- Robust pixel decoder for screenshots, gradients, low-contrast, rotation/mirroring
- Payload helpers for QR (WiFi, payments, contacts, OTP, social, etc.)
- Friendly APIs: one-liners + options + fluent presets

## Roadmap & Website

- Roadmap: `ROADMAP.md`

## Installation

```powershell
dotnet add package CodeGlyphX
```

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
| Renderers (PNG/SVG/SVGZ/HTML/JPEG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA/ICO/PDF/EPS/ASCII) | ✅ | ✅ | ✅ |
| Image decoding (PNG/JPEG/GIF/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) | ✅ | ✅ | ✅ |
| WPF controls | ✅ | ❌ | ❌ |

## Build Status

**Cross-Platform Testing:** Builds and tests run on Windows, Linux, and macOS. Windows additionally builds WPF and .NET Framework targets.

## Benchmarks (local run)

Benchmarks were run on 2026-01-19 (Linux Ubuntu 24.04, Ryzen 9 9950X, .NET 8.0.22). Your results will vary.
Benchmarks run on identical hardware with default settings.

### QR (Encode)

| Scenario | Mean (us) | Allocated |
| --- | --- | --- |
| QR PNG (short text) | 331.33 | 431.94 KB |
| QR PNG (medium text) | 713.68 | 837.75 KB |
| QR PNG (long text) | 2197.99 | 3041.06 KB |
| QR SVG (medium text) | 99.17 | 20.03 KB |
| QR PNG (High EC) | 1094.94 | 1535.88 KB |
| QR HTML (medium text) | 115.46 | 137.43 KB |

### QR (Decode)

| Scenario | Mean (ms) | Allocated | Notes |
| --- | --- | --- | --- |
| QR decode (clean, fast) | 2.148 | 103.9 KB | qr-clean-small.png |
| QR decode (clean, balanced) | 2.124 | 103.9 KB | qr-clean-small.png |
| QR decode (clean, robust) | 2.193 | 103.9 KB | qr-clean-small.png |
| QR decode (noisy, robust) | 170.949 | 8507.41 KB | qr-noisy-ui.png (Robust, MaxMilliseconds=800) |

### 1D Barcodes (Encode)

| Scenario | Mean (us) | Allocated |
| --- | --- | --- |
| Code 128 PNG | 442.41 | 756.24 KB |
| Code 128 SVG | 2.52 | 17.61 KB |
| EAN PNG | 191.33 | 338.54 KB |
| Code 39 PNG | 311.37 | 414.49 KB |
| Code 93 PNG | 222.69 | 367.76 KB |
| UPC-A PNG | 175.83 | 338.85 KB |

### 2D Matrix Codes (Encode)

| Scenario | Mean (us) | Allocated |
| --- | --- | --- |
| Data Matrix PNG (medium) | 303.48 | 447.73 KB |
| Data Matrix PNG (long) | 711.93 | 1509.06 KB |
| Data Matrix SVG | 5.64 | 12.29 KB |
| PDF417 PNG | 1730.92 | 3154.87 KB |
| PDF417 SVG | 28.79 | 64.53 KB |
| Aztec PNG | 260.70 | 452.30 KB |
| Aztec SVG | 12.76 | 59.74 KB |

### Run benchmarks

```powershell
dotnet run -c Release --framework net8.0 --project CodeGlyphX.Benchmarks/CodeGlyphX.Benchmarks.csproj -- --filter "*"
```

## Comparison (selected libraries)

Based on public docs as of 2026-01-18. Capabilities depend on optional renderer packages.

| Library | Encode | Decode | 2D Codes | 1D Codes | Image Dependencies |
| --- | --- | --- | --- | --- | --- |
| CodeGlyphX | ✅ | ✅ | QR, Micro QR, Data Matrix, MicroPDF417, PDF417, Aztec | ✅ | None (built-in PNG/JPEG/GIF/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA/ICO/TIFF decode) |
| ZXing.Net | ✅ | ✅ | QR, Data Matrix, PDF417, Aztec, more | ✅ | Image I/O via bindings on .NET Standard/5+; System.Drawing on full .NET Framework |
| QRCoder | ✅ | ❌ | QR only | ❌ | System.Drawing renderer (Windows) or alt renderers |
| Barcoder | ✅ | ❌ | QR, Data Matrix, PDF417, Aztec | ✅ | ImageSharp.Drawing for image renderer |

## Supported Symbologies

| Symbology | Encode | Decode | Outputs | Notes |
| --- | --- | --- | --- | --- |
| QR | ✅ | ✅ | All (see Output formats) | ECI, FNC1/GS1, Kanji, structured append |
| Micro QR | ✅ | ✅ | All (see Output formats) | Versions M1–M4 |
| Code128 | ✅ | ✅ | All (see Output formats) | Set A/B/C |
| GS1-128 | ✅ | ✅ | All (see Output formats) | FNC1 + AI helpers |
| Code39 | ✅ | ✅ | All (see Output formats) | Optional checksum |
| Code93 | ✅ | ✅ | All (see Output formats) | Optional checksum |
| Code11 | ✅ | ✅ | All (see Output formats) | Optional checksum |
| Codabar | ✅ | ✅ | All (see Output formats) | A/B/C/D start/stop |
| MSI | ✅ | ✅ | All (see Output formats) | Mod10 / Mod10Mod10 |
| Plessey | ✅ | ✅ | All (see Output formats) | CRC |
| Telepen | ✅ | ✅ | All (see Output formats) | ASCII 0-127, checksum |
| Pharmacode (one-track) | ✅ | ✅ | All (see Output formats) | Numeric 3–131070 |
| Pharmacode (two-track) | ✅ | ✅ | All (see Output formats) | Matrix renderers (top/bottom/full bars), numeric 4–64570080 |
| Code 32 (Italian Pharmacode) | ✅ | ✅ | All (see Output formats) | 8 digits + checksum |
| POSTNET / PLANET | ✅ | ✅ | All (see Output formats) | Matrix renderers (tall/short bars), checksum |
| KIX / Royal Mail 4-State | ✅ | ✅ | All (see Output formats) | KIX (headerless) + RM4SCC (headers + checksum), matrix renderers |
| Australia Post (Customer) | ✅ | ✅ | All (see Output formats) | Standard + Customer 2/3, RS parity (ambiguous N/C decode for numeric-only Customer 3) |
| Japan Post | ✅ | ✅ | All (see Output formats) | 67-bar 4-state, modulo-19 check |
| USPS Intelligent Mail (IMB) | ✅ | ✅ | All (see Output formats) | 65-bar 4-state, tracking + routing (5/9/11) |
| GS1 DataBar-14 Truncated | ✅ | ✅ | All (see Output formats) | GTIN-13 input (check digit computed) |
| GS1 DataBar-14 Omnidirectional | ✅ | ✅ | All (see Output formats) | Matrix renderers |
| GS1 DataBar-14 Stacked | ✅ | ✅ | All (see Output formats) | Matrix renderers |
| GS1 DataBar Expanded | ✅ | ✅ | All (see Output formats) | GS1 AI strings (linear) |
| GS1 DataBar Expanded Stacked | ✅ | ✅ | All (see Output formats) | Matrix renderers |
| EAN-8 / EAN-13 | ✅ | ✅ | All (see Output formats) | Checksum validation, +2/+5 add-ons |
| UPC-A / UPC-E | ✅ | ✅ | All (see Output formats) | Checksum validation, +2/+5 add-ons |
| ITF-14 | ✅ | ✅ | All (see Output formats) | Checksum validation |
| ITF (Interleaved 2 of 5) | ✅ | ✅ | All (see Output formats) | Even-length digits, optional checksum |
| Industrial 2 of 5 | ✅ | ✅ | All (see Output formats) | Optional checksum |
| Matrix 2 of 5 | ✅ | ✅ | All (see Output formats) | Optional checksum |
| IATA 2 of 5 | ✅ | ✅ | All (see Output formats) | Optional checksum |
| Patch Code | ✅ | ✅ | All (see Output formats) | Single symbol (1,2,3,4,6,T) |
| Data Matrix | ✅ | ✅ | All (see Output formats) | ASCII/C40/Text/X12/EDIFACT/Base256 |
| MicroPDF417 | ✅ | ✅ | All (see Output formats) | Module matrix encode/decode |
| PDF417 | ✅ | ✅ | All (see Output formats) | Full encode/decode, Macro PDF417 metadata |
| Aztec | ✅ | ✅ | All (see Output formats) | Module matrix + basic pixel decode |

## Features

- [x] QR encode + robust decode
- [x] Micro QR support
- [x] 1D barcode encode + decode
- [x] Data Matrix + MicroPDF417 + PDF417 encode + decode
- [x] Matrix barcode encoding (Data Matrix / MicroPDF417 / PDF417 / KIX / GS1 DataBar) with dedicated matrix renderers
- [x] SVG / SVGZ / HTML / PNG / JPEG / BMP / PPM / PBM / PGM / PAM / XBM / XPM / TGA / ICO / PDF / EPS / ASCII renderers
- [x] Image decode: PNG / JPEG / GIF / BMP / PPM / PBM / PGM / PAM / XBM / XPM / TGA / ICO / TIFF
- [x] Base64 + data URI helpers for rendered outputs
- [x] Payload helpers (URL, WiFi, Email, Phone, SMS, Contact, Calendar, OTP, Social)
- [x] WPF controls and demo apps
- [x] Aztec encode + decode (module matrix + pixel)
- [x] Aztec render helpers (PNG/SVG/SVGZ/HTML/JPEG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA/ICO/PDF/EPS/ASCII + Save by extension)

## AOT & trimming

CodeGlyphX is AOT-friendly (no reflection, no runtime codegen) and ships with trimming/AOT analyzers enabled for .NET 8+ targets.
Recommended publish flags: `PublishAot=true` (native), or `PublishTrimmed=true` (size) for app projects.

## Output formats (Save by extension)

Save(...) chooses the output based on file extension for QR/Barcode/DataMatrix/PDF417/Aztec.
For other matrix barcodes (e.g., KIX/Royal Mail 4‑State), use the `Matrix*` renderers with a `BitMatrix` from `MatrixBarcodeEncoder`.

| Format | Extensions | Notes |
| --- | --- | --- |
| PNG | `.png` | Raster |
| JPEG | `.jpg`, `.jpeg` | Raster, quality via options |
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
| ASCII | API only | Use `RenderAscii` methods |
| Raw RGBA | API only | Use `RenderPixels` methods |

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
| Communication | Email (Mailto/MATMSG/SMTP), Phone, SMS, MMS, Skype |
| Location & Calendar | Geo, Calendar (iCal/vEvent) |
| Contacts | vCard / MeCard |
| OTP | TOTP / HOTP (otpauth://) |
| Social & Stores | App Store (Apple/Google), Facebook, X/Twitter, TikTok, LinkedIn, WhatsApp |
| Payments | UPI, SEPA Girocode (EPC), BezahlCode (contact/payment/debit/periodic), Swiss QR Bill, Slovenian UPN, Russia Payment Order (ST00012) |
| Crypto & Network | Bitcoin / Bitcoin Cash / Litecoin, Monero, ShadowSocks |

## Image decoding (for readers)

- PNG, JPEG (baseline + progressive, EXIF orientation), GIF, BMP, PPM, PBM, PGM, PAM, XBM, XPM, TGA, ICO/CUR
- Pure C# decoders (no native image libraries)

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

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

// PDF/EPS are vector by default. Use Raster when you need pixels.
QR.SavePdf("https://example.com", "qr-raster.pdf", mode: RenderMode.Raster);
```

Notes:
- Vector PDF/EPS support square/rounded/circle modules and eye shapes.
- Gradients and logos automatically fall back to raster to preserve appearance.
- PDF/EPS are output-only. For decoding, rasterize to PNG/BMP/PPM/PBM/PGM/PAM/TGA and use the image decoders.

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
