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
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length == 0) throw new InvalidOperationException("GS1 DataBar content cannot be empty.");
        if (content.Length > 13) throw new InvalidOperationException("GS1 DataBar expects up to 13 digits (GTIN without check digit)." );

        for (var i = 0; i < content.Length; i++) {
            var ch = content[i];
            if (ch < '0' || ch > '9') throw new InvalidOperationException("GS1 DataBar expects numeric digits only.");
        }

        var accum = BigInteger.Parse(content);

        var leftReg = accum / 4537077;
        var rightReg = accum % 4537077;

        var dataCharacter = new int[4];
        dataCharacter[0] = (int)(leftReg / 1597);
        dataCharacter[1] = (int)(leftReg % 1597);
        dataCharacter[2] = (int)(rightReg / 1597);
        dataCharacter[3] = (int)(rightReg % 1597);

        var dataGroup = new int[4];
        dataGroup[0] = GetGroup0or2(dataCharacter[0]);
        dataGroup[2] = GetGroup0or2(dataCharacter[2]);
        dataGroup[1] = GetGroup1or3(dataCharacter[1]);
        dataGroup[3] = GetGroup1or3(dataCharacter[3]);

        var vOdd = new int[4];
        var vEven = new int[4];

        vOdd[0] = (dataCharacter[0] - DataBar14Tables.G_SUM_TABLE[dataGroup[0]]) / DataBar14Tables.T_TABLE[dataGroup[0]];
        vEven[0] = (dataCharacter[0] - DataBar14Tables.G_SUM_TABLE[dataGroup[0]]) % DataBar14Tables.T_TABLE[dataGroup[0]];
        vOdd[1] = (dataCharacter[1] - DataBar14Tables.G_SUM_TABLE[dataGroup[1]]) % DataBar14Tables.T_TABLE[dataGroup[1]];
        vEven[1] = (dataCharacter[1] - DataBar14Tables.G_SUM_TABLE[dataGroup[1]]) / DataBar14Tables.T_TABLE[dataGroup[1]];
        vOdd[3] = (dataCharacter[3] - DataBar14Tables.G_SUM_TABLE[dataGroup[3]]) % DataBar14Tables.T_TABLE[dataGroup[3]];
        vEven[3] = (dataCharacter[3] - DataBar14Tables.G_SUM_TABLE[dataGroup[3]]) / DataBar14Tables.T_TABLE[dataGroup[3]];
        vOdd[2] = (dataCharacter[2] - DataBar14Tables.G_SUM_TABLE[dataGroup[2]]) / DataBar14Tables.T_TABLE[dataGroup[2]];
        vEven[2] = (dataCharacter[2] - DataBar14Tables.G_SUM_TABLE[dataGroup[2]]) % DataBar14Tables.T_TABLE[dataGroup[2]];

        var dataWidths = new int[8][];
        for (var i = 0; i < dataWidths.Length; i++) dataWidths[i] = new int[4];

        for (var i = 0; i < 4; i++) {
            if (i == 0 || i == 2) {
                var widths = DataBarCommon.GetWidths(vOdd[i], DataBar14Tables.MODULES_ODD[dataGroup[i]], 4, DataBar14Tables.WIDEST_ODD[dataGroup[i]], 1);
                dataWidths[0][i] = widths[0];
                dataWidths[2][i] = widths[1];
                dataWidths[4][i] = widths[2];
                dataWidths[6][i] = widths[3];
                widths = DataBarCommon.GetWidths(vEven[i], DataBar14Tables.MODULES_EVEN[dataGroup[i]], 4, DataBar14Tables.WIDEST_EVEN[dataGroup[i]], 0);
                dataWidths[1][i] = widths[0];
                dataWidths[3][i] = widths[1];
                dataWidths[5][i] = widths[2];
                dataWidths[7][i] = widths[3];
            } else {
                var widths = DataBarCommon.GetWidths(vOdd[i], DataBar14Tables.MODULES_ODD[dataGroup[i]], 4, DataBar14Tables.WIDEST_ODD[dataGroup[i]], 0);
                dataWidths[0][i] = widths[0];
                dataWidths[2][i] = widths[1];
                dataWidths[4][i] = widths[2];
                dataWidths[6][i] = widths[3];
                widths = DataBarCommon.GetWidths(vEven[i], DataBar14Tables.MODULES_EVEN[dataGroup[i]], 4, DataBar14Tables.WIDEST_EVEN[dataGroup[i]], 1);
                dataWidths[1][i] = widths[0];
                dataWidths[3][i] = widths[1];
                dataWidths[5][i] = widths[2];
                dataWidths[7][i] = widths[3];
            }
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
        var cLeft = checksum / 9;
        var cRight = checksum % 9;

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
        for (var i = 0; i < 5; i++) {
            totalWidths[i + 10] = DataBar14Tables.FINDER_PATTERN[i + (5 * cLeft)];
            totalWidths[i + 31] = DataBar14Tables.FINDER_PATTERN[(4 - i) + (5 * cRight)];
        }

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

        var writer = 0;
        var latch = false;
        for (var i = 0; i < 23; i++) {
            for (var j = 0; j < totalWidths[i]; j++) {
                grid[0][writer] = latch;
                writer++;
            }
            latch = !latch;
        }
        grid[0][writer] = true;
        grid[0][writer + 1] = false;

        grid[2][0] = true;
        grid[2][1] = false;
        writer = 0;
        latch = true;
        for (var i = 23; i < 46; i++) {
            for (var j = 0; j < totalWidths[i]; j++) {
                grid[2][writer + 2] = latch;
                writer++;
            }
            latch = !latch;
        }

        for (var i = 1; i < 46; i++) {
            if (grid[0][i] == grid[2][i]) {
                if (!grid[0][i]) {
                    grid[1][i] = true;
                }
            } else {
                if (!grid[1][i - 1]) {
                    grid[1][i] = true;
                }
            }
        }
        for (var i = 0; i < 4; i++) {
            grid[1][i] = false;
        }

        return BuildMatrix(grid);
    }

    private static BitMatrix BuildOmniMatrix(int[] totalWidths) {
        var grid = new bool[5][];
        for (var i = 0; i < grid.Length; i++) grid[i] = new bool[50];

        var writer = 0;
        var latch = false;
        for (var i = 0; i < 23; i++) {
            for (var j = 0; j < totalWidths[i]; j++) {
                grid[0][writer] = latch;
                writer++;
            }
            latch = !latch;
        }
        grid[0][writer] = true;
        grid[0][writer + 1] = false;

        grid[4][0] = true;
        grid[4][1] = false;
        writer = 0;
        latch = true;
        for (var i = 23; i < 46; i++) {
            for (var j = 0; j < totalWidths[i]; j++) {
                grid[4][writer + 2] = latch;
                writer++;
            }
            latch = !latch;
        }

        for (var i = 5; i < 46; i += 2) {
            grid[2][i] = true;
        }

        for (var i = 4; i < 46; i++) {
            if (!grid[0][i]) {
                grid[1][i] = true;
            }
        }
        latch = true;
        for (var i = 17; i < 33; i++) {
            if (!grid[0][i]) {
                if (latch) {
                    grid[1][i] = true;
                    latch = false;
                } else {
                    grid[1][i] = false;
                    latch = true;
                }
            } else {
                grid[1][i] = false;
                latch = true;
            }
        }

        for (var i = 4; i < 46; i++) {
            if (!grid[4][i]) {
                grid[3][i] = true;
            }
        }
        latch = true;
        for (var i = 16; i < 32; i++) {
            if (!grid[4][i]) {
                if (latch) {
                    grid[3][i] = true;
                    latch = false;
                } else {
                    grid[3][i] = false;
                    latch = true;
                }
            } else {
                grid[3][i] = false;
                latch = true;
            }
        }

        return BuildMatrix(grid);
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
