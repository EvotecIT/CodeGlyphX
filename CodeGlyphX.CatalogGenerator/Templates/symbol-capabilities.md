---
title: Symbol capabilities
description: Generated CodeGlyphX encoding, module-decoding, and image-scanning support matrix.
---

# Symbol capabilities

CodeGlyphX currently describes {{FORMAT_COUNT}} physical symbol formats through `SymbolCapabilities`. This table is generated from that runtime registry, so the website and the public API report the same support boundary.

“Module decode” means CodeGlyphX can decode an already sampled `BitMatrix` or module sequence. “Image scan” means `SymbolScanner` can recognize the format from pixels. A module-only format is not presented as camera/image recognition support.

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

The directional columns distinguish encoding from decoding. For example, the current QR implementation decodes structured-append metadata but does not yet encode structured-append sequences.
