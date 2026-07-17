// Portions adapted from the Zint backend and ZXing-C++.
// Licensed under BSD-3-Clause and Apache-2.0; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Decodes GS1 DataBar Limited symbols from exact module data.
/// </summary>
public static class DataBarLimitedDecoder {
    /// <summary>
    /// Attempts to decode a GS1 DataBar Limited symbol from a <see cref="Barcode1D"/>.
    /// </summary>
    public static bool TryDecode(Barcode1D barcode, out string content) {
        content = string.Empty;
        if (barcode is null || barcode.Segments.Count != 47 || barcode.Segments[0].IsBar) return false;
        var widths = new int[47];
        for (var i = 0; i < widths.Length; i++) widths[i] = barcode.Segments[i].Modules;
        return TryDecodeWidths(widths, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar Limited symbol from a module array.
    /// </summary>
    public static bool TryDecode(bool[] modules, out string content) {
        content = string.Empty;
        if (modules is null || modules.Length == 0) return false;
        if (!TryExtractWidths(modules, out var widths)) return false;
        return TryDecodeWidths(widths, out content);
    }

    private static bool TryDecodeWidths(int[] widths, out string content) {
        content = string.Empty;
        if (widths.Length != 47 || widths[0] != 1 || widths[1] != 1 ||
            widths[44] != 1 || widths[45] != 1 || widths[46] != 5) return false;

        var leftWidths = new ReadOnlySpan<int>(widths, 2, 14);
        var finderWidths = new ReadOnlySpan<int>(widths, 16, 14);
        var rightWidths = new ReadOnlySpan<int>(widths, 30, 14);
        var finderPattern = WidthsToPattern(finderWidths);
        if (finderPattern < 0) return false;
        var checksum = Array.IndexOf(DataBarLimitedTables.CheckPatterns, finderPattern);
        if (checksum < 0) return false;

        if (!TryDecodePair(leftWidths, out var leftValue) || !TryDecodePair(rightWidths, out var rightValue)) return false;
        var calculated = (DataBarLimitedEncoder.ComputePairChecksum(leftWidths) +
                          20 * DataBarLimitedEncoder.ComputePairChecksum(rightWidths)) % 89;
        if (calculated != checksum) return false;

        var symbolValue = (long)leftValue * DataBarLimitedTables.PairDivisor + rightValue;
        if (symbolValue < 0 || symbolValue > DataBarLimitedTables.MaximumSymbolValue) return false;
        content = symbolValue.ToString().PadLeft(13, '0');
        return true;
    }

    private static bool TryDecodePair(ReadOnlySpan<int> widths, out int pairValue) {
        pairValue = 0;
        Span<int> odd = stackalloc int[7];
        Span<int> even = stackalloc int[7];
        var oddModules = 0;
        for (var i = 0; i < 7; i++) {
            odd[i] = widths[i * 2];
            even[i] = widths[i * 2 + 1];
            oddModules += odd[i];
        }

        var group = Array.IndexOf(DataBarLimitedTables.OddModules, oddModules);
        if (group < 0) return false;
        var oddValue = DataBarCommon.GetValue(odd, oddModules, 7, DataBarLimitedTables.OddWidest[group], noNarrow: 1);
        var evenValue = DataBarCommon.GetValue(even, 26 - oddModules, 7, 9 - DataBarLimitedTables.OddWidest[group], noNarrow: 0);
        if (oddValue < 0 || evenValue < 0) return false;

        pairValue = oddValue * DataBarLimitedTables.EvenTable[group] + evenValue + DataBarLimitedTables.GroupSum[group];
        var upperExclusive = group + 1 < DataBarLimitedTables.GroupSum.Length
            ? DataBarLimitedTables.GroupSum[group + 1]
            : DataBarLimitedTables.MaximumPairValue + 1;
        return pairValue >= DataBarLimitedTables.GroupSum[group] && pairValue < upperExclusive;
    }

    private static int WidthsToPattern(ReadOnlySpan<int> widths) {
        var pattern = 0;
        var modules = 0;
        var isBar = true;
        for (var i = 0; i < widths.Length; i++) {
            if (widths[i] <= 0) return -1;
            modules += widths[i];
            for (var j = 0; j < widths[i]; j++) {
                pattern = (pattern << 1) | (isBar ? 1 : 0);
            }
            isBar = !isBar;
        }
        return modules == 18 ? pattern : -1;
    }

    private static bool TryExtractWidths(bool[] modules, out int[] widths) {
        widths = Array.Empty<int>();
        var runs = new List<int>(47);
        var current = modules[0];
        var count = 1;
        for (var i = 1; i < modules.Length; i++) {
            if (modules[i] == current) {
                count++;
            } else {
                runs.Add(count);
                current = modules[i];
                count = 1;
            }
        }
        runs.Add(count);

        // BarcodeDecoder trims quiet space runs before dispatching. Restore the fixed Limited guards.
        if (modules[0]) runs.Insert(0, 1);
        if (modules[modules.Length - 1]) runs.Add(5);
        if (runs.Count != 47) return false;
        widths = runs.ToArray();
        return true;
    }
}
