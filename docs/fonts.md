# Supplied fonts

`CodeGlyphX.Printing.BarcodeFont` loads a font from supplied bytes and provides matching text
measurement and filled outlines. It preserves case and punctuation, uses the selected face's
metrics, and works without installing the font or contacting a font service.

```csharp
using CodeGlyphX.Printing;

var font = BarcodeFont.FromBytes(File.ReadAllBytes("Label-Regular.ttf"));
var text = font.Shape("Batch aB09", emSize: 4);

// All distances use the same units as emSize; this example uses millimetres.
var advance = text.AdvanceWidth;
var baseline = text.BaselineOffset;
var ink = text.InkBounds;
```

The advance includes spaces. Ink bounds contain the actual outlines and may extend outside
the logical line box, particularly for italics or accents. Each result owns its contours.
Measure and place that result when drawing it; measuring another font with the same family
name does not establish the same geometry. Select regular and bold faces by supplying the
corresponding font files.

TrueType `glyf` outlines, OpenType CFF/CFF2 outlines and TrueType collection faces are supported.
For a collection, pass its zero-based face index to `FromBytes`. Compressed WOFF/WOFF2 containers
are not accepted. The selected font's own license determines whether it may be redistributed
or used to produce outlines.

Strict shaping is the default. Missing glyphs or text requiring unsupported complex shaping
raise an error. Managed shaping supports common Latin substitutions, bidirectional text and
bounded Arabic joining when the font supplies the required glyphs. It does not provide complete
shaping for every script.
Combining marks that require mark attachment are reported as unsupported shaping; precomposed
characters such as `é` are supported when present in the selected font.

For a deliberate preview fallback, pass `strict: false`. Missing characters use `?`, or the
Unicode replacement glyph if available, and appear in `MissingGlyphs`. Unsupported shaping is
reported by `HasIncompleteShaping`. The result's `Text` always preserves the original input.
A fallback preview is not proof that the requested text was rendered faithfully.

`Shape` accepts one line. Font input is capped at 16 MiB, text at 32768 UTF-16 characters, and
outline output and glyph-decoding work are bounded. Invalid UTF-16 and control characters are
rejected. A loaded font can be used concurrently and retains its own snapshot of the bytes.
CFF metadata parsing shares repeated references and has cumulative limits of two million decoded
index entries and 16 MiB of metadata reads. Substitution searches share a per-run work budget.

The existing compact bitmap captions keep their established behavior. Supplied fonts are
an explicit choice and do not change existing rendering defaults.
