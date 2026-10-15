#if NET8_0_OR_GREATER
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    /// <summary>
    /// Finds raw-pixel foreground extrema in an inclusive region. Vector chunks
    /// retain the same unsigned threshold comparison; scalar tails cover every
    /// remaining pixel. Budget polls keep the row and 256-pixel boundaries.
    /// </summary>
    private static bool TryFindForegroundBounds(QrGrayImage image, bool invert, DecodeBudget budget,
        int left, int top, int right, int bottom, out int minX, out int minY, out int maxX, out int maxY) {
        minX = right;
        minY = bottom;
        maxX = maxY = -1;
        var gray = image.Gray;
        var map = image.ThresholdMap;
        var threshold = image.Threshold;
        var checkBudget = budget.CanAbort;
        if (checkBudget && budget.IsExpired) return false;
        var width = right - left + 1;
        var counter = 0;
        var threshold256 = Vector256.Create(threshold);
        var threshold128 = Vector128.Create(threshold);
        for (var y = top; y <= bottom; y++) {
            if (checkBudget && budget.IsExpired) return false;
            var row = y * image.Width + left;
            var first = width;
            var last = -1;
            var x = 0;
            while (x < width) {
                if (checkBudget && (counter & 255) == 0 && budget.IsExpired) return false;
                var count = Math.Min(width - x, 256 - (counter & 255));
                var end = x + count;
                if (Vector256.IsHardwareAccelerated) {
                    for (; x + 32 <= end; x += 32) {
                        var values = MemoryMarshal.Read<Vector256<byte>>(gray.AsSpan(row + x));
                        var limits = map is null ? threshold256 : MemoryMarshal.Read<Vector256<byte>>(map.AsSpan(row + x));
                        var bits = Vector256.ExtractMostSignificantBits(Vector256.LessThanOrEqual(values, limits));
                        if (invert) bits = ~bits;
                        if (bits == 0) continue;
                        first = Math.Min(first, x + BitOperations.TrailingZeroCount(bits));
                        last = Math.Max(last, x + 31 - BitOperations.LeadingZeroCount(bits));
                    }
                }
                if (Vector128.IsHardwareAccelerated) {
                    for (; x + 16 <= end; x += 16) {
                        var values = MemoryMarshal.Read<Vector128<byte>>(gray.AsSpan(row + x));
                        var limits = map is null ? threshold128 : MemoryMarshal.Read<Vector128<byte>>(map.AsSpan(row + x));
                        var bits = Vector128.ExtractMostSignificantBits(Vector128.LessThanOrEqual(values, limits));
                        if (invert) bits ^= 0xFFFFu;
                        if (bits == 0) continue;
                        first = Math.Min(first, x + BitOperations.TrailingZeroCount(bits));
                        last = Math.Max(last, x + 31 - BitOperations.LeadingZeroCount(bits));
                    }
                }
                for (; x < end; x++) {
                    if ((gray[row + x] <= (map is null ? threshold : map[row + x])) == invert) continue;
                    first = Math.Min(first, x);
                    last = x;
                }
                counter += count;
            }
            if (last < 0) continue;
            minX = Math.Min(minX, left + first);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, left + last);
            maxY = y;
        }
        return maxX >= 0;
    }
}
#endif
