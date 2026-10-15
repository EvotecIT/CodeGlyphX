#if NET8_0_OR_GREATER
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace CodeGlyphX.Qr;

internal readonly partial struct QrGrayImage {
    /// <summary>
    /// Writes one logical row as zero or one using the image's unsigned threshold
    /// comparison. Adaptive maps and inverted polarity retain equality at the cutoff;
    /// pooled storage beyond the logical row is never read or written.
    /// </summary>
    internal void WriteBinaryRow(int row, bool invert, Span<byte> destination) {
        var start = row * Width;
        var gray = Gray.AsSpan(start, Width);
        var map = ThresholdMap is null ? default : ThresholdMap.AsSpan(start, Width);
        var threshold256 = Vector256.Create(Threshold);
        var threshold128 = Vector128.Create(Threshold);
        var one256 = Vector256.Create((byte)1);
        var one128 = Vector128.Create((byte)1);
        var at = 0;
        if (Vector256.IsHardwareAccelerated) {
            for (; at + 32 <= Width; at += 32) {
                var values = MemoryMarshal.Read<Vector256<byte>>(gray[at..]);
                var limits = map.IsEmpty ? threshold256 : MemoryMarshal.Read<Vector256<byte>>(map[at..]);
                var selected = Vector256.LessThanOrEqual(values, limits);
                if (invert) selected = ~selected;
                var mask = selected & one256;
                MemoryMarshal.Write(destination[at..], in mask);
            }
        }
        if (Vector128.IsHardwareAccelerated) {
            for (; at + 16 <= Width; at += 16) {
                var values = MemoryMarshal.Read<Vector128<byte>>(gray[at..]);
                var limits = map.IsEmpty ? threshold128 : MemoryMarshal.Read<Vector128<byte>>(map[at..]);
                var selected = Vector128.LessThanOrEqual(values, limits);
                if (invert) selected = ~selected;
                var mask = selected & one128;
                MemoryMarshal.Write(destination[at..], in mask);
            }
        }
        for (; at < Width; at++)
            destination[at] = (byte)((gray[at] <= (map.IsEmpty ? Threshold : map[at])) != invert ? 1 : 0);
    }
}
#endif
