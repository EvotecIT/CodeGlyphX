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
    private readonly struct ImageSamplingGrid {
        public BoundingBox Bounds { get; }
        public bool Invert { get; }
        public int MinimumRun { get; }
        public int Columns { get; }
        public int Rows { get; }

        public ImageSamplingGrid(BoundingBox bounds, bool invert, int minimumRun, int columns, int rows) {
            Bounds = bounds;
            Invert = invert;
            MinimumRun = minimumRun;
            Columns = columns;
            Rows = rows;
        }
    }

    private static bool TryExtractClockModules(PixelSpan pixels, int width, int height, int stride,
        PixelFormat format, ImageSamplingGrid sampling, CancellationToken cancellationToken, out BitMatrix modules) {
        modules = null!;
        var cols = sampling.Columns;
        var rows = sampling.Rows;
        if (!TryResolveTimingGrid(pixels, width, height, stride, format, sampling.Bounds, sampling.Invert,
            sampling.MinimumRun, cancellationToken, ref cols, ref rows)) return false;
        if (cols == sampling.Columns && rows == sampling.Rows) return false;
        return TrySampleGrid(pixels, width, height, stride, format, sampling.Bounds, sampling.Invert,
            cols, rows, cancellationToken, out modules);
    }

    private static bool TrySampleGrid(PixelSpan pixels, int width, int height, int stride,
        PixelFormat format, BoundingBox box, bool invert, int cols, int rows,
        CancellationToken cancellationToken, out BitMatrix modules) {
        modules = new BitMatrix(cols, rows);
        var pitchX = (double)box.Width / cols;
        var pitchY = (double)box.Height / rows;
        for (var y = 0; y < rows; y++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var sy = Clamp((int)Math.Floor(box.Top + ((y + 0.5) * pitchY)), 0, height - 1);
            for (var x = 0; x < cols; x++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var sx = Clamp((int)Math.Floor(box.Left + ((x + 0.5) * pitchX)), 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy);
                modules[x, y] = invert ? !dark : dark;
            }
        }
        return true;
    }

    // Counting the clock tracks avoids rounding a fractional module pitch to
    // the shortest pixel run. Try both opposite edges to retain rotated symbols.
    private static bool TryResolveTimingGrid(PixelSpan pixels, int width, int height, int stride,
        PixelFormat format, BoundingBox box, bool invert, int minimumRun,
        CancellationToken cancellationToken, ref int cols, ref int rows) {
        // Stay on the border for one-pixel modules. If an outer resampling
        // fringe obscures it, try one pixel inward before abandoning inference.
        var inset = minimumRun / 2;
        var maximumInset = Math.Max(1, inset);
        for (; inset <= maximumInset; inset++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var clockCols = Math.Max(
                CountEdgeRuns(pixels, width, height, stride, format, box.Left, box.Right, box.Top + inset, true, invert, cancellationToken),
                CountEdgeRuns(pixels, width, height, stride, format, box.Left, box.Right, box.Bottom - inset, true, invert, cancellationToken));
            var clockRows = Math.Max(
                CountEdgeRuns(pixels, width, height, stride, format, box.Top, box.Bottom, box.Left + inset, false, invert, cancellationToken),
                CountEdgeRuns(pixels, width, height, stride, format, box.Top, box.Bottom, box.Right - inset, false, invert, cancellationToken));
            if (!DataMatrixSymbolInfo.TryGetForSize(clockRows, clockCols, out _) &&
                !DataMatrixSymbolInfo.TryGetForSize(clockCols, clockRows, out _)) continue;

            var pitchX = (double)box.Width / clockCols;
            var pitchY = (double)box.Height / clockRows;
            if (pitchX < 1 || pitchY < 1 || Math.Abs(pitchX - pitchY) > 1) continue;
            cols = clockCols;
            rows = clockRows;
            return true;
        }
        return false;
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
