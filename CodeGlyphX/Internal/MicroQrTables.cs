using System;

namespace CodeGlyphX.Internal;

internal static class MicroQrTables {
    private static readonly int[] Widths = { 0, 11, 13, 15, 17 };
    private static readonly int[,] EccCodewords = {
        { 0, 0, 0, 0 },
        { 2, 0, 0, 0 },
        { 5, 6, 0, 0 },
        { 6, 8, 0, 0 },
        { 8, 10, 14, 0 },
    };

    private static readonly int[,] LengthIndicatorBits = {
        { 3, 4, 5, 6 }, // Numeric
        { 0, 3, 4, 5 }, // Alphanumeric
        { 0, 0, 4, 5 }, // Byte
        { 0, 0, 3, 4 }, // Kanji
    };

    private static readonly int[,] TypeTable = {
        { -1, -1, -1 },
        {  0, -1, -1 },
        {  1,  2, -1 },
        {  3,  4, -1 },
        {  5,  6,  7 },
    };

    private static readonly int[,] FormatInfo = {
        { 0x4445, 0x55ae, 0x6793, 0x7678, 0x06de, 0x1735, 0x2508, 0x34e3 },
        { 0x4172, 0x5099, 0x62a4, 0x734f, 0x03e9, 0x1202, 0x203f, 0x31d4 },
        { 0x4e2b, 0x5fc0, 0x6dfd, 0x7c16, 0x0cb0, 0x1d5b, 0x2f66, 0x3e8d },
        { 0x4b1c, 0x5af7, 0x68ca, 0x7921, 0x0987, 0x186c, 0x2a51, 0x3bba },
    };

    internal static int GetWidth(int version) {
        if (version is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(version));
        return Widths[version];
    }

    internal static int GetEccLength(int version, QrErrorCorrectionLevel ecc) {
        if (version is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(version));
        var level = (int)ecc;
        if (level is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(ecc));
        return EccCodewords[version, level];
    }

    internal static bool IsSupported(int version, QrErrorCorrectionLevel ecc) {
        if (version is < 1 or > 4) return false;
        if (ecc == QrErrorCorrectionLevel.H) return false;
        return GetEccLength(version, ecc) > 0;
    }

    internal static int GetDataLengthBits(int version, QrErrorCorrectionLevel ecc) {
        var eccLen = GetEccLength(version, ecc);
        if (eccLen == 0) return 0;
        var w = GetWidth(version) - 1;
        return w * w - 64 - eccLen * 8;
    }

    internal static int GetDataLengthBytes(int version, QrErrorCorrectionLevel ecc) {
        var bits = GetDataLengthBits(version, ecc);
        if (bits == 0) return 0;
        return (bits + 4) / 8;
    }

    internal static int GetLengthIndicatorBits(MicroQrMode mode, int version) {
        if (version is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(version));
        return LengthIndicatorBits[(int)mode, version - 1];
    }

    internal static int GetModeIndicatorBits(int version) => version - 1;

    internal static int GetTerminatorBits(int version) => version * 2 + 1;

    internal static int GetFormatInfo(int mask, int version, QrErrorCorrectionLevel ecc) {
        if (mask is < 0 or > 3) return 0;
        if (version is < 1 or > 4) return 0;
        if (ecc == QrErrorCorrectionLevel.H) return 0;
        var level = (int)ecc;
        if (level is < 0 or > 2) return 0;
        var type = TypeTable[version, level];
        if (type < 0) return 0;
        return FormatInfo[mask, type];
    }
}
