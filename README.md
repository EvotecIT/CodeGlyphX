# CodeGlyphX - No-deps QR & Barcode Toolkit for .NET

CodeGlyphX is a fast, dependency-free toolkit for QR codes and barcodes, with robust decoding and a minimal API. It targets modern .NET as well as legacy .NET Framework, and includes renderers, payload helpers, and WPF controls.

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
- 2D encoding/decoding (Data Matrix, PDF417, Aztec)
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
- Website plan: `WEBSITE.md`

## Installation

```powershell
dotnet add package CodeGlyphX
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

| Feature | Windows | Linux | macOS |
| --- | --- | --- | --- |
| Core encode/decode (QR/1D/2D) | ✅ | ✅ | ✅ |
| Renderers (PNG/SVG/SVGZ/HTML/JPEG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA/ICO/PDF/EPS/ASCII) | ✅ | ✅ | ✅ |
| Image decoding (PNG/JPEG/GIF/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) | ✅ | ✅ | ✅ |
| WPF controls | ✅ | ❌ | ❌ |

## Build Status

**Cross-Platform Testing:** Builds and tests run on Windows, Linux, and macOS. Windows additionally builds WPF and .NET Framework targets.

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
| EAN-8 / EAN-13 | ✅ | ✅ | All (see Output formats) | Checksum validation |
| UPC-A / UPC-E | ✅ | ✅ | All (see Output formats) | Checksum validation |
| ITF-14 | ✅ | ✅ | All (see Output formats) | Checksum validation |
| Data Matrix | ✅ | ✅ | All (see Output formats) | ASCII/C40/Text/X12/EDIFACT/Base256 |
| PDF417 | ✅ | ✅ | All (see Output formats) | Full encode/decode |
| Aztec | ✅ | ✅ | All (see Output formats) | Module matrix + basic pixel decode |

## Features

- [x] QR encode + robust decode
- [x] Micro QR support
- [x] 1D barcode encode + decode
- [x] Data Matrix + PDF417 encode + decode
- [x] SVG / SVGZ / HTML / PNG / JPEG / BMP / PPM / PBM / PGM / PAM / XBM / XPM / TGA / ICO / PDF / EPS / ASCII renderers
- [x] Image decode: PNG / JPEG / GIF / BMP / PPM / PBM / PGM / PAM / XBM / XPM / TGA
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

- PNG, JPEG (baseline + progressive, EXIF orientation), GIF, BMP, PPM, PBM, PGM, PAM, XBM, XPM, TGA
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

var options = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Fast };
if (QrImageDecoder.TryDecodeImage(File.ReadAllBytes("screen.png"), options, out var decoded)) {
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
