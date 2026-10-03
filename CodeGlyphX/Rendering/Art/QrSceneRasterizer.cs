using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Rendering.Art;

internal static class QrSceneRasterizer {
    internal static byte[] Paint(int size, QrSceneOptions options, QrSceneGeometry geometry, CancellationToken token) {
        const string limit = "QR scene exceeds output limits.";
        RenderGuards.EnsureOutputPixels(size, size, limit);
        var pixels = new byte[RenderGuards.EnsureOutputBytes((long)size * size * 4, limit)];
        for (var i = 0; i < pixels.Length; i += 4) {
            if ((i & 65535) == 0) token.ThrowIfCancellationRequested();
            pixels[i] = options.Paper.R; pixels[i + 1] = options.Paper.G; pixels[i + 2] = options.Paper.B; pixels[i + 3] = 255;
        }
        var crossings = new List<double>(); var coverage = new double[size];
        foreach (var shape in geometry.Shapes) {
            token.ThrowIfCancellationRequested();
            var points = shape.Points; var minY = 1.0; var maxY = 0.0;
            for (var i = 1; i < points.Length; i += 2) { minY = Math.Min(minY, points[i]); maxY = Math.Max(maxY, points[i]); }
            var startY = (int)Math.Max(0, Math.Floor(minY * size)); var endY = (int)Math.Min(size, Math.Ceiling(maxY * size));
            for (var y = startY; y < endY; y++) {
                token.ThrowIfCancellationRequested(); Array.Clear(coverage, 0, size);
                for (var sample = 0; sample < 2; sample++) {
                    var py = (y + .25 + sample * .5) / size; crossings.Clear();
                    for (var i = 0; i < points.Length; i += 2) {
                        var j = (i + 2) % points.Length; var ya = points[i + 1]; var yb = points[j + 1];
                        if ((ya <= py && yb > py) || (yb <= py && ya > py))
                            crossings.Add((points[i] + (py - ya) * (points[j] - points[i]) / (yb - ya)) * size);
                    }
                    crossings.Sort();
                    for (var i = 0; i + 1 < crossings.Count; i += 2) {
                        var left = Math.Max(0, crossings[i]); var right = Math.Min(size, crossings[i + 1]);
                        for (var x = (int)Math.Floor(left); x < (int)Math.Ceiling(right); x++) {
                            if (x >= 0 && x < size) coverage[x] += Math.Max(0, Math.Min(x + 1, right) - Math.Max(x, left)) / 2;
                        }
                    }
                }
                for (var x = 0; x < size; x++) {
                    var a = Math.Min(1, coverage[x]); if (a == 0) continue;
                    var p = (y * size + x) * 4;
                    pixels[p] = Blend(pixels[p], shape.Color.R, a); pixels[p + 1] = Blend(pixels[p + 1], shape.Color.G, a); pixels[p + 2] = Blend(pixels[p + 2], shape.Color.B, a);
                }
            }
        }
        return pixels;
    }
    private static byte Blend(byte source, byte ink, double amount) => (byte)Math.Round(source + (ink - source) * amount);
}
