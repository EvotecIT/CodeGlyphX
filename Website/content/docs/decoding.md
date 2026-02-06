---
title: Image Decoding - CodeGlyphX
description: Decode barcodes and QR codes from images.
slug: decoding
collection: docs
layout: docs
---

{{< edit-link >}}

# Image Decoding

CodeGlyphX includes a built-in decoder for reading QR codes and barcodes from images.

## Basic Usage

```csharp
using CodeGlyphX;

// Decode from file
byte[] imageBytes = File.ReadAllBytes("qrcode.png");

if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine($"Decoded: {result.Text}");
    Console.WriteLine($"Format: {result.BarcodeFormat}");
}

// Decode from stream
using var stream = File.OpenRead("barcode.png");
var decodeResult = QrImageDecoder.DecodeImage(stream);
```

## Supported Formats for Decoding

- **Images:** PNG, JPEG, WebP, BMP, GIF, TIFF, PPM/PGM/PBM/PAM, TGA, ICO/CUR, XBM, XPM

- **QR Codes:** Standard QR, Micro QR

- **1D Barcodes:** Code 128, Code 39/93, EAN/UPC, ITF, Codabar, MSI, Telepen, Plessey, more

- **2D Matrix:** Data Matrix, PDF417, Aztec

## Known Gaps (Decoding)

- AVIF, HEIC, JPEG2000

- ImageReader.DecodeRgba32 returns the first GIF/WebP frame; use ImageReader.DecodeGifAnimationFrames / DecodeWebpAnimationFrames (or GifReader/WebpReader) for animation frames

- ImageReader.DecodeRgba32 returns the first TIFF page; use ImageReader.TryDecodeTiffPagesRgba32 (or TiffReader.DecodePagesRgba32) for multi-page

- PDF decode supports image-only PDFs with embedded JPEG/Flate; PS decode still needs rasterization

## Format Corpus (Optional)

We maintain external image samples to validate edge cases (PNG/TIFF suites, packed bit-depths, interlace, etc.). These are not stored in the repo.

```
// Download the image format corpus
pwsh Build/Download-ImageSamples.ps1

// Download external barcode/QR samples
pwsh Build/Download-ExternalSamples.ps1
```

## Handling Multiple Results

```csharp
// Decode all barcodes in an image
var results = QrImageDecoder.DecodeAllImages(imageBytes);

foreach (var barcode in results)
{
    Console.WriteLine($"{barcode.BarcodeFormat}: {barcode.Text}");
}
```

## Diagnostics

```csharp
using CodeGlyphX;

if (!CodeGlyph.TryDecodeImage(imageBytes, out var decoded, out var diagnostics, options: null))
{
    Console.WriteLine(diagnostics.FailureReason);
    Console.WriteLine(diagnostics.Failure);
}
```
