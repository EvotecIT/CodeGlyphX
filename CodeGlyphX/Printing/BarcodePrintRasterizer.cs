using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Printing;

/// <summary>Scan-converts the scene before blending, preserving touching ink and compound holes.</summary>
internal static class BarcodePrintRasterizer {
    private readonly struct Edge {
        internal readonly double X, Y, Bottom, Slope;
        internal readonly int Winding, Path;
        internal Edge(BarcodePoint a, BarcodePoint b, int path) {
            Winding = b.Y > a.Y ? 1 : -1;
            if (a.Y > b.Y) { var swap = a; a = b; b = swap; }
            X = a.X; Y = a.Y; Bottom = b.Y; Slope = (b.X - a.X) / (b.Y - a.Y); Path = path;
        }
    }
    private readonly struct Crossing {
        internal readonly double X;
        internal readonly int Winding, Path;
        internal Crossing(double x, int winding, int path) { X = x; Winding = winding; Path = path; }
    }

    internal static byte[] Render(BarcodePrintLayout layout, BarcodePrintPlacement? placement, out int width, out int height) {
        var transform = PrintTransform.Resolve(layout, placement, out var mmWidth, out var mmHeight, out _);
        var scale = layout.Dpi / 25.4;
        var w = Math.Ceiling(mmWidth * scale - 1e-9); var h = Math.Ceiling(mmHeight * scale - 1e-9);
        if (w < 1 || h < 1 || w > int.MaxValue || h > int.MaxValue || w * h > 16000000)
            throw new ArgumentException("The print raster exceeds its 16 million pixel limit. Reduce DPI or use vector output.");
        width = (int)w; height = (int)h;
        RenderGuards.EnsureOutputPixels(width, height, "Print raster exceeds output limits.");
        var pixels = new byte[RenderGuards.EnsureOutputBytes((long)width * height * 4, "Print raster exceeds output limits.")];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var c = layout.Background.Sample((x + .5) / scale, (y + .5) / scale);
            var at = (y * width + x) * 4;
            pixels[at] = c.R; pixels[at + 1] = c.G; pixels[at + 2] = c.B; pixels[at + 3] = c.A;
        }
        var edges = new List<Edge>();
        var paints = new List<BarcodePaint>();
        for (var index = 0; index < layout.Paths.Count; index++) {
            var path = layout.Paths[index];
            // Keep scene painting order, including overlapping translucent text. Integrating
            // the final scene before antialiasing already removes seams between touching paths.
            paints.Add(path.Paint);
            foreach (var contour in path.Contours) {
                var previous = PixelPoint(transform, contour[contour.Count - 1], scale);
                foreach (var point in contour) {
                    var current = PixelPoint(transform, point, scale);
                    if (previous.Y != current.Y) edges.Add(new Edge(previous, current, index));
                    previous = current;
                }
            }
        }
        if (edges.Count == 0) return pixels;
        edges.Sort((a, b) => a.Y.CompareTo(b.Y));
        var active = new List<Edge>();
        var crossings = new List<Crossing>();
        var winding = new int[layout.Paths.Count];
        var layers = new SortedSet<int>();
        var row = new double[checked(width * 4)];
        var next = 0;
        long work = 0;
        for (var y = 0; y < height; y++) {
            var rowAt = y * width * 4;
            for (var x = 0; x < width; x++) {
                var at = x * 4; var alpha = pixels[rowAt + at + 3] / 255D;
                row[at] = pixels[rowAt + at] * alpha; row[at + 1] = pixels[rowAt + at + 1] * alpha;
                row[at + 2] = pixels[rowAt + at + 2] * alpha; row[at + 3] = alpha;
            }
            for (var sample = 0; sample < 4; sample++) {
                var sy = y + (sample + .5) / 4;
                active.RemoveAll(edge => edge.Bottom <= sy);
                while (next < edges.Count && edges[next].Y <= sy) {
                    if (edges[next].Bottom > sy) active.Add(edges[next]);
                    next++;
                }
                crossings.Clear();
                foreach (var edge in active) crossings.Add(new Crossing(edge.X + (sy - edge.Y) * edge.Slope, edge.Winding, edge.Path));
                work += active.Count;
                CheckWork(work);
                crossings.Sort((a, b) => a.X.CompareTo(b.X));
                var start = 0D;
                foreach (var crossing in crossings) {
                    if (layers.Count > 0) Cover(row, pixels, rowAt, width, y, scale, transform, paints, layers, start, crossing.X, ref work);
                    var path = crossing.Path;
                    var before = winding[path];
                    winding[path] += crossing.Winding;
                    if (before == 0 && winding[path] != 0) layers.Add(path);
                    else if (before != 0 && winding[path] == 0) layers.Remove(path);
                    start = crossing.X;
                }
            }
            for (var x = 0; x < width; x++) {
                var at = x * 4; var alpha = Math.Max(0, Math.Min(1, row[at + 3]));
                for (var channel = 0; channel < 3; channel++) pixels[rowAt + at + channel] =
                    alpha <= 1e-12 ? (byte)0 : Byte(row[at + channel] / alpha);
                pixels[rowAt + at + 3] = Byte(alpha * 255);
            }
        }
        return pixels;
    }

    private static BarcodePoint PixelPoint(PrintTransform transform, BarcodePoint point, double scale) {
        var p = transform.Map(point);
        return new BarcodePoint(p.X * scale, p.Y * scale);
    }

    private static void Cover(double[] row, byte[] pixels, int rowAt, int width, int y, double scale,
        PrintTransform transform, List<BarcodePaint> paints, SortedSet<int> layers, double start, double end, ref long work) {
        var from = Math.Max(0, start); var to = Math.Min(width, end);
        if (from >= to) return;
        for (var x = (int)Math.Floor(from); x < Math.Ceiling(to); x++) {
            work += layers.Count;
            CheckWork(work);
            var amount = Math.Max(0, Math.Min(to, x + 1) - Math.Max(from, x)) / 4;
            var at = x * 4;
            var ba = pixels[rowAt + at + 3] / 255D;
            var br = pixels[rowAt + at] * ba; var bg = pixels[rowAt + at + 1] * ba; var bb = pixels[rowAt + at + 2] * ba;
            var a = ba; var r = br; var g = bg; var b = bb;
            var original = transform.Inverse((x + .5) / scale, (y + .5) / scale);
            foreach (var layer in layers) {
                var color = paints[layer].Sample(original.X, original.Y);
                var alpha = color.A / 255D;
                r = color.R * alpha + r * (1 - alpha); g = color.G * alpha + g * (1 - alpha);
                b = color.B * alpha + b * (1 - alpha); a = alpha + a * (1 - alpha);
            }
            row[at] += (r - br) * amount; row[at + 1] += (g - bg) * amount;
            row[at + 2] += (b - bb) * amount; row[at + 3] += (a - ba) * amount;
        }
    }
    private static byte Byte(double value) => (byte)Math.Round(Math.Max(0, Math.Min(255, value)));
    private static void CheckWork(long work) {
        if (work > 128000000) throw new ArgumentException("The print raster exceeds its outline sampling budget. Simplify text or use vector output.");
    }
}
