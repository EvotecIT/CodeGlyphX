using System;
using CodeGlyphX.Internal.ReedSolomon;
using CodeGlyphX.MaxiCode;

namespace CodeGlyphX;

/// <summary>
/// Decodes exact sampled MaxiCode module grids with Reed–Solomon correction.
/// </summary>
public static class MaxiCodeDecoder {
    /// <summary>Attempts to decode a 30 by 33 MaxiCode module grid.</summary>
    public static bool TryDecodeDetailed(BitMatrix modules, out MaxiCodeDecoded decoded) {
        decoded = null!;
        if (modules is null || modules.Width != MaxiCodeTables.Width || modules.Height != MaxiCodeTables.Height) return false;
        if (!HasOrientationMarkers(modules)) return false;
        try {
            var codewords = ReadCodewords(modules);
            if (!Correct(codewords, start: 0, dataLength: 10, eccLength: 10, parity: -1)) return false;
            var modeValue = codewords[0] & 0x0F;
            if (modeValue is < 2 or > 6) return false;
            var mode = (MaxiCodeMode)modeValue;
            var secondaryData = mode == MaxiCodeMode.FullEcc ? 68 : 84;
            var secondaryEcc = mode == MaxiCodeMode.FullEcc ? 56 : 40;
            if (!Correct(codewords, 20, secondaryData, secondaryEcc, parity: 0) ||
                !Correct(codewords, 20, secondaryData, secondaryEcc, parity: 1)) return false;

            var message = ExtractMessage(codewords, mode);
            if (!MaxiCodeHighLevelDecoder.TryDecode(message, out var highLevel)) return false;
            string? postalCode = null;
            int? countryCode = null;
            int? serviceClass = null;
            if (mode is MaxiCodeMode.StructuredCarrierNumeric or MaxiCodeMode.StructuredCarrierAlphanumeric) {
                postalCode = mode == MaxiCodeMode.StructuredCarrierNumeric
                    ? DecodeNumericPostalCode(codewords)
                    : DecodeAlphanumericPostalCode(codewords);
                countryCode = GetInt(codewords, 53, 54, 43, 44, 45, 46, 47, 48, 37, 38);
                serviceClass = GetInt(codewords, 55, 56, 57, 58, 59, 60, 49, 50, 51, 52);
            }

            decoded = new MaxiCodeDecoded(mode, highLevel.Text, highLevel.Bytes, postalCode, countryCode, serviceClass,
                highLevel.EciAssignments, highLevel.StructuredAppendIndex, highLevel.StructuredAppendCount);
            return true;
        } catch (ReedSolomonException) {
            return false;
        } catch (ArgumentException) {
            return false;
        }
    }

    /// <summary>Attempts to decode a MaxiCode grid and return only its secondary text message.</summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        if (TryDecodeDetailed(modules, out var decoded)) { text = decoded.Text; return true; }
        text = string.Empty;
        return false;
    }

    private static byte[] ReadCodewords(BitMatrix modules) {
        var codewords = new byte[MaxiCodeTables.CodewordCount];
        for (var y = 0; y < MaxiCodeTables.Height; y++) {
            for (var x = 0; x < MaxiCodeTables.Width; x++) {
                var bit = MaxiCodeTables.ModuleBitNumbers[y * MaxiCodeTables.Width + x];
                if (bit >= 0 && modules[x, y]) codewords[bit / 6] |= (byte)(1 << (5 - bit % 6));
            }
        }
        return codewords;
    }

    private static bool Correct(byte[] codewords, int start, int dataLength, int eccLength, int parity) {
        var divisor = parity < 0 ? 1 : 2;
        var block = new int[(dataLength + eccLength) / divisor];
        var target = 0;
        for (var i = 0; i < dataLength + eccLength; i++) {
            if (parity >= 0 && (i & 1) != parity) continue;
            block[target++] = codewords[start + i];
        }
        new ReedSolomonDecoder(GenericGf.MaxiCode).Decode(block, eccLength / divisor);
        target = 0;
        for (var i = 0; i < dataLength; i++) {
            if (parity >= 0 && (i & 1) != parity) continue;
            codewords[start + i] = (byte)block[target++];
        }
        return true;
    }

    private static byte[] ExtractMessage(byte[] codewords, MaxiCodeMode mode) {
        if (mode is MaxiCodeMode.StructuredCarrierNumeric or MaxiCodeMode.StructuredCarrierAlphanumeric) {
            var carrier = new byte[84];
            Array.Copy(codewords, 20, carrier, 0, carrier.Length);
            return carrier;
        }
        var length = mode == MaxiCodeMode.FullEcc ? 77 : 93;
        var message = new byte[length];
        Array.Copy(codewords, 1, message, 0, 9);
        Array.Copy(codewords, 20, message, 9, length - 9);
        return message;
    }

    private static string DecodeNumericPostalCode(byte[] codewords) {
        var value = GetInt(codewords, 33, 34, 35, 36, 25, 26, 27, 28, 29, 30, 19, 20, 21, 22, 23, 24,
            13, 14, 15, 16, 17, 18, 7, 8, 9, 10, 11, 12, 1, 2);
        var length = Math.Min(GetInt(codewords, 39, 40, 41, 42, 31, 32), 9);
        return value.ToString().PadLeft(length, '0');
    }

    private static string DecodeAlphanumericPostalCode(byte[] codewords) {
        var groups = new[] {
            new[] { 39, 40, 41, 42, 31, 32 }, new[] { 33, 34, 35, 36, 25, 26 },
            new[] { 27, 28, 29, 30, 19, 20 }, new[] { 21, 22, 23, 24, 13, 14 },
            new[] { 15, 16, 17, 18, 7, 8 }, new[] { 9, 10, 11, 12, 1, 2 }
        };
        var chars = new char[6];
        for (var i = 0; i < chars.Length; i++) {
            var symbol = GetInt(codewords, groups[i]);
            if (!MaxiCodeTables.TryDecodeSymbol(0, symbol, out var value)) return string.Empty;
            chars[i] = (char)value;
        }
        return new string(chars).TrimEnd();
    }

    private static int GetInt(byte[] codewords, params int[] bitPositions) {
        var value = 0;
        for (var i = 0; i < bitPositions.Length; i++) {
            var bit = bitPositions[i] - 1;
            value = (value << 1) | ((codewords[bit / 6] >> (5 - bit % 6)) & 1);
        }
        return value;
    }

    private static bool HasOrientationMarkers(BitMatrix matrix) {
        return matrix[28, 0] && matrix[29, 0] && matrix[10, 9] && matrix[11, 9] && matrix[11, 10] &&
               matrix[7, 15] && matrix[8, 16] && matrix[20, 16] && matrix[20, 17] && matrix[10, 22] &&
               matrix[10, 23] && matrix[17, 22] && matrix[17, 23];
    }
}
