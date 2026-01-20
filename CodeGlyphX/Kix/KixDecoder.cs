using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.RoyalMail;

namespace CodeGlyphX.Kix;

/// <summary>
/// Decodes KIX (Royal Mail 4-state, without headers) barcodes.
/// </summary>
public static class KixDecoder {
    /// <summary>
    /// Attempts to decode a KIX barcode from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        text = string.Empty;
        if (modules is null) return false;
        if (modules.Width <= 0 || modules.Height != RoyalMailTables.BarcodeHeight) return false;

        var bars = ExtractBars(modules);
        if (bars.Count == 0 || bars.Count % 4 != 0) return false;

        var output = new StringBuilder(bars.Count / 4);
        for (var i = 0; i < bars.Count; i += 4) {
            var index = GetSymbolIndex(bars, i);
            if (index < 0) return false;
            output.Append(IndexToChar(index));
        }

        text = output.ToString();
        return true;
    }

    private static List<RoyalMailBarTypes> ExtractBars(BitMatrix modules) {
        var bars = new List<RoyalMailBarTypes>(modules.Width / 2);
        var height = modules.Height;

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
                if (!desc && (modules[x, height - 1] || modules[x, height - 2] || modules[x, height - 3])) desc = true;
            }

            if (!tracker) continue;

            var bar = RoyalMailBarTypes.Tracker;
            if (asc) bar |= RoyalMailBarTypes.Ascender;
            if (desc) bar |= RoyalMailBarTypes.Descender;
            bars.Add(bar);
        }

        return bars;
    }

    private static bool HasBar(BitMatrix modules, int x) {
        for (var y = 0; y < modules.Height; y++) {
            if (modules[x, y]) return true;
        }
        return false;
    }

    private static int GetSymbolIndex(List<RoyalMailBarTypes> bars, int start) {
        for (var i = 0; i < RoyalMailTables.Symbols.Length; i++) {
            var symbol = RoyalMailTables.Symbols[i];
            if (symbol[0] != bars[start]) continue;
            if (symbol[1] != bars[start + 1]) continue;
            if (symbol[2] != bars[start + 2]) continue;
            if (symbol[3] != bars[start + 3]) continue;
            return i;
        }
        return -1;
    }

    private static char IndexToChar(int index) {
        if (index >= 0 && index <= 9) return (char)('0' + index);
        if (index >= 10 && index < 36) return (char)('A' + (index - 10));
        throw new InvalidOperationException("Invalid Royal Mail symbol index.");
    }
}
