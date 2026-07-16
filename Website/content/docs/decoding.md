---
title: Image Decoding - CodeGlyphX
description: Decode QR codes, barcodes, and matrix codes from supported image formats.
slug: decoding
collection: docs
layout: docs
---

{{< edit-link >}}

# Image Decoding

Use `QrImageDecoder` when only QR is expected, or `MicroQrDecoder` for direct Micro QR recognition. Use `SymbolScanner` for a unified result model across QR, Micro QR, linear barcodes, Data Matrix, PDF417, and Aztec. The [capability matrix](/docs/symbol-capabilities/) distinguishes pixel recognition from module-only decoding for every format.

## QR-only decoding

```csharp
using CodeGlyphX;

byte[] image = File.ReadAllBytes("qrcode.png");
var imageOptions = ImageDecodeOptions.Strict(
    maxBytes: 8 * 1024 * 1024,
    maxPixels: 8_000_000,
    maxDimension: 1600);
var qrOptions = QrPixelDecodeOptions.Screen(
    budgetMilliseconds: 500,
    maxDimension: 1600);

if (QrImageDecoder.TryDecodeImage(image, imageOptions, qrOptions, out var decoded)) {
    Console.WriteLine(decoded.Text);
}
```

## Unified decoding

```csharp
var options = new ScanOptions {
    Formats = new[] { SymbolFormat.QrCode, SymbolFormat.MicroQrCode, SymbolFormat.DataMatrix, SymbolFormat.Code128 },
    TimeoutMilliseconds = 750,
    Qr = QrPixelDecodeOptions.Screen(budgetMilliseconds: 500, maxDimension: 1600),
    Image = ImageDecodeOptions.Strict(
        maxBytes: 8 * 1024 * 1024,
        maxPixels: 8_000_000,
        maxDimension: 1600)
};

var scan = SymbolScanner.Scan(image, options);
foreach (var symbol in scan.Symbols) {
    Console.WriteLine($"{symbol.Format}: {symbol.Text}");
}
```

`TimeoutMilliseconds` covers compressed-image decoding, pixel conversion, and the complete recognition sequence. Cancellation and decoder budgets remain cooperative rather than hard real-time limits. Requested module-only formats are returned in `ScanResult.UnsupportedFormats` instead of being advertised as image-scannable.

Raw camera and interop buffers use the same scanner without a compressed-image codec:

```csharp
var frame = new ImageFrame(pixels, width, height, stride, PixelFormat.Gray8);
var scan = SymbolScanner.Scan(frame, new ScanOptions {
    Formats = new[] { SymbolFormat.QrCode, SymbolFormat.DataMatrix },
    Region = new ImageRegion(0, 0, width, height)
});
```

## Legacy facade and stream input

`CodeGlyph` remains available for compatibility and for stream-based input:

```csharp
using var stream = File.OpenRead("barcode.png");
if (CodeGlyph.TryDecodeImage(stream, out var decoded)) {
    Console.WriteLine(decoded.Text);
}
```

## Multiple results

```csharp
var scan = SymbolScanner.Scan(image, new ScanOptions {
    Formats = new[] { SymbolFormat.QrCode },
    MaxSymbols = 16,
    Qr = QrPixelDecodeOptions.Robust().WithTileScan(enabled: true, tileGrid: 3)
});

foreach (var symbol in scan.Symbols) {
    Console.WriteLine($"{symbol.Format}: {symbol.Text}");
}
```

## Supported raster inputs

PNG, JPEG, WebP, BMP, GIF, TIFF, PPM/PGM/PBM/PAM, TGA, ICO/CUR, XBM, and XPM are handled by the managed image reader. PSD and PDF have narrower documented decode paths.

## Resource-limit semantics

- `MaxBytes` and `MaxPixels`: `null` inherits the corresponding `ImageReader` global; `0` disables that per-call limit.
- `MaxDimension`: validates the original image first and then resizes the single-image RGBA output. Recognition uses only the resized pixels and does not retry at the original resolution. It does not reduce codec peak memory.
- `RecognitionBudgetMilliseconds`: cooperatively limits symbol recognition after raster decoding; it does not time the codec. Multi-format `CodeGlyph` entry points apply it independently to each candidate decoder, so it is not a wall-clock limit for the complete candidate sequence.
- Animation frame, duration, and per-frame pixel limits follow the same `null`/`0`/positive inheritance model.

## Known limits

- AVIF, HEIC, and JPEG 2000 are not supported.
- `ImageReader.DecodeRgba32` returns the first GIF/WebP frame. Use the animation APIs for multiple frames.
- Unsupported VP8 WebP animation interframes fail; CodeGlyphX does not substitute transparent or repeated pixels.
- `ImageReader.DecodeRgba32` returns the first TIFF page. Use the TIFF page APIs for multi-page input.
- PDF decode is limited to supported embedded JPEG/Flate image cases; PostScript requires external rasterization.
- The `netstandard2.0` and `net472` QR pixel fallback is intended for clean/generated images. Use `net8.0` or newer for the full screenshot and stylized-code pipeline.

## Optional external corpora

The repository keeps downloaded image and barcode corpora out of Git. Fetch them before the extended sample tests:

```powershell
pwsh Build/Download-ImageSamples.ps1
pwsh Build/Download-ExternalSamples.ps1
```

## Diagnostics

```csharp
if (!CodeGlyph.TryDecodeImage(image, out var decoded, out var diagnostics, options: null)) {
    Console.WriteLine(diagnostics.FailureReason);
    Console.WriteLine(diagnostics.Failure);
}
```
