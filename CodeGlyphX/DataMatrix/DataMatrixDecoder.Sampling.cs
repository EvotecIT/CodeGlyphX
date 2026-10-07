using System;
using System.Threading;
using CodeGlyphX.Internal;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixDecoder {
    // Counting the clock tracks avoids rounding a fractional module pitch to
    // the shortest pixel run. Try both opposite edges to retain rotated symbols.
    private static void TryResolveTimingGrid(PixelSpan pixels, int width, int height, int stride,
        PixelFormat format, BoundingBox box, bool invert, int minimumRun,
        CancellationToken cancellationToken, ref int cols, ref int rows) {
        // Fractional resizing can leave a one-pixel threshold run at the outer
        // boundary. Inspect inside that boundary rather than counting its fringe.
        var inset = Math.Max(1, minimumRun / 2);
        var clockCols = Math.Max(
            CountEdgeRuns(pixels, width, height, stride, format, box.Left, box.Right, box.Top + inset, true, invert, cancellationToken),
            CountEdgeRuns(pixels, width, height, stride, format, box.Left, box.Right, box.Bottom - inset, true, invert, cancellationToken));
        var clockRows = Math.Max(
            CountEdgeRuns(pixels, width, height, stride, format, box.Top, box.Bottom, box.Left + inset, false, invert, cancellationToken),
            CountEdgeRuns(pixels, width, height, stride, format, box.Top, box.Bottom, box.Right - inset, false, invert, cancellationToken));
        if (!DataMatrixSymbolInfo.TryGetForSize(clockRows, clockCols, out _) &&
            !DataMatrixSymbolInfo.TryGetForSize(clockCols, clockRows, out _)) return;

        var pitchX = (double)box.Width / clockCols;
        var pitchY = (double)box.Height / clockRows;
        if (pitchX < 1 || pitchY < 1 || Math.Abs(pitchX - pitchY) > 1) return;
        cols = clockCols;
        rows = clockRows;
    }

    private static int CountEdgeRuns(PixelSpan pixels, int width, int height, int stride,
        PixelFormat format, int start, int end, int fixedPosition, bool horizontal,
        bool invert, CancellationToken cancellationToken) {
        var count = 0;
        var previous = false;
        for (var at = start; at <= end; at++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return 0;
            var dark = IsDark(pixels, width, height, stride, format,
                horizontal ? at : fixedPosition, horizontal ? fixedPosition : at);
            var bit = invert ? !dark : dark;
            if (at == start || bit != previous) count++;
            previous = bit;
        }
        return count;
    }
}
