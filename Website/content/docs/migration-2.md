---
title: Migrating to CodeGlyphX 2.0
description: Breaking API and behavior changes in CodeGlyphX 2.0.
slug: migration-2
collection: docs
layout: docs
---

{{< edit-link >}}

# Migrating to CodeGlyphX 2.0

Version 2 deliberately removes obsolete aliases and behavior that reported success without producing a valid result. The smaller API has one rendering path per capability and explicit decode-limit semantics.

## Rendering and saving

Per-format methods such as `RenderPng`, `RenderSvg`, `SavePng`, stream variants, and matching file variants were removed from the QR, barcode, Data Matrix, PDF417, and Aztec facades.

The same cleanup applies to fluent builders. Replace terminal calls such as `.Png()` or `.SaveSvg(path)` with `.Render(OutputFormat.Png).Data` or `.Save(path)`. Builders also provide `Save(stream, format)` for explicit stream output.

Builder types are top-level in 2.0 instead of being nested under their static facades. Code that uses `var` is unchanged; explicit declarations should use `QrBuilder`, `BarcodeBuilder`, `DataMatrixBuilder`, or `Pdf417Builder` instead of `QR.QrBuilder`, `Barcode.BarcodeBuilder`, `DataMatrixCode.DataMatrixBuilder`, or `Pdf417Code.Pdf417Builder`.

Use an explicit format for in-memory rendering:

```csharp
using CodeGlyphX.Rendering;

byte[] png = QrCode.Render("Hello", OutputFormat.Png).Data;
string svg = Barcode.Render(BarcodeType.Code128, "PRODUCT-123", OutputFormat.Svg).GetText();

using var stream = File.Create("hello.pdf");
OutputWriter.Write(stream, QrCode.Render("Hello", OutputFormat.Pdf));
```

Use `Save` when the file extension should select the format:

```csharp
QR.Save("Hello", "hello.png");
Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.svg");
DataMatrixCode.Save("LOT-42", "lot.pdf");
Pdf417Code.Save("DOCUMENT-42", "document.png");
AztecCode.Save("TICKET-42", "ticket.svg");
```

`BarcodeEasy` was removed. `Barcode` is the single linear-barcode facade.

OTP output follows the same generic builder contract. The `TotpPng`, `HotpSvg`, `SaveTotpPng`, `SaveHotpWebp`, and other format-specific combinations were removed. URI-only helpers remain available as `TotpUri(...)` and `HotpUri(...)`.

```csharp
var totp = Otp.Totp("ACME", "alice@example.com", "MZXW6")
    .WithParameters(OtpAlgorithm.Sha256, digits: 8, period: 60);

byte[] png = totp.Render(OutputFormat.Png).Data;
totp.Save("acme-alice.svg");

using var output = File.Create("acme-alice.pdf");
totp.Save(output, OutputFormat.Pdf);
```

## QR art

The implementation-oriented `QrArtPresets` surface was removed. Configure the public art model instead:

```csharp
var options = new QrEasyOptions {
    Art = QrArt.Theme(QrArtTheme.NeonGlow, QrArtVariant.Conservative, intensity: 60)
};
```

Version 2 removes names that implied a static score could prove scanner compatibility. The checks are still useful guardrails, but they do not render and decode the final artifact.

| Before 2.0 | CodeGlyphX 2.0 |
| --- | --- |
| `QrArtVariant.Safe` | `QrArtVariant.Conservative` |
| `QrArtSafetyMode` / `SafetyMode` | `QrArtGuardrailMode` / `GuardrailMode` |
| `QrEasy.EvaluateSafety(...)` | `EvaluateScanHeuristics(...)` |
| `QrArtSafetyReport.IsSafe` | `QrArtHeuristicReport.PassesHeuristics` |
| `ArtAutoTune` / `ArtAutoTuneMinScore` | `ArtGuardrailsEnabled` / `ArtGuardrailMinimumScore` |
| `OtpQrSafety` / `IsOtpSafe` | `OtpQrHeuristics` / `PassesHeuristics` |
| `AsciiConsolePresets.ScanSafe()` | `ConservativeQr()` |
| `AsciiConsoleOptions.PreferScanReliability` | `UseConservativeQrLayout` |

Always validate the final image or terminal output with the devices and applications your product supports.

## Decode option names

The former image `MaxMilliseconds` setting did not time the raster codec. It applied only to symbol recognition after raster decoding, so the API now says that directly.

| Before 2.0 | CodeGlyphX 2.0 |
| --- | --- |
| `ImageDecodeOptions.MaxMilliseconds` | `RecognitionBudgetMilliseconds` |
| `ImageDecodeOptions.WithMaxMilliseconds(...)` | `WithRecognitionBudget(...)` |
| `ImageDecodeOptions.WithBudget(...)` | `WithRecognitionBudget(...)` |
| `CodeGlyphDecodeOptions.MaxImageMilliseconds` | `ImageRecognitionBudgetMilliseconds` |
| `CodeGlyphDecodeOptions.WithImageBudget(...)` | `WithImageRecognitionBudget(...)` |
| `ImageDecodeOptions.Safe(...)` | `ImageDecodeOptions.Guarded(...)` |
| `ImageDecodeOptions.UltraSafe(...)` | `ImageDecodeOptions.Strict(...)` |

The `ImageReader.DecodeRgba32Safe(...)` and `TryDecodeRgba32Safe(...)` convenience overloads were removed. Pass `ImageDecodeOptions.Guarded()` or `Strict()` to the regular decode overloads. These presets apply resource guardrails; they do not make arbitrary input intrinsically safe.

QR decoding also has one time control in 2.0. `QrPixelDecodeOptions.MaxMilliseconds` and its silent profile downgrades were removed. Set `BudgetMilliseconds`, use `WithBudgetMilliseconds(...)`, or set `CodeGlyphDecodeOptions.QrBudgetMilliseconds`. The budget applies to one public decode call, is checked cooperatively between passes, and is not a hard real-time deadline.

The `QrDecoder.TryDecodeAll(..., out QrPixelDecodeInfo, ...)` and `CodeGlyph.TryDecodeAll(..., out CodeGlyphDecodeDiagnostics, ...)` overloads were removed because the multi-code paths did not collect representative QR finder diagnostics and could report success with missing or synthetic details. Use the remaining `TryDecodeAll` overloads for multi-code results. Use single-code `TryDecode(..., out QrPixelDecodeInfo, ...)` or `CodeGlyph.TryDecode(..., out CodeGlyphDecodeDiagnostics, ...)` when detailed attempt diagnostics are required.

`ImageDecodeOptions.MaxBytes`, `MaxPixels`, and animation limits are nullable in 2.0:

- `null` inherits the matching `ImageReader` global setting.
- `0` disables that per-call limit.
- A positive value is an explicit per-call limit.

`MaxDimension` now resizes the decoded single-image RGBA output after the codec validates the original image. Recognition uses that bounded output only and does not silently retry the original resolution. It is not a promise to reduce the codec's peak memory use.

## WebP animation failures

Unsupported VP8 animation interframes now return failure. Earlier releases could report success with fabricated transparent frame data. Callers should handle a `false` result and use a decoder that supports that WebP feature when it is required.

## Package and NativeAOT contract

The 2.0 package contains `netstandard2.0`, `net472`, `net8.0`, and `net10.0` assemblies, XML documentation, and a matching symbol package. `net8.0` and `net10.0` are trim/AOT-analyzed; CI publishes and executes a `net8.0` NativeAOT consumer.
