#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    /// <summary>
    /// Writes zero or one for every logical pixel before component traversal.
    /// Vector lanes keep unsigned threshold equality and polarity; scalar tails
    /// leave unused pooled storage untouched. Budget checks remain at each row.
    /// </summary>
    private static bool TryFillComponentForeground(QrGrayImage image, bool invert, DecodeBudget budget, byte[] foreground) {
        var gray = image.Gray;
        var map = image.ThresholdMap;
        var threshold = image.Threshold;
        var threshold256 = Vector256.Create(threshold);
        var threshold128 = Vector128.Create(threshold);
        var one256 = Vector256.Create((byte)1);
        var one128 = Vector128.Create((byte)1);
        for (var y = 0; y < image.Height; y++) {
            if (budget.IsExpired) return false;
            var at = y * image.Width;
            var end = at + image.Width;
            if (Vector256.IsHardwareAccelerated) {
                for (; at + 32 <= end; at += 32) {
                    var values = MemoryMarshal.Read<Vector256<byte>>(gray.AsSpan(at));
                    var limits = map is null ? threshold256 : MemoryMarshal.Read<Vector256<byte>>(map.AsSpan(at));
                    var selected = Vector256.LessThanOrEqual(values, limits);
                    if (invert) selected = ~selected;
                    var mask = selected & one256;
                    MemoryMarshal.Write(foreground.AsSpan(at), in mask);
                }
            }
            if (Vector128.IsHardwareAccelerated) {
                for (; at + 16 <= end; at += 16) {
                    var values = MemoryMarshal.Read<Vector128<byte>>(gray.AsSpan(at));
                    var limits = map is null ? threshold128 : MemoryMarshal.Read<Vector128<byte>>(map.AsSpan(at));
                    var selected = Vector128.LessThanOrEqual(values, limits);
                    if (invert) selected = ~selected;
                    var mask = selected & one128;
                    MemoryMarshal.Write(foreground.AsSpan(at), in mask);
                }
            }
            for (; at < end; at++)
                foreground[at] = (byte)((gray[at] <= (map is null ? threshold : map[at])) != invert ? 1 : 0);
        }
        return true;
    }
}
#endif
