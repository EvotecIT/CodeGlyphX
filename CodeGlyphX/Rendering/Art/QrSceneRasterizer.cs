using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Rendering.Art;

// Exact-area scanline rasterizer: every edge deposits its signed area into an accumulation strip, and a running
// sum gives each pixel's coverage. Contours of one shape share a strip, so touching cells and glyph parts join
// without seams, and opposite windings cut holes.
internal static class QrSceneRasterizer {
    private const int StripHeight = 64;

    internal static byte[] Paint(int size, QrSceneOptions options, QrSceneGeometry geometry, CancellationToken token) {
        const string limit = "QR scene exceeds output limits.";
        RenderGuards.EnsureOutputPixels(size, size, limit);
        var pixels = new byte[RenderGuards.EnsureOutputBytes((long)size * size * 4, limit)];
        for (var i = 0; i < pixels.Length; i += 4) {
            if ((i & 65535) == 0) token.ThrowIfCancellationRequested();
            pixels[i] = options.Paper.R; pixels[i + 1] = options.Paper.G; pixels[i + 2] = options.Paper.B; pixels[i + 3] = 255;
        }
        float[]? strip = null;
        foreach (var shape in geometry.Shapes) {
            token.ThrowIfCancellationRequested();
            PaintShape(pixels, size, shape, ref strip, token);
        }
        return pixels;
    }

    private static void PaintShape(byte[] pixels, int size, SceneShape shape, ref float[]? strip, CancellationToken token) {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var contour in shape.Contours) for (var i = 0; i < contour.Length; i += 2) {
            minX = Math.Min(minX, contour[i]); maxX = Math.Max(maxX, contour[i]);
            minY = Math.Min(minY, contour[i + 1]); maxY = Math.Max(maxY, contour[i + 1]);
        }
        // Bound storage to three canvas widths while retaining each edge's original geometry.
        var left = (int)Math.Floor(Math.Max(-size, minX * size)); var right = (int)Math.Ceiling(Math.Min(2.0 * size, maxX * size));
        var top = (int)Math.Max(0, Math.Floor(minY * size)); var bottom = (int)Math.Min(size, Math.Ceiling(maxY * size));
        if (right <= left || bottom <= top || right <= 0 || left >= size) return;
        var width = right - left + 3;
        var needed = width * StripHeight;
        if (strip is null || strip.Length < needed) strip = new float[needed];
        var paint = shape.Paint;
        for (var band = top; band < bottom; band += StripHeight) {
            token.ThrowIfCancellationRequested();
            var rows = Math.Min(StripHeight, bottom - band);
            Array.Clear(strip, 0, width * rows);
            foreach (var contour in shape.Contours) {
                for (var i = 0; i < contour.Length; i += 2) {
                    var j = (i + 2) % contour.Length;
                    Accumulate(strip, width, rows, contour[i] * size - left, contour[i + 1] * size - band,
                        contour[j] * size - left, contour[j + 1] * size - band);
                }
            }
            for (var row = 0; row < rows; row++) {
                var y = band + row; var line = row * width; var sum = 0f;
                for (var column = 0; column < width; column++) {
                    sum += strip[line + column];
                    var x = left + column;
                    if (x < 0 || x >= size) continue;
                    var coverage = Math.Min(1, Math.Abs(sum));
                    if (coverage < 1e-4) continue;
                    paint.Evaluate((x + 0.5) / size, (y + 0.5) / size, out var r, out var g, out var b, out var stop);
                    var alpha = coverage * paint.Opacity * stop;
                    var p = (y * size + x) * 4;
                    pixels[p] = Blend(pixels[p], r, alpha); pixels[p + 1] = Blend(pixels[p + 1], g, alpha); pixels[p + 2] = Blend(pixels[p + 2], b, alpha);
                }
            }
        }
    }

    // Signed-area accumulation for one edge inside a strip of rows [0, height).
    private static void Accumulate(float[] acc, int width, int height, double x0, double y0, double x1, double y1) {
        if (Math.Abs(y0 - y1) < 1e-9) return;
        double direction = 1;
        if (y0 > y1) { direction = -1; (x0, x1) = (x1, x0); (y0, y1) = (y1, y0); }
        if (y1 <= 0 || y0 >= height) return;
        var dxdy = (x1 - x0) / (y1 - y0);
        var x = x0;
        if (y0 < 0) { x -= y0 * dxdy; y0 = 0; }
        if (y1 > height) y1 = height;
        var start = (int)Math.Floor(y0);
        var end = (int)Math.Ceiling(y1);
        for (var y = start; y < end; y++) {
            var line = y * width;
            var dy = Math.Min(y + 1, y1) - Math.Max(y, y0);
            var next = x + dxdy * dy;
            var d = (float)(dy * direction);
            double a, b;
            if (x < next) { a = x; b = next; } else { a = next; b = x; }
            // Fold deposits left of storage into its initial winding, and ignore deposits
            // to its right. Clipping the coordinates themselves would change the edge slope.
            var a0 = Math.Floor(a); var ai = (int)a0;
            var bi = (int)Math.Ceiling(b);
            if (bi <= ai + 1) {
                var middle = 0.5 * (x + next) - a0;
                Deposit(acc, line, width, ai, (float)(d - d * middle));
                Deposit(acc, line, width, ai + 1, (float)(d * middle));
            } else {
                var s = 1 / (b - a);
                var af = a - a0;
                var first = 0.5 * s * (1 - af) * (1 - af);
                var bf = b - bi + 1;
                var last = 0.5 * s * bf * bf;
                Deposit(acc, line, width, ai, (float)(d * first));
                if (bi == ai + 2) Deposit(acc, line, width, ai + 1, (float)(d * (1 - first - last)));
                else {
                    var second = s * (1.5 - af);
                    Deposit(acc, line, width, ai + 1, (float)(d * (second - first)));
                    var startColumn = ai + 2;
                    var endColumn = bi - 1;
                    var before = Math.Min(0, endColumn) - startColumn;
                    if (before > 0) acc[line] += (float)(d * s * before);
                    for (var xi = Math.Max(0, startColumn); xi < Math.Min(width, endColumn); xi++) acc[line + xi] += (float)(d * s);
                    var rest = second + (bi - ai - 3) * s;
                    Deposit(acc, line, width, bi - 1, (float)(d * (1 - rest - last)));
                }
                Deposit(acc, line, width, bi, (float)(d * last));
            }
            x = next;
        }
    }

    private static void Deposit(float[] acc, int line, int width, int column, float area) {
        if (column < 0) acc[line] += area;
        else if (column < width) acc[line + column] += area;
    }

    private static byte Blend(byte source, double ink, double amount) => (byte)Math.Round(source + (ink - source) * amount);
}
