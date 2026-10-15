using System;
using System.Buffers;
using System.Threading;

namespace CodeGlyphX.Internal;

/// <summary>Visits four-connected components in a bounded binary image.</summary>
internal static class BinaryImageComponents {
    /// <summary>
    /// Bit zero stores polarity; bit one marks visited pixels. The visitor may
    /// inspect the rented queue only during its call and returns true to stop.
    /// The caller clears visited bits before searching the other polarity.
    /// </summary>
    internal static bool Visit(byte[] mask, int width, int height, bool invert,
        CancellationToken cancellationToken, Func<int[], int, bool, bool> visitor) {
        var count = width * height;
        if (count == 0) return false;
        var coordinates = new ImageCoordinateDivider(width);
        var queue = ArrayPool<int>.Shared.Rent(count);
        try {
            for (var at = 0; at < count; at++) {
                if ((at & 511) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                if ((mask[at] & 2) != 0 || ((mask[at] & 1) != 0) == invert) continue;
                mask[at] |= 2;
                queue[0] = at;
                var length = 1;
                var touchesEdge = false;
                for (var head = 0; head < length; head++) {
                    if ((head & 511) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                    var current = queue[head];
                    coordinates.GetCoordinates(current, out var x, out var y);
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1) touchesEdge = true;
                    if (x > 0) Enqueue(mask, current - 1, invert, queue, ref length);
                    if (x + 1 < width) Enqueue(mask, current + 1, invert, queue, ref length);
                    if (y > 0) Enqueue(mask, current - width, invert, queue, ref length);
                    if (y + 1 < height) Enqueue(mask, current + width, invert, queue, ref length);
                }
                if (visitor(queue, length, touchesEdge)) return true;
            }
            return false;
        } finally {
            ArrayPool<int>.Shared.Return(queue);
        }
    }

    private static void Enqueue(byte[] mask, int at, bool invert, int[] queue, ref int length) {
        if ((mask[at] & 2) != 0 || ((mask[at] & 1) != 0) == invert) return;
        mask[at] |= 2;
        queue[length++] = at;
    }
}
