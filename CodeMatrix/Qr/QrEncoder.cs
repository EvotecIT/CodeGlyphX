using System;

namespace CodeMatrix.Qr;

internal static class QrEncoder {
    public static CodeMatrix.QrCode EncodeByteMode(byte[] data, QrErrorCorrectionLevel ecc, int minVersion, int maxVersion, int? forceMask) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (minVersion is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(minVersion));
        if (maxVersion is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(maxVersion));
        if (minVersion > maxVersion) throw new ArgumentOutOfRangeException(nameof(minVersion));
        if (forceMask is not null && forceMask.Value is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(forceMask));

        var version = 0;
        for (var v = minVersion; v <= maxVersion; v++) {
            var capacityBits = QrTables.GetNumDataCodewords(v, ecc) * 8;
            var requiredBits = 4 + QrTables.GetByteModeCharCountBits(v) + data.Length * 8;
            if (requiredBits <= capacityBits) {
                version = v;
                break;
            }
        }

        if (version == 0)
            throw new ArgumentException($"Data too long for QR version range {minVersion}..{maxVersion} at ECC {ecc}.");

        var dataCodewords = EncodeByteModeData(data, version, ecc);
        var allCodewords = AddEccAndInterleave(dataCodewords, version, ecc);

        var size = version * 4 + 17;
        var modules = new BitMatrix(size, size);
        var isFunction = new BitMatrix(size, size);
        DrawFunctionPatterns(version, ecc, modules, isFunction);
        DrawCodewords(allCodewords, modules, isFunction);

        var bestMask = 0;
        BitMatrix? bestModules = null;
        var bestPenalty = int.MaxValue;

        var startMask = forceMask ?? 0;
        var endMask = forceMask ?? 7;
        for (var mask = startMask; mask <= endMask; mask++) {
            var temp = modules.Clone();
            ApplyMask(mask, temp, isFunction);
            DrawFormatBits(ecc, mask, temp, isFunction);
            var penalty = QrMask.ComputePenalty(temp);
            if (penalty < bestPenalty) {
                bestPenalty = penalty;
                bestMask = mask;
                bestModules = temp;
            }
        }

        if (bestModules is null) throw new InvalidOperationException("Failed to choose mask.");
        return new CodeMatrix.QrCode(version, ecc, bestMask, bestModules);
    }

    private static byte[] EncodeByteModeData(byte[] data, int version, QrErrorCorrectionLevel ecc) {
        var dataCapacityBits = QrTables.GetNumDataCodewords(version, ecc) * 8;
        var bb = new QrBitBuffer();

        // Mode indicator: Byte (0100)
        bb.AppendBits(0b0100, 4);

        // Character count
        bb.AppendBits(data.Length, QrTables.GetByteModeCharCountBits(version));

        // Data bytes
        for (var i = 0; i < data.Length; i++) bb.AppendBits(data[i], 8);

        // Terminator
        var remaining = dataCapacityBits - bb.LengthBits;
        bb.AppendBits(0, Math.Min(4, Math.Max(0, remaining)));

        // Pad to byte boundary
        while ((bb.LengthBits & 7) != 0) bb.AppendBit(false);

        var codewords = bb.ToByteArray();
        var dataCodewords = QrTables.GetNumDataCodewords(version, ecc);
        if (codewords.Length > dataCodewords) throw new InvalidOperationException("Encoded data exceeds capacity.");

        if (codewords.Length == dataCodewords) return codewords;

        // Pad bytes: 0xEC, 0x11 alternating
        var result = new byte[dataCodewords];
        Array.Copy(codewords, result, codewords.Length);
        var pad = 0xEC;
        for (var i = codewords.Length; i < result.Length; i++) {
            result[i] = (byte)pad;
            pad = pad == 0xEC ? 0x11 : 0xEC;
        }
        return result;
    }

    private static byte[] AddEccAndInterleave(byte[] data, int version, QrErrorCorrectionLevel ecc) {
        var numBlocks = QrTables.GetNumBlocks(version, ecc);
        var blockEccLen = QrTables.GetEccCodewordsPerBlock(version, ecc);
        var rawCodewords = QrTables.GetNumRawDataModules(version) / 8;

        var numShortBlocks = numBlocks - (rawCodewords % numBlocks);
        var shortBlockLen = rawCodewords / numBlocks;
        var shortDataLen = shortBlockLen - blockEccLen;
        var longDataLen = shortDataLen + 1;

        var rsDiv = QrReedSolomon.ComputeDivisor(blockEccLen);

        var blocks = new byte[numBlocks][];
        var dataLens = new int[numBlocks];
        var k = 0;
        for (var i = 0; i < numBlocks; i++) {
            var dataLen = i < numShortBlocks ? shortDataLen : longDataLen;
            dataLens[i] = dataLen;
            var dat = new byte[dataLen];
            Array.Copy(data, k, dat, 0, dat.Length);
            k += dat.Length;

            var eccBytes = QrReedSolomon.ComputeRemainder(dat, rsDiv);
            var block = new byte[dataLen + eccBytes.Length];
            Array.Copy(dat, 0, block, 0, dat.Length);
            Array.Copy(eccBytes, 0, block, dat.Length, eccBytes.Length);
            blocks[i] = block;
        }

        var result = new byte[rawCodewords];
        var pos = 0;

        // QR interleaving is done in two phases:
        // 1) data codewords across all blocks
        // 2) error-correction codewords across all blocks
        // This matters when blocks have different data lengths (short/long blocks).
        var maxDataLen = dataLens[numBlocks - 1];
        for (var i = 0; i < maxDataLen; i++) {
            for (var j = 0; j < blocks.Length; j++) {
                if (i < dataLens[j]) result[pos++] = blocks[j][i];
            }
        }

        for (var i = 0; i < blockEccLen; i++) {
            for (var j = 0; j < blocks.Length; j++) {
                result[pos++] = blocks[j][dataLens[j] + i];
            }
        }

        if (pos != result.Length) throw new InvalidOperationException("Interleave error.");
        return result;
    }

    private static void DrawFunctionPatterns(int version, QrErrorCorrectionLevel ecc, BitMatrix modules, BitMatrix isFunction) {
        var size = modules.Width;

        DrawFinderPattern(0, 0, modules, isFunction);
        DrawFinderPattern(size - 7, 0, modules, isFunction);
        DrawFinderPattern(0, size - 7, modules, isFunction);

        // Timing patterns
        for (var i = 0; i < size; i++) {
            if (!isFunction[6, i]) SetFunctionModule(6, i, (i & 1) == 0, modules, isFunction);
            if (!isFunction[i, 6]) SetFunctionModule(i, 6, (i & 1) == 0, modules, isFunction);
        }

        // Alignment patterns
        var align = QrTables.GetAlignmentPatternPositions(version);
        for (var i = 0; i < align.Length; i++) {
            for (var j = 0; j < align.Length; j++) {
                var x = align[i];
                var y = align[j];
                if ((i == 0 && j == 0) || (i == 0 && j == align.Length - 1) || (i == align.Length - 1 && j == 0))
                    continue;
                DrawAlignmentPattern(x, y, modules, isFunction);
            }
        }

        // Dark module
        SetFunctionModule(8, size - 8, true, modules, isFunction);

        // Format bits (dummy mask for now; overwritten during mask scoring + final render)
        DrawFormatBits(ecc, 0, modules, isFunction);

        // Version bits (fixed for a version)
        if (version >= 7) DrawVersionBits(version, modules, isFunction);
    }

    private static void DrawFinderPattern(int x, int y, BitMatrix modules, BitMatrix isFunction) {
        for (var dy = -1; dy <= 7; dy++) {
            for (var dx = -1; dx <= 7; dx++) {
                var xx = x + dx;
                var yy = y + dy;
                if ((uint)xx >= (uint)modules.Width || (uint)yy >= (uint)modules.Height) continue;

                bool isDark;
                if (dx is >= 0 and <= 6 && dy is >= 0 and <= 6) {
                    isDark = (dx == 0 || dx == 6 || dy == 0 || dy == 6) || (dx is >= 2 and <= 4 && dy is >= 2 and <= 4);
                } else {
                    isDark = false; // separator
                }
                SetFunctionModule(xx, yy, isDark, modules, isFunction);
            }
        }
    }

    private static void DrawAlignmentPattern(int x, int y, BitMatrix modules, BitMatrix isFunction) {
        for (var dy = -2; dy <= 2; dy++) {
            for (var dx = -2; dx <= 2; dx++) {
                var isDark = Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1;
                SetFunctionModule(x + dx, y + dy, isDark, modules, isFunction);
            }
        }
    }

    private static void DrawVersionBits(int version, BitMatrix modules, BitMatrix isFunction) {
        // BCH(18,6) generator 0x1F25.
        var rem = version;
        for (var i = 0; i < 12; i++) rem = (rem << 1) ^ (((rem >> 11) & 1) * 0x1F25);
        var bits = (version << 12) | rem;

        var size = modules.Width;
        for (var i = 0; i < 18; i++) {
            var bit = QrTables.GetBit(bits, i);
            var a = size - 11 + (i % 3);
            var b = i / 3;
            SetFunctionModule(a, b, bit, modules, isFunction);
            SetFunctionModule(b, a, bit, modules, isFunction);
        }
    }

    private static void DrawFormatBits(QrErrorCorrectionLevel ecc, int mask, BitMatrix modules, BitMatrix isFunction) {
        // BCH(15,5) generator 0x537, XOR mask 0x5412.
        var data = (QrTables.GetEccFormatBits(ecc) << 3) | mask;
        var rem = data;
        for (var i = 0; i < 10; i++) rem = (rem << 1) ^ (((rem >> 9) & 1) * 0x537);
        var bits = ((data << 10) | rem) ^ 0x5412;

        var size = modules.Width;

        // Top-left
        for (var i = 0; i <= 5; i++) SetFunctionModule(8, i, QrTables.GetBit(bits, i), modules, isFunction);
        SetFunctionModule(8, 7, QrTables.GetBit(bits, 6), modules, isFunction);
        SetFunctionModule(8, 8, QrTables.GetBit(bits, 7), modules, isFunction);
        SetFunctionModule(7, 8, QrTables.GetBit(bits, 8), modules, isFunction);
        for (var i = 9; i < 15; i++) SetFunctionModule(14 - i, 8, QrTables.GetBit(bits, i), modules, isFunction);

        // Top-right / bottom-left
        for (var i = 0; i < 8; i++) SetFunctionModule(size - 1 - i, 8, QrTables.GetBit(bits, i), modules, isFunction);
        for (var i = 8; i < 15; i++) SetFunctionModule(8, size - 15 + i, QrTables.GetBit(bits, i), modules, isFunction);
    }

    private static void DrawCodewords(byte[] codewords, BitMatrix modules, BitMatrix isFunction) {
        var size = modules.Width;
        var bitIndex = 0;
        var upward = true;

        for (var right = size - 1; right >= 1; right -= 2) {
            if (right == 6) right = 5;

            for (var vert = 0; vert < size; vert++) {
                var y = upward ? size - 1 - vert : vert;
                for (var j = 0; j < 2; j++) {
                    var x = right - j;
                    if (isFunction[x, y]) continue;

                    var bit = false;
                    if (bitIndex < codewords.Length * 8) {
                        bit = ((codewords[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                    }
                    modules[x, y] = bit;
                    bitIndex++;
                }
            }

            upward = !upward;
        }
    }

    private static void ApplyMask(int mask, BitMatrix modules, BitMatrix isFunction) {
        var size = modules.Width;
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                if (isFunction[x, y]) continue;
                if (QrMask.ShouldInvert(mask, x, y)) modules[x, y] = !modules[x, y];
            }
        }
    }

    private static void SetFunctionModule(int x, int y, bool isDark, BitMatrix modules, BitMatrix isFunction) {
        modules[x, y] = isDark;
        isFunction[x, y] = true;
    }
}
