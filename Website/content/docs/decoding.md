---
title: Image Decoding - CodeGlyphX
description: Decode QR codes, barcodes, and matrix codes from supported image formats.
slug: decoding
collection: docs
layout: docs
---

{{< edit-link >}}

# Image Decoding

Use `QrImageDecoder` when only QR is expected. Use `CodeGlyph` for unified QR, linear-barcode, Data Matrix, PDF417, and Aztec recognition.

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

Stream input uses the same contract:

```csharp
using var stream = File.OpenRead("barcode.png");
if (CodeGlyph.TryDecodeImage(stream, out var decoded, options)) {
    Console.WriteLine(decoded.Text);
}
```

## Multiple results

```csharp
if (CodeGlyph.TryDecodeAllImage(image, out var results, options)) {
    foreach (var result in results) {
        Console.WriteLine($"{result.Kind}: {result.Text}");
    }
}
```

## Supported raster inputs

PNG, JPEG, WebP, BMP, GIF, TIFF, PPM/PGM/PBM/PAM, TGA, ICO/CUR, XBM, and XPM are handled by the managed image reader. PSD and PDF have narrower documented decode paths.

## Resource-limit semantics

- `MaxBytes` and `MaxPixels`: `null` inherits the corresponding `ImageReader` global; `0` disables that per-call limit.
- `MaxDimension`: validates the original image first and then resizes the single-image RGBA output. It does not reduce codec peak memory.
- `RecognitionBudgetMilliseconds`: cooperatively limits symbol recognition after raster decoding; it does not time the codec.
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
if (!CodeGlyph.TryDecodeImage(image, out var decoded, out var diagnostics, options)) {
    Console.WriteLine(diagnostics.FailureReason);
    Console.WriteLine(diagnostics.Failure);
}
```
