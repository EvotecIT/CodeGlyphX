using System;
using System.Collections.Generic;

namespace CodeGlyphX.JapanPost;

internal static class JapanPostTables {
    public const int BarcodeHeight = 8;

    public static readonly string[] Patterns = {
        "FFT", "FDA", "DFA", "FAD", "FTF", "DAF", "AFD", "ADF", "TFF", "FTT",
        "TFT", "DAT", "DTA", "ADT", "TDA", "ATD", "TAD", "TTF", "FFF"
    };

    public static readonly char[] KasutSet = {
        '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', 'a', 'b', 'c',
        'd', 'e', 'f', 'g', 'h'
    };

    public static readonly char[] CheckSet = {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', 'a', 'b', 'c',
        'd', 'e', 'f', 'g', 'h'
    };

    public static readonly Dictionary<char, int> KasutIndex = BuildIndex(KasutSet);
    public static readonly Dictionary<char, int> CheckIndex = BuildIndex(CheckSet);
    public static readonly Dictionary<string, int> PatternIndex = BuildPatternIndex();

    private static Dictionary<char, int> BuildIndex(char[] set) {
        var map = new Dictionary<char, int>(set.Length);
        for (var i = 0; i < set.Length; i++) {
            map[set[i]] = i;
        }
        return map;
    }

    private static Dictionary<string, int> BuildPatternIndex() {
        var map = new Dictionary<string, int>(Patterns.Length);
        for (var i = 0; i < Patterns.Length; i++) {
            map[Patterns[i]] = i;
        }
        return map;
    }
}
