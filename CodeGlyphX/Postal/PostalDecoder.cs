using System;
using System.Collections.Generic;
using System.Globalization;

namespace CodeGlyphX.Postal;

/// <summary>
/// Decodes POSTNET barcodes.
/// </summary>
public static class PostnetDecoder {
    /// <summary>
    /// Attempts to decode a POSTNET barcode from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        return PostalDecoder.TryDecode(modules, invert: false, out text);
    }
}

/// <summary>
/// Decodes PLANET barcodes.
/// </summary>
public static class PlanetDecoder {
    /// <summary>
    /// Attempts to decode a PLANET barcode from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        return PostalDecoder.TryDecode(modules, invert: true, out text);
    }
}

internal static class PostalDecoder {
    internal static bool TryDecode(BitMatrix modules, bool invert, out string text) {
        text = string.Empty;
        if (modules is null) return false;
        if (modules.Width <= 0 || modules.Height < 2) return false;

        var topRow = 0;
        var bottomRow = modules.Height - 1;

        var firstBar = -1;
        var lastBar = -1;
        for (var x = 0; x < modules.Width; x++) {
            if (modules[x, bottomRow]) {
                if (firstBar < 0) firstBar = x;
                lastBar = x;
            }
        }

        if (firstBar < 0 || lastBar < 0) return false;

        var barRuns = new List<(bool isBar, int start, int length)>(modules.Width / 2);
        var current = modules[firstBar, bottomRow];
        var runStart = firstBar;
        for (var x = firstBar + 1; x <= lastBar; x++) {
            var isBar = modules[x, bottomRow];
            if (isBar == current) continue;
            barRuns.Add((current, runStart, x - runStart));
            current = isBar;
            runStart = x;
        }
        barRuns.Add((current, runStart, lastBar - runStart + 1));

        var bars = new List<(bool tall, int length)>(barRuns.Count / 2 + 1);
        foreach (var run in barRuns) {
            if (!run.isBar) continue;
            var tall = false;
            for (var x = run.start; x < run.start + run.length; x++) {
                if (modules[x, topRow]) { tall = true; break; }
            }
            bars.Add((tall, run.length));
        }

        if (bars.Count < 7) return false;
        if ((bars.Count - 2) % 5 != 0) return false;
        if (!bars[0].tall || !bars[bars.Count - 1].tall) return false;

        var digitCount = (bars.Count - 2) / 5;
        var digits = new char[digitCount];
        var idx = 1;
        for (var d = 0; d < digitCount; d++) {
            var sum = 0;
            for (var i = 0; i < 5; i++) {
                var isTall = bars[idx++].tall;
                var tall = invert ? !isTall : isTall;
                if (tall) sum += PostalTables.Weights[i];
            }
            var digit = sum == 11 ? 0 : sum;
            if ((uint)digit > 9) return false;
            digits[d] = (char)('0' + digit);
        }

        if (digits.Length < 2) return false;
        var checksum = digits[digits.Length - 1] - '0';
        var expected = PostalTables.CalcChecksum(digits.AsSpan(0, digits.Length - 1));
        if (expected < 0 || checksum != expected) return false;

        text = new string(digits);
        return true;
    }
}
