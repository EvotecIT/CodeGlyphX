using System;
using System.Collections.Generic;
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

        var functionMask = BuildFunctionMask(version, modules.Width);
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
        var codewords = new byte[rawCodewords];
        var totalBits = rawCodewords * 8;
        var moduleWords = modules.Words;
        var functionWords = functionMask.Words;

        var bitIndex = 0;
        var upward = true;
        var done = false;
        for (var right = size - 1; right >= 1; right -= 2) {
            if (right == 6) right = 5;

            for (var vert = 0; vert < size; vert++) {
                if (shouldStop?.Invoke() == true) return false;
                var y = upward ? size - 1 - vert : vert;
                var rowOffset = y * size;
                for (var j = 0; j < 2; j++) {
                    var x = right - j;
                    var idx = rowOffset + x;
                    var wordIndex = idx >> 5;
                    var bitMask = 1u << (idx & 31);
                    if ((functionWords[wordIndex] & bitMask) != 0) continue;

                    if (bitIndex < totalBits) {
                        var bit = (moduleWords[wordIndex] & bitMask) != 0;
                        if (QrMask.ShouldInvert(mask, x, y)) bit = !bit;
                        if (bit) {
                            codewords[bitIndex >> 3] |= (byte)(1 << (7 - (bitIndex & 7)));
                        }
                    }

                    bitIndex++;
                    if (bitIndex == totalBits) {
                        done = true;
                        break;
                    }
                }
                if (done) break;
            }
            if (done) break;

            upward = !upward;
        }

        return TryCorrectAndExtractData(codewords, version, ecc, shouldStop, out dataCodewords);
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

    private static bool TryCorrectAndExtractData(byte[] codewords, int version, QrErrorCorrectionLevel ecc, Func<bool>? shouldStop, out byte[] dataCodewords) {
        dataCodewords = null!;

        var numBlocks = QrTables.GetNumBlocks(version, ecc);
        var blockEccLen = QrTables.GetEccCodewordsPerBlock(version, ecc);
        var rawCodewords = QrTables.GetNumRawDataModules(version) / 8;
        var dataLen = QrTables.GetNumDataCodewords(version, ecc);

        if (numBlocks == 1) {
            if (!QrReedSolomonDecoder.TryCorrectInPlace(codewords, blockEccLen, shouldStop)) return false;
            var dataOut = new byte[dataLen];
            Array.Copy(codewords, 0, dataOut, 0, dataLen);
            dataCodewords = dataOut;
            return true;
        }

        var numShortBlocks = numBlocks - (rawCodewords % numBlocks);
        var shortBlockLen = rawCodewords / numBlocks;
        var shortDataLen = shortBlockLen - blockEccLen;
        var longDataLen = shortDataLen + 1;

        var blocks = new byte[numBlocks][];
        var dataLens = new int[numBlocks];
        for (var i = 0; i < numBlocks; i++) {
            var dataWords = i < numShortBlocks ? shortDataLen : longDataLen;
            dataLens[i] = dataWords;
            blocks[i] = new byte[dataWords + blockEccLen];
        }

        // Deinterleave:
        // 1) data codewords across all blocks
        // 2) error-correction codewords across all blocks
        var k = 0;

        var maxDataLen = dataLens[numBlocks - 1];
        for (var i = 0; i < maxDataLen; i++) {
            if (shouldStop?.Invoke() == true) return false;
            for (var j = 0; j < blocks.Length; j++) {
                if (i < dataLens[j]) blocks[j][i] = codewords[k++];
            }
        }

        for (var i = 0; i < blockEccLen; i++) {
            if (shouldStop?.Invoke() == true) return false;
            for (var j = 0; j < blocks.Length; j++) {
                blocks[j][dataLens[j] + i] = codewords[k++];
            }
        }
        if (k != codewords.Length) return false;

        // Correct each block and concatenate data parts
        var data = new byte[dataLen];
        var di = 0;
        for (var i = 0; i < blocks.Length; i++) {
            if (shouldStop?.Invoke() == true) return false;
            var block = blocks[i];
            if (!QrReedSolomonDecoder.TryCorrectInPlace(block, blockEccLen, shouldStop)) return false;

            var partLen = dataLens[i];
            Array.Copy(block, 0, data, di, partLen);
            di += partLen;
        }
        if (di != data.Length) return false;

        dataCodewords = data;
        return true;
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
