using System;
using System.Buffers;
using CodeGlyphX.Qr;

namespace CodeGlyphX;

public static partial class QrDecoder {
    private static bool TryExtractDataCodewords(BitMatrix modules, BitMatrix functionMask, int version, QrErrorCorrectionLevel ecc, int mask, Func<bool>? shouldStop, byte[]? extractionCache, ref int extractedMasks, out byte[] dataCodewords) {
        dataCodewords = null!;

        var size = modules.Width;
        var rawCodewords = QrTables.GetNumRawDataModules(version) / 8;
        var codewords = ArrayPool<byte>.Shared.Rent(rawCodewords);
        if (extractionCache is not null && (extractedMasks & (1 << mask)) != 0) {
            try {
                // Correction can mutate a single-block stream; keep the cached bytes immutable.
                Array.Copy(extractionCache, mask * rawCodewords, codewords, 0, rawCodewords);
                return TryCorrectAndExtractData(codewords, rawCodewords, version, ecc, shouldStop, out dataCodewords);
            } finally {
                ArrayPool<byte>.Shared.Return(codewords, clearArray: false);
            }
        }
        Array.Clear(codewords, 0, rawCodewords);
        var totalBits = rawCodewords * 8;
        var moduleWords = modules.Words;
        var functionWords = functionMask.Words;
        Span<byte> xMod2 = stackalloc byte[size];
        Span<byte> xMod3 = stackalloc byte[size];
        Span<byte> xDiv3Parity = stackalloc byte[size];
        Span<byte> yMod2 = stackalloc byte[size];
        Span<byte> yMod3 = stackalloc byte[size];
        Span<byte> yMod3Expected = stackalloc byte[size];
        Span<byte> yDiv2Parity = stackalloc byte[size];
        Span<byte> yMod2Zero = stackalloc byte[size];
        Span<byte> yMod3Zero = stackalloc byte[size];
        for (var i = 0; i < size; i++) {
            xMod2[i] = (byte)(i & 1);
            xMod3[i] = (byte)(i % 3);
            xDiv3Parity[i] = (byte)((i / 3) & 1);
        }
        for (var i = 0; i < size; i++) {
            var mod3 = (byte)(i % 3);
            var mod2 = (byte)(i & 1);
            yMod2[i] = mod2;
            yMod3[i] = mod3;
            yMod3Expected[i] = mod3 == 0 ? (byte)0 : (byte)(3 - mod3);
            yDiv2Parity[i] = (byte)((i >> 1) & 1);
            yMod2Zero[i] = (byte)(mod2 == 0 ? 1 : 0);
            yMod3Zero[i] = (byte)(mod3 == 0 ? 1 : 0);
        }

        var bitIndex = 0;
        var upward = true;
        var done = false;
        try {
            if (shouldStop is null) {
                for (var right = size - 1; right >= 1; right -= 2) {
                    if (right == 6) right = 5;
                    var x0 = right;
                    var x1 = right - 1;
                    var x0m2 = xMod2[x0];
                    var x1m2 = xMod2[x1];
                    var x0m3 = xMod3[x0];
                    var x1m3 = xMod3[x1];
                    var x0d3 = xDiv3Parity[x0];
                    var x1d3 = xDiv3Parity[x1];

                    for (var vert = 0; vert < size; vert++) {
                        var y = upward ? size - 1 - vert : vert;
                        var rowOffset = y * size;
                        var ym2 = yMod2[y];
                        var ym3 = yMod3[y];
                        var yExp3 = yMod3Expected[y];
                        var yd2 = yDiv2Parity[y];
                        var ym2Zero = yMod2Zero[y] != 0;
                        var ym3Zero = yMod3Zero[y] != 0;
                        bool invert0;
                        bool invert1;
                        switch (mask) {
                            case 0:
                                invert0 = x0m2 == ym2;
                                invert1 = x1m2 == ym2;
                                break;
                            case 1:
                                invert0 = ym2 == 0;
                                invert1 = invert0;
                                break;
                            case 2:
                                invert0 = x0m3 == 0;
                                invert1 = x1m3 == 0;
                                break;
                            case 3:
                                invert0 = x0m3 == yExp3;
                                invert1 = x1m3 == yExp3;
                                break;
                            case 4:
                                invert0 = x0d3 == yd2;
                                invert1 = x1d3 == yd2;
                                break;
                            case 5:
                                invert0 = (x0m2 == 0 || ym2Zero) && (x0m3 == 0 || ym3Zero);
                                invert1 = (x1m2 == 0 || ym2Zero) && (x1m3 == 0 || ym3Zero);
                                break;
                            case 6: {
                                var xyMod3Parity0 = x0m3 == ym3 && x0m3 != 0;
                                invert0 = (x0m2 & ym2) == (xyMod3Parity0 ? (byte)1 : (byte)0);
                                var xyMod3Parity1 = x1m3 == ym3 && x1m3 != 0;
                                invert1 = (x1m2 & ym2) == (xyMod3Parity1 ? (byte)1 : (byte)0);
                                break;
                            }
                            case 7: {
                                var xyMod3Parity0 = x0m3 == ym3 && x0m3 != 0;
                                invert0 = (x0m2 != ym2) == xyMod3Parity0;
                                var xyMod3Parity1 = x1m3 == ym3 && x1m3 != 0;
                                invert1 = (x1m2 != ym2) == xyMod3Parity1;
                                break;
                            }
                            default:
                                invert0 = QrMask.ShouldInvert(mask, x0, y);
                                invert1 = QrMask.ShouldInvert(mask, x1, y);
                                break;
                        }

                        var idx0 = rowOffset + x0;
                        var wordIndex0 = idx0 >> 5;
                        var bitMask0 = 1u << (idx0 & 31);
                        if ((functionWords[wordIndex0] & bitMask0) == 0) {
                            var bit = (moduleWords[wordIndex0] & bitMask0) != 0;
                            if (invert0) bit = !bit;
                            if (bit) {
                                codewords[bitIndex >> 3] |= (byte)(1 << (7 - (bitIndex & 7)));
                            }
                            bitIndex++;
                            if (bitIndex == totalBits) {
                                done = true;
                            }
                        }

                        if (done) break;

                        var idx1 = rowOffset + x1;
                        var wordIndex1 = idx1 >> 5;
                        var bitMask1 = 1u << (idx1 & 31);
                        if ((functionWords[wordIndex1] & bitMask1) == 0) {
                            var bit = (moduleWords[wordIndex1] & bitMask1) != 0;
                            if (invert1) bit = !bit;
                            if (bit) {
                                codewords[bitIndex >> 3] |= (byte)(1 << (7 - (bitIndex & 7)));
                            }
                            bitIndex++;
                            if (bitIndex == totalBits) {
                                done = true;
                            }
                        }
                        if (done) break;
                    }
                    if (done) break;

                    upward = !upward;
                }
            } else {
                for (var right = size - 1; right >= 1; right -= 2) {
                    if (right == 6) right = 5;
                    var x0 = right;
                    var x1 = right - 1;
                    var x0m2 = xMod2[x0];
                    var x1m2 = xMod2[x1];
                    var x0m3 = xMod3[x0];
                    var x1m3 = xMod3[x1];
                    var x0d3 = xDiv3Parity[x0];
                    var x1d3 = xDiv3Parity[x1];

                    for (var vert = 0; vert < size; vert++) {
                        // Each row visits at most two modules. Poll every 64 modules,
                        // including the first row of every column pair, instead of
                        // paying a deadline callback for each pair of sampled bits.
                        if ((vert & 31) == 0 && shouldStop()) return false;
                        var y = upward ? size - 1 - vert : vert;
                        var rowOffset = y * size;
                        var ym2 = yMod2[y];
                        var ym3 = yMod3[y];
                        var yExp3 = yMod3Expected[y];
                        var yd2 = yDiv2Parity[y];
                        var ym2Zero = yMod2Zero[y] != 0;
                        var ym3Zero = yMod3Zero[y] != 0;
                        bool invert0;
                        bool invert1;
                        switch (mask) {
                            case 0:
                                invert0 = x0m2 == ym2;
                                invert1 = x1m2 == ym2;
                                break;
                            case 1:
                                invert0 = ym2 == 0;
                                invert1 = invert0;
                                break;
                            case 2:
                                invert0 = x0m3 == 0;
                                invert1 = x1m3 == 0;
                                break;
                            case 3:
                                invert0 = x0m3 == yExp3;
                                invert1 = x1m3 == yExp3;
                                break;
                            case 4:
                                invert0 = x0d3 == yd2;
                                invert1 = x1d3 == yd2;
                                break;
                            case 5:
                                invert0 = (x0m2 == 0 || ym2Zero) && (x0m3 == 0 || ym3Zero);
                                invert1 = (x1m2 == 0 || ym2Zero) && (x1m3 == 0 || ym3Zero);
                                break;
                            case 6: {
                                var xyMod3Parity0 = x0m3 == ym3 && x0m3 != 0;
                                invert0 = (x0m2 & ym2) == (xyMod3Parity0 ? (byte)1 : (byte)0);
                                var xyMod3Parity1 = x1m3 == ym3 && x1m3 != 0;
                                invert1 = (x1m2 & ym2) == (xyMod3Parity1 ? (byte)1 : (byte)0);
                                break;
                            }
                            case 7: {
                                var xyMod3Parity0 = x0m3 == ym3 && x0m3 != 0;
                                invert0 = (x0m2 != ym2) == xyMod3Parity0;
                                var xyMod3Parity1 = x1m3 == ym3 && x1m3 != 0;
                                invert1 = (x1m2 != ym2) == xyMod3Parity1;
                                break;
                            }
                            default:
                                invert0 = QrMask.ShouldInvert(mask, x0, y);
                                invert1 = QrMask.ShouldInvert(mask, x1, y);
                                break;
                        }

                        var idx0 = rowOffset + x0;
                        var wordIndex0 = idx0 >> 5;
                        var bitMask0 = 1u << (idx0 & 31);
                        if ((functionWords[wordIndex0] & bitMask0) == 0) {
                            var bit = (moduleWords[wordIndex0] & bitMask0) != 0;
                            if (invert0) bit = !bit;
                            if (bit) {
                                codewords[bitIndex >> 3] |= (byte)(1 << (7 - (bitIndex & 7)));
                            }
                            bitIndex++;
                            if (bitIndex == totalBits) {
                                done = true;
                            }
                        }

                        if (done) break;

                        var idx1 = rowOffset + x1;
                        var wordIndex1 = idx1 >> 5;
                        var bitMask1 = 1u << (idx1 & 31);
                        if ((functionWords[wordIndex1] & bitMask1) == 0) {
                            var bit = (moduleWords[wordIndex1] & bitMask1) != 0;
                            if (invert1) bit = !bit;
                            if (bit) {
                                codewords[bitIndex >> 3] |= (byte)(1 << (7 - (bitIndex & 7)));
                            }
                            bitIndex++;
                            if (bitIndex == totalBits) {
                                done = true;
                            }
                        }
                        if (done) break;
                    }
                    if (done) break;

                    upward = !upward;
                }
            }

            if (extractionCache is not null) {
                Array.Copy(codewords, 0, extractionCache, mask * rawCodewords, rawCodewords);
                extractedMasks |= 1 << mask;
            }
            return TryCorrectAndExtractData(codewords, rawCodewords, version, ecc, shouldStop, out dataCodewords);
        } finally {
            ArrayPool<byte>.Shared.Return(codewords, clearArray: false);
        }
    }

    private static bool TryCorrectAndExtractData(byte[] codewords, int codewordCount, int version, QrErrorCorrectionLevel ecc, Func<bool>? shouldStop, out byte[] dataCodewords) {
        dataCodewords = null!;

        var numBlocks = QrTables.GetNumBlocks(version, ecc);
        var blockEccLen = QrTables.GetEccCodewordsPerBlock(version, ecc);
        var rawCodewords = QrTables.GetNumRawDataModules(version) / 8;
        var dataLen = QrTables.GetNumDataCodewords(version, ecc);
        if (codewordCount != rawCodewords) return false;

        if (numBlocks == 1) {
            if (!QrReedSolomonDecoder.TryCorrectInPlace(codewords, codewordCount, blockEccLen, shouldStop)) return false;
            var dataOut = new byte[dataLen];
            Array.Copy(codewords, 0, dataOut, 0, dataLen);
            dataCodewords = dataOut;
            return true;
        }

        var numShortBlocks = numBlocks - (rawCodewords % numBlocks);
        var shortBlockLen = rawCodewords / numBlocks;
        var shortDataLen = shortBlockLen - blockEccLen;
        var longDataLen = shortDataLen + 1;

        var blocks = ArrayPool<byte[]>.Shared.Rent(numBlocks);
        var dataLens = ArrayPool<int>.Shared.Rent(numBlocks);
        var blocksInitialized = 0;
        try {
            for (var i = 0; i < numBlocks; i++) {
                var dataWords = i < numShortBlocks ? shortDataLen : longDataLen;
                dataLens[i] = dataWords;
                var blockLen = dataWords + blockEccLen;
                blocks[i] = ArrayPool<byte>.Shared.Rent(blockLen);
                Array.Clear(blocks[i], 0, blockLen);
                blocksInitialized++;
            }

            // Deinterleave:
            // 1) data codewords across all blocks
            // 2) error-correction codewords across all blocks
            var k = 0;
            var maxDataLen = dataLens[numBlocks - 1];
            if (shouldStop is null) {
                for (var i = 0; i < maxDataLen; i++) {
                    for (var j = 0; j < numBlocks; j++) {
                        if (i < dataLens[j]) blocks[j][i] = codewords[k++];
                    }
                }

                for (var i = 0; i < blockEccLen; i++) {
                    for (var j = 0; j < numBlocks; j++) {
                        blocks[j][dataLens[j] + i] = codewords[k++];
                    }
                }
            } else {
                for (var i = 0; i < maxDataLen; i++) {
                    if (shouldStop() == true) return false;
                    for (var j = 0; j < numBlocks; j++) {
                        if (i < dataLens[j]) blocks[j][i] = codewords[k++];
                    }
                }

                for (var i = 0; i < blockEccLen; i++) {
                    if (shouldStop() == true) return false;
                    for (var j = 0; j < numBlocks; j++) {
                        blocks[j][dataLens[j] + i] = codewords[k++];
                    }
                }
            }
            if (k != codewordCount) return false;

            // Correct each block and concatenate data parts
            var data = new byte[dataLen];
            var di = 0;
            if (shouldStop is null) {
                for (var i = 0; i < numBlocks; i++) {
                    var block = blocks[i];
                    var blockLen = dataLens[i] + blockEccLen;
                    if (!QrReedSolomonDecoder.TryCorrectInPlace(block, blockLen, blockEccLen, shouldStop)) return false;

                    var partLen = dataLens[i];
                    Array.Copy(block, 0, data, di, partLen);
                    di += partLen;
                }
            } else {
                for (var i = 0; i < numBlocks; i++) {
                    if (shouldStop() == true) return false;
                    var block = blocks[i];
                    var blockLen = dataLens[i] + blockEccLen;
                    if (!QrReedSolomonDecoder.TryCorrectInPlace(block, blockLen, blockEccLen, shouldStop)) return false;

                    var partLen = dataLens[i];
                    Array.Copy(block, 0, data, di, partLen);
                    di += partLen;
                }
            }
            if (di != data.Length) return false;

            dataCodewords = data;
            return true;
        } finally {
            for (var i = 0; i < blocksInitialized; i++) {
                var block = blocks[i];
                if (block is null) continue;
                ArrayPool<byte>.Shared.Return(block, clearArray: false);
                blocks[i] = null!;
            }
            ArrayPool<byte[]>.Shared.Return(blocks, clearArray: true);
            ArrayPool<int>.Shared.Return(dataLens, clearArray: true);
        }
    }

}
