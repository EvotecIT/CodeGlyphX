using System;
using System.Collections.Generic;
using System.Buffers;
using System.Text;
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

    private static bool TryDecodeWithCandidates(BitMatrix modules, int version, QrFormatCandidate[] candidates, int formatBestDistance, Func<bool>? shouldStop, out QrDecoded result, out QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Length == 0) return false;

        var functionMask = QrStructureAnalysis.BuildFunctionMask(version, modules.Width);
        var failureEcc = candidates[0].ErrorCorrectionLevel;
        var failureMask = candidates[0].Mask;
        var sawPayloadFailure = false;
        // The extracted stream depends on the mask, not the error-correction level.
        // Only broad fallback searches need storage for sharing it between candidates.
        var rawCodewords = QrTables.GetNumRawDataModules(version) / 8;
        var extractedMasks = 0;
        var extractionCache = candidates.Length > 8 ? ArrayPool<byte>.Shared.Rent(rawCodewords * 8) : null;

        try {
            foreach (var candidate in candidates) {
                if (shouldStop?.Invoke() == true) {
                    diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled, version);
                    return false;
                }
                if (!TryExtractDataCodewords(modules, functionMask, version, candidate.ErrorCorrectionLevel, candidate.Mask, shouldStop, extractionCache, ref extractedMasks, out var dataCodewords)) {
                    failureEcc = candidate.ErrorCorrectionLevel;
                    failureMask = candidate.Mask;
                    if (shouldStop?.Invoke() == true) {
                        diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled, version, candidate.ErrorCorrectionLevel, candidate.Mask, formatBestDistance);
                        return false;
                    }
                    continue;
                }
                if (!QrPayloadParser.TryParse(dataCodewords, version, shouldStop, out var payload, out var segments, out var structuredAppend, out var fnc1Mode, out var fnc1ApplicationIndicator)) {
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
                result = new QrDecoded(version, candidate.ErrorCorrectionLevel, candidate.Mask, payload, text, structuredAppend, fnc1Mode, fnc1ApplicationIndicator);
                diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.None, version, candidate.ErrorCorrectionLevel, candidate.Mask, formatBestDistance);
                return true;
            }

            var failure = sawPayloadFailure ? QrDecodeFailure.Payload : QrDecodeFailure.ReedSolomon;
            diagnostics = new QrDecodeDiagnostics(failure, version, failureEcc, failureMask, formatBestDistance);
            return false;
        } finally {
            if (extractionCache is not null) ArrayPool<byte>.Shared.Return(extractionCache, clearArray: false);
        }
    }

    private static int CountBits(int x) {
        unchecked {
            x = x - ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            return (((x + (x >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
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
