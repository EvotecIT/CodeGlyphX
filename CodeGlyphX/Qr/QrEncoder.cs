using System;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Qr;

internal static partial class QrEncoder {

    private static byte[] AddEccAndInterleave(byte[] data, int version, QrErrorCorrectionLevel ecc) {
        var numBlocks = QrTables.GetNumBlocks(version, ecc);
        var blockEccLen = QrTables.GetEccCodewordsPerBlock(version, ecc);
        var rawCodewords = QrTables.GetNumRawDataModules(version) / 8;

        var numShortBlocks = numBlocks - (rawCodewords % numBlocks);
        var shortBlockLen = rawCodewords / numBlocks;
        var shortDataLen = shortBlockLen - blockEccLen;
        var longDataLen = shortDataLen + 1;

        var rsDiv = QrReedSolomon.ComputeDivisor(blockEccLen);

        var useStack = numBlocks <= 128;
        Span<int> dataLens = useStack ? stackalloc int[numBlocks] : new int[numBlocks];
        Span<int> dataOffsets = useStack ? stackalloc int[numBlocks] : new int[numBlocks];

        var dataOffset = 0;
        for (var i = 0; i < numBlocks; i++) {
            var dataLen = i < numShortBlocks ? shortDataLen : longDataLen;
            dataLens[i] = dataLen;
            dataOffsets[i] = dataOffset;
            dataOffset += dataLen;
        }

        if (dataOffset != data.Length) throw new InvalidOperationException("Data block sizing error.");

        var result = new byte[rawCodewords];
        var pos = 0;

        // QR interleaving is done in two phases:
        // 1) data codewords across all blocks
        // 2) error-correction codewords across all blocks
        // This matters when blocks have different data lengths (short/long blocks).
        var maxDataLen = numShortBlocks == numBlocks ? shortDataLen : longDataLen;
        for (var i = 0; i < maxDataLen; i++) {
            for (var j = 0; j < numBlocks; j++) {
                if (i < dataLens[j]) result[pos++] = data[dataOffsets[j] + i];
            }
        }

        var eccStart = pos;
        Span<byte> eccBuffer = blockEccLen <= 256 ? stackalloc byte[blockEccLen] : new byte[blockEccLen];
        for (var j = 0; j < numBlocks; j++) {
            var dataLen = dataLens[j];
            var dataStart = dataOffsets[j];
            QrReedSolomon.ComputeRemainder(data.AsSpan(dataStart, dataLen), rsDiv, eccBuffer);
            for (var i = 0; i < blockEccLen; i++) {
                result[eccStart + i * numBlocks + j] = eccBuffer[i];
            }
        }

        if (eccStart + blockEccLen * numBlocks != result.Length) throw new InvalidOperationException("Interleave error.");
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
        var modulesWords = modules.Words;
        var functionWords = isFunction.Words;
        var dataBitIndex = 0;
        var dataBitCount = codewords.Length << 3;
        var upward = true;

        for (var right = size - 1; right >= 1; right -= 2) {
            if (right == 6) right = 5;

            for (var vert = 0; vert < size; vert++) {
                var y = upward ? size - 1 - vert : vert;
                var rowBase = y * size;
                for (var j = 0; j < 2; j++) {
                    var x = right - j;
                    var bitIndex = rowBase + x;
                    var wordIndex = bitIndex >> 5;
                    var bitMask = 1u << (bitIndex & 31);
                    if ((functionWords[wordIndex] & bitMask) != 0) continue;

                    var bit = false;
                    if (dataBitIndex < dataBitCount) {
                        bit = ((codewords[dataBitIndex >> 3] >> (7 - (dataBitIndex & 7))) & 1) != 0;
                    }
                    if (bit) modulesWords[wordIndex] |= bitMask;
                    else modulesWords[wordIndex] &= ~bitMask;
                    dataBitIndex++;
                }
            }

            upward = !upward;
        }
    }

    private static void ApplyMask(int mask, BitMatrix modules, BitMatrix isFunction) {
        var size = modules.Width;
        var modulesWords = modules.Words;
        var functionWords = isFunction.Words;
        for (var y = 0; y < size; y++) {
            var rowBase = y * size;
            for (var x = 0; x < size; x++) {
                var bitIndex = rowBase + x;
                var wordIndex = bitIndex >> 5;
                var bitMask = 1u << (bitIndex & 31);
                if ((functionWords[wordIndex] & bitMask) != 0) continue;
                if (QrMask.ShouldInvert(mask, x, y)) modulesWords[wordIndex] ^= bitMask;
            }
        }
    }

    private static void SetFunctionModule(int x, int y, bool isDark, BitMatrix modules, BitMatrix isFunction) {
        modules[x, y] = isDark;
        isFunction[x, y] = true;
    }
}
