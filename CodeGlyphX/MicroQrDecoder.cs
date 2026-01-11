using System;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX;

/// <summary>
/// Decodes Micro QR codes from a module grid.
/// </summary>
public static class MicroQrDecoder {
    /// <summary>
    /// Attempts to decode a Micro QR code from an exact module grid (no quiet zone).
    /// </summary>
    /// <param name="modules">Square matrix of Micro QR modules (dark = <c>true</c>).</param>
    /// <param name="result">Decoded payload.</param>
    /// <returns><c>true</c> when decoding succeeded; otherwise <c>false</c>.</returns>
    public static bool TryDecode(BitMatrix modules, out MicroQrDecoded result) {
        result = null!;
        if (modules is null || modules.Width != modules.Height) return false;

        var size = modules.Width;
        if (size is < 11 or > 17 || ((size - 9) & 1) != 0) return false;
        var version = (size - 9) / 2;
        if (version is < 1 or > 4) return false;

        if (!TryDecodeFormat(modules, version, out var ecc, out var mask)) return false;
        var dataBits = MicroQrTables.GetDataLengthBits(version, ecc);
        var dataLen = MicroQrTables.GetDataLengthBytes(version, ecc);
        var eccLen = MicroQrTables.GetEccLength(version, ecc);
        if (dataBits <= 0 || dataLen <= 0 || eccLen <= 0) return false;

        var isFunction = new BitMatrix(size, size);
        MicroQrEncoder_DrawFunctionPatterns(version, isFunction);

        var unmasked = modules.Clone();
        ApplyMask(mask, unmasked, isFunction);

        var dataBytes = new byte[dataLen];
        var eccBytes = new byte[eccLen];
        var filler = new MicroQrFrameFiller(size, isFunction);

        for (var i = 0; i < dataBits; i++) {
            var bit = filler.ReadNext(unmasked);
            if (bit is null) return false;
            if (bit.Value) dataBytes[i >> 3] |= (byte)(1 << (7 - (i & 7)));
        }

        for (var i = 0; i < eccLen * 8; i++) {
            var bit = filler.ReadNext(unmasked);
            if (bit is null) return false;
            if (bit.Value) eccBytes[i >> 3] |= (byte)(1 << (7 - (i & 7)));
        }

        var codewords = new byte[dataLen + eccLen];
        Array.Copy(dataBytes, 0, codewords, 0, dataLen);
        Array.Copy(eccBytes, 0, codewords, dataLen, eccLen);
        if (!QrReedSolomonDecoder.TryCorrectInPlace(codewords, eccLen)) return false;

        Array.Copy(codewords, 0, dataBytes, 0, dataLen);
        if (!MicroQrPayloadParser.TryParse(dataBytes, dataBits, version, out var payload, out var text)) return false;

        result = new MicroQrDecoded(version, ecc, mask, payload, text);
        return true;
    }

    private static bool TryDecodeFormat(BitMatrix modules, int version, out QrErrorCorrectionLevel ecc, out int mask) {
        ecc = QrErrorCorrectionLevel.L;
        mask = 0;
        var raw = ReadFormatBits(modules);
        var bestDist = int.MaxValue;
        var bestMask = 0;
        var bestEcc = QrErrorCorrectionLevel.L;
        var found = false;

        foreach (var level in new[] { QrErrorCorrectionLevel.L, QrErrorCorrectionLevel.M, QrErrorCorrectionLevel.Q }) {
            if (!MicroQrTables.IsSupported(version, level)) continue;
            for (var m = 0; m < 4; m++) {
                var expected = MicroQrTables.GetFormatInfo(m, version, level);
                if (expected == 0) continue;
                var dist = CountBits(raw ^ expected);
                if (dist < bestDist) {
                    bestDist = dist;
                    bestMask = m;
                    bestEcc = level;
                    found = true;
                }
            }
        }

        if (!found || bestDist > 3) return false;
        ecc = bestEcc;
        mask = bestMask;
        return true;
    }

    private static int ReadFormatBits(BitMatrix modules) {
        var v = 0;
        for (var i = 0; i < 8; i++) {
            if (modules[8, i + 1]) v |= 1 << i;
        }
        for (var i = 0; i < 7; i++) {
            if (modules[7 - i, 8]) v |= 1 << (8 + i);
        }
        return v;
    }

    private static int CountBits(int x) {
        var count = 0;
        while (x != 0) {
            x &= x - 1;
            count++;
        }
        return count;
    }

    private static void ApplyMask(int mask, BitMatrix modules, BitMatrix isFunction) {
        var size = modules.Width;
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                if (isFunction[x, y]) continue;
                if (MicroQrMask.ShouldInvert(mask, x, y)) modules[x, y] = !modules[x, y];
            }
        }
    }

    private static void MicroQrEncoder_DrawFunctionPatterns(int version, BitMatrix isFunction) {
        var size = MicroQrTables.GetWidth(version);
        var dummy = new BitMatrix(size, size);
        MicroQrEncoder_DrawFinderPattern(dummy, isFunction);
        MicroQrEncoder_DrawSeparator(dummy, isFunction);
        MicroQrEncoder_DrawFormatArea(dummy, isFunction);
        MicroQrEncoder_DrawTimingPattern(version, dummy, isFunction);
    }

    private static void MicroQrEncoder_DrawFinderPattern(BitMatrix modules, BitMatrix isFunction) {
        for (var y = 0; y < 7; y++) {
            for (var x = 0; x < 7; x++) {
                var dark = x == 0 || x == 6 || y == 0 || y == 6 || (x is >= 2 and <= 4 && y is >= 2 and <= 4);
                modules[x, y] = dark;
                isFunction[x, y] = true;
            }
        }
    }

    private static void MicroQrEncoder_DrawSeparator(BitMatrix modules, BitMatrix isFunction) {
        for (var y = 0; y < 7; y++) {
            modules[7, y] = false;
            isFunction[7, y] = true;
        }
        for (var x = 0; x < 8; x++) {
            modules[x, 7] = false;
            isFunction[x, 7] = true;
        }
    }

    private static void MicroQrEncoder_DrawFormatArea(BitMatrix modules, BitMatrix isFunction) {
        for (var x = 1; x <= 8; x++) {
            modules[x, 8] = false;
            isFunction[x, 8] = true;
        }
        for (var y = 1; y <= 7; y++) {
            modules[8, y] = false;
            isFunction[8, y] = true;
        }
    }

    private static void MicroQrEncoder_DrawTimingPattern(int version, BitMatrix modules, BitMatrix isFunction) {
        var size = MicroQrTables.GetWidth(version);
        for (var i = 1; i < size - 7; i++) {
            var x = 7 + i;
            var dark = (i & 1) == 1;
            modules[x, 0] = dark;
            isFunction[x, 0] = true;
            modules[0, x] = dark;
            isFunction[0, x] = true;
        }
    }
}
