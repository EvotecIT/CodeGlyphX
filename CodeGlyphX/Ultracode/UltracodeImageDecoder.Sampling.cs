using System;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Ultracode;

internal static partial class UltracodeImageDecoder {
    private static readonly double[] SampleOffsetsX = { 0, -.18, .18, -.18, .18 };
    private static readonly double[] SampleOffsetsY = { 0, -.18, -.18, .18, .18 };

    private static UltracodeGrid? Sample(byte[] rgba, int width, int height, SymbolPoint[] corners,
        int columns, int rows, CancellationToken token) {
        var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0, columns, 0, columns, rows, 0, rows,
            corners[0].X, corners[0].Y, corners[1].X, corners[1].Y,
            corners[2].X, corners[2].Y, corners[3].X, corners[3].Y);
        var grid = new UltracodeGrid(columns, rows);
        var counts = new int[7];
        for (var y = 0; y < rows; y++) {
            if (DecodeBudget.ShouldAbort(token)) return null;
            for (var x = 0; x < columns; x++) {
                Array.Clear(counts, 0, counts.Length);
                for (var sample = 0; sample < SampleOffsetsX.Length; sample++) {
                    transform.Transform(x + .5 + SampleOffsetsX[sample], y + .5 + SampleOffsetsY[sample], out var sx, out var sy);
                    if (double.IsNaN(sx) || double.IsNaN(sy) || sx < 0 || sy < 0 || sx >= width || sy >= height) continue;
                    Channels(rgba, ((int)sy * width + (int)sx) * 4, out var r, out var g, out var b);
                    counts[(int)Classify(r, g, b)]++;
                }
                var best = 0;
                for (var color = 1; color < counts.Length; color++) if (counts[color] > counts[best]) best = color;
                grid[x, y] = counts[best] >= 3 ? (UltracodeColor)best : UltracodeColor.Unknown;
            }
        }
        return grid;
    }

    private static UltracodeColor Classify(int r, int g, int b) {
        var minimum = Math.Min(r, Math.Min(g, b)); var maximum = Math.Max(r, Math.Max(g, b));
        if (maximum < 110) return UltracodeColor.Black;
        if (minimum > 175 && maximum - minimum < 65) return UltracodeColor.White;
        if (maximum - minimum < 60) return UltracodeColor.Unknown;
        if (r < Math.Min(g, b) * .65) return UltracodeColor.Cyan;
        if (g < Math.Min(r, b) * .65) return UltracodeColor.Magenta;
        if (b < Math.Min(r, g) * .65) return UltracodeColor.Yellow;
        if (r < g * .65 && b < g * .65) return UltracodeColor.Green;
        return UltracodeColor.Unknown;
    }

    private static void Channels(byte[] rgba, int at, out int r, out int g, out int b) {
        var alpha = rgba[at + 3];
        r = (rgba[at] * alpha + 255 * (255 - alpha) + 127) / 255;
        g = (rgba[at + 1] * alpha + 255 * (255 - alpha) + 127) / 255;
        b = (rgba[at + 2] * alpha + 255 * (255 - alpha) + 127) / 255;
    }
}
