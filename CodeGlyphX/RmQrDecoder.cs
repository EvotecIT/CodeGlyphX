using System;
using CodeGlyphX.Qr;
using CodeGlyphX.RmQr;

namespace CodeGlyphX;

/// <summary>
/// Decodes rectangular Micro QR (rMQR) symbols from exact module grids.
/// </summary>
public static class RmQrDecoder {
    /// <summary>
    /// Attempts to decode an exact rMQR module grid without a quiet zone.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out RmQrDecoded decoded) {
        decoded = null!;
        if (modules is null) return false;
        var version = RmQrTables.FindVersion(modules.Width, modules.Height);
        if (version == 0) return false;
        if (!TryDecodeFormat(modules, version, out var ecc)) return false;

        var function = new BitMatrix(modules.Width, modules.Height);
        var expected = new BitMatrix(modules.Width, modules.Height);
        RmQrMatrix.SetupFunctionPatterns(expected, function);
        if (!MatchesCriticalPatterns(modules, expected, function)) return false;

        var unmasked = modules.Clone();
        RmQrMatrix.ApplyMask(unmasked, function);
        byte[] interleaved;
        try {
            interleaved = RmQrMatrix.ReadCodewords(unmasked, function, RmQrTables.GetTotalCodewords(version));
        } catch (InvalidOperationException) {
            return false;
        }
        if (!TryCorrectAndDeinterleave(interleaved, version, ecc, out var data)) return false;
        if (!RmQrPayloadParser.TryParse(data, version, out var payload, out var text, out var isGs1, out var eci)) return false;

        decoded = new RmQrDecoded(version, RmQrTables.GetVersionName(version), ecc, isGs1, eci, payload, text);
        return true;
    }

    private static bool TryDecodeFormat(BitMatrix modules, int version, out QrErrorCorrectionLevel ecc) {
        ecc = QrErrorCorrectionLevel.M;
        var left = RmQrMatrix.ReadLeftFormatInformation(modules);
        var right = RmQrMatrix.ReadRightFormatInformation(modules);
        var mData = version - 1;
        var hData = version - 1 + 32;
        var mDistance = Math.Min(CountBits(left ^ RmQrTables.GetLeftFormatInformation(mData)), CountBits(right ^ RmQrTables.GetRightFormatInformation(mData)));
        var hDistance = Math.Min(CountBits(left ^ RmQrTables.GetLeftFormatInformation(hData)), CountBits(right ^ RmQrTables.GetRightFormatInformation(hData)));
        var best = Math.Min(mDistance, hDistance);
        if (best > 3) return false;
        ecc = hDistance < mDistance ? QrErrorCorrectionLevel.H : QrErrorCorrectionLevel.M;
        return true;
    }

    private static bool MatchesCriticalPatterns(BitMatrix modules, BitMatrix expected, BitMatrix function) {
        var mismatches = 0;
        for (var y = 0; y < modules.Height; y++) {
            for (var x = 0; x < modules.Width; x++) {
                if (!function[x, y] || IsFormatCoordinate(modules, x, y)) continue;
                if (modules[x, y] != expected[x, y] && ++mismatches > 4) return false;
            }
        }
        return true;
    }

    private static bool IsFormatCoordinate(BitMatrix modules, int x, int y) {
        if (y is >= 1 and <= 5 && x is >= 8 and <= 10) return true;
        if (x == 11 && y is >= 1 and <= 3) return true;
        if (y >= modules.Height - 6 && y <= modules.Height - 2 && x >= modules.Width - 8 && x <= modules.Width - 6) return true;
        return y == modules.Height - 6 && x >= modules.Width - 5 && x <= modules.Width - 3;
    }

    private static bool TryCorrectAndDeinterleave(
        byte[] interleaved,
        int version,
        QrErrorCorrectionLevel ecc,
        out byte[] data) {
        var dataLength = RmQrTables.GetDataCodewords(version, ecc);
        var blocks = RmQrTables.GetBlocks(version, ecc);
        var eccPerBlock = (interleaved.Length - dataLength) / blocks;
        var shortDataLength = dataLength / blocks;
        var shortBlocks = blocks - dataLength % blocks;
        data = new byte[dataLength];
        var output = 0;
        for (var blockIndex = 0; blockIndex < blocks; blockIndex++) {
            var blockDataLength = shortDataLength + (blockIndex >= shortBlocks ? 1 : 0);
            var block = new byte[blockDataLength + eccPerBlock];
            for (var i = 0; i < shortDataLength; i++) block[i] = interleaved[i * blocks + blockIndex];
            if (blockDataLength > shortDataLength) {
                block[shortDataLength] = interleaved[shortDataLength * blocks + blockIndex - shortBlocks];
            }
            for (var i = 0; i < eccPerBlock; i++) block[blockDataLength + i] = interleaved[dataLength + i * blocks + blockIndex];
            if (!QrReedSolomonDecoder.TryCorrectInPlace(block, eccPerBlock)) return false;
            Buffer.BlockCopy(block, 0, data, output, blockDataLength);
            output += blockDataLength;
        }
        return output == data.Length;
    }

    private static int CountBits(int value) {
        var count = 0;
        while (value != 0) {
            value &= value - 1;
            count++;
        }
        return count;
    }
}
