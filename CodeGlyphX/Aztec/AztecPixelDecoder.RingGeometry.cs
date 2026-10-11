using System;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Aztec;

internal static partial class AztecPixelDecoder {
    private readonly struct RingPoint {
        internal readonly double X, Y;
        internal RingPoint(double x, double y) { X = x; Y = y; }
    }

    private struct RingLineFit {
        internal int Count;
        internal double X, Y, XX, YY, XY;
        internal void Add(int x, int y) {
            Count++; X += x; Y += y; XX += (double)x * x; YY += (double)y * y; XY += (double)x * y;
        }
        internal bool TryLine(RingPoint center, out RingPoint normal, out double offset) {
            normal = default; offset = 0;
            if (Count < 4) return false;
            var mx = X / Count; var my = Y / Count;
            var angle = 0.5 * Math.Atan2(2 * (XY / Count - mx * my), XX / Count - mx * mx - YY / Count + my * my);
            var nx = -Math.Sin(angle); var ny = Math.Cos(angle);
            if ((mx - center.X) * nx + (my - center.Y) * ny < 0) { nx = -nx; ny = -ny; }
            normal = new RingPoint(nx, ny);
            offset = nx * mx + ny * my + 0.5;
            return true;
        }
    }

    /// <summary>
    /// Fits the four outer ring edges using row and column extrema. Fitting
    /// stair steps rather than single corner pixels limits extrapolation error
    /// when the small bullseye anchors a much larger data grid.
    /// </summary>
    private static bool TryFitRing(int[] pixels, int length, int width, int height, int[] extrema,
        CancellationToken cancellationToken, out RingPoint[] corners) {
        corners = Array.Empty<RingPoint>();
        var first = new RingPoint(pixels[0] % width, pixels[0] / width);
        var quad = new[] { first, first, first, first };
        var minX = width; var minY = height; var maxX = 0; var maxY = 0;
        for (var i = 0; i < length; i++) {
            if ((i & 511) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var x = pixels[i] % width; var y = pixels[i] / width;
            minX = Math.Min(minX, x); minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
            if (x + y < quad[0].X + quad[0].Y) quad[0] = new RingPoint(x, y);
            if (x - y > quad[1].X - quad[1].Y) quad[1] = new RingPoint(x, y);
            if (x + y > quad[2].X + quad[2].Y) quad[2] = new RingPoint(x, y);
            if (x - y < quad[3].X - quad[3].Y) quad[3] = new RingPoint(x, y);
        }
        var fill = length / (double)((maxX - minX + 1) * (maxY - minY + 1));
        if (maxX - minX < 9 || maxY - minY < 9 || fill < 0.12 || fill > 0.75) return false;
        var center = new RingPoint((quad[0].X + quad[1].X + quad[2].X + quad[3].X) / 4,
            (quad[0].Y + quad[1].Y + quad[2].Y + quad[3].Y) / 4);
        var normals = new RingPoint[4];
        for (var i = 0; i < 4; i++) {
            var dx = quad[(i + 1) % 4].X - quad[i].X;
            var dy = quad[(i + 1) % 4].Y - quad[i].Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < 8) return false;
            normals[i] = new RingPoint(-dy / distance, dx / distance);
        }
        // Left/right extrema by row, then top/bottom extrema by column.
        for (var y = minY; y <= maxY; y++) { extrema[y] = width; extrema[height + y] = -1; }
        for (var x = minX; x <= maxX; x++) { extrema[2 * height + x] = height; extrema[2 * height + width + x] = -1; }
        for (var i = 0; i < length; i++) {
            if ((i & 511) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var x = pixels[i] % width; var y = pixels[i] / width;
            extrema[y] = Math.Min(extrema[y], x); extrema[height + y] = Math.Max(extrema[height + y], x);
            extrema[2 * height + x] = Math.Min(extrema[2 * height + x], y);
            extrema[2 * height + width + x] = Math.Max(extrema[2 * height + width + x], y);
        }
        var fits = new RingLineFit[4];
        for (var i = 0; i < length; i++) {
            if ((i & 511) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var x = pixels[i] % width; var y = pixels[i] / width;
            if (x != extrema[y] && x != extrema[height + y] && y != extrema[2 * height + x] && y != extrema[2 * height + width + x]) continue;
            var closest = -1; var best = 2.0;
            for (var side = 0; side < 4; side++) {
                var distance = Math.Abs((x - quad[side].X) * normals[side].X + (y - quad[side].Y) * normals[side].Y);
                if (distance < best) { closest = side; best = distance; }
            }
            if (closest >= 0) fits[closest].Add(x, y);
        }
        var offsets = new double[4];
        for (var i = 0; i < 4; i++) if (!fits[i].TryLine(center, out normals[i], out offsets[i])) return false;
        corners = new RingPoint[4];
        for (var i = 0; i < 4; i++) {
            var previous = (i + 3) % 4;
            var a = normals[previous]; var b = normals[i];
            var determinant = a.X * b.Y - a.Y * b.X;
            if (Math.Abs(determinant) < 0.1) return false;
            var x = (offsets[previous] * b.Y - a.Y * offsets[i]) / determinant;
            var y = (a.X * offsets[i] - offsets[previous] * b.X) / determinant;
            if (!(x >= 0 && x < width && y >= 0 && y < height) ||
                Math.Abs(x - quad[i].X) > 4 || Math.Abs(y - quad[i].Y) > 4) return false;
            corners[i] = new RingPoint(x, y);
        }
        return true;
    }
}
