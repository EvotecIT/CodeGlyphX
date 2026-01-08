using System;
using System.Text;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX;

/// <summary>
/// Decodes QR codes from a module grid or from raw pixels (clean images).
/// </summary>
/// <remarks>
/// Current scope: byte-mode payloads on clean/generated images and clean on-screen QR codes.
/// </remarks>
public static class QrDecoder {
    /// <summary>
    /// Attempts to decode a QR code from an exact module grid (no quiet zone).
    /// </summary>
    /// <param name="modules">Square matrix of QR modules (dark = <c>true</c>).</param>
    /// <param name="result">Decoded payload.</param>
    /// <returns><c>true</c> when decoding succeeded; otherwise <c>false</c>.</returns>
    public static bool TryDecode(BitMatrix modules, out QrDecoded result) {
        return TryDecode(modules, out result, out _);
    }

    internal static bool TryDecode(BitMatrix modules, out QrDecoded result, out QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

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

        if (!TryDecodeFormat(modules, out var formatCandidates, out var formatBestDistance)) {
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.FormatInfo, version, formatBestDistance: formatBestDistance);
            return false;
        }

        var functionMask = BuildFunctionMask(version, size);
        var failureEcc = formatCandidates[0].ErrorCorrectionLevel;
        var failureMask = formatCandidates[0].Mask;
        var sawPayloadFailure = false;

        foreach (var candidate in formatCandidates) {
            if (!TryExtractDataCodewords(modules, functionMask, version, candidate.ErrorCorrectionLevel, candidate.Mask, out var dataCodewords)) {
                failureEcc = candidate.ErrorCorrectionLevel;
                failureMask = candidate.Mask;
                continue;
            }
            if (!QrPayloadParser.TryParse(dataCodewords, version, out var payload, out var segments)) {
                sawPayloadFailure = true;
                failureEcc = candidate.ErrorCorrectionLevel;
                failureMask = candidate.Mask;
                continue;
            }

            var text = DecodeSegments(segments);
            result = new QrDecoded(version, candidate.ErrorCorrectionLevel, candidate.Mask, payload, text);
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.None, version, candidate.ErrorCorrectionLevel, candidate.Mask, formatBestDistance);
            return true;
        }

        var failure = sawPayloadFailure ? QrDecodeFailure.Payload : QrDecodeFailure.ReedSolomon;
        diagnostics = new QrDecodeDiagnostics(failure, version, failureEcc, failureMask, formatBestDistance);
        return false;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs).
    /// </summary>
    /// <param name="pixels">Pixel buffer.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="stride">Bytes per row.</param>
    /// <param name="fmt">Pixel format (4 bytes per pixel).</param>
    /// <param name="result">Decoded payload.</param>
    /// <returns><c>true</c> when decoding succeeded; otherwise <c>false</c>.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result) {
        return QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, out result);
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs).
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results) {
        return QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, fmt, out results);
    }

    internal static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        return QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, out result, out diagnostics);
    }
#endif

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs).
    /// </summary>
    /// <param name="pixels">Pixel buffer.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="stride">Bytes per row.</param>
    /// <param name="fmt">Pixel format (4 bytes per pixel).</param>
    /// <param name="result">Decoded payload.</param>
    /// <returns><c>true</c> when decoding succeeded; otherwise <c>false</c>.</returns>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result) {
        if (pixels is null) {
            result = null!;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out result);
#else
        result = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs).
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results) {
        if (pixels is null) {
            results = Array.Empty<QrDecoded>();
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out results);
#else
        results = Array.Empty<QrDecoded>();
        return false;
#endif
    }

#if NET8_0_OR_GREATER
    internal static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        if (pixels is null) {
            result = null!;
            diagnostics = default;
            return false;
        }

        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out result, out diagnostics);
    }
#endif

    private static bool TryDecodeFormat(BitMatrix modules, out QrFormatCandidate[] candidates, out int bestDistance) {
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

    private static bool TryDecodeFormatBits(int bitsA, int bitsB, out QrFormatCandidate[] candidates, out int bestDistance) {
        bestDistance = int.MaxValue;
        var list = new System.Collections.Generic.List<QrFormatCandidate>(4);

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
                list.Add(new QrFormatCandidate(i, ecc, mask, minDist, maxDist, distA + distB));
            }
        }

        if (list.Count == 0) {
            candidates = Array.Empty<QrFormatCandidate>();
            return false;
        }

        list.Sort(static (a, b) => {
            if (a.BothWithin != b.BothWithin) return a.BothWithin ? -1 : 1;
            var cmp = a.SumDistance.CompareTo(b.SumDistance);
            if (cmp != 0) return cmp;
            cmp = a.MaxDistance.CompareTo(b.MaxDistance);
            if (cmp != 0) return cmp;
            return a.Distance.CompareTo(b.Distance);
        });

        candidates = list.ToArray();
        return true;
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

    private static bool TryExtractDataCodewords(BitMatrix modules, BitMatrix functionMask, int version, QrErrorCorrectionLevel ecc, int mask, out byte[] dataCodewords) {
        dataCodewords = null!;

        var size = modules.Width;
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

        return TryCorrectAndExtractData(codewords, version, ecc, out dataCodewords);
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
            for (var j = 0; j < blocks.Length; j++) {
                if (i < dataLens[j]) blocks[j][i] = codewords[k++];
            }
        }

        for (var i = 0; i < blockEccLen; i++) {
            for (var j = 0; j < blocks.Length; j++) {
                blocks[j][dataLens[j] + i] = codewords[k++];
            }
        }
        if (k != codewords.Length) return false;

        // Correct each block and concatenate data parts
        var data = new byte[dataLen];
        var di = 0;
        for (var i = 0; i < blocks.Length; i++) {
            var block = blocks[i];
            if (!QrReedSolomonDecoder.TryCorrectInPlace(block, blockEccLen)) return false;

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
        return QrEncoding.Decode(segment.Encoding, segment.Bytes);
    }
}
