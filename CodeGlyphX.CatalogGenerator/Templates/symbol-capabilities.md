---
title: Symbol capabilities
description: Generated CodeGlyphX encoding, module-decoding, and image-scanning support matrix.
---

# Symbol capabilities

CodeGlyphX currently describes {{FORMAT_COUNT}} physical symbol formats through `SymbolCapabilities`. This table is generated from that runtime registry, so the website and the public API report the same support boundary.

“Module decode” means CodeGlyphX can decode an already sampled `BitMatrix` or module sequence. “Image scan” means `SymbolScanner` can recognize the format from pixels. A module-only format is not presented as camera/image recognition support.

“Default scan” identifies formats selected when `ScanOptions.Formats` is null or empty. Pharmacode and PatchCode require explicit selection because their patterns can match unrelated content. Telepen Numeric requires an explicit hint because the same bars also carry a valid Telepen ASCII interpretation. Check `IsDefaultScanFormat` when building a format picker that follows the scanner defaults.

{{CAPABILITY_TABLE}}

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

The new Code 16K, Code 49, Codablock F, Mailmark 4-state and D3 AQR decoders accept sampled module grids. Their presence in this table does not promise recognition from photographs or camera frames. Postal grids describe bar states; final mailpiece dimensions and acceptance require separate qualification.
