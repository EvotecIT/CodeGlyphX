using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

/// <summary>Shares the demonstration logo between the style board and website hero specimens.</summary>
internal static class QrSampleBrandMark {
    /// <summary>Renders a rounded sample tile with an open C and detached terminal, using 4x4 supersampling.</summary>
    internal static byte[] Render(int size, Rgba32 tile, Rgba32 mark) {
        var rgba = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++) {
                int tileHits = 0, markHits = 0;
                for (var sy = 0; sy < 4; sy++)
                    for (var sx = 0; sx < 4; sx++) {
                        var u = (x + (sx + .5) / 4) / size - .5;
                        var v = (y + (sy + .5) / 4) / size - .5;
                        if (!InRoundedSquare(u, v, .5, .23)) continue;
                        tileHits++;
                        var r = Math.Sqrt(u * u + v * v);
                        var opening = u > 0 && Math.Abs(v) < u * .9;
                        var arc = r <= .29 && r >= .165 && !opening;
                        var terminal = InRoundedSquare(u - .2275, v, .0625, .018);
                        if (arc || terminal) markHits++;
                    }
                if (tileHits == 0) continue;
                var p = (y * size + x) * 4;
                var t = markHits / (double)tileHits;
                rgba[p] = (byte)Math.Round(tile.R + (mark.R - tile.R) * t);
                rgba[p + 1] = (byte)Math.Round(tile.G + (mark.G - tile.G) * t);
                rgba[p + 2] = (byte)Math.Round(tile.B + (mark.B - tile.B) * t);
                rgba[p + 3] = (byte)((tileHits * 255 + 8) / 16);
            }
        return PngImageEncoder.EncodeRgba32(rgba, size, size);

        static bool InRoundedSquare(double u, double v, double half, double radius) {
            var dx = Math.Abs(u) - (half - radius);
            var dy = Math.Abs(v) - (half - radius);
            if (dx <= 0 || dy <= 0) return Math.Abs(u) <= half && Math.Abs(v) <= half;
            return dx * dx + dy * dy <= radius * radius;
        }
    }

}
