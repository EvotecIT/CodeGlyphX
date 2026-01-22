using System;
using System.Collections.Generic;
using System.Numerics;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Encodes GS1 DataBar-14 symbols (Truncated / Omni / Stacked).
/// </summary>
public static class DataBar14Encoder {
    /// <summary>
    /// Encodes a GS1 DataBar-14 Truncated symbol into a <see cref="Barcode1D"/>.
    /// </summary>
    public static Barcode1D EncodeTruncated(string content) {
        var widths = BuildTotalWidths(content, out _);
        var segments = new List<BarSegment>(widths.Length);
        var isBar = false;
        for (var i = 0; i < widths.Length; i++) {
            segments.Add(new BarSegment(isBar, widths[i]));
            isBar = !isBar;
        }
        return new Barcode1D(segments);
    }

    /// <summary>
    /// Encodes a GS1 DataBar-14 Omnidirectional symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeOmni(string content) {
        var widths = BuildTotalWidths(content, out _);
        return BuildOmniMatrix(widths);
    }

    /// <summary>
    /// Encodes a GS1 DataBar-14 Stacked symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeStacked(string content) {
        var widths = BuildTotalWidths(content, out _);
        return BuildStackedMatrix(widths);
    }

    private static int[] BuildTotalWidths(string content, out int checkDigit) {
        var accum = ParseContent(content);
        var dataCharacters = SplitDataCharacters(accum);
        var dataGroup = BuildDataGroups(dataCharacters);
        var vOdd = new int[4];
        var vEven = new int[4];
        for (var i = 0; i < 4; i++) {
            ComputeOddEven(i, dataCharacters[i], dataGroup[i], vOdd, vEven);
        }
        var dataWidths = BuildDataWidths(vOdd, vEven, dataGroup);
        var totalWidths = BuildTotalWidths(dataWidths, out var cLeft, out var cRight);
        ApplyFinderPattern(totalWidths, cLeft, cRight);
        checkDigit = ComputeCheckDigit(content);
        return totalWidths;
    }

    private static int GetGroup0or2(int value) {
        if (value <= 160) return 0;
        if (value <= 960) return 1;
        if (value <= 2014) return 2;
        if (value <= 2714) return 3;
        return 4;
    }

    private static int GetGroup1or3(int value) {
        if (value <= 335) return 5;
        if (value <= 1035) return 6;
        if (value <= 1515) return 7;
        return 8;
    }

    private static int ComputeCheckDigit(string content) {
        var count = 0;
        var pad = 13 - content.Length;
        for (var i = 0; i < pad; i++) {
            count += 0;
        }
        for (var i = 0; i < content.Length; i++) {
            var digit = content[i] - '0';
            count += digit;
            if (((i + pad) & 1) == 0) {
                count += 2 * digit;
            }
        }
        var check = 10 - (count % 10);
        return check == 10 ? 0 : check;
    }

    private static BitMatrix BuildStackedMatrix(int[] totalWidths) {
        var grid = new bool[3][];
        for (var i = 0; i < grid.Length; i++) grid[i] = new bool[50];

        var topWriter = FillWidthsRow(grid[0], totalWidths, start: 0, end: 23, offset: 0, startLatch: false);
        grid[0][topWriter] = true;
        grid[0][topWriter + 1] = false;

        grid[2][0] = true;
        grid[2][1] = false;
        FillWidthsRow(grid[2], totalWidths, start: 23, end: 46, offset: 2, startLatch: true);

        FillStackedMiddleRow(grid[0], grid[2], grid[1]);

        return BuildMatrix(grid);
    }

    private static BitMatrix BuildOmniMatrix(int[] totalWidths) {
        var grid = new bool[5][];
        for (var i = 0; i < grid.Length; i++) grid[i] = new bool[50];

        var topWriter = FillWidthsRow(grid[0], totalWidths, start: 0, end: 23, offset: 0, startLatch: false);
        grid[0][topWriter] = true;
        grid[0][topWriter + 1] = false;

        grid[4][0] = true;
        grid[4][1] = false;
        FillWidthsRow(grid[4], totalWidths, start: 23, end: 46, offset: 2, startLatch: true);

        FillGuardRow(grid[2], start: 5, end: 46, step: 2);
        FillSolidFromRow(grid[0], grid[1], start: 4, end: 46);
        ApplyLatchPattern(grid[0], grid[1], start: 17, end: 33, startLatch: true);

        FillSolidFromRow(grid[4], grid[3], start: 4, end: 46);
        ApplyLatchPattern(grid[4], grid[3], start: 16, end: 32, startLatch: true);

        return BuildMatrix(grid);
    }

    private static BigInteger ParseContent(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length == 0) throw new InvalidOperationException("GS1 DataBar content cannot be empty.");
        if (content.Length > 13) throw new InvalidOperationException("GS1 DataBar expects up to 13 digits (GTIN without check digit).");
        for (var i = 0; i < content.Length; i++) {
            var ch = content[i];
            if (ch < '0' || ch > '9') throw new InvalidOperationException("GS1 DataBar expects numeric digits only.");
        }
        return BigInteger.Parse(content);
    }

    private static int[] SplitDataCharacters(BigInteger accum) {
        var leftReg = accum / 4537077;
        var rightReg = accum % 4537077;
        return new[] {
            (int)(leftReg / 1597),
            (int)(leftReg % 1597),
            (int)(rightReg / 1597),
            (int)(rightReg % 1597)
        };
    }

    private static int[] BuildDataGroups(int[] dataCharacters) {
        return new[] {
            GetGroup0or2(dataCharacters[0]),
            GetGroup1or3(dataCharacters[1]),
            GetGroup0or2(dataCharacters[2]),
            GetGroup1or3(dataCharacters[3])
        };
    }

    private static void ComputeOddEven(int index, int dataCharacter, int dataGroup, int[] vOdd, int[] vEven) {
        var baseValue = dataCharacter - DataBar14Tables.G_SUM_TABLE[dataGroup];
        var divisor = DataBar14Tables.T_TABLE[dataGroup];
        if (index == 0 || index == 2) {
            vOdd[index] = baseValue / divisor;
            vEven[index] = baseValue % divisor;
        } else {
            vOdd[index] = baseValue % divisor;
            vEven[index] = baseValue / divisor;
        }
    }

    private static int[][] BuildDataWidths(int[] vOdd, int[] vEven, int[] dataGroup) {
        var dataWidths = new int[8][];
        for (var i = 0; i < dataWidths.Length; i++) dataWidths[i] = new int[4];
        for (var i = 0; i < 4; i++) {
            FillDataWidthsForIndex(i, vOdd, vEven, dataGroup, dataWidths);
        }
        return dataWidths;
    }

    private static void FillDataWidthsForIndex(int index, int[] vOdd, int[] vEven, int[] dataGroup, int[][] dataWidths) {
        var oddNoNarrow = (index == 0 || index == 2) ? 1 : 0;
        var evenNoNarrow = oddNoNarrow == 1 ? 0 : 1;
        var oddWidths = DataBarCommon.GetWidths(vOdd[index], DataBar14Tables.MODULES_ODD[dataGroup[index]], 4, DataBar14Tables.WIDEST_ODD[dataGroup[index]], oddNoNarrow);
        SetWidths(dataWidths, index, oddWidths, rowStart: 0);
        var evenWidths = DataBarCommon.GetWidths(vEven[index], DataBar14Tables.MODULES_EVEN[dataGroup[index]], 4, DataBar14Tables.WIDEST_EVEN[dataGroup[index]], evenNoNarrow);
        SetWidths(dataWidths, index, evenWidths, rowStart: 1);
    }

    private static void SetWidths(int[][] dataWidths, int index, int[] widths, int rowStart) {
        dataWidths[rowStart + 0][index] = widths[0];
        dataWidths[rowStart + 2][index] = widths[1];
        dataWidths[rowStart + 4][index] = widths[2];
        dataWidths[rowStart + 6][index] = widths[3];
    }

    private static int[] BuildTotalWidths(int[][] dataWidths, out int cLeft, out int cRight) {
        var checksum = ComputeChecksum(dataWidths);
        cLeft = checksum / 9;
        cRight = checksum % 9;

        var totalWidths = new int[46];
        totalWidths[0] = 1;
        totalWidths[1] = 1;
        totalWidths[44] = 1;
        totalWidths[45] = 1;
        for (var i = 0; i < 8; i++) {
            totalWidths[i + 2] = dataWidths[i][0];
            totalWidths[i + 15] = dataWidths[7 - i][1];
            totalWidths[i + 23] = dataWidths[i][3];
            totalWidths[i + 36] = dataWidths[7 - i][2];
        }
        return totalWidths;
    }

    private static int ComputeChecksum(int[][] dataWidths) {
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
        return checksum;
    }

    private static void ApplyFinderPattern(int[] totalWidths, int cLeft, int cRight) {
        for (var i = 0; i < 5; i++) {
            totalWidths[i + 10] = DataBar14Tables.FINDER_PATTERN[i + (5 * cLeft)];
            totalWidths[i + 31] = DataBar14Tables.FINDER_PATTERN[(4 - i) + (5 * cRight)];
        }
    }

    private static int FillWidthsRow(bool[] row, int[] totalWidths, int start, int end, int offset, bool startLatch) {
        var writer = 0;
        var latch = startLatch;
        for (var i = start; i < end; i++) {
            for (var j = 0; j < totalWidths[i]; j++) {
                row[writer + offset] = latch;
                writer++;
            }
            latch = !latch;
        }
        return writer;
    }

    private static void FillStackedMiddleRow(bool[] top, bool[] bottom, bool[] middle) {
        for (var i = 1; i < 46; i++) {
            if (top[i] == bottom[i]) {
                if (!top[i]) middle[i] = true;
            } else if (!middle[i - 1]) {
                middle[i] = true;
            }
        }
        for (var i = 0; i < 4; i++) {
            middle[i] = false;
        }
    }

    private static void FillGuardRow(bool[] row, int start, int end, int step) {
        for (var i = start; i < end; i += step) {
            row[i] = true;
        }
    }

    private static void FillSolidFromRow(bool[] source, bool[] target, int start, int end) {
        for (var i = start; i < end; i++) {
            if (!source[i]) target[i] = true;
        }
    }

    private static void ApplyLatchPattern(bool[] source, bool[] target, int start, int end, bool startLatch) {
        var latch = startLatch;
        for (var i = start; i < end; i++) {
            if (!source[i]) {
                target[i] = latch;
                latch = !latch;
            } else {
                target[i] = false;
                latch = true;
            }
        }
    }

    private static BitMatrix BuildMatrix(bool[][] grid) {
        var height = grid.Length;
        var width = grid[0].Length;
        var matrix = new BitMatrix(width, height);
        for (var y = 0; y < height; y++) {
            var row = grid[y];
            for (var x = 0; x < width; x++) {
                if (row[x]) matrix[x, y] = true;
            }
        }
        return matrix;
    }
}
