---
title: Documentation - CodeGlyphX
description: CodeGlyphX documentation - learn how to generate and decode QR codes, barcodes, and 2D matrix codes.
slug: index
collection: docs
layout: docs
---

{{< edit-link >}}

# CodeGlyphX Documentation

CodeGlyphX is a pure-managed .NET library for generating and decoding QR codes, linear barcodes, and 2D matrix codes. It does not require native graphics libraries.

## Key Features

- **No native graphics dependencies** - No System.Drawing, SkiaSharp, or ImageSharp required
- **Encode and decode** - Supported symbologies have explicit APIs and documented limits
- **Multiple output formats** - PNG, SVG, PDF, EPS, HTML, and many more
- **Cross-platform** - Windows, Linux, macOS
- **AOT validated** - CI publishes and executes a `net8.0` NativeAOT consumer

## Supported Symbologies

### 2D Matrix Codes

QR Code, Micro QR, Data Matrix, PDF417 / MicroPDF417, Aztec, GS1 DataBar (Omni/Stacked), and postal 4-state (IMB/RM4SCC/AusPost)

### 1D Linear Barcodes

Code 128, GS1-128, Code 39/93/11, Codabar, MSI, Plessey, Telepen, Pharmacode, Code 32, EAN/UPC, ITF and 2-of-5 variants, GS1 DataBar (Truncated/Expanded)

## Documentation Map

- **QR & Micro QR** - Core encoding, error correction, and QR specifics. [QR docs](/docs/qr/)
- **Styling & presets** - Module shapes, palettes, logos, and the style board gallery. [Styling options](/docs/qr/#styling-options)
- **Payload helpers** - WiFi, vCards, OTP, SEPA, and more. [Payload helpers](/docs/payloads/)
- **Image decoding** - Decode from images and screenshots. [Image decoding](/docs/decoding/)
- **Output formats** - PNG, SVG, PDF, EPS, HTML, and more. [Output formats](/docs/renderers/)
- **API Reference** - Full type and method documentation. [API reference](/api/)
- **FAQ** - Common questions and troubleshooting. [FAQ](/faq/)
- **2.0 migration** - Removed aliases and behavior changes. [Migration guide](/docs/migration-2/)

## Quick Example

```csharp
using CodeGlyphX;

// Generate a QR code
QR.Save("https://evotec.xyz", "website.png");

// Generate a barcode
Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.png");

// Decode an image
if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine(result.Text);
}
```

## Getting Help

If you encounter issues or have questions, please visit the
[GitHub Issues](https://github.com/EvotecIT/CodeGlyphX/issues) page.

For planned work and known gaps, see the
[ROADMAP](https://github.com/EvotecIT/CodeGlyphX/blob/master/ROADMAP.md).
