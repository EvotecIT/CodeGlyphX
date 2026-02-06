---
title: Other 1D Symbologies - CodeGlyphX
description: Additional 1D and postal barcode formats.
slug: other-1d
collection: docs
layout: docs
---

{{< edit-link >}}

# Other 1D &amp; Postal Barcodes

CodeGlyphX supports a wide range of additional 1D symbologies beyond the most common retail formats.

**Note:** Some postal and stacked formats use the matrix pipeline and must be rendered with `MatrixBarcodeEncoder` + matrix renderers.

## Supported Types

| Category | Symbologies | Notes |
| --- | --- | --- |
| Interleaved / 2-of-5 | ITF, ITF-14, Industrial 2-of-5, Matrix 2-of-5, IATA 2-of-5 | Logistics, cartons, older systems |
| Specialty / Legacy | Codabar, MSI, Code 11, Plessey, Telepen, Code 32 | Warehousing, libraries, healthcare |
| Pharma | Pharmacode (one-track), Pharmacode (two-track) | Packaging and pharmaceuticals |
| Postal | POSTNET, PLANET, Royal Mail 4-State, Australia Post, Japan Post, USPS IMB | Matrix encoder required |
| GS1 DataBar | DataBar Truncated, Omni, Stacked, Expanded, Expanded Stacked | Omni/Stacked/Expanded Stacked use matrix encoder |

## Examples

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;

Barcode.Save(BarcodeType.ITF14, "12345678901231", "itf14.png");
Barcode.Save(BarcodeType.Codabar, "A40156B", "codabar.png");
Barcode.Save(BarcodeType.Telepen, "TELEPEN-123", "telepen.png");
Barcode.Save(BarcodeType.GS1DataBarExpanded, "01095011015300031725010110ABC123", "gs1-expanded.png");

var imb = MatrixBarcodeEncoder.EncodeUspsImb("0123456709498765432101234567891");
MatrixPngRenderer.RenderToFile(
    imb,
    new MatrixPngRenderOptions { ModuleSize = 2, QuietZone = 2 },
    "usps-imb.png");
```
