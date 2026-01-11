using System;

namespace CodeGlyphX.Qr;

internal static class QrTables {
    private const int TableStride = 41; // [eccIndex, version] with version 0 as padding

    // Values from QR Code Model 2 spec (and widely used reference implementations).
    private static readonly int[] EccCodewordsPerBlock = {
        // Low (L)
        -1, 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30,
        // Medium (M)
        -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        // Quartile (Q)
        -1, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30,
        // High (H)
        -1, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30,
    };

    private static readonly int[] NumErrorCorrectionBlocks = {
        // Low (L)
        -1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25,
        // Medium (M)
        -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49,
        // Quartile (Q)
        -1, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68,
        // High (H)
        -1, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81,
    };

    public static int GetNumRawDataModules(int version) {
        if (version is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(version));

        var result = (16 * version + 128) * version + 64;
        if (version >= 2) {
            var numAlign = (version / 7) + 2;
            result -= (25 * numAlign - 10) * numAlign - 55;
            if (version >= 7) result -= 36;
        }
        return result;
    }

    public static int GetNumBlocks(int version, QrErrorCorrectionLevel ecc) {
        var eccIndex = (int)ecc;
        return NumErrorCorrectionBlocks[eccIndex * TableStride + version];
    }

    public static int GetEccCodewordsPerBlock(int version, QrErrorCorrectionLevel ecc) {
        var eccIndex = (int)ecc;
        return EccCodewordsPerBlock[eccIndex * TableStride + version];
    }

    public static int GetNumDataCodewords(int version, QrErrorCorrectionLevel ecc) {
        var rawCodewords = GetNumRawDataModules(version) / 8;
        var numBlocks = GetNumBlocks(version, ecc);
        var eccPerBlock = GetEccCodewordsPerBlock(version, ecc);
        return rawCodewords - (numBlocks * eccPerBlock);
    }

    public static int GetNumericModeCharCountBits(int version) {
        if (version is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(version));
        if (version <= 9) return 10;
        if (version <= 26) return 12;
        return 14;
    }

    public static int GetAlphanumericModeCharCountBits(int version) {
        if (version is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(version));
        if (version <= 9) return 9;
        if (version <= 26) return 11;
        return 13;
    }

    public static int GetByteModeCharCountBits(int version) => version < 10 ? 8 : 16;

    public static int GetKanjiModeCharCountBits(int version) {
        if (version is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(version));
        if (version <= 9) return 8;
        if (version <= 26) return 10;
        return 12;
    }

    public static int GetEccFormatBits(QrErrorCorrectionLevel ecc) {
        return ecc switch {
            QrErrorCorrectionLevel.L => 1,
            QrErrorCorrectionLevel.M => 0,
            QrErrorCorrectionLevel.Q => 3,
            QrErrorCorrectionLevel.H => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(ecc)),
        };
    }

    public static int[] GetAlignmentPatternPositions(int version) {
        if (version is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(version));
        if (version == 1) return Array.Empty<int>();

        var numAlign = (version / 7) + 2;
        var size = (version * 4) + 17;
        var step = version == 32
            ? 26
            : ((version * 4 + numAlign * 2 + 1) / (2 * numAlign - 2)) * 2;

        var result = new int[numAlign];
        result[0] = 6;
        result[numAlign - 1] = size - 7;
        for (var i = 1; i < numAlign - 1; i++) result[i] = result[numAlign - 1] - step * (numAlign - 1 - i);
        return result;
    }

    public static bool GetBit(int value, int bitIndex) => ((value >> bitIndex) & 1) != 0;
}
