using System;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    // Signed-distance bloom has finite support: the final edge is transparent and the layout
    // reserves its extent. This avoids blurred rectangles cropped at the image boundary.
    private static void DrawSoftPanelEffect(byte[] pixels, int width, int height, int stride,
        int x, int y, int w, int h, int radius, int extent, Rgba32 color) {
        if (extent <= 0 || color.A == 0) return;
        var r = Math.Max(0, Math.Min(radius, Math.Min(w, h) / 2));
        for (var py = Math.Max(0, y - extent); py < Math.Min(height, y + h + extent); py++)
            for (var px = Math.Max(0, x - extent); px < Math.Min(width, x + w + extent); px++) {
                var dx = Math.Abs(px + .5 - x - w / 2.0) - w / 2.0 + r;
                var dy = Math.Abs(py + .5 - y - h / 2.0) - h / 2.0 + r;
                var distance = Math.Sqrt(Math.Max(0, dx) * Math.Max(0, dx) + Math.Max(0, dy) * Math.Max(0, dy)) + Math.Min(0, Math.Max(dx, dy)) - r;
                var t = Math.Max(0, Math.Min(1, 1 - Math.Max(0, distance) / extent));
                var alpha = (byte)Math.Round(color.A * t * t * (3 - 2 * t));
                if (alpha > 0) BlendPixel(pixels, stride, px, py, new Rgba32(color.R, color.G, color.B, alpha));
            }
    }
}
