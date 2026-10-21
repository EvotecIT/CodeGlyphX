---
title: Symbol capabilities
description: Generated CodeGlyphX encoding, module-decoding, and image-scanning support matrix.
---

# Symbol capabilities

CodeGlyphX currently describes 65 physical symbol formats through `SymbolCapabilities`. This table is generated from that runtime registry, so the website and the public API report the same support boundary.

“Module decode” means CodeGlyphX can decode an already sampled `BitMatrix` or module sequence. “Image scan” means `SymbolScanner` can recognize the format from pixels. All listed formats have an image path; raw marks can require the known grid and orientation described below.

“Colour-cell decode” uses `UltracodeGrid` and preserves information carried by the palette. Ultracode is decoded through `SymbolDecoder.Decode(cells)` or `UltracodeDecoder`; a `BitMatrix` cannot represent its payload. Its scanner reads RGB pixels before grayscale processing. A colour image is required.

“Default scan” identifies formats selected when `ScanOptions.Formats` is null or empty. Pharmacode, Two-track Pharmacode, PatchCode, FIM, DAFT, Flattermarken and KIX require explicit selection because their raw or unchecked patterns can match unrelated content. Telepen Numeric requires an explicit hint because the same bars also carry a valid Telepen ASCII interpretation. Check `IsDefaultScanFormat` when building a format picker that follows the scanner defaults.

| Format | Family | Encode | Module decode | Colour-cell decode | Image scan | Default scan | Multiple | GS1 | ECI | Structured append | Geometry |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | ---: |
| QR Code | Matrix | Yes | Yes | No | Yes | Yes | Yes | Encode + decode | Encode + decode | Encode + decode | No |
| D3 AQR (provisional) | Matrix | Yes | Yes | No | Yes | Yes | No | Encode + decode | Encode + decode | No | Yes |
| Micro QR Code | Matrix | Yes | Yes | No | Yes | Yes | Yes | No | No | No | Yes |
| Rectangular Micro QR Code | Matrix | Yes | Yes | No | Yes | Yes | No | Encode + decode | Encode + decode | No | Yes |
| Aztec Code | Matrix | Yes | Yes | No | Yes | Yes | Yes | No | Encode + decode | No | No |
| Aztec Rune | Matrix | Yes | Yes | No | Yes | Yes | No | No | No | No | Yes |
| Code One | Matrix | Yes | Yes | No | Yes | Yes | No | Encode + decode | Encode + decode | No | Yes |
| Ultracode | Matrix | Yes | No | Yes | Yes | Yes | Yes | No | No | No | Yes |
| Code 128 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| GS1-128 | Linear | Yes | Yes | No | Yes | Yes | Yes | Encode + decode | No | No | No |
| Code 39 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Code 93 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| EAN | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| EAN-2 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| EAN-5 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| UPC-A | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| UPC-E | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| ITF-14 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Interleaved 2 of 5 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Industrial 2 of 5 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Matrix 2 of 5 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| IATA 2 of 5 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| COOP 2 of 5 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Datalogic 2 of 5 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Patch Code | Linear | Yes | Yes | No | Yes | No | Yes | No | No | No | No |
| Codabar | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| MSI | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Code 11 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| BC412 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Facing Identification Mark | Linear | Yes | Yes | No | Yes | No | No | No | No | No | Yes |
| DAFT | Postal | Yes | Yes | No | Yes | No | No | No | No | No | Yes |
| Flattermarken | Linear | Yes | Yes | No | Yes | No | No | No | No | No | Yes |
| Channel Code | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| PosiCode | Linear | Yes | Yes | No | Yes | Yes | Yes | Encode + decode | No | No | No |
| Mailmark 4-State | Postal | Yes | Yes | No | Yes | Yes | No | No | No | No | Yes |
| Code 16K | Stacked | Yes | Yes | No | Yes | Yes | No | Encode + decode | No | Encode + decode | Yes |
| Code 49 | Stacked | Yes | Yes | No | Yes | Yes | No | Encode + decode | No | Encode + decode | Yes |
| Codablock F | Stacked | Yes | Yes | No | Yes | Yes | No | Encode + decode | No | No | Yes |
| Plessey | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Telepen | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| Telepen Numeric | Linear | Yes | Yes | No | Yes | No | Yes | No | No | No | No |
| Pharmacode | Linear | Yes | Yes | No | Yes | No | Yes | No | No | No | No |
| Two-track Pharmacode | Stacked | Yes | Yes | No | Yes | No | No | No | No | No | Yes |
| Code 32 | Linear | Yes | Yes | No | Yes | Yes | Yes | No | No | No | No |
| POSTNET | Postal | Yes | Yes | No | Yes | Yes | No | No | No | No | Yes |
| PLANET | Postal | Yes | Yes | No | Yes | Yes | No | No | No | No | Yes |
| Royal Mail 4-State | Postal | Yes | Yes | No | Yes | Yes | No | No | No | No | Yes |
| Australia Post | Postal | Yes | Yes | No | Yes | Yes | No | No | No | No | Yes |
| Japan Post | Postal | Yes | Yes | No | Yes | Yes | No | No | No | No | Yes |
| GS1 DataBar Truncated | Linear | Yes | Yes | No | Yes | Yes | Yes | Encode + decode | No | No | No |
| GS1 DataBar Omnidirectional | Linear | Yes | Yes | No | Yes | Yes | Yes | Encode + decode | No | No | No |
| GS1 DataBar Stacked | Stacked | Yes | Yes | No | Yes | Yes | No | Encode + decode | No | No | No |
| GS1 DataBar Expanded | Linear | Yes | Yes | No | Yes | Yes | Yes | Encode + decode | No | No | No |
| GS1 DataBar Expanded Stacked | Stacked | Yes | Yes | No | Yes | Yes | No | Encode + decode | No | No | No |
| GS1 DataBar Limited | Linear | Yes | Yes | No | Yes | Yes | Yes | Encode + decode | No | No | No |
| GS1 DataBar Stacked Omnidirectional | Stacked | Yes | Yes | No | Yes | Yes | No | Encode + decode | No | No | No |
| MaxiCode | Matrix | Yes | Yes | No | Yes | Yes | Yes | No | Encode + decode | Encode + decode | No |
| DotCode | Matrix | Yes | Yes | No | Yes | Yes | No | Encode + decode | Encode + decode | Encode + decode | Yes |
| Han Xin Code | Matrix | Yes | Yes | No | Yes | Yes | No | No | Encode + decode | No | Yes |
| GS1 Composite | Stacked | Yes | Yes | No | Yes | Yes | No | Encode + decode | No | No | Yes |
| USPS Intelligent Mail | Postal | Yes | Yes | No | Yes | Yes | No | No | No | No | Yes |
| KIX Code | Postal | Yes | Yes | No | Yes | No | No | No | No | No | Yes |
| Data Matrix | Matrix | Yes | Yes | No | Yes | Yes | Yes | Encode + decode | Encode + decode | Encode + decode | No |
| PDF417 | Stacked | Yes | Yes | No | Yes | Yes | Yes | No | Encode + decode | No | No |
| MicroPDF417 | Stacked | Yes | Yes | No | Yes | Yes | No | No | Encode + decode | No | Yes |

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

## Application identifiers and encoding variants

Application helpers use the existing physical carriers. [Barcode profiles](/docs/barcode-profiles/) covers ISBN, ISMN, ISSN, PZN, Deutsche Post identifiers, HIBC, USPS packages, coupons, Mailmark 2D and Swiss QR payments. They do not add duplicate physical-format entries to this table.

`Code16KEncoder` and `Code49Encoder` expose sequence controls through their encoding options. `PosiCodeEncoder` selects A/B and Limited A/B variants and linked rows. `Bc412Encoder` selects SEMI or IBM conventions. DAFT and Flattermarken represent raw marks and require a format hint when decoding. D3 AQR uses a provisional frame definition and requires an explicit category when encoding.

The specialist image samplers recognize matrix, stacked-row and postal-state patterns, validate them through their module decoders, and report geometry in source-image coordinates. They handle clean exports, resampling, right-angle rotations and inversion. A bounded deskew pass handles modest in-plane rotation. These samplers return one symbol per format; arbitrary perspective and exhaustive multiple-symbol localization are not established.

Flattermarken needs `ScanOptions.SamplingGrid` and a cell-aligned `Region` without quiet zones or captions: each digit occupies nine columns and one row. White edge cells carry data and cannot be inferred after cropping. A calibrated grid also supplies a known `rotationDegrees` for unchecked marks such as DAFT, KIX or Two-track Pharmacode, where more than one orientation can decode to a valid message. Calibration requires exactly one selected format. Postal grids describe bar states; final mailpiece dimensions and acceptance require separate qualification.
