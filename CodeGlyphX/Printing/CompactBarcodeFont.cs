using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Printing;

/// <summary>The existing 5 by 7 barcode lettering, explicitly selected as a compact font.</summary>
/// <remarks>This is not OCR-A or OCR-B. Its limited uppercase repertoire rejects lowercase and missing characters
/// in strict mode. Non-strict mode reports replacement characters rather than silently uppercasing.</remarks>
public sealed class CompactBarcodeFont : IBarcodeFont {
    /// <summary>Gets the immutable compact-font instance.</summary>
    public static CompactBarcodeFont Instance { get; } = new();
    /// <inheritdoc />
    public string Name => "CodeGlyphX compact 5x7";
    private CompactBarcodeFont() { }
    /// <inheritdoc />
    public BarcodeTextShape Shape(string text, double emSize, bool strict = true) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (text.Length > 32768) throw new ArgumentException("Text exceeds the compact-font limit.", nameof(text));
        if (!BarcodeBounds.Finite(emSize) || emSize <= 0 || emSize > 1000000) throw new ArgumentOutOfRangeException(nameof(emSize));
        var contours = new List<IEnumerable<BarcodePoint>>();
        var missing = new List<int>();
        var unit = emSize / 8;
        var origin = 0D;
        for (var index = 0; index < text.Length; index++) {
            var c = text[index];
            var scalar = (int)c;
            if (char.IsControl(c) || char.IsSurrogate(c)) throw new ArgumentException("Compact lettering accepts one line of BMP characters without controls.", nameof(text));
            if (!BarcodeLabelFont.ContainsExact(c)) {
                if (strict) throw new ArgumentException("Compact lettering does not contain every requested character.", nameof(text));
                missing.Add(scalar); c = '?';
            }
            var glyph = BarcodeLabelFont.GetGlyph(c);
            for (var y = 0; y < 7; y++) for (var x = 0; x < 5; x++) if ((glyph[y] & (1 << (4 - x))) != 0) {
                if (contours.Count >= 65536) throw new ArgumentException("Compact lettering exceeds the outline limit.", nameof(text));
                var left = origin + x * unit; var top = y * unit;
                contours.Add(new[] { new BarcodePoint(left, top), new BarcodePoint(left + unit, top),
                    new BarcodePoint(left + unit, top + unit), new BarcodePoint(left, top + unit) });
            }
            origin += 6 * unit;
        }
        return new BarcodeTextShape(text, text.Length == 0 ? 0 : origin - unit, emSize, 7 * unit, contours, missing);
    }
}
