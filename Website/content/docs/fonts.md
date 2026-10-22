---
title: Barcode fonts
description: Bundled OCR-A and OCR-B faces, supplied TrueType and OpenType fonts, and consistent text outlines across print exports.
---

# Barcode fonts

CodeGlyphX measures and draws readable label text from bundled or supplied font bytes. It needs no installed font, font service or native graphics library. The same measured glyph outlines are used for PNG, SVG and PDF.

These APIs follow the current source. See [source and package versions](/docs/installation/#source-and-package-versions) before choosing an installed package.

| Choice | Included support | License and use |
| --- | --- | --- |
| `BarcodeFont.OcrA` | Bundled GG Ocra regular face, printable ASCII with letter case | CC0 1.0; the font and full notice ship with the package. |
| `BarcodeFont.OcrB` | Bundled CodeGlyphX Scan B regular face, printable ASCII with letter case | SIL OFL 1.1; a renamed derivative of Raisty's OCR-B, with full license and provenance. |
| `BarcodeFont.FromBytes(...)` | Supplied TTF, OpenType CFF/CFF2, and collection faces | Choose a font licensed for your intended output and redistribution. |
| `CompactBarcodeFont.Instance` | The existing compact 5 by 7 uppercase lettering | Limited repertoire; this is not an OCR-A or OCR-B face. |

The bundled faces work across all supported target frameworks, trimming and NativeAOT. They are text fonts; selecting one does not provide OCR recognition or certify a printed label. Full notices are included in the package and linked from the browser print studio. See [third-party notices](https://github.com/EvotecIT/CodeGlyphX/blob/master/THIRD-PARTY-NOTICES.md).

## Supply your own face

```csharp
using CodeGlyphX.Printing;

var font = BarcodeFont.FromBytes(File.ReadAllBytes("Label-Regular.ttf"));
var options = new BarcodePrintOptions {
    Font = font,
    FontSizePoints = 8,
    Dpi = 300
};
```

For a collection, select its zero-based face index. WOFF and WOFF2 containers are not supported. Supply actual regular and bold faces when needed; synthetic bold is an explicit print option. Fonts are kept local and are not installed into the operating system.

## Text coverage and limits

Strict shaping is the default: missing glyphs or unsupported shaping produce an error. Supported shaping includes common Latin substitutions, bidirectional text and bounded Arabic joining when the selected font supplies the required glyphs. It does not cover every script. Combining marks that need mark attachment are unsupported; precomposed characters such as `é` work when present in the font.

Non-strict previews report missing glyphs and incomplete shaping. A preview fallback is not proof that the requested text was rendered faithfully. Font input, text length, metadata processing and glyph work have resource limits; the [maintained font guide](https://github.com/EvotecIT/CodeGlyphX/blob/master/docs/fonts.md) records the exact boundaries.

Continue with [physical printing](/docs/printing/) for module size, text placement, captions, preflight and exports, or use the [browser print studio](/playground/) to choose a bundled face or upload a local font.
