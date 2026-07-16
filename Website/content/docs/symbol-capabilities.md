---
title: Symbol capabilities
description: Generated CodeGlyphX encoding, module-decoding, and image-scanning support matrix.
---

# Symbol capabilities

CodeGlyphX currently describes 46 physical symbol formats through `SymbolCapabilities`. This table is generated from that runtime registry, so the website and the public API report the same support boundary.

“Module decode” means CodeGlyphX can decode an already sampled `BitMatrix` or module sequence. “Image scan” means `SymbolScanner` can recognize the format from pixels. A module-only format is not presented as camera/image recognition support.

| Format | Family | Encode | Module decode | Image scan | Multiple | GS1 | ECI | Structured append | Geometry |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- | --- | ---: |
| QR Code | Matrix | Yes | Yes | Yes | Yes | Encode + decode | Encode + decode | Encode + decode | No |
| Micro QR Code | Matrix | Yes | Yes | Yes | No | No | No | No | Yes |
| Rectangular Micro QR Code | Matrix | Yes | Yes | No | No | Encode + decode | Encode + decode | No | No |
| Aztec Code | Matrix | Yes | Yes | Yes | No | No | No | No | No |
| Code 128 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| GS1-128 | Linear | Yes | Yes | Yes | Yes | Encode + decode | No | No | No |
| Code 39 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Code 93 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| EAN | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| UPC-A | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| UPC-E | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| ITF-14 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Interleaved 2 of 5 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Industrial 2 of 5 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Matrix 2 of 5 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| IATA 2 of 5 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Patch Code | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Codabar | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| MSI | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Code 11 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Plessey | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Telepen | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Pharmacode | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| Two-track Pharmacode | Stacked | Yes | Yes | No | No | No | No | No | No |
| Code 32 | Linear | Yes | Yes | Yes | Yes | No | No | No | No |
| POSTNET | Postal | Yes | Yes | No | No | No | No | No | No |
| PLANET | Postal | Yes | Yes | No | No | No | No | No | No |
| Royal Mail 4-State | Postal | Yes | Yes | No | No | No | No | No | No |
| Australia Post | Postal | Yes | Yes | No | No | No | No | No | No |
| Japan Post | Postal | Yes | Yes | No | No | No | No | No | No |
| GS1 DataBar Truncated | Linear | Yes | Yes | Yes | Yes | Encode + decode | No | No | No |
| GS1 DataBar Omnidirectional | Linear | Yes | Yes | No | No | Encode + decode | No | No | No |
| GS1 DataBar Stacked | Stacked | Yes | Yes | No | No | Encode + decode | No | No | No |
| GS1 DataBar Expanded | Linear | Yes | Yes | Yes | Yes | Encode + decode | No | No | No |
| GS1 DataBar Expanded Stacked | Stacked | Yes | Yes | No | No | Encode + decode | No | No | No |
| GS1 DataBar Limited | Linear | Yes | Yes | No | No | Encode + decode | No | No | No |
| GS1 DataBar Stacked Omnidirectional | Stacked | Yes | Yes | No | No | Encode + decode | No | No | No |
| MaxiCode | Matrix | Yes | Yes | No | No | No | Encode + decode | Encode + decode | No |
| DotCode | Matrix | Yes | Yes | No | No | Encode + decode | Encode + decode | Encode + decode | No |
| Han Xin Code | Matrix | Yes | Yes | No | No | No | Encode + decode | No | No |
| GS1 Composite | Stacked | Yes | Yes | No | No | Encode + decode | No | No | No |
| USPS Intelligent Mail | Postal | Yes | Yes | No | No | No | No | No | No |
| KIX Code | Postal | Yes | Yes | No | No | No | No | No | No |
| Data Matrix | Matrix | Yes | Yes | Yes | No | Encode + decode | Encode + decode | Encode + decode | No |
| PDF417 | Stacked | Yes | Yes | Yes | No | No | No | No | No |
| MicroPDF417 | Stacked | Yes | Yes | No | No | No | No | No | No |

## Check a capability at runtime

```csharp
var capability = SymbolCapabilities.Get(SymbolFormat.DataMatrix);

if (capability.CanScanImages)
{
    var result = SymbolScanner.Scan(imageBytes, new ScanOptions
    {
        Formats = new[] { SymbolFormat.DataMatrix }
    });
}
```

The directional columns distinguish encoding from decoding, so callers can check the exact operation they need instead of treating a format name as a blanket support claim.
