# Ultracode colour symbols

`UltracodeEncoder` encodes Latin-1 text or exact bytes, chooses a two-, three-,
four- or five-tile-row layout, and applies error correction at EC1 through EC5.
EC2 is the default. It emits the revision-two data-column-count pattern; the
decoder accepts both revisions.

```csharp
using CodeGlyphX;

var symbol = UltracodeEncoder.EncodeText("Batch 123", UltracodeErrorCorrectionLevel.EC3);
File.WriteAllBytes("batch.png", symbol.RenderPng());
File.WriteAllText("batch.svg", symbol.RenderSvg());
```

The maximum eight-bit payloads at EC1, EC2, EC3, EC4 and EC5 are 263, 252, 234,
220 and 202 bytes respectively. Text accepts U+0000 through U+00FF without
changing case or punctuation. Characters outside that range fail explicitly;
`EncodeBytes` preserves caller-supplied bytes without assigning a different text
encoding. These APIs use eight-bit mode; compressed, GS1, ECI and linked-message
modes are not exposed.

The owned `Cells` grid stores logical colours rather than booleans. It is frozen;
`Clone()` creates an editable copy for sampled input or damage simulation.
`UltracodeColor.Unknown` marks unreadable data cells, which become codeword
erasures. Data colours remain cyan, magenta, yellow and green. Render options
control cell size, structural ink and background, with at least one quiet cell.
Changing structural paints can reduce recognition contrast; a theme must retain
the information-carrying data colours.

```csharp
var decoded = SymbolDecoder.Decode(symbol.Cells);
var scan = SymbolScanner.Scan(File.ReadAllBytes("batch.png"), new ScanOptions
{
    Formats = new[] { SymbolFormat.Ultracode },
    MaxSymbols = 1
});
```

The scanner works directly with colour pixels, locates black frames, samples
the colour cells and validates the protected message. It reports symbol geometry
and error-correction metadata. Sampled decoding accepts orthogonal rotations.
Image qualification includes six-pixel cells, orthogonal rotations, representative
15-degree skew and five-percent corner displacement, independent PNGs, and
multiple separated frames. This evidence does not establish arbitrary photographic
distortion or physical scanner acceptance. A grayscale conversion discards
information needed to decode Ultracode.

For a calibrated image region, `ImageSamplingGrid` describes the symbol cells
without quiet zones or captions. The ordinary scanner can also locate complete
frames without calibration. Structural cells must remain readable; Reed-Solomon
correction protects message codewords rather than repairing the outer finder.

`CanDecodeColorCells` distinguishes this route from monochrome module decoding.
The image locator bounds its workspace to 16 million pixels and its candidate
search to 128 plausible frames. Reaching either allowance reports
`ScanCompletionReason.ResourceLimitReached` and `IsPartial`, preserving any
symbols already found. Cancellation and total/family deadlines remain active.
With `Deduplicate = true`, equivalent format-and-payload results are deduplicated,
as with other formats; use `false` when observations of repeated labels are needed.

The fixed tile definitions retain the Zint reference's BSD notice. The independent
fixture generator uses bwip-js/BWIPP only for validation; deployed applications
require neither tool.
