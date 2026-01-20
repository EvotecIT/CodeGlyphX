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
        if (bars.Count != 67) return false;
        if (bars[0] != 'F' || bars[1] != 'D') return false;
        if (bars[65] != 'D' || bars[66] != 'F') return false;

        var inter = new char[20];
        var sum = 0;
        var offset = 2;
        for (var i = 0; i < 20; i++) {
            var pattern = new string(new[] { bars[offset], bars[offset + 1], bars[offset + 2] });
            if (!JapanPostTables.PatternIndex.TryGetValue(pattern, out var value)) return false;
            var ch = JapanPostTables.KasutSet[value];
            inter[i] = ch;
            if (!JapanPostTables.CheckIndex.TryGetValue(ch, out var checkIdx)) return false;
            sum += checkIdx;
            offset += 3;
        }

        var checkPattern = new string(new[] { bars[offset], bars[offset + 1], bars[offset + 2] });
        if (!JapanPostTables.PatternIndex.TryGetValue(checkPattern, out var checkValue)) return false;
        var checkChar = JapanPostTables.KasutSet[checkValue];
        var expected = 19 - (sum % 19);
        if (expected == 19) expected = 0;
        if (JapanPostTables.CheckSet[expected] != checkChar) return false;

        if (!TryTrimInter(inter, out var trimmed)) return false;
        if (!TryDecodeInter(trimmed, out text)) return false;
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
        for (var i = 0; i < inter.Length; i++) {
            var ch = inter[i];
            if ((ch >= '0' && ch <= '9') || ch == '-') {
                output.Append(ch);
                continue;
            }

            if (ch == 'a' || ch == 'b' || ch == 'c') {
                if (i + 1 >= inter.Length) { text = string.Empty; return false; }
                var next = inter[++i];
                if (!JapanPostTables.CheckIndex.TryGetValue(next, out var value)) { text = string.Empty; return false; }
                if (value < 0 || value > 9) { text = string.Empty; return false; }
                char decoded;
                if (ch == 'a') {
                    decoded = (char)('A' + value);
                } else if (ch == 'b') {
                    decoded = (char)('K' + value);
                } else {
                    if (value > 5) { text = string.Empty; return false; }
                    decoded = (char)('U' + value);
                }
                output.Append(decoded);
                continue;
            }

            text = string.Empty;
            return false;
        }

        text = output.ToString();
        return true;
    }

    private static List<char> ExtractBars(BitMatrix modules) {
        var bars = new List<char>(modules.Width / 2);

        var firstBar = -1;
        var lastBar = -1;
        for (var x = 0; x < modules.Width; x++) {
            if (HasBar(modules, x)) {
                if (firstBar < 0) firstBar = x;
                lastBar = x;
            }
        }

        if (firstBar < 0 || lastBar < 0) return bars;

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

        foreach (var run in runs) {
            if (!run.isBar) continue;
            var asc = false;
            var desc = false;
            var tracker = false;
            for (var x = run.start; x < run.start + run.length; x++) {
                if (!tracker && (modules[x, 3] || modules[x, 4])) tracker = true;
                if (!asc && (modules[x, 0] || modules[x, 1] || modules[x, 2])) asc = true;
                if (!desc && (modules[x, 5] || modules[x, 6] || modules[x, 7])) desc = true;
            }

            if (!tracker) continue;

            if (asc && desc) {
                bars.Add('F');
            } else if (asc) {
                bars.Add('A');
            } else if (desc) {
                bars.Add('D');
            } else {
                bars.Add('T');
            }
        }

        return bars;
    }

    private static bool HasBar(BitMatrix modules, int x) {
        for (var y = 0; y < modules.Height; y++) {
            if (modules[x, y]) return true;
        }
        return false;
    }
}
