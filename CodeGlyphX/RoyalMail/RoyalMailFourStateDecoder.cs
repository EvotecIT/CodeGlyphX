using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.RoyalMail;

/// <summary>
/// Decodes Royal Mail 4-State Customer Code (RM4SCC) barcodes.
/// </summary>
public static class RoyalMailFourStateDecoder {
    /// <summary>
    /// Attempts to decode a Royal Mail 4-State barcode from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        text = string.Empty;
        if (modules is null) return false;
        if (modules.Width <= 0 || modules.Height != RoyalMailTables.BarcodeHeight) return false;

        var bars = ExtractBars(modules);
        if (bars.Count < 6) return false;
        if ((bars.Count - 2) % 4 != 0) return false;
        if (bars[0] != RoyalMailBarTypes.Ascender) return false;
        if (bars[bars.Count - 1] != RoyalMailBarTypes.FullHeight) return false;

        var symbols = (bars.Count - 2) / 4;
        if (symbols < 2) return false;

        var payloadSymbols = symbols - 1;
        var indices = new List<int>(payloadSymbols);
        var output = new StringBuilder(payloadSymbols);

        var start = 1;
        for (var i = 0; i < payloadSymbols; i++) {
            var index = GetSymbolIndex(bars, start + i * 4);
            if (index < 0) return false;
            indices.Add(index);
            output.Append(IndexToChar(index));
        }

        var checksumIndex = GetSymbolIndex(bars, start + payloadSymbols * 4);
        if (checksumIndex < 0) return false;
        if (!ChecksumMatches(indices, checksumIndex)) return false;

        text = output.ToString();
        return true;
    }

    private static bool ChecksumMatches(List<int> indices, int checksumIndex) {
        var sumAsc = 0;
        var sumDesc = 0;
        foreach (var idx in indices) {
            var symbol = RoyalMailTables.Symbols[idx];
            sumAsc += symbol[0].HasFlag(RoyalMailBarTypes.Ascender) ? 4 : 0;
            sumAsc += symbol[1].HasFlag(RoyalMailBarTypes.Ascender) ? 2 : 0;
            sumAsc += symbol[2].HasFlag(RoyalMailBarTypes.Ascender) ? 1 : 0;
            sumDesc += symbol[0].HasFlag(RoyalMailBarTypes.Descender) ? 4 : 0;
            sumDesc += symbol[1].HasFlag(RoyalMailBarTypes.Descender) ? 2 : 0;
            sumDesc += symbol[2].HasFlag(RoyalMailBarTypes.Descender) ? 1 : 0;
        }

        var chkAsc = sumAsc % 6 == 0 ? 5 : (sumAsc % 6 - 1);
        var chkDesc = sumDesc % 6 == 0 ? 5 : (sumDesc % 6 - 1);
        var expected = chkAsc * 6 + chkDesc;
        return checksumIndex == expected;
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
