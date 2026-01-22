using System;
using System.Collections.Generic;
using System.Globalization;

namespace CodeGlyphX.Pharmacode;

/// <summary>
/// Decodes Pharmacode (two-track) barcodes.
/// </summary>
public static class PharmacodeTwoTrackDecoder {
    /// <summary>
    /// Attempts to decode a Pharmacode (two-track) barcode from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        text = string.Empty;
        if (modules is null) return false;
        if (modules.Width <= 0 || modules.Height < 2) return false;

        var topRow = 0;
        var bottomRow = modules.Height - 1;
        if (!TryFindBarBounds(modules, topRow, bottomRow, out var firstBar, out var lastBar)) return false;

        var runs = BuildRuns(modules, topRow, bottomRow, firstBar, lastBar);
        if (!TryExtractDigits(modules, topRow, bottomRow, runs, out var digits)) return false;
        if (!TryComputeValue(digits, out var value)) return false;

        text = value.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryFindBarBounds(BitMatrix modules, int topRow, int bottomRow, out int firstBar, out int lastBar) {
        firstBar = -1;
        lastBar = -1;
        for (var x = 0; x < modules.Width; x++) {
            if (HasBar(modules, x, topRow, bottomRow)) {
                if (firstBar < 0) firstBar = x;
                lastBar = x;
            }
        }
        return firstBar >= 0 && lastBar >= 0;
    }

    private static List<(bool isBar, int start, int length)> BuildRuns(BitMatrix modules, int topRow, int bottomRow, int firstBar, int lastBar) {
        var runs = new List<(bool isBar, int start, int length)>(modules.Width / 2);
        var current = HasBar(modules, firstBar, topRow, bottomRow);
        var runStart = firstBar;
        for (var x = firstBar + 1; x <= lastBar; x++) {
            var isBar = HasBar(modules, x, topRow, bottomRow);
            if (isBar == current) continue;
            runs.Add((current, runStart, x - runStart));
            current = isBar;
            runStart = x;
        }
        runs.Add((current, runStart, lastBar - runStart + 1));
        return runs;
    }

    private static bool TryExtractDigits(BitMatrix modules, int topRow, int bottomRow, List<(bool isBar, int start, int length)> runs, out List<int> digits) {
        digits = new List<int>(runs.Count / 2 + 1);
        foreach (var run in runs) {
            if (!run.isBar) continue;
            var top = false;
            var bottom = false;
            for (var x = run.start; x < run.start + run.length; x++) {
                if (modules[x, topRow]) top = true;
                if (modules[x, bottomRow]) bottom = true;
            }

            var digit = (bottom ? 1 : 0) + (top ? 2 : 0);
            digits.Add(digit);
        }

        return digits.Count >= PharmacodeTwoTrackEncoder.MinBars && digits.Count <= PharmacodeTwoTrackEncoder.MaxBars;
    }

    private static bool TryComputeValue(List<int> digits, out int value) {
        value = 0;
        var power = 1;
        for (var i = digits.Count - 1; i >= 0; i--) {
            value += digits[i] * power;
            if (value > PharmacodeTwoTrackEncoder.MaxValue) return false;
            if (i > 0) power *= 3;
        }

        return value >= PharmacodeTwoTrackEncoder.MinValue && value <= PharmacodeTwoTrackEncoder.MaxValue;
    }

    private static bool HasBar(BitMatrix modules, int x, int topRow, int bottomRow) {
        return modules[x, topRow] || modules[x, bottomRow];
    }
}
