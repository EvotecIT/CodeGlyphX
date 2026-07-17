---
title: Micro QR - CodeGlyphX
description: Generate, decode, and recognize Micro QR symbols from modules or images.
slug: microqr
collection: docs
layout: docs
---

{{< edit-link >}}

# Micro QR Code

Micro QR is a smaller version of the standard QR code, designed for applications where space is limited.

## Basic Usage

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

// Generate Micro QR
var micro = MicroQrCodeEncoder.EncodeAlphanumeric(
    "ABC123",
    QrErrorCorrectionLevel.L);

OutputWriter.Write(
    "microqr.png",
    MatrixBarcode.Render(micro.Modules, OutputFormat.Png));
```

## Decode modules or images

`MicroQrDecoder.TryDecode(BitMatrix, ...)` decodes an exact 11x11, 13x13, 15x15, or 17x17 module grid. Pixel overloads recognize RGBA32 or BGRA32 frames directly; `TryDecodeImage` first decodes a supported raster image.

```csharp
byte[] image = File.ReadAllBytes("microqr.png");

if (MicroQrDecoder.TryDecodeImage(
        image,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info)) {
    Console.WriteLine(decoded.Text);
    Console.WriteLine($"Rotation: {info.Geometry.RotationDegrees:0.#} degrees");
}
```

The recognizer handles all M1-M4 sizes, quarter turns, arbitrary in-plane rotation, inverted symbols, and mirrored input. It returns the symbol quadrilateral in source-image coordinates. For a unified raw-frame or encoded-image workflow, request `SymbolFormat.MicroQrCode` from `SymbolScanner`; the scanner also converts Gray8/Gray16, RGB/BGR, ARGB/ABGR, and RGB565 frames before recognition.

## Comparison with Standard QR

| Feature | Standard QR | Micro QR |
| --- | --- | --- |
| Finder patterns | 3 corners | 1 corner |
| Max capacity | ~3KB | M4: 35 digits, 21 alphanumeric characters, 15 bytes, or 9 Kanji |
| Best for | General use | Small labels, PCBs |

Micro QR image scanning currently returns one result per frame. The generated [capability table](/docs/symbol-capabilities/) is the source of truth for this boundary.
