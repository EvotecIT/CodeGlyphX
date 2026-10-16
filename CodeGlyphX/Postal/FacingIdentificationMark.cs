using System;
using System.Collections.Generic;

namespace CodeGlyphX.Postal;

/// <summary>Encodes and decodes USPS Facing Identification Marks A through E.</summary>
public static class FacingIdentificationMark {
    private static readonly string[] Patterns = { "110010011", "101101101", "110101011", "111010111", "101000101" };

    /// <summary>Encodes a single uppercase letter A through E into a seventeen-module identification mark.</summary>
    public static Barcode1D Encode(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length != 1 || value[0] < 'A' || value[0] > 'E')
            throw new ArgumentException("FIM requires one uppercase letter A through E.", nameof(value));
        var pattern = Patterns[value[0] - 'A'];
        var segments = new List<BarSegment>();
        for (var x = 0; x < 17; x++) {
            var dark = (x & 1) == 0 && pattern[x / 2] == '1';
            if (segments.Count > 0 && segments[segments.Count - 1].IsBar == dark) {
                var last = segments[segments.Count - 1];
                segments[segments.Count - 1] = new BarSegment(dark, last.Modules + 1);
            } else segments.Add(new BarSegment(dark, 1));
        }
        return new Barcode1D(segments, SymbolFormat.FacingIdentificationMark);
    }

    /// <summary>Decodes an exact seventeen-module mark without a quiet zone.</summary>
    public static bool TryDecode(ReadOnlySpan<bool> modules, out string text) {
        text = string.Empty;
        if (modules.Length != 17) return false;
        for (var index = 0; index < Patterns.Length; index++) {
            var valid = true;
            for (var x = 0; x < modules.Length; x++) {
                if (modules[x] == ((x & 1) == 0 && Patterns[index][x / 2] == '1')) continue;
                valid = false;
                break;
            }
            if (!valid) continue;
            text = ((char)('A' + index)).ToString();
            return true;
        }
        return false;
    }
}
