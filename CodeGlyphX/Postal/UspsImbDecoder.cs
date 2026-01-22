using System;
using System.Collections.Generic;
using System.Numerics;

namespace CodeGlyphX.Postal;

/// <summary>
/// Decodes USPS Intelligent Mail Barcodes (IMB).
/// </summary>
public static class UspsImbDecoder {
    /// <summary>
    /// Attempts to decode a USPS IMB symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        text = string.Empty;
        if (modules is null) return false;
        if (modules.Width <= 0 || modules.Height != UspsImbTables.BarcodeHeight) return false;

        if (!TryExtractBars(modules, out var bars)) return false;
        if (bars.Count != 65) return false;

        var barMap = new bool[130];
        for (var i = 0; i < 65; i++) {
            var bar = bars[i];
            barMap[i] = bar is BarType.Descender or BarType.Full;
            barMap[i + 65] = bar is BarType.Ascender or BarType.Full;
        }

        var characters = new int[10];
        for (var i = 0; i < 10; i++) {
            var value = 0;
            for (var j = 0; j < 13; j++) {
                var idx = UspsImbTables.AppxDIV[(13 * i) + j] - 1;
                if (barMap[idx]) value |= 1 << j;
            }
            characters[i] = value;
        }

        var codewords = new int[10];
        var crcLowBits = 0;
        for (var i = 0; i < 10; i++) {
            var cw = UspsImbTables.CharToCodeword[characters[i]];
            if (cw >= 0) {
                codewords[i] = cw;
                continue;
            }

            var complement = 0x1FFF - characters[i];
            cw = UspsImbTables.CharToCodeword[complement];
            if (cw < 0) return false;
            codewords[i] = cw;
            crcLowBits |= 1 << i;
        }

        if (TryDecodeWithCrc(codewords, crcLowBits, false, out text)) return true;
        if (TryDecodeWithCrc(codewords, crcLowBits, true, out text)) return true;

        text = string.Empty;
        return false;
    }

    private static bool TryDecodeWithCrc(int[] codewords, int crcLowBits, bool crcHighBit, out string text) {
        text = string.Empty;

        var codeword0 = codewords[0];
        if (crcHighBit) {
            codeword0 -= 659;
            if (codeword0 < 0 || codeword0 > 1364) return false;
        }

        var codeword9 = codewords[9];
        if ((codeword9 & 1) != 0) return false;
        codeword9 /= 2;
        if (codeword9 < 0 || codeword9 >= 636) return false;

        BigInteger accum = 0;
        accum = accum * 1365 + codeword0;
        for (var i = 1; i < 9; i++) {
            accum = accum * 1365 + codewords[i];
        }
        accum = accum * 636 + codeword9;

        var crc = UspsImbHelpers.ComputeCrc(accum);
        if ((crc & 0x3FF) != crcLowBits) return false;
        if ((crc & 0x400) != (crcHighBit ? 0x400 : 0)) return false;

        if (!UspsImbHelpers.TryDecodeAccum(accum, out var tracker, out var zip)) return false;
        text = zip.Length == 0 ? tracker : $"{tracker}-{zip}";
        return true;
    }

    private static bool TryExtractBars(BitMatrix modules, out List<BarType> bars) {
        bars = new List<BarType>(65);
        var height = modules.Height;

        var first = -1;
        var last = -1;
        for (var x = 0; x < modules.Width; x++) {
            if (HasBar(modules, x)) {
                if (first < 0) first = x;
                last = x;
            }
        }

        if (first < 0 || last < 0) return false;

        var runs = new List<(bool isBar, int start, int length)>(modules.Width / 2);
        var current = HasBar(modules, first);
        var runStart = first;
        for (var x = first + 1; x <= last; x++) {
            var isBar = HasBar(modules, x);
            if (isBar == current) continue;
            runs.Add((current, runStart, x - runStart));
            current = isBar;
            runStart = x;
        }
        runs.Add((current, runStart, last - runStart + 1));

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

            if (!tracker) return false;

            if (asc && desc) {
                bars.Add(BarType.Full);
            } else if (asc) {
                bars.Add(BarType.Ascender);
            } else if (desc) {
                bars.Add(BarType.Descender);
            } else {
                bars.Add(BarType.Tracker);
            }
        }

        return true;
    }

    private static bool HasBar(BitMatrix modules, int x) {
        for (var y = 0; y < modules.Height; y++) {
            if (modules[x, y]) return true;
        }
        return false;
    }

    private enum BarType {
        Tracker,
        Ascender,
        Descender,
        Full
    }
}
