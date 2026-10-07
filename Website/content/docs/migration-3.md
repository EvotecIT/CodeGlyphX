---
title: Migrating to 3.0 - CodeGlyphX
description: Update QR options, scanning, symbol results, and output ownership for CodeGlyphX 3.0.
slug: migration-3
collection: docs
layout: docs
---

{{< edit-link >}}

# Migrating to 3.0

CodeGlyphX 3.0 separates encoding from appearance, uses one general scanner, and gives completed results stable ownership. These changes require recompiling consumers. Start with the public entry points below; the expert format encoders and decoders remain available for specialized workflows.

| 2.x API | 3.0 API |
| --- | --- |
| `QrCode.Encode`, `QrEasy.Encode` | `QR.Encode` |
| Static `QrCode.Render`, `QrEasy` rendering helpers | `QR.Render` or an encoded `QrCode.Render` |
| `QrEasyOptions` | `QrEncodingOptions` and `QrRenderOptions` |
| `RenderExtras` | `OutputOptions` |
| Shared `QrPng*` appearance models in `CodeGlyphX.Rendering.Png` | Format-neutral `Qr*` appearance models in `CodeGlyphX.Rendering` |
| `CodeGlyphX.Rendering.Png.Rgba32` | `CodeGlyphX.Rendering.Rgba32` |
| `QrEasy.EvaluateScanHeuristics` | `QR.EvaluateScanHeuristics` or `qr.EvaluateScanHeuristics` |
| `CodeGlyph.TryDecodeImage`, `TryDecodeAll` | `SymbolScanner.Scan`, `ScanFile` and stream/file async variants |
| `CodeGlyph.TryDecode(BitMatrix, ...)` | `SymbolDecoder.TryDecode` |
| `CodeGlyphDecoded.Kind` and nullable family properties | `DetectedSymbol.Format`, common payload properties and typed `Metadata` |
| Public `Barcode` and `MatrixBarcode` calls with `BarcodeType` | The same facade calls with `SymbolFormat` |
| Mutable `RenderedOutput.Data` array | Read-only `Data`; call `ToArray()` for an owned copy |
| `qr.Save(format, stream, ...)` | `qr.Save(stream, format, ...)` |

## QR encoding and appearance

Simple saves stay simple:

```csharp
QR.Save("Hello", "hello.png");
```

Move error correction, version bounds, forced mask, text encoding, ECI and segmentation settings into `QrEncodingOptions`. Use `EciMode` instead of the old `IncludeEci` boolean. Keep module sizes, colors, finder shapes, logos and artwork in `QrRenderOptions`.

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

var encoding = new QrEncodingOptions {
    ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
    EciMode = QrEciMode.Auto
};
var appearance = new QrRenderOptions { ModuleSize = 6, QuietZone = 4 };
var qr = QR.Encode("Zażółć 😀", encoding);
var png = qr.Render(OutputFormat.Png, appearance);
qr.Save("hello.png", appearance);

using var stream = File.Create("hello.svg");
qr.Save(stream, OutputFormat.Svg, appearance);
```

Rendering an encoded symbol cannot change its encoded payload, error correction, version or mask. String encoding defaults to error correction `M`, UTF-8 and automatic ECI. A `QrPayloadData` uses its recommended encoding only when `encodingOptions` is omitted; supplying an encoding object selects every encoding setting from that object, including its defaults.

Logo and artwork choices do not silently change the encoding. Select sufficient error correction and version bounds explicitly, then validate the final artifact on target readers. The static scan heuristic report remains configuration feedback, not an interoperability certificate.

Shared appearance models use format-neutral names in `CodeGlyphX.Rendering`, such as `QrModuleShape` and `QrGradientOptions`. Format-specific renderers and their low-level options, such as `QrPngRenderer` and `QrPngRenderOptions`, retain their format names.

Codec and container settings belong to `OutputOptions`. This includes PNG compression, JPEG quality/options, WebP quality, ICO sizes/aspect policy, HTML title/email table mode and PDF/EPS raster mode. Use named arguments when skipping the encoding options:

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

var jpeg = QR.Render("Hello", OutputFormat.Jpeg,
    new QrRenderOptions { ModuleSize = 8 },
    extras: new OutputOptions { JpegQuality = 90 });
```

The builder exposes separate `Encoding` and `Rendering` state. Replace `WithOptions` with `WithEncoding` or `WithRendering`. The builder takes snapshots of supplied options, so subsequent edits to the caller's option objects do not change that builder.

```csharp
QR.Create("Hello")
    .WithEncoding(new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H })
    .WithRendering(new QrRenderOptions { ModuleSize = 8 })
    .Save("hello.png");
```

SVG, SVGZ and HTML support a defined subset of QR appearance. An explicit raster effect that cannot be represented throws `NotSupportedException` instead of silently disappearing. Use PNG, another raster format, or raster-mode PDF/EPS for those effects.

## General image scanning

Use `SymbolScanner` for bytes, streams, files or an `ImageFrame`. Select formats with `SymbolFormat` and inspect structured completion separately from the decoded symbols.

```csharp
var options = new ScanOptions {
    Formats = new[] { SymbolFormat.QrCode, SymbolFormat.DataMatrix, SymbolFormat.Code128 },
    TimeoutMilliseconds = 2000,
    MaxSymbols = 16,
    EnableTileScan = true,
    Image = ImageDecodeOptions.Strict(maxBytes: 8 * 1024 * 1024, maxPixels: 8_000_000)
};
var scan = SymbolScanner.ScanFile("label.png", options);
foreach (var symbol in scan.Symbols) {
    Console.WriteLine($"{symbol.Format}: {symbol.Text}");
    if (symbol.Metadata is QrSymbolMetadata qrMetadata) {
        Console.WriteLine(qrMetadata.Version);
    }
}
Console.WriteLine(scan.CompletionReason);
```

`Status == Success` means at least one symbol was decoded. `CompletionReason` records whether the requested scan completed, reached `MaxSymbols`, was cancelled or exceeded its deadline. Partial results stay available after cancellation or a deadline. `IsPartial` also reports when a symbol limit stopped the scan. `MaxSymbols = 1` requests the first match; it does not establish that the image contains only one symbol.

`RawBytes` are the bytes recovered by a decoder that exposes them. `HasRawBytes` distinguishes unavailable bytes from a decoded empty payload. Do not reconstruct original encoded bytes by applying UTF-8 to `Text`. Family-specific information belongs in typed metadata rather than a nullable union of unrelated decoder result types. Unpopulated `FrameIndex` and `PageIndex` properties are removed; a scan processes the image selected by the image reader, not every frame or page.

Data Matrix, PDF417 and Aztec can return multiple results through bounded tile retries when `EnableTileScan` is enabled. This is not exhaustive recognition of arbitrary layouts.

The capability catalogue distinguishes image recognition from module decoding. Unsupported requested image formats appear in `UnsupportedFormats`. For a symbol matrix already available in memory, use the separate module decoder:

```csharp
var encoded = QR.Encode("Module decode");
if (SymbolDecoder.TryDecode(encoded.Modules, out var decoded, SymbolFormat.QrCode)) {
    Console.WriteLine(decoded.Text);
}
```

## Format identity and encoded results

Facade calls use `SymbolFormat` consistently:

```csharp
Barcode.Save(SymbolFormat.Ean, "5901234123457", "product.svg");
var matrix = MatrixBarcode.Encode(SymbolFormat.DataMatrix, "LOT-2026-0042");
Console.WriteLine(matrix.Format);
```

Specialist matrix encoders return symbols that retain their format metadata. `DataMatrixCode.Encode` (including GS1, Macro and structured-append helpers), `Pdf417Code.Encode`, `AztecCode.Encode`, `DotCodeCode.Encode` and `HanXinCode.Encode` return typed symbol objects; pass `symbol.Modules` to expert matrix decoders or generic square-module renderers. MaxiCode uses its dedicated renderer because its hexagonal geometry cannot be represented faithfully by square modules. Low-level barcode encoders and decoders still use `BarcodeType` where their format-specific contract requires it.

## Ownership, streams and cancellation

`RenderedOutput` owns its bytes and exposes read-only data. `GetText()` and stream/file writing refer to the same output. Use `ToArray()` when an API needs a mutable `byte[]`; modifying that copy does not change the output. Encoded symbols and linear barcode segments similarly own their construction data. `ImageFrame` is deliberately a non-owning view: keep its backing pixel memory alive and unchanged while scanning.

Stream readers consume from the current position through the end, including `MemoryStream` fast paths. Size limits apply to the remaining input. Set `Position = 0` explicitly before reading a stream again.

Async stream decoding follows task cancellation: a cancelled token produces `OperationCanceledException` rather than `null` or `FormatException`. The unified scanner returns structured cancellation in `ScanResult`, retaining any decoded symbols. Cancellation and recognition deadlines are cooperative.

A default `DecodeResult<T>` is not a successful result. Construct success and failure results explicitly, and never use `DecodeFailureReason.None` to construct a failure. Invalid option values fail validation instead of silently disabling resource guards.
