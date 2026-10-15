using System;
using System.Buffers;
using System.Threading;

namespace CodeGlyphX.Internal;

/// <summary>Filters a binary square using one pooled row of moving column counts.</summary>
internal static class BinaryMaskMorphology {
    /// <summary>
    /// Writes 0/1 pixels into a distinct destination. Erosion either clips the
    /// neighborhood or treats pixels outside the image as zero. Returns false
    /// on cancellation or expiry; that partially written destination must not
    /// be consumed. The supplied budget belongs to this synchronous operation.
    /// </summary>
    internal static bool FilterSquare(ReadOnlySpan<byte> source, Span<byte> destination,
        int width, int height, int radius, bool erode, bool zeroOutside,
        CancellationToken token, DecodeBudgetState budget) {
        if (budget.ShouldAbort(token)) return false;
        var columns = ArrayPool<int>.Shared.Rent(width);
        try {
            Array.Clear(columns, 0, width);
            var verticalRadius = Math.Min(radius, height - 1);
            var horizontalRadius = Math.Min(radius, width - 1);
            for (var y = 0; y <= verticalRadius; y++) {
                if (budget.ShouldAbort(token)) return false;
                var row = y * width;
                for (var x = 0; x < width; x++) {
                    if ((x & 4095) == 0 && budget.ShouldAbort(token)) return false;
                    columns[x] += source[row + x] != 0 ? 1 : 0;
                }
            }
            for (var y = 0; y < height; y++) {
                if (budget.ShouldAbort(token)) return false;
                if (y > 0) {
                    var leaving = y - verticalRadius - 1;
                    if (leaving >= 0) {
                        var row = leaving * width;
                        for (var x = 0; x < width; x++) columns[x] -= source[row + x] != 0 ? 1 : 0;
                    }
                    if (verticalRadius < height - y) {
                        var row = (y + verticalRadius) * width;
                        for (var x = 0; x < width; x++) columns[x] += source[row + x] != 0 ? 1 : 0;
                    }
                }
                var windowHeight = Math.Min(verticalRadius, y) + Math.Min(verticalRadius, height - y - 1) + 1;
                var count = 0;
                for (var x = 0; x <= horizontalRadius; x++) count += columns[x];
                var outputRow = y * width;
                for (var x = 0; x < width; x++) {
                    if ((x & 4095) == 0 && budget.ShouldAbort(token)) return false;
                    var windowWidth = Math.Min(horizontalRadius, x) + Math.Min(horizontalRadius, width - x - 1) + 1;
                    var set = count != 0;
                    if (erode) {
                        var inside = !zeroOutside || (x >= radius && y >= radius && width - x > radius && height - y > radius);
                        set = inside && count == windowWidth * windowHeight;
                    }
                    destination[outputRow + x] = set ? (byte)1 : (byte)0;
                    var leaving = x - horizontalRadius;
                    if (leaving >= 0) count -= columns[leaving];
                    if (horizontalRadius < width - x - 1) count += columns[x + horizontalRadius + 1];
                }
            }
            return !budget.ShouldAbort(token);
        } finally {
            ArrayPool<int>.Shared.Return(columns);
        }
    }
}
