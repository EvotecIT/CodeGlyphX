// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;

namespace CodeGlyphX.RmQr;

/// <summary>
/// ISO/IEC 23941:2022 rMQR symbol tables. Values are indexed by version 1..32.
/// </summary>
internal static class RmQrTables {
    private static readonly byte[] Heights = {
        7, 7, 7, 7, 7,
        9, 9, 9, 9, 9,
        11, 11, 11, 11, 11, 11,
        13, 13, 13, 13, 13, 13,
        15, 15, 15, 15, 15,
        17, 17, 17, 17, 17
    };

    private static readonly byte[] Widths = {
        43, 59, 77, 99, 139,
        43, 59, 77, 99, 139,
        27, 43, 59, 77, 99, 139,
        27, 43, 59, 77, 99, 139,
        43, 59, 77, 99, 139,
        43, 59, 77, 99, 139
    };

    private static readonly byte[][] DataCodewords = {
        new byte[] {
            6, 12, 20, 28, 44,
            12, 21, 31, 42, 63,
            7, 19, 31, 43, 57, 84,
            12, 27, 38, 53, 73, 106,
            33, 48, 67, 88, 127,
            39, 56, 78, 100, 152
        },
        new byte[] {
            3, 7, 10, 14, 24,
            7, 11, 17, 22, 33,
            5, 11, 15, 23, 29, 42,
            7, 13, 20, 29, 35, 54,
            15, 26, 31, 48, 69,
            21, 28, 38, 56, 76
        }
    };

    private static readonly byte[] TotalCodewords = {
        13, 21, 32, 44, 68,
        21, 33, 49, 66, 99,
        15, 31, 47, 67, 89, 132,
        21, 41, 60, 85, 113, 166,
        51, 74, 103, 136, 199,
        61, 88, 122, 160, 232
    };

    private static readonly byte[][] CharacterCountBits = {
        new byte[] {
            4, 5, 6, 7, 7, 5, 6, 7, 7, 8, 4, 6, 7, 7, 8, 8,
            5, 6, 7, 7, 8, 8, 7, 7, 8, 8, 9, 7, 8, 8, 8, 9
        },
        new byte[] {
            3, 5, 5, 6, 6, 5, 5, 6, 6, 7, 4, 5, 6, 6, 7, 7,
            5, 6, 6, 7, 7, 8, 6, 7, 7, 7, 8, 6, 7, 7, 8, 8
        },
        new byte[] {
            3, 4, 5, 5, 6, 4, 5, 5, 6, 6, 3, 5, 5, 6, 6, 7,
            4, 5, 6, 6, 7, 7, 6, 6, 7, 7, 7, 6, 6, 7, 7, 8
        },
        new byte[] {
            2, 3, 4, 5, 5, 3, 4, 5, 5, 6, 2, 4, 5, 5, 6, 6,
            3, 5, 5, 6, 6, 7, 5, 5, 6, 6, 7, 5, 6, 6, 6, 7
        }
    };

    private static readonly byte[][] Blocks = {
        new byte[] {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 2, 2,
            1, 1, 1, 2, 2, 3, 1, 1, 2, 2, 3, 1, 2, 2, 3, 4
        },
        new byte[] {
            1, 1, 1, 1, 2, 1, 1, 2, 2, 3, 1, 1, 2, 2, 2, 3,
            1, 1, 2, 2, 3, 4, 2, 2, 3, 4, 5, 2, 2, 3, 4, 6
        }
    };

    internal static readonly byte[] AlignmentPatternColumns = {
        21, 0, 0, 0,
        19, 39, 0, 0,
        25, 51, 0, 0,
        23, 49, 75, 0,
        27, 55, 83, 111
    };

    private static readonly int[] LeftFormatInformation = {
        0x1FAB2, 0x1E597, 0x1DBDD, 0x1C4F8, 0x1B86C, 0x1A749, 0x19903, 0x18626,
        0x17F0E, 0x1602B, 0x15E61, 0x14144, 0x13DD0, 0x122F5, 0x11CBF, 0x1039A,
        0x0F1CA, 0x0EEEF, 0x0D0A5, 0x0CF80, 0x0B314, 0x0AC31, 0x0927B, 0x08D5E,
        0x07476, 0x06B53, 0x05519, 0x04A3C, 0x036A8, 0x0298D, 0x017C7, 0x008E2,
        0x3F367, 0x3EC42, 0x3D208, 0x3CD2D, 0x3B1B9, 0x3AE9C, 0x390D6, 0x38FF3,
        0x376DB, 0x369FE, 0x357B4, 0x34891, 0x33405, 0x32B20, 0x3156A, 0x30A4F,
        0x2F81F, 0x2E73A, 0x2D970, 0x2C655, 0x2BAC1, 0x2A5E4, 0x29BAE, 0x2848B,
        0x27DA3, 0x26286, 0x25CCC, 0x243E9, 0x23F7D, 0x22058, 0x21E12, 0x20137
    };

    private static readonly int[] RightFormatInformation = {
        0x20A7B, 0x2155E, 0x22B14, 0x23431, 0x248A5, 0x25780, 0x269CA, 0x276EF,
        0x28FC7, 0x290E2, 0x2AEA8, 0x2B18D, 0x2CD19, 0x2D23C, 0x2EC76, 0x2F353,
        0x30103, 0x31E26, 0x3206C, 0x33F49, 0x343DD, 0x35CF8, 0x362B2, 0x37D97,
        0x384BF, 0x39B9A, 0x3A5D0, 0x3BAF5, 0x3C661, 0x3D944, 0x3E70E, 0x3F82B,
        0x003AE, 0x01C8B, 0x022C1, 0x03DE4, 0x04170, 0x05E55, 0x0601F, 0x07F3A,
        0x08612, 0x09937, 0x0A77D, 0x0B858, 0x0C4CC, 0x0DBE9, 0x0E5A3, 0x0FA86,
        0x108D6, 0x117F3, 0x129B9, 0x1369C, 0x14A08, 0x1552D, 0x16B67, 0x17442,
        0x18D6A, 0x1924F, 0x1AC05, 0x1B320, 0x1CFB4, 0x1D091, 0x1EEDB, 0x1F1FE
    };

    internal static int GetWidth(int version) => Widths[ToIndex(version)];
    internal static int GetHeight(int version) => Heights[ToIndex(version)];
    internal static int GetDataCodewords(int version, QrErrorCorrectionLevel ecc) => DataCodewords[GetEccIndex(ecc)][ToIndex(version)];
    internal static int GetTotalCodewords(int version) => TotalCodewords[ToIndex(version)];
    internal static int GetBlocks(int version, QrErrorCorrectionLevel ecc) => Blocks[GetEccIndex(ecc)][ToIndex(version)];
    internal static int GetCharacterCountBits(int version, RmQrMode mode) => CharacterCountBits[(int)mode - 1][ToIndex(version)];
    internal static int GetLeftFormatInformation(int formatData) => LeftFormatInformation[formatData];
    internal static int GetRightFormatInformation(int formatData) => RightFormatInformation[formatData];
    internal static string GetVersionName(int version) => $"R{GetHeight(version)}x{GetWidth(version)}";

    internal static int FindVersion(int width, int height) {
        for (var i = 0; i < Widths.Length; i++) {
            if (Widths[i] == width && Heights[i] == height) return i + 1;
        }
        return 0;
    }

    internal static int GetHorizontalVersion(int width) {
        return width switch {
            43 => 0,
            59 => 1,
            77 => 2,
            99 => 3,
            139 => 4,
            _ => -1
        };
    }

    internal static void ValidateEcc(QrErrorCorrectionLevel ecc) {
        if (ecc != QrErrorCorrectionLevel.M && ecc != QrErrorCorrectionLevel.H) {
            throw new ArgumentOutOfRangeException(nameof(ecc), ecc, "rMQR supports error correction levels M and H only.");
        }
    }

    private static int GetEccIndex(QrErrorCorrectionLevel ecc) {
        ValidateEcc(ecc);
        return ecc == QrErrorCorrectionLevel.M ? 0 : 1;
    }

    private static int ToIndex(int version) {
        if (version is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(version));
        return version - 1;
    }
}
