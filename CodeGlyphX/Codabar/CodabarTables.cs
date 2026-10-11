using System;
using System.Collections.Generic;

namespace CodeGlyphX.Codabar;

internal static class CodabarTables {
    public static readonly IReadOnlyDictionary<char, string> EncodingTable = new Dictionary<char, string> {
        { '0', "0000011" },
        { '1', "0000110" },
        { '2', "0001001" },
        { '3', "1100000" },
        { '4', "0010010" },
        { '5', "1000010" },
        { '6', "0100001" },
        { '7', "0100100" },
        { '8', "0110000" },
        { '9', "1001000" },
        { 'A', "0011010" },
        { 'B', "0101001" },
        { 'C', "0001011" },
        { 'D', "0001110" },
        { '-', "0001100" },
        { '$', "0011000" },
        { '.', "1010100" },
        { '/', "1010001" },
        { ':', "1000101" },
        { '+', "0010101" }
    };

    public static readonly HashSet<char> StartStopChars = new() { 'A', 'B', 'C', 'D' };

    // Both module decoding and pixel-run localization use the same patterns.
    internal static readonly Lazy<Dictionary<int, char>> PatternMap = new(() => {
        var patterns = new Dictionary<int, char>();
        foreach (var pair in EncodingTable) {
            var key = 0;
            foreach (var bit in pair.Value) key = (key << 1) | (bit == '1' ? 1 : 0);
            patterns[key] = pair.Key;
        }
        return patterns;
    });
}
