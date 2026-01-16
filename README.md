# CodeGlyphX - No-deps QR & Barcode Toolkit for .NET

CodeGlyphX is a fast, dependency-free toolkit for QR codes and barcodes, with robust decoding and a minimal API. It targets modern .NET as well as legacy .NET Framework, and includes renderers, payload helpers, and WPF controls.

📦 NuGet Package

[![nuget downloads](https://img.shields.io/nuget/dt/CodeGlyphX?label=nuget%20downloads)](https://www.nuget.org/packages/CodeGlyphX)
[![nuget version](https://img.shields.io/nuget/v/CodeGlyphX)](https://www.nuget.org/packages/CodeGlyphX)

🛠️ Project Information

[![top language](https://img.shields.io/github/languages/top/EvotecIT/CodeGlyphX.svg)](https://github.com/EvotecIT/CodeGlyphX)
[![license](https://img.shields.io/github/license/EvotecIT/CodeGlyphX.svg)](https://github.com/EvotecIT/CodeGlyphX)
[![build](https://github.com/EvotecIT/CodeGlyphX/actions/workflows/ci.yml/badge.svg)](https://github.com/EvotecIT/CodeGlyphX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/EvotecIT/CodeGlyphX/branch/main/graph/badge.svg)](https://codecov.io/gh/EvotecIT/CodeGlyphX)

👨‍💻 Author & Social

[![Twitter follow](https://img.shields.io/twitter/follow/PrzemyslawKlys.svg?label=Twitter%20%40PrzemyslawKlys&style=social)](https://twitter.com/PrzemyslawKlys)
[![Blog](https://img.shields.io/badge/Blog-evotec.xyz-2A6496.svg)](https://evotec.xyz/hub)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-pklys-0077B5.svg?logo=LinkedIn)](https://www.linkedin.com/in/pklys)
[![Threads](https://img.shields.io/badge/Threads-@PrzemyslawKlys-000000.svg?logo=Threads&logoColor=White)](https://www.threads.net/@przemyslaw.klys)
[![Discord](https://img.shields.io/discord/508328927853281280?style=flat-square&label=discord%20chat)](https://evo.yt/discord)

## What it's all about

**CodeGlyphX** is a no-deps QR + barcode toolkit for .NET with:
- Reliable QR decoding (ECI, FNC1/GS1, Kanji, structured append, Micro QR)
- 1D barcode encoding/decoding (Code128/GS1-128, Code39, Code93, EAN/UPC, ITF-14)
- 2D encoding/decoding (Data Matrix, PDF417)
- Renderers (SVG / HTML / PNG / JPEG / BMP / PDF / EPS / ASCII) and image decoding (PNG/JPEG/GIF/BMP/PPM/TGA)
- OTP helpers (otpauth://totp + Base32)
- WPF controls + demo apps

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

## Build Status

**Cross-Platform Testing:** Builds and tests run on Windows, Linux, and macOS. Windows additionally builds WPF and .NET Framework targets.

## Supported Symbologies

| Symbology | Encode | Decode | Notes |
| --- | --- | --- | --- |
| QR | ✅ | ✅ | ECI, FNC1/GS1, Kanji, structured append |
| Micro QR | ✅ | ✅ | Versions M1–M4 |
| Code128 | ✅ | ✅ | Set B/C |
| GS1-128 | ✅ | ✅ | FNC1 + AI helpers |
| Code39 | ✅ | ✅ | Optional checksum |
| Code93 | ✅ | ✅ | Optional checksum |
| EAN-8 / EAN-13 | ✅ | ✅ | Checksum validation |
| UPC-A / UPC-E | ✅ | ✅ | Checksum validation |
| ITF-14 | ✅ | ✅ | Checksum validation |
| Data Matrix | ✅ | ✅ | ASCII/C40/Text/X12/EDIFACT/Base256 |
| PDF417 | ✅ | ✅ | Full encode/decode |

## Features

- [x] QR encode + robust decode
- [x] Micro QR support
- [x] 1D barcode encode + decode
- [x] Data Matrix + PDF417 encode + decode
- [x] SVG / HTML / PNG / JPEG / BMP / PDF / EPS / ASCII renderers
- [x] Image decode: PNG / JPEG / GIF / BMP / PPM / TGA
- [x] Base64 helpers for rendered outputs
- [x] Payload helpers (URL, WiFi, Email, Phone, SMS, Contact, Calendar, OTP, Social)
- [x] WPF controls and demo apps

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

Barcode.Save(BarcodeType.Code128, "CODE128-12345", "code128.png");
```

```csharp
using CodeGlyphX;

DataMatrixCode.Save("DataMatrix-12345", "datamatrix.png");
Pdf417Code.Save("PDF417-12345", "pdf417.png");
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

Commercial support and custom licensing are available. Contact: contact@evotec.pl.
