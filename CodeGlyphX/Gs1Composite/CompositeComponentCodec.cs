// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;
using System.Numerics;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Pdf417.Ec;

namespace CodeGlyphX.Gs1Composite;

internal static class CompositeComponentCodec {
    private static readonly int[] CcACoefficients = {
        522, 568, 723, 809,
        427, 919, 460, 155, 566,
        861, 285, 19, 803, 17, 766,
        76, 925, 537, 597, 784, 691, 437,
        237, 308, 436, 284, 646, 653, 428, 379
    };

    private static readonly int[][] CcAVariants = {
        new[] { 5, 6, 7, 8, 9, 10, 12, 4, 5, 6, 7, 8, 3, 4, 5, 6, 7 },
        new[] { 4, 4, 5, 5, 6, 6, 7, 4, 5, 6, 7, 7, 4, 5, 6, 7, 8 },
        new[] { 0, 0, 4, 4, 9, 9, 15, 0, 4, 9, 15, 15, 0, 4, 9, 15, 22 }
    };

    private static readonly int[][] CcARap = {
        new[] { 39, 1, 32, 8, 14, 43, 20, 11, 1, 5, 15, 21, 40, 43, 46, 34, 29 },
        new[] { 0, 0, 0, 0, 0, 0, 0, 43, 33, 37, 47, 1, 20, 23, 26, 14, 9 },
        new[] { 19, 33, 12, 40, 46, 23, 52, 23, 13, 17, 27, 33, 52, 3, 6, 46, 41 },
        new[] { 2, 0, 1, 1, 1, 0, 1, 1, 0, 1, 2, 2, 0, 0, 0, 0, 1 }
    };

    private static readonly int[] CcATargetBits = {
        59, 78, 88, 108, 118, 138, 167,
        78, 98, 118, 138, 167,
        78, 108, 138, 167, 197
    };

    private static readonly int[][] CcBTargetBits = {
        new[] { 56, 104, 160, 208, 256, 296, 336 },
        new[] { 32, 72, 112, 152, 208, 304, 416, 536, 648, 768 },
        new[] { 56, 96, 152, 208, 264, 352, 496, 672, 840, 1016, 1184 }
    };

    internal static BitMatrix Encode(bool[] bits, Gs1CompositeComponent component, int columns, int errorCorrectionLevel) {
        return component switch {
            Gs1CompositeComponent.CcA => EncodeCcA(bits, columns),
            Gs1CompositeComponent.CcB => EncodeCcB(bits, columns),
            Gs1CompositeComponent.CcC => EncodeCcC(bits, columns, errorCorrectionLevel),
            _ => throw new ArgumentOutOfRangeException(nameof(component))
        };
    }

    internal static bool TryDecode(BitMatrix modules, out bool[] bits, out Gs1CompositeComponent component) {
        bits = Array.Empty<bool>();
        component = Gs1CompositeComponent.Auto;
        if (TryDecodeCcA(modules, out bits)) {
            component = Gs1CompositeComponent.CcA;
            return true;
        }
        if (TryDecodeCcB(modules, out bits)) {
            component = Gs1CompositeComponent.CcB;
            return true;
        }
        if (TryDecodeCcC(modules, out bits)) {
            component = Gs1CompositeComponent.CcC;
            return true;
        }
        return false;
    }

    private static BitMatrix EncodeCcA(bool[] bits, int columns) {
        var data = EncodeBase928(bits);
        var variant = FindCcAVariant(columns, data.Count);
        if (variant < 0) throw new ArgumentException("No CC-A variant can hold the supplied data.", nameof(bits));
        var eccCount = CcAVariants[1][variant];
        data.AddRange(GenerateCorrection(data, eccCount, CcACoefficients, CcAVariants[2][variant]));
        return BuildCcAMatrix(data, columns, variant);
    }

    private static bool TryDecodeCcA(BitMatrix modules, out bool[] bits) {
        bits = Array.Empty<bool>();
        var columns = modules.Width == 55 ? 2 : modules.Width == 72 ? 3 : modules.Width == 99 ? 4 : 0;
        if (columns == 0) return false;
        var variant = FindCcAVariantByRows(columns, modules.Height);
        if (variant < 0) return false;

        var received = new int[columns * modules.Height];
        var left = CcARap[0][variant];
        var centre = CcARap[1][variant];
        var right = CcARap[2][variant];
        var cluster = CcARap[3][variant];
        var output = 0;
        for (var row = 0; row < modules.Height; row++) {
            var position = 0;
            if (columns != 3) {
                if (ReadBits(modules, row, position, 10) != MicroPdf417Tables.RapLRBits[left]) return false;
                position += 10;
            }
            if (!ReadCodeword(modules, row, ref position, cluster, out received[output++])) return false;
            if (columns == 3) {
                if (ReadBits(modules, row, position, 10) != MicroPdf417Tables.RapCBits[centre]) return false;
                position += 10;
            }
            if (!ReadCodeword(modules, row, ref position, cluster, out received[output++])) return false;
            if (columns == 4) {
                if (ReadBits(modules, row, position, 10) != MicroPdf417Tables.RapCBits[centre]) return false;
                position += 10;
            }
            if (columns >= 3 && !ReadCodeword(modules, row, ref position, cluster, out received[output++])) return false;
            if (columns == 4 && !ReadCodeword(modules, row, ref position, cluster, out received[output++])) return false;
            if (ReadBits(modules, row, position, 10) != MicroPdf417Tables.RapLRBits[right]) return false;
            position += 10;
            if (position >= modules.Width || !modules[position, row] || position + 1 != modules.Width) return false;
            left = NextRap(left);
            centre = NextRap(centre);
            right = NextRap(right);
            cluster = (cluster + 1) % 3;
        }

        var eccCount = CcAVariants[1][variant];
        var dataCount = received.Length - eccCount;
        if (!ValidateCorrection(received, dataCount, eccCount, CcACoefficients, CcAVariants[2][variant])) {
            if (!CorrectAndValidate(received, eccCount) ||
                !ValidateCorrection(received, dataCount, eccCount, CcACoefficients, CcAVariants[2][variant])) return false;
        }
        var data = new int[dataCount];
        Array.Copy(received, data, dataCount);
        bits = DecodeBase928(data, CcATargetBits[variant]);
        return true;
    }

    private static BitMatrix EncodeCcB(bool[] bits, int columns) {
        var bytes = BitsToBytes(bits);
        var data = EncodeBytes(bytes);
        data.Insert(0, 920);
        var variant = FindMicroVariant(columns, data.Count);
        if (variant < 0) throw new ArgumentException("No CC-B variant can hold the supplied data.", nameof(bits));
        var rows = MicroPdf417Tables.MicroVariants[variant + 34];
        var eccCount = MicroPdf417Tables.MicroVariants[variant + 68];
        var capacity = columns * rows - eccCount;
        while (data.Count < capacity) data.Add(900);
        data.AddRange(GenerateCorrection(data, eccCount, MicroPdf417Tables.MicroCoefficients,
            MicroPdf417Tables.MicroVariants[variant + 102]));
        return BuildMicroMatrix(data, columns, rows, variant);
    }

    private static bool TryDecodeCcB(BitMatrix modules, out bool[] bits) {
        bits = Array.Empty<bool>();
        var columns = modules.Width == 55 ? 2 : modules.Width == 82 ? 3 : modules.Width == 99 ? 4 : 0;
        if (columns == 0 || !MicroPdf417Tables.TryGetVariant(columns, modules.Height, out var variant)) return false;
        var received = new int[columns * modules.Height];
        if (!ReadMicroCodewords(modules, columns, variant, received)) return false;
        var eccCount = MicroPdf417Tables.MicroVariants[variant + 68];
        var dataCount = received.Length - eccCount;
        var coefficientOffset = MicroPdf417Tables.MicroVariants[variant + 102];
        if (!ValidateCorrection(received, dataCount, eccCount, MicroPdf417Tables.MicroCoefficients, coefficientOffset)) {
            if (!CorrectAndValidate(received, eccCount) ||
                !ValidateCorrection(received, dataCount, eccCount, MicroPdf417Tables.MicroCoefficients, coefficientOffset)) return false;
        }
        if (dataCount < 2 || received[0] != 920 || received[1] != 901) return false;
        var target = FindCcBTarget(columns, modules.Height);
        if (target == 0) return false;
        var data = new int[dataCount - 1];
        Array.Copy(received, 1, data, 0, data.Length);
        if (!TryDecodeBytes(data, target / 8, out var bytes)) return false;
        bits = BytesToBits(bytes, target);
        return true;
    }

    private static BitMatrix EncodeCcC(bool[] bits, int columns, int errorCorrectionLevel) {
        var bytes = BitsToBytes(bits);
        var payload = EncodeBytes(bytes);
        var data = new List<int>(payload.Count + 2) { 0, 920 };
        data.AddRange(payload);
        data[0] = data.Count;
        var correction = Pdf417ErrorCorrection.GenerateErrorCorrection(data, errorCorrectionLevel);
        var rows = (data.Count + correction.Length) / columns;
        if (rows * columns != data.Count + correction.Length) throw new InvalidOperationException("CC-C sizing mismatch.");
        data.AddRange(correction);
        return BuildPdfMatrix(data, columns, rows, errorCorrectionLevel);
    }

    private static bool TryDecodeCcC(BitMatrix modules, out bool[] bits) {
        bits = Array.Empty<bool>();
        if (modules.Width < 86 || (modules.Width - 69) % 17 != 0) return false;
        var columns = (modules.Width - 69) / 17;
        if (columns is < 1 or > 30 || modules.Height is < 3 or > 30) return false;
        var received = new int[columns * modules.Height];
        var output = 0;
        for (var row = 0; row < modules.Height; row++) {
            var cluster = row % 3;
            var position = 0;
            if (ReadBits(modules, row, position, 17) != 0x1fea8) return false;
            position += 34; // Start and left row indicator.
            for (var column = 0; column < columns; column++) {
                var pattern = ReadBits(modules, row, position, 17);
                var codeword = FindPdfCodeword(cluster, pattern);
                if (codeword < 0) return false;
                received[output++] = codeword;
                position += 17;
            }
            position += 17; // Right row indicator.
            if (ReadBits(modules, row, position, 18) != 0x3fa29) return false;
        }

        for (var level = 2; level <= 5; level++) {
            var eccCount = 1 << (level + 1);
            if (received.Length <= eccCount + 3) continue;
            var candidate = (int[])received.Clone();
            if (!CorrectAndValidate(candidate, eccCount)) continue;
            var dataCount = candidate.Length - eccCount;
            if (candidate[0] != dataCount || candidate[1] != 920 || candidate[2] != 901) continue;
            var byteCodewords = dataCount - 3;
            var byteLength = 6 * (byteCodewords / 5) + byteCodewords % 5;
            var data = new int[dataCount - 2];
            Array.Copy(candidate, 2, data, 0, data.Length);
            if (!TryDecodeBytes(data, byteLength, out var bytes)) continue;
            bits = BytesToBits(bytes, byteLength * 8);
            return true;
        }
        return false;
    }

    private static List<int> EncodeBase928(bool[] bits) {
        var result = new List<int>(bits.Length / 10 + 3);
        for (var start = 0; start < bits.Length; start += 69) {
            var count = Math.Min(69, bits.Length - start);
            var value = BigInteger.Zero;
            for (var i = 0; i < count; i++) value = (value << 1) + (bits[start + i] ? 1 : 0);
            var codewordCount = count / 10 + 1;
            var chunk = new int[codewordCount];
            for (var i = codewordCount - 1; i >= 0; i--) {
                value = BigInteger.DivRem(value, 928, out var remainder);
                chunk[i] = (int)remainder;
            }
            result.AddRange(chunk);
        }
        return result;
    }

    private static bool[] DecodeBase928(int[] codewords, int bitLength) {
        var bits = new bool[bitLength];
        var codewordOffset = 0;
        for (var bitOffset = 0; bitOffset < bitLength; bitOffset += 69) {
            var count = Math.Min(69, bitLength - bitOffset);
            var codewordCount = count / 10 + 1;
            var value = BigInteger.Zero;
            for (var i = 0; i < codewordCount; i++) value = value * 928 + codewords[codewordOffset++];
            for (var i = count - 1; i >= 0; i--) {
                bits[bitOffset + i] = !value.IsEven;
                value >>= 1;
            }
        }
        return bits;
    }

    private static List<int> EncodeBytes(byte[] bytes) {
        var result = new List<int>(bytes.Length + 1) { 901 };
        var index = 0;
        while (index + 6 <= bytes.Length) {
            long value = 0;
            for (var i = 0; i < 6; i++) value = (value << 8) + bytes[index + i];
            var chunk = new int[5];
            for (var i = 4; i >= 0; i--) {
                chunk[i] = (int)(value % 900);
                value /= 900;
            }
            result.AddRange(chunk);
            index += 6;
        }
        while (index < bytes.Length) result.Add(bytes[index++]);
        return result;
    }

    private static bool TryDecodeBytes(int[] codewords, int byteLength, out byte[] bytes) {
        bytes = Array.Empty<byte>();
        if (codewords.Length == 0 || codewords[0] != 901) return false;
        var result = new byte[byteLength];
        var codeword = 1;
        var output = 0;
        var groups = byteLength / 6;
        for (var group = 0; group < groups; group++) {
            if (codeword + 5 > codewords.Length) return false;
            long value = 0;
            for (var i = 0; i < 5; i++) value = value * 900 + codewords[codeword++];
            for (var i = 5; i >= 0; i--) {
                result[output + i] = (byte)(value & 0xff);
                value >>= 8;
            }
            output += 6;
        }
        while (output < byteLength) {
            if (codeword >= codewords.Length || codewords[codeword] > 255) return false;
            result[output++] = (byte)codewords[codeword++];
        }
        bytes = result;
        return true;
    }

    private static BitMatrix BuildCcAMatrix(IReadOnlyList<int> codewords, int columns, int variant) {
        var rows = CcAVariants[0][variant];
        var width = columns == 2 ? 55 : columns == 3 ? 72 : 99;
        var matrix = new BitMatrix(width, rows);
        var left = CcARap[0][variant];
        var centre = CcARap[1][variant];
        var right = CcARap[2][variant];
        var cluster = CcARap[3][variant];
        var input = 0;
        for (var row = 0; row < rows; row++) {
            var position = 0;
            if (columns != 3) WriteBits(matrix, row, ref position, MicroPdf417Tables.RapLRBits[left], 10);
            WriteCodeword(matrix, row, ref position, cluster, codewords[input++]);
            if (columns == 3) WriteBits(matrix, row, ref position, MicroPdf417Tables.RapCBits[centre], 10);
            WriteCodeword(matrix, row, ref position, cluster, codewords[input++]);
            if (columns == 4) WriteBits(matrix, row, ref position, MicroPdf417Tables.RapCBits[centre], 10);
            if (columns >= 3) WriteCodeword(matrix, row, ref position, cluster, codewords[input++]);
            if (columns == 4) WriteCodeword(matrix, row, ref position, cluster, codewords[input++]);
            WriteBits(matrix, row, ref position, MicroPdf417Tables.RapLRBits[right], 10);
            matrix[position++, row] = true;
            if (position != width) throw new InvalidOperationException("CC-A row width mismatch.");
            left = NextRap(left);
            centre = NextRap(centre);
            right = NextRap(right);
            cluster = (cluster + 1) % 3;
        }
        return matrix;
    }

    private static BitMatrix BuildMicroMatrix(IReadOnlyList<int> codewords, int columns, int rows, int variant) {
        var matrix = new BitMatrix(MicroPdf417Tables.GetRowWidth(columns), rows);
        var left = MicroPdf417Tables.RapTable[variant];
        var centre = MicroPdf417Tables.RapTable[variant + 34];
        var right = MicroPdf417Tables.RapTable[variant + 68];
        var cluster = MicroPdf417Tables.RapTable[variant + 102] / 3;
        var input = 0;
        for (var row = 0; row < rows; row++) {
            var position = 0;
            WriteBits(matrix, row, ref position, MicroPdf417Tables.RapLRBits[left], 10);
            WriteCodeword(matrix, row, ref position, cluster, codewords[input++]);
            if (columns == 3) WriteBits(matrix, row, ref position, MicroPdf417Tables.RapCBits[centre], 10);
            if (columns >= 2) WriteCodeword(matrix, row, ref position, cluster, codewords[input++]);
            if (columns == 4) WriteBits(matrix, row, ref position, MicroPdf417Tables.RapCBits[centre], 10);
            if (columns >= 3) WriteCodeword(matrix, row, ref position, cluster, codewords[input++]);
            if (columns == 4) WriteCodeword(matrix, row, ref position, cluster, codewords[input++]);
            WriteBits(matrix, row, ref position, MicroPdf417Tables.RapLRBits[right], 10);
            matrix[position++, row] = true;
            left = NextRap(left);
            centre = NextRap(centre);
            right = NextRap(right);
            cluster = (cluster + 1) % 3;
        }
        return matrix;
    }

    private static BitMatrix BuildPdfMatrix(IReadOnlyList<int> codewords, int columns, int rows, int errorCorrectionLevel) {
        var matrix = new BitMatrix(69 + columns * 17, rows);
        var input = 0;
        for (var row = 0; row < rows; row++) {
            var cluster = row % 3;
            var block = row / 3 * 30;
            var c1 = (rows - 1) / 3;
            var c2 = errorCorrectionLevel * 3 + (rows - 1) % 3;
            var c3 = columns - 1;
            var left = block + (cluster == 0 ? c1 : cluster == 1 ? c2 : c3);
            var right = block + (cluster == 0 ? c3 : cluster == 1 ? c1 : c2);
            var position = 0;
            WriteBits(matrix, row, ref position, 0x1fea8, 17);
            WriteBits(matrix, row, ref position, Pdf417CodewordTable.Table[cluster][left], 17);
            for (var column = 0; column < columns; column++) {
                WriteBits(matrix, row, ref position, Pdf417CodewordTable.Table[cluster][codewords[input++]], 17);
            }
            WriteBits(matrix, row, ref position, Pdf417CodewordTable.Table[cluster][right], 17);
            WriteBits(matrix, row, ref position, 0x3fa29, 18);
        }
        return matrix;
    }

    private static bool ReadMicroCodewords(BitMatrix modules, int columns, int variant, int[] output) {
        var left = MicroPdf417Tables.RapTable[variant];
        var centre = MicroPdf417Tables.RapTable[variant + 34];
        var right = MicroPdf417Tables.RapTable[variant + 68];
        var cluster = MicroPdf417Tables.RapTable[variant + 102] / 3;
        var index = 0;
        for (var row = 0; row < modules.Height; row++) {
            var position = 0;
            if (ReadBits(modules, row, position, 10) != MicroPdf417Tables.RapLRBits[left]) return false;
            position += 10;
            if (!ReadCodeword(modules, row, ref position, cluster, out output[index++])) return false;
            if (columns == 3) {
                if (ReadBits(modules, row, position, 10) != MicroPdf417Tables.RapCBits[centre]) return false;
                position += 10;
            }
            if (columns >= 2 && !ReadCodeword(modules, row, ref position, cluster, out output[index++])) return false;
            if (columns == 4) {
                if (ReadBits(modules, row, position, 10) != MicroPdf417Tables.RapCBits[centre]) return false;
                position += 10;
            }
            if (columns >= 3 && !ReadCodeword(modules, row, ref position, cluster, out output[index++])) return false;
            if (columns == 4 && !ReadCodeword(modules, row, ref position, cluster, out output[index++])) return false;
            if (ReadBits(modules, row, position, 10) != MicroPdf417Tables.RapLRBits[right]) return false;
            position += 10;
            if (position >= modules.Width || !modules[position, row]) return false;
            left = NextRap(left);
            centre = NextRap(centre);
            right = NextRap(right);
            cluster = (cluster + 1) % 3;
        }
        return true;
    }

    private static int[] GenerateCorrection(IReadOnlyList<int> data, int count, int[] coefficients, int offset) {
        var correction = new int[count];
        for (var i = 0; i < data.Count; i++) {
            var total = (data[i] + correction[count - 1]) % 929;
            for (var j = count - 1; j >= 0; j--) {
                correction[j] = j == 0
                    ? (929 - total * coefficients[offset + j] % 929) % 929
                    : (correction[j - 1] + 929 - total * coefficients[offset + j] % 929) % 929;
            }
        }
        var result = new int[count];
        for (var i = 0; i < count; i++) result[i] = correction[count - 1 - i] == 0 ? 0 : 929 - correction[count - 1 - i];
        return result;
    }

    private static bool CorrectAndValidate(int[] received, int eccCount) {
        var corrected = (int[])received.Clone();
        if (!new ErrorCorrection().Decode(corrected, eccCount)) return false;
        Array.Copy(corrected, received, received.Length);
        return true;
    }

    private static bool ValidateCorrection(int[] received, int dataCount, int eccCount, int[] coefficients, int offset) {
        var data = new int[dataCount];
        Array.Copy(received, data, dataCount);
        var expected = GenerateCorrection(data, eccCount, coefficients, offset);
        for (var i = 0; i < eccCount; i++) if (received[dataCount + i] != expected[i]) return false;
        return true;
    }

    private static int FindCcAVariant(int columns, int dataCount) {
        var start = columns == 2 ? 0 : columns == 3 ? 7 : columns == 4 ? 12 : -1;
        var end = columns == 2 ? 7 : columns == 3 ? 12 : columns == 4 ? 17 : -1;
        for (var variant = start; variant >= 0 && variant < end; variant++) {
            if (CcAVariants[0][variant] * columns - CcAVariants[1][variant] == dataCount) return variant;
        }
        return -1;
    }

    private static int FindCcAVariantByRows(int columns, int rows) {
        var start = columns == 2 ? 0 : columns == 3 ? 7 : columns == 4 ? 12 : -1;
        var end = columns == 2 ? 7 : columns == 3 ? 12 : columns == 4 ? 17 : -1;
        for (var variant = start; variant >= 0 && variant < end; variant++) if (CcAVariants[0][variant] == rows) return variant;
        return -1;
    }

    private static int FindMicroVariant(int columns, int dataCount) {
        for (var variant = 0; variant < 34; variant++) {
            if (MicroPdf417Tables.MicroVariants[variant] != columns) continue;
            var capacity = columns * MicroPdf417Tables.MicroVariants[variant + 34] - MicroPdf417Tables.MicroVariants[variant + 68];
            if (dataCount <= capacity) return variant;
        }
        return -1;
    }

    private static int FindCcBTarget(int columns, int rows) {
        var targets = CcBTargetBits[columns - 2];
        for (var i = 0; i < targets.Length; i++) {
            var bytes = new byte[targets[i] / 8];
            var dataCount = EncodeBytes(bytes).Count + 1;
            var variant = FindMicroVariant(columns, dataCount);
            if (variant >= 0 && MicroPdf417Tables.MicroVariants[variant + 34] == rows) return targets[i];
        }
        return 0;
    }

    private static byte[] BitsToBytes(bool[] bits) {
        if ((bits.Length & 7) != 0) throw new ArgumentException("Composite byte components require a byte-aligned bit stream.", nameof(bits));
        var bytes = new byte[bits.Length / 8];
        for (var i = 0; i < bits.Length; i++) if (bits[i]) bytes[i >> 3] |= (byte)(0x80 >> (i & 7));
        return bytes;
    }

    private static bool[] BytesToBits(byte[] bytes, int bitLength) {
        var bits = new bool[bitLength];
        for (var i = 0; i < bitLength; i++) bits[i] = (bytes[i >> 3] & (0x80 >> (i & 7))) != 0;
        return bits;
    }

    private static void WriteCodeword(BitMatrix matrix, int row, ref int position, int cluster, int codeword) {
        WriteBits(matrix, row, ref position, Pdf417CodewordTable.Table[cluster][codeword], 17);
    }

    private static bool ReadCodeword(BitMatrix matrix, int row, ref int position, int cluster, out int codeword) {
        var pattern = ReadBits(matrix, row, position, 17);
        if (pattern < 0) {
            codeword = -1;
            return false;
        }
        position += 17;
        codeword = FindPdfCodeword(cluster, pattern);
        return codeword >= 0;
    }

    private static void WriteBits(BitMatrix matrix, int row, ref int position, int value, int count) {
        for (var bit = count - 1; bit >= 0; bit--) matrix[position++, row] = ((value >> bit) & 1) != 0;
    }

    private static int ReadBits(BitMatrix matrix, int row, int position, int count) {
        if (position < 0 || position + count > matrix.Width) return -1;
        var value = 0;
        for (var i = 0; i < count; i++) value = (value << 1) | (matrix[position + i, row] ? 1 : 0);
        return value;
    }

    private static int FindPdfCodeword(int cluster, int pattern) {
        var table = Pdf417CodewordTable.Table[cluster];
        for (var i = 0; i < table.Length; i++) if (table[i] == pattern) return i;
        return -1;
    }

    private static int NextRap(int value) => value == 52 ? 1 : value + 1;
}
