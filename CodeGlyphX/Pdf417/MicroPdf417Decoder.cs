using System;

namespace CodeGlyphX.Pdf417;

/// <summary>
/// Decodes MicroPDF417 symbols from a <see cref="BitMatrix"/>.
/// </summary>
public static class MicroPdf417Decoder {
    /// <summary>
    /// Attempts to decode a MicroPDF417 symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        text = string.Empty;
        if (modules is null) return false;
        if (modules.Width <= 0 || modules.Height <= 0) return false;

        if (!TryGetColumns(modules.Width, out var columns)) return false;
        var rows = modules.Height;
        if (!MicroPdf417Tables.TryGetVariant(columns, rows, out var variant)) return false;

        var eccCount = MicroPdf417Tables.MicroVariants[variant + 68];
        var coeffOffset = MicroPdf417Tables.MicroVariants[variant + 102];

        var leftRap = MicroPdf417Tables.RapTable[variant];
        var centreRap = MicroPdf417Tables.RapTable[variant + 34];
        var rightRap = MicroPdf417Tables.RapTable[variant + 68];
        var cluster = MicroPdf417Tables.RapTable[variant + 102] / 3;

        var totalCodewords = columns * rows;
        if (totalCodewords <= eccCount) return false;

        var codewords = new int[totalCodewords];
        var index = 0;

        for (var row = 0; row < rows; row++) {
            if (!TryDecodeRow(modules, row, columns, cluster, leftRap, centreRap, rightRap, codewords, index)) {
                return false;
            }
            index += columns;
            leftRap = NextRap(leftRap);
            centreRap = NextRap(centreRap);
            rightRap = NextRap(rightRap);
            cluster = (cluster + 1) % 3;
        }

        var dataCount = totalCodewords - eccCount;
        var ecc = GenerateErrorCorrection(codewords, dataCount, eccCount, coeffOffset);
        for (var i = 0; i < eccCount; i++) {
            if (codewords[dataCount + i] != ecc[i]) return false;
        }

        var dataCodewords = new int[dataCount];
        Array.Copy(codewords, 0, dataCodewords, 0, dataCount);
        var decoded = Pdf417DecodedBitStreamParser.Decode(dataCodewords);
        if (decoded is null) return false;
        text = decoded;
        return true;
    }

    private static bool TryDecodeRow(BitMatrix modules, int row, int columns, int cluster, int leftRap, int centreRap, int rightRap, int[] output, int outputIndex) {
        var pos = 0;
        if (!MatchRap(modules, row, pos, MicroPdf417Tables.RapLRBits[leftRap])) return false;
        pos += 10;
        pos += 1;

        if (!TryReadCodeword(modules, row, pos, cluster, out output[outputIndex])) return false;
        pos += 15;
        pos += 1;

        switch (columns) {
            case 1: {
                if (!MatchRap(modules, row, pos, MicroPdf417Tables.RapLRBits[rightRap])) return false;
                pos += 10;
                pos += 1;
                break;
            }
            case 2: {
                pos += 1; // double separator before second codeword
                if (!TryReadCodeword(modules, row, pos, cluster, out output[outputIndex + 1])) return false;
                pos += 15;
                pos += 1;
                if (!MatchRap(modules, row, pos, MicroPdf417Tables.RapLRBits[rightRap])) return false;
                pos += 10;
                pos += 1;
                break;
            }
            case 3: {
                if (!MatchRap(modules, row, pos, MicroPdf417Tables.RapCBits[centreRap])) return false;
                pos += 10;
                pos += 1;
                if (!TryReadCodeword(modules, row, pos, cluster, out output[outputIndex + 1])) return false;
                pos += 15;
                pos += 1;
                pos += 1; // double separator before third codeword
                if (!TryReadCodeword(modules, row, pos, cluster, out output[outputIndex + 2])) return false;
                pos += 15;
                pos += 1;
                if (!MatchRap(modules, row, pos, MicroPdf417Tables.RapLRBits[rightRap])) return false;
                pos += 10;
                pos += 1;
                break;
            }
            case 4: {
                pos += 1; // double separator before second codeword
                if (!TryReadCodeword(modules, row, pos, cluster, out output[outputIndex + 1])) return false;
                pos += 15;
                pos += 1;
                if (!MatchRap(modules, row, pos, MicroPdf417Tables.RapCBits[centreRap])) return false;
                pos += 10;
                pos += 1;
                pos += 1; // double separator before third codeword
                if (!TryReadCodeword(modules, row, pos, cluster, out output[outputIndex + 2])) return false;
                pos += 15;
                pos += 1;
                pos += 1; // double separator before fourth codeword
                if (!TryReadCodeword(modules, row, pos, cluster, out output[outputIndex + 3])) return false;
                pos += 15;
                pos += 1;
                if (!MatchRap(modules, row, pos, MicroPdf417Tables.RapLRBits[rightRap])) return false;
                pos += 10;
                pos += 1;
                break;
            }
            default:
                return false;
        }

        return pos == modules.Width;
    }

    private static bool TryReadCodeword(BitMatrix modules, int row, int start, int cluster, out int codeword) {
        var pattern = ReadBits(modules, row, start, 15);
        var lookup = MicroPdf417Tables.CodewordLookup[cluster];
        codeword = lookup[pattern];
        return codeword >= 0;
    }

    private static bool MatchRap(BitMatrix modules, int row, int start, int expected) {
        return ReadBits(modules, row, start, 10) == expected;
    }

    private static int ReadBits(BitMatrix modules, int row, int start, int length) {
        var value = 0;
        for (var i = 0; i < length; i++) {
            value = (value << 1) | (modules[start + i, row] ? 1 : 0);
        }
        return value;
    }

    private static int NextRap(int value) => value == 52 ? 1 : value + 1;

    private static bool TryGetColumns(int width, out int columns) {
        columns = width switch {
            38 => 1,
            55 => 2,
            82 => 3,
            99 => 4,
            _ => 0
        };
        return columns != 0;
    }

    private static int[] GenerateErrorCorrection(int[] codewords, int dataCount, int eccCount, int coeffOffset) {
        var correction = new int[eccCount];
        for (var i = 0; i < dataCount; i++) {
            var total = (codewords[i] + correction[eccCount - 1]) % 929;
            for (var j = eccCount - 1; j >= 0; j--) {
                var coefficient = MicroPdf417Tables.MicroCoefficients[coeffOffset + j];
                if (j == 0) {
                    correction[j] = (929 - (total * coefficient) % 929) % 929;
                } else {
                    correction[j] = (correction[j - 1] + 929 - (total * coefficient) % 929) % 929;
                }
            }
        }

        for (var j = 0; j < eccCount; j++) {
            if (correction[j] != 0) correction[j] = 929 - correction[j];
        }

        var ecc = new int[eccCount];
        for (var i = 0; i < eccCount; i++) {
            ecc[i] = correction[eccCount - 1 - i];
        }

        return ecc;
    }
}
