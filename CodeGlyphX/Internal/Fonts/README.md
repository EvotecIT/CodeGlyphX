# Managed font implementation

This is an intentionally separate source copy of the dependency-free font implementation in
[OfficeIMO.Core](https://github.com/EvotecIT/OfficeIMO/tree/cc808496403a2c46174ba0d0702907cb56525b34/OfficeIMO.Core),
adapted from commit `cc808496403a2c46174ba0d0702907cb56525b34`. CodeGlyphX owns and qualifies this copy;
normal builds do not download, reference or synchronize OfficeIMO.

The retained code reads supplied TrueType and OpenType outlines, font metrics, common OpenType
substitutions, kerning, bidirectional text and bounded Arabic joining. Its types are internal.
The public entry point is `CodeGlyphX.Printing.BarcodeFont`, which snapshots and validates font
bytes, bounds text and outline output, and reports unsupported characters and shaping.

Adaptations remove installed-font discovery, document math and colour-font painting, provider
registration and unrelated drawing types. TrueType character mapping and outline extraction
are separate partials. Shaping uses the exact loaded face instead of reopening a collection's
first face. No operating-system font APIs, native libraries or new runtime packages are used.
TrueType outline reads also enforce each glyph's declared byte extent, reject malformed references,
and share a work budget across a complete text run, including composites that produce no points.

The source retains the MIT notice in [the bundled license](../../../Licenses/OfficeIMO-MIT.txt).
Supplied font programs have their own licenses. Test-only font fixtures are not bundled in the
production package.
