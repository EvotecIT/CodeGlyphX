using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static bool TryGetEye(int x, int y, int size, out int ex, out int ey, out EyeKind kind) {
        if (IsInEye(x, y, 0, 0)) {
            ex = 0;
            ey = 0;
            kind = GetEyeModuleKind(x, y, 0, 0);
            return true;
        }
        if (IsInEye(x, y, size - 7, 0)) {
            ex = size - 7;
            ey = 0;
            kind = GetEyeModuleKind(x, y, size - 7, 0);
            return true;
        }
        if (IsInEye(x, y, 0, size - 7)) {
            ex = 0;
            ey = size - 7;
            kind = GetEyeModuleKind(x, y, 0, size - 7);
            return true;
        }
        ex = 0;
        ey = 0;
        kind = EyeKind.None;
        return false;
    }

    private static bool IsInEye(int x, int y, int ex, int ey) {
        return x >= ex && x < ex + 7 && y >= ey && y < ey + 7;
    }

    private static EyeKind GetEyeModuleKind(int x, int y, int ex, int ey) {
        var lx = x - ex;
        var ly = y - ey;
        if (lx >= 2 && lx <= 4 && ly >= 2 && ly <= 4) return EyeKind.Inner;
        return EyeKind.Outer;
    }

    private static void DrawEyeFrame(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngRenderOptions opts,
        int ex,
        int ey) {
        var moduleSize = opts.ModuleSize;
        var x0 = (ex + opts.QuietZone) * moduleSize;
        var y0 = (ey + opts.QuietZone) * moduleSize;

        var outerSize = 7 * moduleSize;
        var innerSize = 5 * moduleSize;
        var dotSize = 3 * moduleSize;

        var eye = opts.Eyes!;
        var outerScaled = ScaleSize(outerSize, eye.OuterScale);
        var innerScaled = ScaleSize(innerSize, eye.OuterScale);
        var dotScaled = ScaleSize(dotSize, eye.InnerScale);

        var outerX = x0 + (outerSize - outerScaled) / 2;
        var outerY = y0 + (outerSize - outerScaled) / 2;
        var innerX = x0 + (outerSize - innerScaled) / 2;
        var innerY = y0 + (outerSize - innerScaled) / 2;
        var dotX = x0 + (outerSize - dotScaled) / 2;
        var dotY = y0 + (outerSize - dotScaled) / 2;

        var outerColor = eye.OuterColor ?? opts.Foreground;
        var innerColor = eye.InnerColor ?? opts.Foreground;
        var outerGradient = eye.OuterGradient;
        var innerGradient = eye.InnerGradient;

        if (outerGradient is null) {
            FillShape(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerColor, eye.OuterShape, eye.OuterCornerRadiusPx);
        } else {
            FillShapeGradient(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerGradient, eye.OuterShape, eye.OuterCornerRadiusPx);
        }
        FillShape(scanlines, widthPx, heightPx, stride, innerX, innerY, innerScaled, innerScaled, opts.Background, eye.OuterShape, eye.InnerCornerRadiusPx);

        if (dotScaled > 0) {
            if (innerGradient is null) {
                FillShape(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerColor, eye.InnerShape, eye.InnerCornerRadiusPx);
            } else {
                FillShapeGradient(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerGradient, eye.InnerShape, eye.InnerCornerRadiusPx);
            }
        }
    }

    private static void FillShape(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color,
        QrPngModuleShape shape,
        int radius) {
        switch (shape) {
            case QrPngModuleShape.Circle:
                FillEllipse(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            case QrPngModuleShape.Rounded:
                FillRoundedRect(scanlines, widthPx, heightPx, stride, x, y, w, h, color, radius);
                return;
            case QrPngModuleShape.Diamond:
                FillDiamond(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            case QrPngModuleShape.Squircle:
                FillSquircle(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            case QrPngModuleShape.Dot:
                FillDot(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            case QrPngModuleShape.DotGrid:
                FillDotGrid(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            default:
                FillRoundedRect(scanlines, widthPx, heightPx, stride, x, y, w, h, color, 0);
                return;
        }
    }

    private static void FillShapeGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient,
        QrPngModuleShape shape,
        int radius) {
        switch (shape) {
            case QrPngModuleShape.Circle:
                FillEllipseGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            case QrPngModuleShape.Rounded:
                FillRoundedRectGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient, radius);
                return;
            case QrPngModuleShape.Diamond:
                FillDiamondGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            case QrPngModuleShape.Squircle:
                FillSquircleGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            case QrPngModuleShape.Dot:
                FillDotGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            case QrPngModuleShape.DotGrid:
                FillDotGridGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            default:
                FillRoundedRectGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient, 0);
                return;
        }
    }

    private static void FillEllipse(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;

        for (var py = y0; py < y1; py++) {
            var dy = (py + 0.5 - cy) / ry;
            for (var px = x0; px < x1; px++) {
                var dx = (px + 0.5 - cx) / rx;
                if (dx * dx + dy * dy > 1.0) continue;
                var p = py * (stride + 1) + 1 + px * 4;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillDiamond(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;

        for (var py = y0; py < y1; py++) {
            var dy = Math.Abs(py + 0.5 - cy) / ry;
            for (var px = x0; px < x1; px++) {
                var dx = Math.Abs(px + 0.5 - cx) / rx;
                if (dx + dy > 1.0) continue;
                var p = py * (stride + 1) + 1 + px * 4;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillEllipseGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;

        for (var py = y0; py < y1; py++) {
            var dy = (py + 0.5 - cy) / ry;
            for (var px = x0; px < x1; px++) {
                var dx = (px + 0.5 - cx) / rx;
                if (dx * dx + dy * dy > 1.0) continue;
                var color = GetGradientColorInBox(gradient, px, py, x0, y0, x1 - x0, y1 - y0);
                var p = py * (stride + 1) + 1 + px * 4;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillDot(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        var size = Math.Min(w, h);
        var dotSize = (int)Math.Round(size * QrPngShapeDefaults.DotScale);
        if (dotSize <= 0) return;
        var insetX = (w - dotSize) / 2;
        var insetY = (h - dotSize) / 2;
        FillEllipse(scanlines, widthPx, heightPx, stride, x + insetX, y + insetY, dotSize, dotSize, color);
    }

    private static void FillDotGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        var size = Math.Min(w, h);
        var dotSize = (int)Math.Round(size * QrPngShapeDefaults.DotScale);
        if (dotSize <= 0) return;
        var insetX = (w - dotSize) / 2;
        var insetY = (h - dotSize) / 2;
        FillEllipseGradient(scanlines, widthPx, heightPx, stride, x + insetX, y + insetY, dotSize, dotSize, gradient);
    }

    private static void FillDiamondGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;

        for (var py = y0; py < y1; py++) {
            var dy = Math.Abs(py + 0.5 - cy) / ry;
            for (var px = x0; px < x1; px++) {
                var dx = Math.Abs(px + 0.5 - cx) / rx;
                if (dx + dy > 1.0) continue;
                var color = GetGradientColorInBox(gradient, px, py, x0, y0, x1 - x0, y1 - y0);
                var p = py * (stride + 1) + 1 + px * 4;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillSquircle(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;

        for (var py = y0; py < y1; py++) {
            var dy = Math.Abs(py + 0.5 - cy) / ry;
            var dy2 = dy * dy;
            for (var px = x0; px < x1; px++) {
                var dx = Math.Abs(px + 0.5 - cx) / rx;
                var dx2 = dx * dx;
                if (dx2 * dx2 + dy2 * dy2 > 1.0) continue;
                var p = py * (stride + 1) + 1 + px * 4;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillSquircleGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;

        for (var py = y0; py < y1; py++) {
            var dy = Math.Abs(py + 0.5 - cy) / ry;
            var dy2 = dy * dy;
            for (var px = x0; px < x1; px++) {
                var dx = Math.Abs(px + 0.5 - cx) / rx;
                var dx2 = dx * dx;
                if (dx2 * dx2 + dy2 * dy2 > 1.0) continue;
                var color = GetGradientColorInBox(gradient, px, py, x0, y0, x1 - x0, y1 - y0);
                var p = py * (stride + 1) + 1 + px * 4;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillDotGrid(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        var size = Math.Min(w, h);
        var gridSize = size * QrPngShapeDefaults.DotGridScale;
        if (gridSize <= 0) return;
        var insetX = (w - gridSize) / 2.0;
        var insetY = (h - gridSize) / 2.0;
        var baseX = x + insetX;
        var baseY = y + insetY;

        var radius = Math.Max(QrPngShapeDefaults.DotGridMinRadius, gridSize * QrPngShapeDefaults.DotGridRadiusFactor);
        var c0 = QrPngShapeDefaults.DotGridCenterFactor;
        var c1 = 1.0 - QrPngShapeDefaults.DotGridCenterFactor;

        FillEllipse(scanlines, widthPx, heightPx, stride, (int)Math.Round(baseX + gridSize * c0 - radius), (int)Math.Round(baseY + gridSize * c0 - radius), (int)Math.Round(radius * 2), (int)Math.Round(radius * 2), color);
        FillEllipse(scanlines, widthPx, heightPx, stride, (int)Math.Round(baseX + gridSize * c1 - radius), (int)Math.Round(baseY + gridSize * c0 - radius), (int)Math.Round(radius * 2), (int)Math.Round(radius * 2), color);
        FillEllipse(scanlines, widthPx, heightPx, stride, (int)Math.Round(baseX + gridSize * c0 - radius), (int)Math.Round(baseY + gridSize * c1 - radius), (int)Math.Round(radius * 2), (int)Math.Round(radius * 2), color);
        FillEllipse(scanlines, widthPx, heightPx, stride, (int)Math.Round(baseX + gridSize * c1 - radius), (int)Math.Round(baseY + gridSize * c1 - radius), (int)Math.Round(radius * 2), (int)Math.Round(radius * 2), color);
    }

    private static void FillDotGridGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        if (w <= 0 || h <= 0) return;
        var size = Math.Min(w, h);
        var gridSize = size * QrPngShapeDefaults.DotGridScale;
        if (gridSize <= 0) return;
        var insetX = (w - gridSize) / 2.0;
        var insetY = (h - gridSize) / 2.0;
        var baseX = x + insetX;
        var baseY = y + insetY;

        var radius = Math.Max(QrPngShapeDefaults.DotGridMinRadius, gridSize * QrPngShapeDefaults.DotGridRadiusFactor);
        var r2 = radius * radius;
        var c0 = gridSize * QrPngShapeDefaults.DotGridCenterFactor;
        var c1 = gridSize * (1.0 - QrPngShapeDefaults.DotGridCenterFactor);

        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);

        for (var py = y0; py < y1; py++) {
            var localY = py - baseY;
            for (var px = x0; px < x1; px++) {
                var localX = px - baseX;
                var inside = InsideCircleLocal(localX, localY, c0, r2) ||
                             InsideCircleLocal(localX, localY, c1, r2) ||
                             InsideCircleLocal(localX, localY, c0, r2, c1) ||
                             InsideCircleLocal(localX, localY, c1, r2, c0);
                if (!inside) continue;
                var color = GetGradientColorInBox(gradient, px, py, x0, y0, x1 - x0, y1 - y0);
                var p = py * (stride + 1) + 1 + px * 4;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static bool InsideCircleLocal(double x, double y, double center, double radiusSq) {
        var dx = x - center;
        var dy = y - center;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static bool InsideCircleLocal(double x, double y, double cx, double radiusSq, double cy) {
        var dx = x - cx;
        var dy = y - cy;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static int ScaleSize(int size, double scale) {
        if (scale <= 0) return 0;
        var scaled = (int)Math.Round(size * scale);
        if (scaled < 1) scaled = 1;
        return scaled;
    }

    private static void ApplyLogo(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int qrSizePx,
        QrPngLogoOptions logo) {
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

        var x0 = (widthPx - targetW) / 2;
        var y0 = (heightPx - targetH) / 2;

        if (logo.DrawBackground) {
            var pad = Math.Max(0, logo.PaddingPx);
            var bgX = x0 - pad;
            var bgY = y0 - pad;
            var bgW = targetW + pad * 2;
            var bgH = targetH + pad * 2;
            FillRoundedRect(scanlines, widthPx, heightPx, stride, bgX, bgY, bgW, bgH, logo.Background, logo.CornerRadiusPx);
        }

        BlitLogo(scanlines, widthPx, heightPx, stride, x0, y0, targetW, targetH, logo);
    }

    private static void FillRoundedRect(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color,
        int radius) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var r = Math.Max(0, radius);
        var maxR = Math.Min((x1 - x0) / 2, (y1 - y0) / 2);
        if (r > maxR) r = maxR;
        var r2 = r * r;

        for (var py = y0; py < y1; py++) {
            for (var px = x0; px < x1; px++) {
                if (r > 0 && !InsideRounded(px, py, x0, y0, x1 - 1, y1 - 1, r, r2)) {
                    continue;
                }
                var p = py * (stride + 1) + 1 + px * 4;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillRoundedRectGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient,
        int radius) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var r = Math.Max(0, radius);
        var maxR = Math.Min((x1 - x0) / 2, (y1 - y0) / 2);
        if (r > maxR) r = maxR;
        var r2 = r * r;

        for (var py = y0; py < y1; py++) {
            for (var px = x0; px < x1; px++) {
                if (r > 0 && !InsideRounded(px, py, x0, y0, x1 - 1, y1 - 1, r, r2)) {
                    continue;
                }
                var color = GetGradientColorInBox(gradient, px, py, x0, y0, x1 - x0, y1 - y0);
                var p = py * (stride + 1) + 1 + px * 4;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static bool InsideRounded(int px, int py, int x0, int y0, int x1, int y1, int r, int r2) {
        if (px >= x0 + r && px <= x1 - r) return true;
        if (py >= y0 + r && py <= y1 - r) return true;

        var cx = px < x0 + r ? x0 + r - 1 : x1 - r + 1;
        var cy = py < y0 + r ? y0 + r - 1 : y1 - r + 1;
        var dx = px - cx;
        var dy = py - cy;
        return dx * dx + dy * dy <= r2;
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
        QrPngLogoOptions logo) {
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
                if (sa == 0) continue;

                var dst = y * (stride + 1) + 1 + x * 4;
                if (sa == 255) {
                    scanlines[dst + 0] = sr;
                    scanlines[dst + 1] = sg;
                    scanlines[dst + 2] = sb;
                    scanlines[dst + 3] = 255;
                    continue;
                }

                var dr = scanlines[dst + 0];
                var dg = scanlines[dst + 1];
                var db = scanlines[dst + 2];
                var da = scanlines[dst + 3];

                var inv = 255 - sa;
                scanlines[dst + 0] = (byte)((sr * sa + dr * inv + 127) / 255);
                scanlines[dst + 1] = (byte)((sg * sa + dg * inv + 127) / 255);
                scanlines[dst + 2] = (byte)((sb * sa + db * inv + 127) / 255);
                scanlines[dst + 3] = (byte)((sa + da * inv + 127) / 255);
            }
        }
    }

}
