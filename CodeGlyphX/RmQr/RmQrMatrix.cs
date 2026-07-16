// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;

namespace CodeGlyphX.RmQr;

internal static class RmQrMatrix {
    internal static void SetupFunctionPatterns(BitMatrix modules, BitMatrix function) {
        if (modules.Width != function.Width || modules.Height != function.Height) {
            throw new ArgumentException("rMQR module and function matrices must have matching dimensions.");
        }
        var width = modules.Width;
        var height = modules.Height;

        for (var x = 0; x < width; x++) {
            SetFunction(modules, function, x, 0, (x & 1) == 0);
            SetFunction(modules, function, x, height - 1, (x & 1) == 0);
        }
        for (var y = 0; y < height; y++) {
            SetFunction(modules, function, 0, y, (y & 1) == 0);
            SetFunction(modules, function, width - 1, y, (y & 1) == 0);
        }

        DrawFinder(modules, function);
        DrawSubFinder(modules, function);

        SetFunction(modules, function, 0, height - 2, true);
        SetFunction(modules, function, 1, height - 2, false);
        SetFunction(modules, function, 1, height - 1, true);
        SetFunction(modules, function, width - 2, 0, true);
        SetFunction(modules, function, width - 2, 1, false);
        SetFunction(modules, function, width - 1, 1, true);

        for (var y = 0; y < 7; y++) SetFunction(modules, function, 7, y, false);
        if (height > 7) {
            for (var x = 0; x < 8; x++) SetFunction(modules, function, x, 7, false);
        }

        DrawAlignmentPatterns(modules, function);
        ReserveFormatInformation(modules, function);
    }

    internal static void PopulateData(BitMatrix modules, BitMatrix function, byte[] codewords) {
        var width = modules.Width;
        var height = modules.Height;
        var bitLength = checked(codewords.Length * 8);
        var bitIndex = 0;
        var pair = 0;
        var y = height - 1;
        var upward = true;
        while (bitIndex < bitLength) {
            var x = width - 3 - pair * 2;
            if (x < 0) throw new InvalidOperationException("rMQR data placement exhausted the matrix.");
            if (!function[x + 1, y]) modules[x + 1, y] = ReadBit(codewords, bitIndex++);
            if (bitIndex < bitLength && !function[x, y]) modules[x, y] = ReadBit(codewords, bitIndex++);

            if (upward) {
                y--;
                if (y < 0) {
                    pair++;
                    y = 0;
                    upward = false;
                }
            } else {
                y++;
                if (y == height) {
                    pair++;
                    y = height - 1;
                    upward = true;
                }
            }
        }
    }

    internal static byte[] ReadCodewords(BitMatrix modules, BitMatrix function, int codewordCount) {
        var width = modules.Width;
        var height = modules.Height;
        var result = new byte[codewordCount];
        var bitLength = checked(codewordCount * 8);
        var bitIndex = 0;
        var pair = 0;
        var y = height - 1;
        var upward = true;
        while (bitIndex < bitLength) {
            var x = width - 3 - pair * 2;
            if (x < 0) throw new InvalidOperationException("rMQR data extraction exhausted the matrix.");
            if (!function[x + 1, y]) WriteBit(result, bitIndex++, modules[x + 1, y]);
            if (bitIndex < bitLength && !function[x, y]) WriteBit(result, bitIndex++, modules[x, y]);

            if (upward) {
                y--;
                if (y < 0) {
                    pair++;
                    y = 0;
                    upward = false;
                }
            } else {
                y++;
                if (y == height) {
                    pair++;
                    y = height - 1;
                    upward = true;
                }
            }
        }
        return result;
    }

    internal static void ApplyMask(BitMatrix modules, BitMatrix function) {
        for (var y = 0; y < modules.Height; y++) {
            for (var x = 0; x < modules.Width; x++) {
                if (!function[x, y] && ((y / 2 + x / 3) & 1) == 0) modules[x, y] = !modules[x, y];
            }
        }
    }

    internal static void DrawFormatInformation(BitMatrix modules, int version, QrErrorCorrectionLevel ecc) {
        var formatData = version - 1 + (ecc == QrErrorCorrectionLevel.H ? 32 : 0);
        var left = RmQrTables.GetLeftFormatInformation(formatData);
        var right = RmQrTables.GetRightFormatInformation(formatData);
        for (var i = 0; i < 5; i++) {
            for (var j = 0; j < 3; j++) {
                modules[8 + j, 1 + i] = ((left >> (j * 5 + i)) & 1) != 0;
                modules[modules.Width - 8 + j, modules.Height - 6 + i] = ((right >> (j * 5 + i)) & 1) != 0;
            }
        }
        modules[11, 1] = ((left >> 15) & 1) != 0;
        modules[11, 2] = ((left >> 16) & 1) != 0;
        modules[11, 3] = ((left >> 17) & 1) != 0;
        modules[modules.Width - 5, modules.Height - 6] = ((right >> 15) & 1) != 0;
        modules[modules.Width - 4, modules.Height - 6] = ((right >> 16) & 1) != 0;
        modules[modules.Width - 3, modules.Height - 6] = ((right >> 17) & 1) != 0;
    }

    internal static int ReadLeftFormatInformation(BitMatrix modules) {
        var value = 0;
        for (var i = 0; i < 5; i++) {
            for (var j = 0; j < 3; j++) if (modules[8 + j, 1 + i]) value |= 1 << (j * 5 + i);
        }
        if (modules[11, 1]) value |= 1 << 15;
        if (modules[11, 2]) value |= 1 << 16;
        if (modules[11, 3]) value |= 1 << 17;
        return value;
    }

    internal static int ReadRightFormatInformation(BitMatrix modules) {
        var value = 0;
        for (var i = 0; i < 5; i++) {
            for (var j = 0; j < 3; j++) {
                if (modules[modules.Width - 8 + j, modules.Height - 6 + i]) value |= 1 << (j * 5 + i);
            }
        }
        if (modules[modules.Width - 5, modules.Height - 6]) value |= 1 << 15;
        if (modules[modules.Width - 4, modules.Height - 6]) value |= 1 << 16;
        if (modules[modules.Width - 3, modules.Height - 6]) value |= 1 << 17;
        return value;
    }

    private static void DrawFinder(BitMatrix modules, BitMatrix function) {
        for (var y = 0; y < 7; y++) {
            for (var x = 0; x < 7; x++) {
                var dark = x == 0 || x == 6 || y == 0 || y == 6 || (x is >= 2 and <= 4 && y is >= 2 and <= 4);
                SetFunction(modules, function, x, y, dark);
            }
        }
    }

    private static void DrawSubFinder(BitMatrix modules, BitMatrix function) {
        var left = modules.Width - 5;
        var top = modules.Height - 5;
        for (var y = 0; y < 5; y++) {
            for (var x = 0; x < 5; x++) {
                var dark = x == 0 || x == 4 || y == 0 || y == 4 || (x == 2 && y == 2);
                SetFunction(modules, function, left + x, top + y, dark);
            }
        }
    }

    private static void DrawAlignmentPatterns(BitMatrix modules, BitMatrix function) {
        if (modules.Width <= 27) return;
        var horizontalVersion = RmQrTables.GetHorizontalVersion(modules.Width);
        if (horizontalVersion < 0) throw new InvalidOperationException("Unknown rMQR width.");
        for (var i = 0; i < 4; i++) {
            var column = RmQrTables.AlignmentPatternColumns[horizontalVersion * 4 + i];
            if (column == 0) continue;
            for (var y = 0; y < modules.Height; y++) SetFunction(modules, function, column, y, (y & 1) == 0);
            SetFunction(modules, function, column - 1, 1, true);
            SetFunction(modules, function, column - 1, 2, true);
            SetFunction(modules, function, column + 1, 1, true);
            SetFunction(modules, function, column + 1, 2, true);
            SetFunction(modules, function, column - 1, modules.Height - 3, true);
            SetFunction(modules, function, column - 1, modules.Height - 2, true);
            SetFunction(modules, function, column + 1, modules.Height - 3, true);
            SetFunction(modules, function, column + 1, modules.Height - 2, true);
        }
    }

    private static void ReserveFormatInformation(BitMatrix modules, BitMatrix function) {
        for (var i = 0; i < 5; i++) {
            for (var j = 0; j < 3; j++) {
                SetFunction(modules, function, 8 + j, 1 + i, false);
                SetFunction(modules, function, modules.Width - 8 + j, modules.Height - 6 + i, false);
            }
        }
        SetFunction(modules, function, 11, 1, false);
        SetFunction(modules, function, 11, 2, false);
        SetFunction(modules, function, 11, 3, false);
        SetFunction(modules, function, modules.Width - 5, modules.Height - 6, false);
        SetFunction(modules, function, modules.Width - 4, modules.Height - 6, false);
        SetFunction(modules, function, modules.Width - 3, modules.Height - 6, false);
    }

    private static void SetFunction(BitMatrix modules, BitMatrix function, int x, int y, bool dark) {
        modules[x, y] = dark;
        function[x, y] = true;
    }

    private static bool ReadBit(byte[] data, int bitIndex) => (data[bitIndex >> 3] & (0x80 >> (bitIndex & 7))) != 0;

    private static void WriteBit(byte[] data, int bitIndex, bool value) {
        if (value) data[bitIndex >> 3] |= (byte)(0x80 >> (bitIndex & 7));
    }
}
