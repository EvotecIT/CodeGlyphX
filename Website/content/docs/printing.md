---
title: Fonts and physical printing
description: Measure barcode labels in millimetres and export bundled or supplied-font outlines to PNG, SVG and PDF.
---

# Fonts and physical printing

`BarcodePrint` builds a measured label before rendering it. The result contains
bar and glyph outlines, quiet zones, ink and logical bounds, and preflight findings.
Use it for output with a declared physical size and printer resolution.

These APIs follow the current source. See [source and package versions](/docs/installation/#source-and-package-versions) before choosing an installed package.

| Need | Print support |
| --- | --- |
| Physical dimensions | Millimetres, printer DPI and a module width snapped to printer pixels by default. |
| Readable text | Bundled OCR-A/OCR-B or supplied fonts, separate caption faces, point sizes and multiline text. |
| Family layout | EAN/UPC digit groups and guards, supplements, publishing captions, ITF-14 bearers, postal bar states, MaxiCode hexagons and linked Composite carriers. |
| Placement | Measured bounds, quiet zones, translation, uniform scale, rotation and optional canvas dimensions. |
| Export | PNG with DPI metadata, SVG with millimetre dimensions and PDF with physical page dimensions, using the same bar and glyph outlines. |
| Checks | Preflight for selected size, text, quiet-zone, placement and output constraints. Qualify the final printed artifact for its application. |

```csharp
using CodeGlyphX;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;

var font = BarcodeFont.OcrB;
var label = Barcode.Create(SymbolFormat.Code39, "aX")
    .WithEncodingOptions(new BarcodeEncodingOptions {
        Code39FullAscii = true,
        PrintCheckCharacters = false
    })
    .LayoutPrint(new BarcodePrintOptions { Dpi = 300, Font = font, FontSizePoints = 8 });

foreach (var finding in label.Preflight(format: OutputFormat.Pdf))
    Console.WriteLine(finding.Message);

label.Save("label.png");
label.Save("label.svg");
label.Save("label.pdf");
```

Choose `BarcodeFont.OcrA` for GG Ocra or `BarcodeFont.OcrB` for CodeGlyphX Scan B.
Both bundled faces include printable ASCII and load without installed fonts.
Their full CC0/OFL licenses and provenance are included in the package. You can
also supply TTF, OTF or TTC bytes. CodeGlyphX uses the same measured
glyph run for raster and vector output. Strict text rejects missing glyphs and
unsupported shaping. Select a font whose license permits your intended use;
the compact legacy font has a limited repertoire and is not an OCR font.

The [font guide](/docs/fonts/) explains each bundled face, collection selection,
supplied-font formats, text coverage and shaping limits.

The [playground](/playground/) includes a physical print studio with bundled font
selection, local font upload, printer DPI, module width, readable text, captions and rotated exports.
It also exposes Rune, Code One, Ultracode, linked Composite and raw-run layouts.
Prepared exports are cleared when settings change or a font fails to load.

EAN/UPC layouts position guards, digit groups and supplements. Publishing
profiles add captions, ITF-14 adds bearers, postal formats retain bar states, and
MaxiCode retains its hexagons. PNG records DPI; SVG uses millimetres; PDF uses
physical page dimensions. See the maintained [physical printing guide](https://github.com/EvotecIT/CodeGlyphX/blob/master/docs/printing.md)
for geometry, placement, paint, output and resource limits.

Preflight checks selected layout rules. It does not establish print quality,
postal acceptance or scanner performance on a particular device. Qualify the
final printed artifact for its application.
