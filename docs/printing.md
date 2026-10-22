# Physical barcode printing

`CodeGlyphX.Printing.BarcodePrint` lays out encoded symbols in millimetres. It returns the paths,
positioned glyph outlines, required clear space and measurements before creating an image.
Use this opt-in route for labels with a declared physical size. Existing pixel-based `Render`
methods retain their existing dimensions and caption behavior.

```csharp
using CodeGlyphX;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;

var font = BarcodeFont.OcrB;
var symbol = Barcode.Encode(SymbolFormat.Ean, "5901234123457");
var layout = BarcodePrint.Layout(symbol, new BarcodePrintOptions {
    Dpi = 300,
    Font = font,
    FontSizePoints = 8
});

foreach (var finding in layout.Preflight(format: OutputFormat.Pdf))
    Console.WriteLine($"{finding.Code}: {finding.Message}");

layout.Save("retail-label.pdf");
layout.Save("retail-label.png");
```

`BarcodeFont.OcrA` (GG Ocra) and `BarcodeFont.OcrB` (CodeGlyphX Scan B) provide bundled
regular faces with printable ASCII. Their licenses and provenance are included in the package.
See the [font guide](fonts.md) for details and supplied-font support.

Fonts are scoped to bundled or supplied bytes. The library does not look up installed fonts or fetch fonts
over the network. Select a face whose license permits the intended outline use and distribution.
`BarcodeFont.FromBytes` supports TrueType, OpenType CFF/CFF2 and collection faces. The same
shaped line supplies advance, baseline, ink bounds and contours to every print export. Strict
text rejects missing glyphs and unsupported shaping; non-strict text records a preflight error
when it uses fallback. `CompactBarcodeFont.Instance` explicitly selects the existing 5 by 7
uppercase lettering. It has a limited repertoire and is not an OCR-A or OCR-B font.

`Font` and `CaptionFont` may select separate supplied faces. Sizes are in points; one point is
25.4/72 mm. Load an actual bold face when needed and declare `FontIsBold` for application
preflight. `SyntheticBold` is an explicit alternative that expands the measured outline by two
percent of em. Its additional ink is included in the result. It is not a substitute for qualifying
a printed postal or healthcare label.

`HumanReadableText` supplies exact application text and accepts line breaks. Retail layouts
derive checked digits from the encoded bars when it is null. Use `BarcodeProfiles`, `Hibc` and
the GS1 owners to prepare application text; a physical carrier cannot establish every application
profile. `IncludeHumanReadable = false` explicitly omits text. A requested printable line needs
a bundled or supplied font. Captions sit above the symbol; `FitCaptionToSymbolWidth` can shrink them down
to `MinimumCaptionSizePoints`, then reports `CaptionDoesNotFit` if the line is still too wide.

## Physical size and bounds

`XDimensionMm` requests a narrow module width. By default it is rounded to a whole pixel at the
selected `Dpi`. `XDimensionMm` and `ModulePitchPixels` on the result report the resolved size.
A printer resolution that cannot meet a selected physical range produces a preflight error.
`SnapToPrinterPixels = false` retains the requested fractional pitch and reports the need to
qualify raster output. Swiss QR retains its fixed physical dimensions. MaxiCode retains its
hexagonal lattice at the selected pitch, within 24/29 to 27/29 mm; its nominal pitch is 0.88 mm.

`SymbolBounds` contains the symbol and its quiet zones. `LogicalBounds` also includes text line
boxes. `InkBounds` contains actual bars, recognition patterns, bearers and text outlines, including
negative bearings and overhang. `Bounds` is their union and is the extent needed to render the
complete label. The background does not turn reserved whitespace into symbol ink.

```csharp
var placement = new BarcodePrintPlacement {
    X = 60, Y = 10,
    RotationDegrees = 90,
    CanvasWidthMm = 80,
    CanvasHeightMm = 100
};
var placedBounds = placement.Measure(layout.Bounds);
var findings = layout.Preflight(placement, OutputFormat.Svg);
layout.Save("placed-label.svg", placement);
```

Placement applies uniform scale, clockwise rotation and translation to the measured paths.
Without explicit canvas dimensions, output reserves the complete transformed extent. An explicit
canvas that cuts ink or required space throws unless `AllowClipping` is selected. Preflight
reports that clipping even when export is allowed. Scaling after layout requires revalidation of
the final module and text sizes. This placement API inserts a barcode on a canvas; it does not
define a complete shipping label, payment part or printer language.

## Family geometry

Retail EAN/UPC layouts reserve asymmetric quiet zones, position outside digits and digit groups,
extend guard bars, and put supplement text above the shorter add-on bars. Existing encodings
retain their payload; physical layout supplies the permitted supplement gap. ISBN, ISMN and ISSN
profile overloads add their separate captions. ITF-14 supplies horizontal bearers or a full bearer
frame through `Itf14BearerFrame`. The frame defaults to the 4.83 mm plate-printing thickness;
`Itf14BearerThicknessMm` selects another permitted thickness, subject to the two-module minimum.
Its readable ink starts at least 1.02 mm below the lower bearer. The physical ITF-14 route uses
a target wide-to-narrow ratio of 2.5, with wide elements rounded half up to a whole printer pixel.
Small ITF-14 module sizes receive an application-qualification warning.

Physical ISBN captions retain the input's hyphen grouping and omit its check character, while the
bars retain the complete checked EAN identifier. ISMN captions use the current `979-0` form and
the [International ISMN Agency's registrant ranges](https://ismn-international.org/ismn/ranges/).
The existing `BarcodeProfiles.HumanReadable` helper keeps its canonical identifier behavior.

The M&S profile uses EAN-8 bars with unextended centre guards, the supplied seven or eight
checked digits and the M/S letters outside the data area. Identcode and Leitcode retain their
postal field grouping, 25 mm bars and absolute clear-space minimum. PZN readable text includes
its `PZN -` prefix. These profile routes keep presentation separate from the carrier's payload.

Stacked layouts retain their row and separator ratios. Composite layouts retain the carrier,
component and linkage geometry, including retail supplements. Use the encoded
`Gs1CompositeSymbol` or `Gs1CompositeComponentSymbol` overload so that this metadata remains
available. An anonymous Composite grid is insufficient to choose its physical carrier layout.

Postal layouts position actual bar states and bar pitch rather than stretching a small logical
grid as square pixels. Their clear-space and dimension checks are separate from text size and
postal acceptance. MaxiCode retains staggered hexagons and its circular finder. DotCode uses
round dots. Swiss QR retains the 46 mm data area and the 7 mm recognition overlay. Ultracode
retains its fixed opaque black, white and data colours and rejects replacement palette paints.

Pharmacode uses 6 mm of horizontal clear space. Two-track bars and gaps have equal widths,
and each half bar is half the full height. Flattermarken uses its application dimensions of
2–3 mm module width and 5–10 mm bar height. DataBar, DAFT, Flattermarken, Aztec and ordinary
Code One A–H do not receive a required-quiet-zone error when quiet zones are disabled.
Code One S/T retain their side and bottom clear space. Code 16K and Code 49 reserve 10 modules
on the left and one on the right; Codablock F reserves 10 on both sides. DotCode and Han Xin
reserve three modules on every side.
`DotCodeEncodingOptions.Width` and `Height` select columns and rows independently or together.
A fixed grid must have an odd sum of dimensions and enough room for data and error correction.

## Application text and checked input

`BarcodeEncodingOptions` selects `CheckMode` (`None`, `Compute` or `Validate`) and whether checks
appear in readable text. Use `Validate` when input already includes an optional check. Mandatory
Plessey and BC412 checks remain required; the BC412 check occupies the second character.
The explicit ITF-14 encoding route accepts spaces in the readable identifier and removes them
from the bars. Code 39 Full ASCII readable text replaces nonprinting controls with spaces while
preserving the encoded data.

`BarcodePrint.LayoutHibc` accepts `HibcEncodingOptions` for supplied checked payloads and the
normal carrier size, shape and error-correction controls. It retains the HIBC delimiters,
underscores, slashed zero and primary/secondary line break. HIBC Code 39/128 use a nominal
0.010 inch module and a 0.0067 inch minimum; HIBC Data Matrix uses 0.015 and 0.010 inch.

For GS1 matrix symbols, prepare the payload with `Gs1.ElementString(input)`, encode it with
the carrier's GS1 option and supply
`HumanReadableText = Gs1.HumanReadable(input, multiline: true)` together with
`TextStyle = BarcodePrintTextStyle.Gs1`. AI 8200's machine URL stays in the payload and is
omitted from this readable interpretation. The selected GS1 QR, Data Matrix or DotCode print
policy checks its module range as well as readable-text size. A typed
`NorthAmericanCouponData` layout prints the company-prefix/offer line above its fixed 34-module
data rows and selects the coupon's 0.254–0.330 mm module range.

With a supplied font, Intelligent Mail layouts derive field-grouped text from the checked bars.
The `IntelligentMailPackageData` overload creates the USPS package service banner, grouped PIC,
identification bars and physical text clearances. Supply a sans-serif font and declare actual
bold faces with `FontIsBold` and `CaptionFontIsBold`; this package layout applies synthetic
weight to an undeclared regular face. Its rules follow
[USPS2000508 Rev E, sections 2.2–2.4](https://postalpro.usps.com/mnt/glusterfs/2018-02/BarcodePackageIMSpec.pdf).
The readable PIC excludes routing data. These checks do not establish postal certification.

## Export behavior

| Output | Geometry and text | Physical size |
| --- | --- | --- |
| PNG | Rasterized shared outlines, with alpha | DPI recorded in `pHYs` |
| SVG / SVGZ | Compound vector outlines, gradients and embedded pattern tiles | Millimetre dimensions and view box |
| PDF, opaque solid paints | Compound vector outlines | Page dimensions in PDF points |
| PDF, decorative or translucent paints | Complete scene rasterized at the selected DPI and flattened onto white; preflight reports this fallback | Page dimensions in PDF points |
| EPS | Compound vector outlines with opaque solid paints; other paints are rejected | Point bounding boxes, including high-resolution dimensions |
| BMP / lossless WebP / PAM / TGA | Raster fallback preserving the scene's colour pixels | This route does not record the layout DPI; preflight reports the limitation |

Other print output routes are rejected explicitly. Use SVG when portable vector gradients or
image patterns matter. `BarcodePaint.Pattern` snapshots its RGBA tile; changes to the caller's
array cannot change a completed scene. Gradient and pattern coordinates are millimetres.
Decorative or translucent paints receive a contrast-qualification finding. Their visual appearance
alone does not establish readability on a particular substrate or printer.

Images are limited to 16 million pixels, with additional bounded outline sampling work. Excessive
text, contour complexity, invalid dimensions and unsupported output routes fail before an
unbounded rendering operation. Streams remain caller-owned. Layout and font output are owned
snapshots suitable for independent rendering calls.

## Standards and qualification

The retail geometry uses GS1 General Specifications 26, section 5.2.3: proportional bar heights,
five-module guard extensions, the EAN/UPC quiet-zone table and supplement spacing. Application
rules depend on the selected use; a generic carrier does not confer every GS1 application size.
See the [GS1 General Specifications](https://ref.gs1.org/standards/genspecs/26.0.0/).

Postal dimensions use the operator's technical specifications: [USPS-B-3200 Rev H](https://postalpro.usps.com/storages/2017-08/2190_USPSB3200IntelligentMailBarcode4State_0.pdf),
[Australia Post Customer Barcoding, revised August 2012](https://auspost.com.au/content/dam/auspost_corp/media/documents/customer-barcode-technical-specifications-aug2012.pdf),
[Royal Mail Mailmark definition, November 2023](https://www.royalmailtechnical.com/rmt_docs/User_Guides_2023/Mailmark_Barcode_definition_document_20231130.pdf),
[PostNL KIX technical specification](https://www.postnl.nl/Images/Brochure-KIX-code-van-PostNL_tcm10-10210.pdf)
and the [Japan Post customer-barcode dimensions](https://www.post.japanpost.jp/service/search/zipcode/zipmanual/p12.html).

Preflight identifies selected geometric and typography constraints. It does not measure physical
ink spread, substrate reflectance, printer calibration, barcode grade, identifier assignment or
postal/payment acceptance. Qualify the actual printed output with the relevant application owner.
