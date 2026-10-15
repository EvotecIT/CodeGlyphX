using System;
using System.Collections.Generic;

namespace CodeGlyphX.Flattermarken;

/// <summary>Encodes and decodes decimal collation marks in fixed nine-module character cells.</summary>
/// <remarks>White edge cells are data. Decoding requires an exact cell-aligned module span, not a trimmed scanline.</remarks>
public static class FlattermarkenCode {
    /// <summary>Encodes decimal digits; zero produces a white cell and one through nine select the bar position.</summary>
    public static Barcode1D Encode(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length == 0) throw new ArgumentException("At least one collation digit is required.", nameof(value));
        var segments = new List<BarSegment>();
        foreach (var digit in value) {
            if ((uint)(digit - '0') > 9) throw new ArgumentException("Collation marks accept decimal digits only.", nameof(value));
            for (var x = 0; x < 9; x++) {
                var dark = digit != '0' && x == digit - '1';
                if (segments.Count > 0 && segments[segments.Count - 1].IsBar == dark) {
                    var previous = segments[segments.Count - 1];
                    segments[segments.Count - 1] = new BarSegment(dark, checked(previous.Modules + 1));
                } else segments.Add(new BarSegment(dark, 1));
            }
        }
        return new Barcode1D(segments, SymbolFormat.Flattermarken);
    }

    /// <summary>Reads exact nine-module cells, rejecting cells containing more than one bar.</summary>
    public static bool TryDecode(ReadOnlySpan<bool> modules, out string text) {
        text = string.Empty;
        if (modules.Length == 0 || modules.Length % 9 != 0) return false;
        var digits = new char[modules.Length / 9];
        for (var i = 0; i < digits.Length; i++) {
            var position = -1;
            for (var x = 0; x < 9; x++) {
                if (!modules[i * 9 + x]) continue;
                if (position >= 0) return false;
                position = x;
            }
            digits[i] = position < 0 ? '0' : (char)('1' + position);
        }
        text = new string(digits);
        return true;
    }
}
