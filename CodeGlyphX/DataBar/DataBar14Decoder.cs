using System;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Decodes GS1 DataBar-14 symbols (Truncated / Omnidirectional / Stacked / Stacked Omnidirectional).
/// </summary>
public static partial class DataBar14Decoder {
    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Truncated symbol from a <see cref="Barcode1D">Barcode1D</see>.
    /// </summary>
    public static bool TryDecodeTruncated(Barcode1D barcode, out string content) {
        content = string.Empty;
        if (barcode is null || barcode.Segments.Count != 46 || barcode.Segments[0].IsBar) return false;
        Span<int> widths = stackalloc int[46];
        for (var i = 0; i < widths.Length; i++) widths[i] = barcode.Segments[i].Modules;
        return TryDecodeFromWidths(widths, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Truncated symbol from a module array.
    /// </summary>
    public static bool TryDecodeTruncated(bool[] modules, out string content) {
        content = string.Empty;
        if (modules is null || modules.Length == 0) return false;
        if (modules.Length < 2) return false;
        Span<int> widths = stackalloc int[46];
        if (!DataBarModuleRuns.TryRead(modules, widths, out var count)) return false;
        // Complete run sequences retain their existing polarity-independent interpretation.
        // Only a 45-run dark-starting sequence has a missing leading light guard.
        if (count == 45 && modules[0]) {
            widths.Slice(0, 45).CopyTo(widths.Slice(1));
            widths[0] = 1;
        } else if (count != 46) return false;
        return TryDecodeFromWidths(widths, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Omnidirectional symbol from a <see cref="Barcode1D">Barcode1D</see>.
    /// </summary>
    public static bool TryDecodeOmnidirectional(Barcode1D barcode, out string content) {
        return TryDecodeTruncated(barcode, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Omnidirectional symbol from a module array.
    /// </summary>
    public static bool TryDecodeOmnidirectional(bool[] modules, out string content) {
        return TryDecodeTruncated(modules, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Stacked Omnidirectional symbol from a <see cref="BitMatrix">BitMatrix</see>.
    /// </summary>
    public static bool TryDecodeStackedOmnidirectional(BitMatrix modules, out string content) {
        content = string.Empty;
        if (modules is null || modules.Width <= 0 || modules.Height != 5) return false;
        Span<int> widths = stackalloc int[46];
        if (!TryExtractWidthsFromStacked(modules, topRow: 0, bottomRow: 4, widths)) return false;
        return TryDecodeFromWidths(widths, out content);
    }

    /// <summary>
    /// Compatibility alias for <see cref="TryDecodeStackedOmnidirectional"/>.
    /// </summary>
    /// <remarks>
    /// Earlier CodeGlyphX releases called the five-row Stacked Omnidirectional symbol "Omni".
    /// </remarks>
    public static bool TryDecodeOmni(BitMatrix modules, out string content) {
        return TryDecodeStackedOmnidirectional(modules, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Stacked symbol from a <see cref="BitMatrix">BitMatrix</see>.
    /// </summary>
    public static bool TryDecodeStacked(BitMatrix modules, out string content) {
        content = string.Empty;
        if (modules is null || modules.Width <= 0 || modules.Height != 3) return false;
        Span<int> widths = stackalloc int[46];
        if (!TryExtractWidthsFromStacked(modules, topRow: 0, bottomRow: 2, widths)) return false;
        return TryDecodeFromWidths(widths, out content);
    }

    internal static bool TryDecodeComposite(BitMatrix modules, out string content) {
        content = string.Empty;
        Span<int> widths = stackalloc int[46];
        if (modules.Height == 1) {
            if (modules.Width != 96) return false;
            var row = new bool[96];
            for (var x = 0; x < row.Length; x++) row[x] = modules[x, 0];
            if (!DataBarModuleRuns.TryRead(row, widths, out var count) || count != 46) return false;
        } else if (modules.Height == 3 || modules.Height == 5) {
            if (!TryExtractWidthsFromStacked(modules, 0, modules.Height - 1, widths)) return false;
        } else return false;
        return TryDecodeFromWidths(widths, out content, linked: true);
    }

    private static bool TryDecodeFromWidths(ReadOnlySpan<int> totalWidths, out string content, bool linked = false) {
        content = string.Empty;
        if (!HasValidGuards(totalWidths)) return false;

        if (!TryFindFinder(totalWidths, out var cLeft, out var cRight)) return false;

        Span<int> dataWidths = stackalloc int[32];
        BuildDataWidths(totalWidths, dataWidths);
        if (!HasValidChecksum(dataWidths, cLeft, cRight)) return false;

        Span<int> dataCharacters = stackalloc int[4];
        if (!TryDecodeDataCharacters(dataWidths, dataCharacters)) return false;

        if (!TryBuildContent(dataCharacters, out content, linked)) return false;
        return true;
    }

    private static bool TryDecodeDataCharacter(int index, ReadOnlySpan<int> dataWidths, out int dataCharacter) {
        dataCharacter = 0;
        Span<int> odd = stackalloc int[4];
        Span<int> even = stackalloc int[4];
        for (var i = 0; i < 4; i++) {
            odd[i] = dataWidths[i * 8 + index];
            even[i] = dataWidths[i * 8 + 4 + index];
        }

        if (index == 0 || index == 2) {
            return TryDecodeDataCharacterGroups(odd, even, 0, 4, swapEvenOdd: false, out dataCharacter);
        }

        return TryDecodeDataCharacterGroups(odd, even, 5, 8, swapEvenOdd: true, out dataCharacter);
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

    private static bool TryFindFinder(ReadOnlySpan<int> totalWidths, out int cLeft, out int cRight) {
        cLeft = FindFinder(totalWidths, offset: 10, reverse: false);
        cRight = FindFinder(totalWidths, offset: 31, reverse: true);
        return cLeft >= 0 && cRight >= 0;
    }

    private static bool HasValidGuards(ReadOnlySpan<int> totalWidths) {
        return totalWidths.Length == 46
            && totalWidths[0] == 1
            && totalWidths[1] == 1
            && totalWidths[44] == 1
            && totalWidths[45] == 1;
    }

    private static void BuildDataWidths(ReadOnlySpan<int> totalWidths, Span<int> dataWidths) {
        // Eight element widths, each containing the same four physical characters as the original table.
        for (var i = 0; i < 8; i++) {
            dataWidths[i * 4] = totalWidths[i + 2];
            dataWidths[i * 4 + 1] = totalWidths[15 + (7 - i)];
            dataWidths[i * 4 + 3] = totalWidths[i + 23];
            dataWidths[i * 4 + 2] = totalWidths[36 + (7 - i)];
        }
    }

    private static bool HasValidChecksum(ReadOnlySpan<int> dataWidths, int cLeft, int cRight) {
        var checksum = ComputeChecksum(dataWidths);
        return checksum / 9 == cLeft && checksum % 9 == cRight;
    }

    private static int ComputeChecksum(ReadOnlySpan<int> dataWidths) {
        var checksum = 0;
        for (var i = 0; i < 8; i++) {
            checksum += DataBar14Tables.CHECKSUM_WEIGHT[i] * dataWidths[i * 4];
            checksum += DataBar14Tables.CHECKSUM_WEIGHT[i + 8] * dataWidths[i * 4 + 1];
            checksum += DataBar14Tables.CHECKSUM_WEIGHT[i + 16] * dataWidths[i * 4 + 2];
            checksum += DataBar14Tables.CHECKSUM_WEIGHT[i + 24] * dataWidths[i * 4 + 3];
        }
        checksum %= 79;
        if (checksum >= 8) checksum++;
        if (checksum >= 72) checksum++;
        return checksum;
    }

    private static bool TryDecodeDataCharacters(ReadOnlySpan<int> dataWidths, Span<int> dataCharacters) {
        for (var i = 0; i < 4; i++) {
            if (!TryDecodeDataCharacter(i, dataWidths, out dataCharacters[i])) return false;
        }
        return true;
    }

    private static bool TryBuildContent(ReadOnlySpan<int> dataCharacters, out string content, bool linked = false) {
        content = string.Empty;
        var leftReg = dataCharacters[0] * 1597 + dataCharacters[1];
        var rightReg = dataCharacters[2] * 1597 + dataCharacters[3];
        var accum = leftReg * 4537077L + rightReg;
        if (linked) accum -= 10000000000000L;
        if (accum < 0 || accum > 9999999999999L) return false;
        content = accum.ToString().PadLeft(13, '0');
        return true;
    }

    private static bool TryDecodeDataCharacterGroups(Span<int> odd, Span<int> even, int startGroup, int endGroup, bool swapEvenOdd, out int dataCharacter) {
        dataCharacter = 0;
        for (var group = startGroup; group <= endGroup; group++) {
            var vOdd = DataBarCommon.GetValue(odd, DataBar14Tables.MODULES_ODD[group], 4, DataBar14Tables.WIDEST_ODD[group], swapEvenOdd ? 0 : 1);
            var vEven = DataBarCommon.GetValue(even, DataBar14Tables.MODULES_EVEN[group], 4, DataBar14Tables.WIDEST_EVEN[group], swapEvenOdd ? 1 : 0);
            if (vOdd < 0 || vEven < 0) continue;
            var candidate = swapEvenOdd
                ? vEven * DataBar14Tables.T_TABLE[group] + vOdd + DataBar14Tables.G_SUM_TABLE[group]
                : vOdd * DataBar14Tables.T_TABLE[group] + vEven + DataBar14Tables.G_SUM_TABLE[group];
            if (candidate < GroupMin(group) || candidate > GroupMax(group)) continue;
            dataCharacter = candidate;
            return true;
        }
        return false;
    }

    private static int FindFinder(ReadOnlySpan<int> totalWidths, int offset, bool reverse) {
        for (var c = 0; c < 9; c++) {
            if (FinderMatches(totalWidths, offset, c, reverse)) return c;
        }
        return -1;
    }

    private static bool FinderMatches(ReadOnlySpan<int> totalWidths, int offset, int c, bool reverse) {
        var baseIndex = 5 * c;
        for (var i = 0; i < 5; i++) {
            var idx = reverse ? baseIndex + (4 - i) : baseIndex + i;
            if (totalWidths[offset + i] != DataBar14Tables.FINDER_PATTERN[idx]) return false;
        }
        return true;
    }

    private static bool TryExtractWidthsFromStacked(BitMatrix modules, int topRow, int bottomRow, Span<int> widths) {
        var top = ExtractRow(modules, topRow);
        var bottom = ExtractRow(modules, bottomRow);
        if (top.Length == 0 || bottom.Length == 0) return false;
        Span<int> topRuns = stackalloc int[25];
        Span<int> bottomRuns = stackalloc int[25];
        if (!DataBarModuleRuns.TryRead(top, topRuns, out var topCount) || topCount != 25
            || !DataBarModuleRuns.TryRead(bottom, bottomRuns, out var bottomCount) || bottomCount != 25) return false;
        topRuns.Slice(0, 23).CopyTo(widths);
        bottomRuns.Slice(2, 23).CopyTo(widths.Slice(23));
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

}
