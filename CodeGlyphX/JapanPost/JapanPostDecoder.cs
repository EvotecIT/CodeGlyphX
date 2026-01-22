using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.JapanPost;

/// <summary>
/// Decodes Japan Post barcodes.
/// </summary>
public static class JapanPostDecoder {
    /// <summary>
    /// Attempts to decode a Japan Post barcode from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        text = string.Empty;
        if (modules is null) return false;
        if (modules.Width <= 0 || modules.Height != JapanPostTables.BarcodeHeight) return false;

        var bars = ExtractBars(modules);
        if (!TryParseBars(bars, out var inter)) return false;
        if (!TryTrimInter(inter, out var trimmed)) return false;
        return TryDecodeInter(trimmed, out text);
    }

    private static bool TryParseBars(List<char> bars, out char[] inter) {
        inter = Array.Empty<char>();
        if (bars.Count != 67) return false;
        if (bars[0] != 'F' || bars[1] != 'D') return false;
        if (bars[65] != 'D' || bars[66] != 'F') return false;

        var decoded = new char[20];
        if (!TryFillInter(bars, decoded, out var sum)) return false;
        if (!TryValidateChecksum(bars, sum)) return false;

        inter = decoded;
        return true;
    }

    private static bool TryFillInter(List<char> bars, char[] inter, out int sum) {
        sum = 0;
        var offset = 2;
        for (var i = 0; i < inter.Length; i++) {
            if (!TryDecodePattern(bars, offset, out var ch, out var checkIdx)) return false;
            inter[i] = ch;
            sum += checkIdx;
            offset += 3;
        }
        return true;
    }

    private static bool TryValidateChecksum(List<char> bars, int sum) {
        var offset = 2 + 20 * 3;
        if (!TryDecodePattern(bars, offset, out var checkChar, out _)) return false;
        var expected = 19 - (sum % 19);
        if (expected == 19) expected = 0;
        return JapanPostTables.CheckSet[expected] == checkChar;
    }

    private static bool TryDecodePattern(List<char> bars, int offset, out char ch, out int checkIdx) {
        ch = default;
        checkIdx = 0;
        var pattern = new string(new[] { bars[offset], bars[offset + 1], bars[offset + 2] });
        if (!JapanPostTables.PatternIndex.TryGetValue(pattern, out var value)) return false;
        ch = JapanPostTables.KasutSet[value];
        if (!JapanPostTables.CheckIndex.TryGetValue(ch, out checkIdx)) return false;
        return true;
    }

    private static bool TryTrimInter(char[] inter, out string trimmed) {
        var end = inter.Length;
        while (end > 0 && inter[end - 1] == 'd') end--;
        for (var i = end; i < inter.Length; i++) {
            if (inter[i] != 'd') { trimmed = string.Empty; return false; }
        }
        trimmed = new string(inter, 0, end);
        return true;
    }

    private static bool TryDecodeInter(string inter, out string text) {
        var output = new StringBuilder(inter.Length);
        for (var i = 0; i < inter.Length; ) {
            var ch = inter[i];
            if (IsDigitOrDash(ch)) {
                output.Append(ch);
                i++;
                continue;
            }

            if (!TryDecodeAlphaSequence(inter, ref i, out var decoded)) { text = string.Empty; return false; }
            output.Append(decoded);
        }

        text = output.ToString();
        return true;
    }

    private static bool IsDigitOrDash(char ch) {
        return (ch >= '0' && ch <= '9') || ch == '-';
    }

    private static bool TryDecodeAlphaSequence(string inter, ref int index, out char decoded) {
        decoded = default;
        var prefix = inter[index];
        if (prefix != 'a' && prefix != 'b' && prefix != 'c') return false;
        if (index + 1 >= inter.Length) return false;
        var next = inter[index + 1];
        if (!JapanPostTables.CheckIndex.TryGetValue(next, out var value)) return false;
        if (value > 9) return false;

        if (prefix == 'a') {
            decoded = (char)('A' + value);
        } else if (prefix == 'b') {
            decoded = (char)('K' + value);
        } else {
            if (value > 5) return false;
            decoded = (char)('U' + value);
        }

        index += 2;
        return true;
    }

    private static List<char> ExtractBars(BitMatrix modules) {
        var bars = new List<char>(modules.Width / 2);

        if (!TryFindBarBounds(modules, out var firstBar, out var lastBar)) return bars;

        var runs = BuildRuns(modules, firstBar, lastBar);
        foreach (var run in runs) {
            if (!run.isBar) continue;
            if (TryClassifyRun(modules, run, out var symbol)) {
                bars.Add(symbol);
            }
        }

        return bars;
    }

    private static bool TryFindBarBounds(BitMatrix modules, out int firstBar, out int lastBar) {
        firstBar = -1;
        lastBar = -1;
        for (var x = 0; x < modules.Width; x++) {
            if (HasBar(modules, x)) {
                if (firstBar < 0) firstBar = x;
                lastBar = x;
            }
        }
        return firstBar >= 0 && lastBar >= 0;
    }

    private static List<(bool isBar, int start, int length)> BuildRuns(BitMatrix modules, int firstBar, int lastBar) {
        var runs = new List<(bool isBar, int start, int length)>(modules.Width / 2);
        var current = HasBar(modules, firstBar);
        var runStart = firstBar;
        for (var x = firstBar + 1; x <= lastBar; x++) {
            var isBar = HasBar(modules, x);
            if (isBar == current) continue;
            runs.Add((current, runStart, x - runStart));
            current = isBar;
            runStart = x;
        }
        runs.Add((current, runStart, lastBar - runStart + 1));
        return runs;
    }

    private static bool TryClassifyRun(BitMatrix modules, (bool isBar, int start, int length) run, out char symbol) {
        var asc = false;
        var desc = false;
        var tracker = false;
        for (var x = run.start; x < run.start + run.length; x++) {
            if (!tracker && (modules[x, 3] || modules[x, 4])) tracker = true;
            if (!asc && (modules[x, 0] || modules[x, 1] || modules[x, 2])) asc = true;
            if (!desc && (modules[x, 5] || modules[x, 6] || modules[x, 7])) desc = true;
        }

        if (!tracker) {
            symbol = default;
            return false;
        }

        if (asc && desc) symbol = 'F';
        else if (asc) symbol = 'A';
        else if (desc) symbol = 'D';
        else symbol = 'T';
        return true;
    }

    private static bool HasBar(BitMatrix modules, int x) {
        for (var y = 0; y < modules.Height; y++) {
            if (modules[x, y]) return true;
        }
        return false;
    }
}
