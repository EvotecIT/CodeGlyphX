using System;
using System.Text;
using CodeMatrix.Qr;

namespace CodeMatrix;

public static class QrDecoder {
    public static bool TryDecode(BitMatrix modules, out QrDecoded result) {
        result = null!;
        if (modules is null) return false;
        if (modules.Width != modules.Height) return false;

        var size = modules.Width;
        var version = (size - 17) / 4;
        if (version is < 1 or > 40) return false;
        if (size != version * 4 + 17) return false;

        if (!TryDecodeFormat(modules, out var ecc, out var mask)) return false;

        var functionMask = BuildFunctionMask(version, size);

        var unmasked = modules.Clone();
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                if (functionMask[x, y]) continue;
                if (QrMask.ShouldInvert(mask, x, y)) unmasked[x, y] = !unmasked[x, y];
            }
        }

        var rawCodewords = QrTables.GetNumRawDataModules(version) / 8;
        var codewords = new byte[rawCodewords];

        var bitIndex = 0;
        var upward = true;
        for (var right = size - 1; right >= 1; right -= 2) {
            if (right == 6) right = 5;

            for (var vert = 0; vert < size; vert++) {
                var y = upward ? size - 1 - vert : vert;
                for (var j = 0; j < 2; j++) {
                    var x = right - j;
                    if (functionMask[x, y]) continue;

                    if (bitIndex < rawCodewords * 8 && unmasked[x, y]) {
                        codewords[bitIndex >> 3] |= (byte)(1 << (7 - (bitIndex & 7)));
                    }

                    bitIndex++;
                }
            }

            upward = !upward;
        }

        if (!TryCorrectAndExtractData(codewords, version, ecc, out var dataCodewords)) return false;
        if (!TryParseByteMode(dataCodewords, version, out var payload)) return false;

        var text = Encoding.UTF8.GetString(payload);
        result = new QrDecoded(version, ecc, mask, payload, text);
        return true;
    }

#if NET8_0_OR_GREATER
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result) {
        return QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, out result);
    }
#endif

    private static bool TryDecodeFormat(BitMatrix modules, out QrErrorCorrectionLevel ecc, out int mask) {
        var size = modules.Width;

        var bitsA = 0;
        for (var i = 0; i <= 5; i++) if (modules[8, i]) bitsA |= 1 << i;
        if (modules[8, 7]) bitsA |= 1 << 6;
        if (modules[8, 8]) bitsA |= 1 << 7;
        if (modules[7, 8]) bitsA |= 1 << 8;
        for (var i = 9; i < 15; i++) if (modules[14 - i, 8]) bitsA |= 1 << i;

        var bitsB = 0;
        for (var i = 0; i < 8; i++) if (modules[size - 1 - i, 8]) bitsB |= 1 << i;
        for (var i = 8; i < 15; i++) if (modules[8, size - 15 + i]) bitsB |= 1 << i;

        return TryDecodeFormatBits(bitsA, bitsB, out ecc, out mask);
    }

    private static readonly int[] FormatPatterns = BuildFormatPatterns();

    private static int[] BuildFormatPatterns() {
        var patterns = new int[32];
        var idx = 0;
        foreach (QrErrorCorrectionLevel ecc in new[] { QrErrorCorrectionLevel.L, QrErrorCorrectionLevel.M, QrErrorCorrectionLevel.Q, QrErrorCorrectionLevel.H }) {
            for (var mask = 0; mask < 8; mask++) {
                var data = (QrTables.GetEccFormatBits(ecc) << 3) | mask;
                var rem = data;
                for (var i = 0; i < 10; i++) rem = (rem << 1) ^ (((rem >> 9) & 1) * 0x537);
                var bits = ((data << 10) | rem) ^ 0x5412;
                patterns[idx++] = bits;
            }
        }
        return patterns;
    }

    private static bool TryDecodeFormatBits(int bitsA, int bitsB, out QrErrorCorrectionLevel ecc, out int mask) {
        var bestDist = int.MaxValue;
        var best = -1;

        for (var i = 0; i < FormatPatterns.Length; i++) {
            var candidate = FormatPatterns[i];
            var dist = Math.Min(CountBits(bitsA ^ candidate), CountBits(bitsB ^ candidate));
            if (dist < bestDist) {
                bestDist = dist;
                best = i;
            }
        }

        if (bestDist > 3 || best < 0) {
            ecc = default;
            mask = default;
            return false;
        }

        // best index layout: ecc in {L,M,Q,H} each with 8 masks
        var eccIndex = best / 8;
        mask = best % 8;
        ecc = eccIndex switch {
            0 => QrErrorCorrectionLevel.L,
            1 => QrErrorCorrectionLevel.M,
            2 => QrErrorCorrectionLevel.Q,
            _ => QrErrorCorrectionLevel.H,
        };
        return true;
    }

    private static int CountBits(int x) {
        unchecked {
            x = x - ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            return (((x + (x >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }
    }

    private static BitMatrix BuildFunctionMask(int version, int size) {
        var isFunction = new BitMatrix(size, size);

        MarkFinder(0, 0, isFunction);
        MarkFinder(size - 7, 0, isFunction);
        MarkFinder(0, size - 7, isFunction);

        // Timing patterns
        for (var i = 0; i < size; i++) {
            isFunction[6, i] = true;
            isFunction[i, 6] = true;
        }

        // Alignment patterns
        var align = QrTables.GetAlignmentPatternPositions(version);
        for (var i = 0; i < align.Length; i++) {
            for (var j = 0; j < align.Length; j++) {
                if ((i == 0 && j == 0) || (i == 0 && j == align.Length - 1) || (i == align.Length - 1 && j == 0))
                    continue;
                MarkAlignment(align[i], align[j], isFunction);
            }
        }

        // Dark module
        isFunction[8, size - 8] = true;

        // Format info
        for (var i = 0; i <= 5; i++) isFunction[8, i] = true;
        isFunction[8, 7] = true;
        isFunction[8, 8] = true;
        isFunction[7, 8] = true;
        for (var i = 9; i < 15; i++) isFunction[14 - i, 8] = true;
        for (var i = 0; i < 8; i++) isFunction[size - 1 - i, 8] = true;
        for (var i = 8; i < 15; i++) isFunction[8, size - 15 + i] = true;

        // Version info
        if (version >= 7) {
            for (var i = 0; i < 18; i++) {
                var a = size - 11 + (i % 3);
                var b = i / 3;
                isFunction[a, b] = true;
                isFunction[b, a] = true;
            }
        }

        return isFunction;
    }

    private static void MarkFinder(int x, int y, BitMatrix isFunction) {
        for (var dy = -1; dy <= 7; dy++) {
            for (var dx = -1; dx <= 7; dx++) {
                var xx = x + dx;
                var yy = y + dy;
                if ((uint)xx >= (uint)isFunction.Width || (uint)yy >= (uint)isFunction.Height) continue;
                isFunction[xx, yy] = true;
            }
        }
    }

    private static void MarkAlignment(int x, int y, BitMatrix isFunction) {
        for (var dy = -2; dy <= 2; dy++) {
            for (var dx = -2; dx <= 2; dx++) {
                isFunction[x + dx, y + dy] = true;
            }
        }
    }

    private static bool TryCorrectAndExtractData(byte[] codewords, int version, QrErrorCorrectionLevel ecc, out byte[] dataCodewords) {
        dataCodewords = null!;

        var numBlocks = QrTables.GetNumBlocks(version, ecc);
        var blockEccLen = QrTables.GetEccCodewordsPerBlock(version, ecc);
        var rawCodewords = QrTables.GetNumRawDataModules(version) / 8;
        var dataLen = QrTables.GetNumDataCodewords(version, ecc);

        var numShortBlocks = numBlocks - (rawCodewords % numBlocks);
        var shortBlockLen = rawCodewords / numBlocks;
        var shortDataLen = shortBlockLen - blockEccLen;
        var longDataLen = shortDataLen + 1;

        var blocks = new byte[numBlocks][];
        for (var i = 0; i < numBlocks; i++) {
            var len = shortBlockLen + (i < numShortBlocks ? 0 : 1);
            blocks[i] = new byte[len];
        }

        // Deinterleave
        var maxLen = blocks[numBlocks - 1].Length;
        var k = 0;
        for (var i = 0; i < maxLen; i++) {
            for (var j = 0; j < blocks.Length; j++) {
                if (i < blocks[j].Length) {
                    blocks[j][i] = codewords[k++];
                }
            }
        }
        if (k != codewords.Length) return false;

        // Correct each block and concatenate data parts
        var data = new byte[dataLen];
        var di = 0;
        for (var i = 0; i < blocks.Length; i++) {
            var block = blocks[i];
            if (!QrReedSolomonDecoder.TryCorrectInPlace(block, blockEccLen)) return false;

            var partLen = i < numShortBlocks ? shortDataLen : longDataLen;
            Array.Copy(block, 0, data, di, partLen);
            di += partLen;
        }
        if (di != data.Length) return false;

        dataCodewords = data;
        return true;
    }

    private static bool TryParseByteMode(byte[] dataCodewords, int version, out byte[] payload) {
        payload = null!;

        var bitLen = dataCodewords.Length * 8;
        var bitPos = 0;

        int ReadBits(int n) {
            if (n == 0) return 0;
            if (n < 0 || n > 31) throw new ArgumentOutOfRangeException(nameof(n));
            if (bitPos + n > bitLen) return -1;
            var val = 0;
            for (var i = 0; i < n; i++) {
                var b = (dataCodewords[(bitPos + i) >> 3] >> (7 - ((bitPos + i) & 7))) & 1;
                val = (val << 1) | b;
            }
            bitPos += n;
            return val;
        }

        var mode = ReadBits(4);
        if (mode < 0) return false;
        if (mode == 0) {
            payload = Array.Empty<byte>();
            return true;
        }
        if (mode != 0b0100) return false;

        var countBits = QrTables.GetByteModeCharCountBits(version);
        var count = ReadBits(countBits);
        if (count < 0) return false;

        var bytes = new byte[count];
        for (var i = 0; i < bytes.Length; i++) {
            var b = ReadBits(8);
            if (b < 0) return false;
            bytes[i] = (byte)b;
        }

        payload = bytes;
        return true;
    }
}
