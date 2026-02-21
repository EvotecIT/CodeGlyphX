using System;
using System.Collections.Generic;
using System.Buffers;
using System.Text;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX;

public static partial class QrDecoder {
    private static bool TryDecodeFormat(BitMatrix modules, out QrFormatCandidate[] candidates, out int bestDistance, out int bitsA, out int bitsB) {
        var size = modules.Width;

        bitsA = 0;
        for (var i = 0; i <= 5; i++) if (modules[8, i]) bitsA |= 1 << i;
        if (modules[8, 7]) bitsA |= 1 << 6;
        if (modules[8, 8]) bitsA |= 1 << 7;
        if (modules[7, 8]) bitsA |= 1 << 8;
        for (var i = 9; i < 15; i++) if (modules[14 - i, 8]) bitsA |= 1 << i;

        bitsB = 0;
        for (var i = 0; i < 8; i++) if (modules[size - 1 - i, 8]) bitsB |= 1 << i;
        for (var i = 8; i < 15; i++) if (modules[8, size - 15 + i]) bitsB |= 1 << i;

        return TryDecodeFormatBits(bitsA, bitsB, out candidates, out bestDistance);
    }

    private static readonly int[] FormatPatterns = BuildFormatPatterns();

    private readonly struct QrFormatCandidate {
        public int Index { get; }
        public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }
        public int Mask { get; }
        public int Distance { get; }
        public int MaxDistance { get; }
        public int SumDistance { get; }
        public bool BothWithin { get; }

        public QrFormatCandidate(int index, QrErrorCorrectionLevel ecc, int mask, int distance, int maxDistance, int sumDistance) {
            Index = index;
            ErrorCorrectionLevel = ecc;
            Mask = mask;
            Distance = distance;
            MaxDistance = maxDistance;
            SumDistance = sumDistance;
            BothWithin = maxDistance <= 3;
        }
    }

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

    private static readonly QrErrorCorrectionLevel[] FormatEccOrder = {
        QrErrorCorrectionLevel.L,
        QrErrorCorrectionLevel.M,
        QrErrorCorrectionLevel.Q,
        QrErrorCorrectionLevel.H
    };
    private static readonly IComparer<QrFormatCandidate> FormatCandidateComparer = new QrFormatCandidateComparer();

    private static bool TryDecodeFormatBits(int bitsA, int bitsB, out QrFormatCandidate[] candidates, out int bestDistance) {
        bestDistance = int.MaxValue;
        Span<QrFormatCandidate> buffer = stackalloc QrFormatCandidate[FormatPatterns.Length];
        var count = 0;

        for (var i = 0; i < FormatPatterns.Length; i++) {
            var pattern = FormatPatterns[i];
            var distA = CountBits(bitsA ^ pattern);
            var distB = CountBits(bitsB ^ pattern);
            var minDist = Math.Min(distA, distB);
            if (minDist < bestDistance) bestDistance = minDist;

            if (minDist <= 3) {
                var ecc = FormatEccOrder[i / 8];
                var mask = i % 8;
                var maxDist = Math.Max(distA, distB);
                buffer[count++] = new QrFormatCandidate(i, ecc, mask, minDist, maxDist, distA + distB);
            }
        }

        if (count == 0) {
            // No candidates within the spec-recommended distance. For degraded images, try the closest
            // patterns anyway and rely on RS + payload validation to reject wrong masks.
            if (bestDistance > 5) {
                candidates = Array.Empty<QrFormatCandidate>();
                return false;
            }

            for (var i = 0; i < FormatPatterns.Length; i++) {
                var pattern = FormatPatterns[i];
                var distA = CountBits(bitsA ^ pattern);
                var distB = CountBits(bitsB ^ pattern);
                var minDist = Math.Min(distA, distB);
                if (minDist != bestDistance) continue;

                var ecc = FormatEccOrder[i / 8];
                var mask = i % 8;
                var maxDist = Math.Max(distA, distB);
                buffer[count++] = new QrFormatCandidate(i, ecc, mask, minDist, maxDist, distA + distB);
            }

            if (count == 0) {
                candidates = Array.Empty<QrFormatCandidate>();
                return false;
            }
        }

        var result = new QrFormatCandidate[count];
        for (var i = 0; i < count; i++) result[i] = buffer[i];
        Array.Sort(result, FormatCandidateComparer);
        candidates = result;
        return true;
    }

    private sealed class QrFormatCandidateComparer : IComparer<QrFormatCandidate> {
        public int Compare(QrFormatCandidate x, QrFormatCandidate y) => CompareCandidates(x, y);
    }

    private static int CompareCandidates(QrFormatCandidate a, QrFormatCandidate b) {
        if (a.BothWithin != b.BothWithin) return a.BothWithin ? -1 : 1;
        var cmp = a.SumDistance.CompareTo(b.SumDistance);
        if (cmp != 0) return cmp;
        cmp = a.MaxDistance.CompareTo(b.MaxDistance);
        if (cmp != 0) return cmp;
        return a.Distance.CompareTo(b.Distance);
    }

    private static QrFormatCandidate[] BuildAllFormatCandidates(int bitsA, int bitsB) {
        var list = new QrFormatCandidate[FormatPatterns.Length];
        var count = 0;
        for (var i = 0; i < FormatPatterns.Length; i++) {
            var pattern = FormatPatterns[i];
            var distA = CountBits(bitsA ^ pattern);
            var distB = CountBits(bitsB ^ pattern);
            var minDist = Math.Min(distA, distB);
            var ecc = FormatEccOrder[i / 8];
            var mask = i % 8;
            var maxDist = Math.Max(distA, distB);
            list[count++] = new QrFormatCandidate(i, ecc, mask, minDist, maxDist, distA + distB);
        }

        Array.Sort(list, 0, count, FormatCandidateComparer);
        if (count == list.Length) return list;
        var result = new QrFormatCandidate[count];
        Array.Copy(list, result, count);
        return result;
    }

    private static bool TryDecodeWithCandidates(BitMatrix modules, int version, QrFormatCandidate[] candidates, int formatBestDistance, bool requireBothWithin, Func<bool>? shouldStop, out QrDecoded result, out QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Length == 0) return false;

        var functionMask = QrStructureAnalysis.GetFunctionMask(version);
        var failureEcc = candidates[0].ErrorCorrectionLevel;
        var failureMask = candidates[0].Mask;
        var sawPayloadFailure = false;

        foreach (var candidate in candidates) {
            if (shouldStop?.Invoke() == true) {
                diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled, version);
                return false;
            }
            if (requireBothWithin && !candidate.BothWithin) continue;
            if (!TryExtractDataCodewords(modules, functionMask, version, candidate.ErrorCorrectionLevel, candidate.Mask, shouldStop, out var dataCodewords)) {
                failureEcc = candidate.ErrorCorrectionLevel;
                failureMask = candidate.Mask;
                if (shouldStop?.Invoke() == true) {
                    diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled, version, candidate.ErrorCorrectionLevel, candidate.Mask, formatBestDistance);
                    return false;
                }
                continue;
            }
            if (!QrPayloadParser.TryParse(dataCodewords, version, shouldStop, out var payload, out var segments, out var structuredAppend, out var fnc1Mode)) {
                sawPayloadFailure = true;
                failureEcc = candidate.ErrorCorrectionLevel;
                failureMask = candidate.Mask;
                if (shouldStop?.Invoke() == true) {
                    diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled, version, candidate.ErrorCorrectionLevel, candidate.Mask, formatBestDistance);
                    return false;
                }
                continue;
            }

            var text = DecodeSegments(segments);
            result = new QrDecoded(version, candidate.ErrorCorrectionLevel, candidate.Mask, payload, text, structuredAppend, fnc1Mode);
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.None, version, candidate.ErrorCorrectionLevel, candidate.Mask, formatBestDistance);
            return true;
        }

        var failure = sawPayloadFailure ? QrDecodeFailure.Payload : QrDecodeFailure.ReedSolomon;
        diagnostics = new QrDecodeDiagnostics(failure, version, failureEcc, failureMask, formatBestDistance);
        return false;
    }

    private static bool HasBothWithin(QrFormatCandidate[] candidates) {
        for (var i = 0; i < candidates.Length; i++) {
            if (candidates[i].BothWithin) return true;
        }
        return false;
    }

    internal static int GetBestFormatDistance(int bitsA, int bitsB) {
        var best = int.MaxValue;
        for (var i = 0; i < FormatPatterns.Length; i++) {
            var pattern = FormatPatterns[i];
            var distA = CountBits(bitsA ^ pattern);
            var distB = CountBits(bitsB ^ pattern);
            var minDist = Math.Min(distA, distB);
            if (minDist < best) best = minDist;
        }
        return best;
    }

    internal static bool TryDecodeAllFormatCandidates(BitMatrix modules, Func<bool>? shouldStop, out QrDecoded result, out QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (shouldStop?.Invoke() == true) {
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled);
            return false;
        }

        if (modules is null || modules.Width != modules.Height) {
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.InvalidInput);
            return false;
        }

        var size = modules.Width;
        var version = (size - 17) / 4;
        if (version is < 1 or > 40 || size != version * 4 + 17) {
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.InvalidSize, version);
            return false;
        }

        if (shouldStop?.Invoke() == true) {
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled, version);
            return false;
        }

        TryDecodeFormat(modules, out _, out var formatBestDistance, out var bitsA, out var bitsB);
        var allCandidates = BuildAllFormatCandidates(bitsA, bitsB);
        return TryDecodeWithCandidates(modules, version, allCandidates, formatBestDistance, requireBothWithin: false, shouldStop, out result, out diagnostics);
    }

    private static int CountBits(int x) {
        unchecked {
            x = x - ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            return (((x + (x >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }
    }

    private static bool TryExtractDataCodewords(BitMatrix modules, BitMatrix functionMask, int version, QrErrorCorrectionLevel ecc, int mask, Func<bool>? shouldStop, out byte[] dataCodewords) {
        dataCodewords = null!;

        var size = modules.Width;
        var rawCodewords = QrTables.GetNumRawDataModules(version) / 8;
        var codewords = ArrayPool<byte>.Shared.Rent(rawCodewords);
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
                if (!ExtractMaskNoStop(
                        size,
                        moduleWords,
                        functionWords,
                        totalBits,
                        codewords,
                        mask,
                        xMod2,
                        yMod2,
                        xMod3,
                        yMod3,
                        yMod3Expected,
                        xDiv3Parity,
                        yDiv2Parity,
                        yMod2Zero,
                        yMod3Zero)) {
                    return false;
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
                        if (shouldStop() == true) return false;
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

            return TryCorrectAndExtractData(codewords, rawCodewords, version, ecc, shouldStop, out dataCodewords);
        } finally {
            ArrayPool<byte>.Shared.Return(codewords, clearArray: false);
        }
    }

    private static bool ExtractMaskNoStop(
        int size,
        uint[] moduleWords,
        uint[] functionWords,
        int totalBits,
        byte[] codewords,
        int mask,
        ReadOnlySpan<byte> xMod2,
        ReadOnlySpan<byte> yMod2,
        ReadOnlySpan<byte> xMod3,
        ReadOnlySpan<byte> yMod3,
        ReadOnlySpan<byte> yMod3Expected,
        ReadOnlySpan<byte> xDiv3Parity,
        ReadOnlySpan<byte> yDiv2Parity,
        ReadOnlySpan<byte> yMod2Zero,
        ReadOnlySpan<byte> yMod3Zero) {
        var bitIndex = 0;
        var upward = true;
        var done = false;

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
                var ym2IsZero = yMod2Zero[y] != 0;
                var ym3IsZero = yMod3Zero[y] != 0;

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
                        invert0 = (x0m2 == 0 || ym2IsZero) && (x0m3 == 0 || ym3IsZero);
                        invert1 = (x1m2 == 0 || ym2IsZero) && (x1m3 == 0 || ym3IsZero);
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
                    if (bitIndex == totalBits) done = true;
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
                    if (bitIndex == totalBits) done = true;
                }

                if (done) break;
            }

            if (done) break;
            upward = !upward;
        }

        return true;
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

    private static string DecodeLatin1(byte[] bytes) {
        if (bytes.Length == 0) return string.Empty;

        var chars = new char[bytes.Length];
        for (var i = 0; i < bytes.Length; i++) {
            chars[i] = (char)bytes[i];
        }

        return new string(chars);
    }

    internal static string DecodeSegments(QrPayloadSegment[] segments) {
        if (segments is null || segments.Length == 0) return string.Empty;
        if (segments.Length == 1) return DecodeSegment(segments[0]);

        var sb = new StringBuilder();
        for (var i = 0; i < segments.Length; i++) {
            sb.Append(DecodeSegment(segments[i]));
        }

        return sb.ToString();
    }

    private static string DecodeSegment(QrPayloadSegment segment) {
        return QrEncoding.Decode(segment.Encoding, segment.Buffer, segment.Offset, segment.Length);
    }

}
