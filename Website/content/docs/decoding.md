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

The default total deadline is 500 ms, whether options are omitted or created with `new ScanOptions()`. Set `TimeoutMilliseconds = 0` explicitly to disable the total deadline. Choose a longer deadline when scanning large images or requesting more recognition attempts.

Null or empty `Formats` uses the catalogue's default image formats, exposed by `SymbolCapability.IsDefaultScanFormat`. Pharmacode and PatchCode are excluded from this automatic selection because their patterns can match unrelated content. Request them explicitly when expected:

```csharp
var pharmacodeScan = SymbolScanner.Scan(image, new ScanOptions {
    Formats = new[] { SymbolFormat.Pharmacode },
    TimeoutMilliseconds = 1000
});
```

Pharmacode Two-Track supports module decoding only; explicitly requesting it from the image scanner reports it in `UnsupportedFormats`.

Raw camera and interop buffers use the same scanner without a compressed-image codec:

```csharp
var frame = new ImageFrame(pixels, width, height, stride, PixelFormat.Gray8);
var scan = SymbolScanner.Scan(frame, new ScanOptions {
    Formats = new[] { SymbolFormat.QrCode, SymbolFormat.DataMatrix },
    Region = new ImageRegion(0, 0, width, height)
});
```

## Streams and async file input

Stream scans consume the remaining input from its current position. Set `Position = 0` before scanning the same stream again.

```csharp
using var stream = File.OpenRead("barcode.png");
var scan = SymbolScanner.Scan(stream, ScanOptions.Balanced(2000));
foreach (var symbol in scan.Symbols) {
    Console.WriteLine(symbol.Text);
}

var fileScan = await SymbolScanner.ScanFileAsync("barcode.png", ScanOptions.Balanced(2000));
```

`Status == Success` means at least one result exists. Inspect `CompletionReason` for `Completed`, `SymbolLimitReached`, `Cancelled` or `DeadlineExceeded`; results remain available when the scan stops early. `Metadata` holds typed family information, while `HasRawBytes` indicates whether the decoder recovered an exact byte payload.

## Multiple results

```csharp
var scan = SymbolScanner.Scan(image, new ScanOptions {
    Formats = new[] { SymbolFormat.QrCode },
    MaxSymbols = 16,
    EnableTileScan = true,
    TileGrid = 3,
    Qr = QrPixelDecodeOptions.Robust().WithTileScan(enabled: true, tileGrid: 3)
});

foreach (var symbol in scan.Symbols) {
    Console.WriteLine($"{symbol.Format}: {symbol.Text}");
}
```

`EnableTileScan` also enables bounded overlapping retries for Data Matrix, PDF417 and Aztec. These retries can find symbols in separate tiles; they do not guarantee exhaustive recognition of arbitrary layouts. `MaxSymbols` and the total deadline apply across all families and tiles.

## Supported raster inputs

PNG, JPEG, WebP, BMP, GIF, TIFF, PPM/PGM/PBM/PAM, TGA, ICO/CUR, XBM, and XPM are handled by the managed image reader. PSD and PDF have narrower documented decode paths.

## Resource-limit semantics

- `MaxBytes` and `MaxPixels`: `null` inherits the corresponding `ImageReader` global; `0` disables that per-call limit.
- `MaxDecodedBytes`: limits individual decoded pixel or raster working buffers and retained animation frame pixels. It excludes encoded input copies and codec metadata, so it is not a cap on total process memory. `null` inherits the `ImageReader` global; `0` disables this limit.
- `MaxDimension`: validates the original image first and then resizes the single-image RGBA output. Recognition uses only the resized pixels and does not retry at the original resolution. It does not reduce codec peak memory.
- `RecognitionBudgetMilliseconds`: cooperatively limits specialist symbol recognition after raster decoding; it does not time the codec. Use `ScanOptions.TimeoutMilliseconds` for one deadline across image decoding and all selected scanner families.
- Animation frame, duration, and per-frame pixel limits follow the same `null`/`0`/positive inheritance model.

Negative limit values throw `ArgumentOutOfRangeException` when assigning properties or calling the `Screen`, `Guarded` and `Strict` factories. Use zero where the documented intent is to disable a limit.

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
var scan = SymbolScanner.Scan(image, ScanOptions.Balanced(2000));
Console.WriteLine(scan.Status);
Console.WriteLine(scan.CompletionReason);
Console.WriteLine(scan.Failure);
```

Use a specialist decoder when its detailed attempt diagnostics are required. A scan's search region records where recognition ran; only formats flagged with `ReportsGeometry` provide measured symbol geometry.
