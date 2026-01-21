using System;
using System.Collections.Generic;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Decodes GS1 DataBar-14 symbols (Truncated / Omni / Stacked).
/// </summary>
public static class DataBar14Decoder {
    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Truncated symbol from a <see cref="Barcode1D"/>.
    /// </summary>
    public static bool TryDecodeTruncated(Barcode1D barcode, out string content) {
        content = string.Empty;
        if (barcode is null) return false;
        if (!TryExtractWidths(barcode, out var widths)) return false;
        return TryDecodeFromWidths(widths, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Truncated symbol from a module array.
    /// </summary>
    public static bool TryDecodeTruncated(bool[] modules, out string content) {
        content = string.Empty;
        if (modules is null || modules.Length == 0) return false;
        if (!TryExtractWidths(modules, out var widths)) return false;
        return TryDecodeFromWidths(widths, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Omnidirectional symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeOmni(BitMatrix modules, out string content) {
        content = string.Empty;
        if (modules is null || modules.Width <= 0 || modules.Height != 5) return false;
        if (!TryExtractWidthsFromStacked(modules, topRow: 0, bottomRow: 4, out var widths)) return false;
        return TryDecodeFromWidths(widths, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Stacked symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeStacked(BitMatrix modules, out string content) {
        content = string.Empty;
        if (modules is null || modules.Width <= 0 || modules.Height != 3) return false;
        if (!TryExtractWidthsFromStacked(modules, topRow: 0, bottomRow: 2, out var widths)) return false;
        return TryDecodeFromWidths(widths, out content);
    }

    private static bool TryDecodeFromWidths(int[] totalWidths, out string content) {
        content = string.Empty;
        if (totalWidths.Length != 46) return false;
        if (totalWidths[0] != 1 || totalWidths[1] != 1 || totalWidths[44] != 1 || totalWidths[45] != 1) return false;

        if (!TryFindFinder(totalWidths, out var cLeft, out var cRight)) return false;

        var dataWidths = new int[8][];
        for (var i = 0; i < dataWidths.Length; i++) dataWidths[i] = new int[4];

        for (var i = 0; i < 8; i++) {
            dataWidths[i][0] = totalWidths[i + 2];
            dataWidths[i][1] = totalWidths[15 + (7 - i)];
            dataWidths[i][3] = totalWidths[i + 23];
            dataWidths[i][2] = totalWidths[36 + (7 - i)];
        }

        var checksum = 0;
        for (var i = 0; i < 8; i++) {
            checksum += DataBar14Tables.CHECKSUM_WEIGHT[i] * dataWidths[i][0];
            checksum += DataBar14Tables.CHECKSUM_WEIGHT[i + 8] * dataWidths[i][1];
            checksum += DataBar14Tables.CHECKSUM_WEIGHT[i + 16] * dataWidths[i][2];
            checksum += DataBar14Tables.CHECKSUM_WEIGHT[i + 24] * dataWidths[i][3];
        }
        checksum %= 79;
        if (checksum >= 8) checksum++;
        if (checksum >= 72) checksum++;
        if (checksum / 9 != cLeft || checksum % 9 != cRight) return false;

        var dataCharacters = new int[4];
        for (var i = 0; i < 4; i++) {
            if (!TryDecodeDataCharacter(i, dataWidths, out dataCharacters[i])) return false;
        }

        var leftReg = dataCharacters[0] * 1597 + dataCharacters[1];
        var rightReg = dataCharacters[2] * 1597 + dataCharacters[3];
        var accum = leftReg * 4537077L + rightReg;

        if (accum < 0 || accum > 9999999999999L) return false;
        content = accum.ToString().PadLeft(13, '0');
        return true;
    }

    private static bool TryDecodeDataCharacter(int index, int[][] dataWidths, out int dataCharacter) {
        dataCharacter = 0;
        Span<int> odd = stackalloc int[4];
        Span<int> even = stackalloc int[4];
        odd[0] = dataWidths[0][index];
        odd[1] = dataWidths[2][index];
        odd[2] = dataWidths[4][index];
        odd[3] = dataWidths[6][index];
        even[0] = dataWidths[1][index];
        even[1] = dataWidths[3][index];
        even[2] = dataWidths[5][index];
        even[3] = dataWidths[7][index];

        if (index == 0 || index == 2) {
            for (var group = 0; group <= 4; group++) {
                var vOdd = DataBarCommon.GetValue(odd, DataBar14Tables.MODULES_ODD[group], 4, DataBar14Tables.WIDEST_ODD[group], 1);
                var vEven = DataBarCommon.GetValue(even, DataBar14Tables.MODULES_EVEN[group], 4, DataBar14Tables.WIDEST_EVEN[group], 0);
                if (vOdd < 0 || vEven < 0) continue;
                var candidate = vOdd * DataBar14Tables.T_TABLE[group] + vEven + DataBar14Tables.G_SUM_TABLE[group];
                if (candidate >= GroupMin(group) && candidate <= GroupMax(group)) {
                    dataCharacter = candidate;
                    return true;
                }
            }
        } else {
            for (var group = 5; group <= 8; group++) {
                var vOdd = DataBarCommon.GetValue(odd, DataBar14Tables.MODULES_ODD[group], 4, DataBar14Tables.WIDEST_ODD[group], 0);
                var vEven = DataBarCommon.GetValue(even, DataBar14Tables.MODULES_EVEN[group], 4, DataBar14Tables.WIDEST_EVEN[group], 1);
                if (vOdd < 0 || vEven < 0) continue;
                var candidate = vEven * DataBar14Tables.T_TABLE[group] + vOdd + DataBar14Tables.G_SUM_TABLE[group];
                if (candidate >= GroupMin(group) && candidate <= GroupMax(group)) {
                    dataCharacter = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static int GroupMin(int group) => group switch {
        0 => 0,
        1 => 161,
        2 => 961,
        3 => 2015,
        4 => 2715,
        5 => 0,
        6 => 336,
        7 => 1036,
        8 => 1516,
        _ => 0
    };

    private static int GroupMax(int group) => group switch {
        0 => 160,
        1 => 960,
        2 => 2014,
        3 => 2714,
        4 => 2840,
        5 => 335,
        6 => 1035,
        7 => 1515,
        8 => 1596,
        _ => -1
    };

    private static bool TryFindFinder(int[] totalWidths, out int cLeft, out int cRight) {
        cLeft = -1;
        cRight = -1;
        for (var c = 0; c < 9; c++) {
            var match = true;
            for (var i = 0; i < 5; i++) {
                if (totalWidths[10 + i] != DataBar14Tables.FINDER_PATTERN[(5 * c) + i]) { match = false; break; }
            }
            if (match) { cLeft = c; break; }
        }

        for (var c = 0; c < 9; c++) {
            var match = true;
            for (var i = 0; i < 5; i++) {
                if (totalWidths[31 + i] != DataBar14Tables.FINDER_PATTERN[(5 * c) + (4 - i)]) { match = false; break; }
            }
            if (match) { cRight = c; break; }
        }

        return cLeft >= 0 && cRight >= 0;
    }

    private static bool TryExtractWidths(Barcode1D barcode, out int[] widths) {
        widths = Array.Empty<int>();
        if (barcode.Segments.Count != 46) return false;
        if (barcode.Segments[0].IsBar) return false;
        widths = new int[46];
        for (var i = 0; i < 46; i++) {
            widths[i] = barcode.Segments[i].Modules;
        }
        return true;
    }

    private static bool TryExtractWidths(bool[] modules, out int[] widths) {
        widths = Array.Empty<int>();
        if (modules.Length < 2) return false;

        var runs = new List<int>(48);
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

        if (runs.Count == 45 && modules[0]) {
            runs.Insert(0, 1);
        }

        if (runs.Count != 46) return false;
        widths = runs.ToArray();
        return true;
    }

    private static bool TryExtractWidthsFromStacked(BitMatrix modules, int topRow, int bottomRow, out int[] widths) {
        widths = Array.Empty<int>();
        var top = ExtractRow(modules, topRow);
        var bottom = ExtractRow(modules, bottomRow);
        if (top.Length == 0 || bottom.Length == 0) return false;

        var topRuns = RunLengths(top);
        var bottomRuns = RunLengths(bottom);
        if (topRuns.Count != 25 || bottomRuns.Count != 25) return false;

        widths = new int[46];
        for (var i = 0; i < 23; i++) {
            widths[i] = topRuns[i];
        }
        for (var i = 0; i < 23; i++) {
            widths[i + 23] = bottomRuns[i + 2];
        }
        return true;
    }

    private static bool[] ExtractRow(BitMatrix modules, int row) {
        var width = modules.Width;
        var data = new bool[width];
        for (var x = 0; x < width; x++) {
            data[x] = modules[x, row];
        }
        return data;
    }

    private static List<int> RunLengths(bool[] row) {
        var runs = new List<int>(row.Length / 2);
        var current = row[0];
        var count = 1;
        for (var i = 1; i < row.Length; i++) {
            if (row[i] == current) {
                count++;
            } else {
                runs.Add(count);
                current = row[i];
                count = 1;
            }
        }
        runs.Add(count);
        return runs;
    }
}
