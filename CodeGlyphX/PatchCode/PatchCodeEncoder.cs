using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.PatchCode;

/// <summary>
/// Encodes Patch Code symbols.
/// </summary>
public static class PatchCodeEncoder {
    /// <summary>
    /// Encodes a Patch Code symbol. Valid values are: 1, 2, 3, 4, 6, T.
    /// </summary>
    public static Barcode1D Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length != 1) throw new ArgumentException("Patch Code expects a single symbol (1,2,3,4,6,T).", nameof(content));

        var ch = char.ToUpperInvariant(content[0]);
        if (!PatchCodeTables.EncodingTable.TryGetValue(ch, out var pattern)) {
            throw new InvalidOperationException($"Invalid Patch Code symbol: '{content}'.");
        }

        var segments = new List<BarSegment>(8);
        for (var i = 0; i < pattern.Length; i++) {
            var width = pattern[i] ? PatchCodeTables.WideWidth : PatchCodeTables.NarrowWidth;
            for (var m = 0; m < width; m++) {
                BarcodeSegments.AppendBit(segments, true);
            }
            if (i < pattern.Length - 1) {
                BarcodeSegments.AppendBit(segments, false);
            }
        }

        return new Barcode1D(segments);
    }
}

internal static class PatchCodeTables {
    internal const int NarrowWidth = 1;
    internal const int WideWidth = 3;

    internal static readonly Dictionary<char, bool[]> EncodingTable = new() {
        ['1'] = new[] { true, false, false, true },
        ['2'] = new[] { true, false, true, false },
        ['3'] = new[] { true, true, false, false },
        ['4'] = new[] { false, true, true, false },
        ['6'] = new[] { false, false, true, true },
        ['T'] = new[] { false, true, false, true }
    };

    internal static readonly Dictionary<int, char> PatternMap = new() {
        [0b1001] = '1',
        [0b1010] = '2',
        [0b1100] = '3',
        [0b0110] = '4',
        [0b0011] = '6',
        [0b0101] = 'T'
    };
}
