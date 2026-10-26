using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static void ApplyLogo(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int originX,
        int originY,
        int qrSizePx,
        QrRasterLogoOptions logo) {
        if (logo.Rgba.Length == 0) return;
        if (logo.Scale <= 0) return;

        var maxLogoPx = (int)Math.Round(qrSizePx * logo.Scale);
        if (maxLogoPx <= 0) return;
        if (maxLogoPx > qrSizePx) maxLogoPx = qrSizePx;

        var scale = Math.Min(maxLogoPx / (double)logo.Width, maxLogoPx / (double)logo.Height);
        if (scale <= 0) return;

        var targetW = Math.Max(1, (int)Math.Round(logo.Width * scale));
        var targetH = Math.Max(1, (int)Math.Round(logo.Height * scale));
        if (targetW <= 0 || targetH <= 0) return;

        var logoX = logo.OffsetXPx ?? (qrSizePx - targetW) / 2;
        var logoY = logo.OffsetYPx ?? (qrSizePx - targetH) / 2;
        if (logoX < 0 || logoX > qrSizePx - targetW) throw new ArgumentOutOfRangeException(nameof(logo.OffsetXPx));
        if (logoY < 0 || logoY > qrSizePx - targetH) throw new ArgumentOutOfRangeException(nameof(logo.OffsetYPx));
        var x0 = originX + logoX;
        var y0 = originY + logoY;

        if (logo.DrawBackground) {
            var pad = Math.Max(0, logo.PaddingPx);
            var bgX = x0 - pad;
            var bgY = y0 - pad;
            var bgW = targetW + pad * 2;
            var bgH = targetH + pad * 2;
            DrawLogoPlate(scanlines, widthPx, heightPx, stride, bgX, bgY, bgW, bgH, logo);
        }

        BlitLogo(scanlines, widthPx, heightPx, stride, x0, y0, targetW, targetH, logo);
    }

    private static void DrawLogoPlate(byte[] pixels, int width, int height, int stride, int x0, int y0, int w, int h, QrRasterLogoOptions logo) {
        var silhouette = logo.PlateShape switch {
            QrLogoPlateShape.Circle => QrModuleShape.Circle,
            QrLogoPlateShape.Squircle => QrModuleShape.Squircle,
            QrLogoPlateShape.Square => QrModuleShape.Square,
            _ => QrModuleShape.Rounded
        };
        var radius = logo.CornerRadiusPx > 0 ? logo.CornerRadiusPx / (double)Math.Min(w, h) : .2;
        var ring = Math.Max(0, Math.Min(logo.RingWidthPx, Math.Min(w, h) / 4));
        for (var y = Math.Max(0, y0); y < Math.Min(height, y0 + h); y++)
            for (var x = Math.Max(0, x0); x < Math.Min(width, x0 + w); x++) {
                var outer = 0; var inner = 0;
                for (var sy = 0; sy < 4; sy++) for (var sx = 0; sx < 4; sx++) {
                    var xx = x - x0 + (sx + .5) / 4; var yy = y - y0 + (sy + .5) / 4;
                    if (QrShapeGeometry.Contains(silhouette, xx / w, yy / h, radius: radius)) outer++;
                    if (ring > 0 && QrShapeGeometry.Contains(silhouette, (xx - ring) / (w - 2 * ring), (yy - ring) / (h - 2 * ring), radius: radius)) inner++;
                }
                var at = y * (stride + 1) + 1 + x * 4;
                if (outer > 0) BlendPixelAt(pixels, at, CoverageColor(logo.Background, outer));
                if (ring > 0 && outer > inner && logo.RingColor.HasValue) BlendPixelAt(pixels, at, CoverageColor(logo.RingColor.Value, outer - inner));
            }
    }

    private static void BlitLogo(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x0,
        int y0,
        int targetW,
        int targetH,
        QrRasterLogoOptions logo) {
        var opaqueSource = true;
        for (var i = 3; i < logo.Rgba.Length; i += 4) if (logo.Rgba[i] < 255) { opaqueSource = false; break; }
        var x1 = Math.Min(widthPx, x0 + targetW);
        var y1 = Math.Min(heightPx, y0 + targetH);
        if (x1 <= x0 || y1 <= y0) return;

        for (var y = y0; y < y1; y++) {
            var ty = (y - y0) * logo.Height / targetH;
            var srcRow = ty * logo.Width * 4;
            for (var x = x0; x < x1; x++) {
                var tx = (x - x0) * logo.Width / targetW;
                var src = srcRow + tx * 4;

                var sr = logo.Rgba[src + 0];
                var sg = logo.Rgba[src + 1];
                var sb = logo.Rgba[src + 2];
                var sa = logo.Rgba[src + 3];
                if (logo.Tint.HasValue) {
                    var tint = logo.Tint.Value;
                    if (opaqueSource) sa = (byte)(sa * (255 - (299 * sr + 587 * sg + 114 * sb) / 1000) / 255);
                    sr = tint.R; sg = tint.G; sb = tint.B; sa = (byte)(sa * tint.A / 255);
                }
                if (sa == 0) continue;

                var dst = y * (stride + 1) + 1 + x * 4;
                if (sa == 255) {
                    scanlines[dst + 0] = sr;
                    scanlines[dst + 1] = sg;
                    scanlines[dst + 2] = sb;
                    scanlines[dst + 3] = 255;
                    continue;
                }

                BlendPixelAt(scanlines, dst, new Rgba32(sr, sg, sb, sa));
            }
        }
    }
}
