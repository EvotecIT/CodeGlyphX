using System;

namespace CodeMatrix.Rendering.Png;

/// <summary>
/// Renders QR modules to a PNG image (RGBA8).
/// </summary>
public static class QrPngRenderer {
    /// <summary>
    /// Renders the QR module matrix to a PNG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        var scanlines = RenderScanlines(modules, opts, out var widthPx, out var heightPx, out _);
        return PngWriter.WriteRgba8(widthPx, heightPx, scanlines);
    }

    internal static byte[] RenderScanlines(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.ModuleScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleScale));
        opts.ForegroundGradient?.Validate();
        opts.Eyes?.Validate();

        var size = modules.Width;
        var outModules = size + opts.QuietZone * 2;
        widthPx = outModules * opts.ModuleSize;
        heightPx = widthPx;
        stride = widthPx * 4;

        var scanlines = new byte[heightPx * (stride + 1)];
        FillBackground(scanlines, widthPx, heightPx, stride, opts.Background);

        var mask = BuildModuleMask(opts.ModuleSize, opts.ModuleShape, opts.ModuleScale, opts.ModuleCornerRadiusPx);
        var eyeOuterMask = opts.Eyes is null
            ? mask
            : BuildModuleMask(opts.ModuleSize, opts.Eyes.OuterShape, opts.Eyes.OuterScale, opts.Eyes.OuterCornerRadiusPx);
        var eyeInnerMask = opts.Eyes is null
            ? mask
            : BuildModuleMask(opts.ModuleSize, opts.Eyes.InnerShape, opts.Eyes.InnerScale, opts.Eyes.InnerCornerRadiusPx);
        var qrOrigin = opts.QuietZone * opts.ModuleSize;
        var qrSizePx = size * opts.ModuleSize;

        for (var my = 0; my < size; my++) {
            for (var mx = 0; mx < size; mx++) {
                if (!modules[mx, my]) continue;
                var eye = opts.Eyes is not null ? GetEyeKind(mx, my, size) : EyeKind.None;
                var useMask = eye == EyeKind.Outer ? eyeOuterMask : eye == EyeKind.Inner ? eyeInnerMask : mask;
                var useColor = eye switch {
                    EyeKind.Outer => opts.Eyes!.OuterColor ?? opts.Foreground,
                    EyeKind.Inner => opts.Eyes!.InnerColor ?? opts.Foreground,
                    _ => opts.Foreground,
                };
                var useGradient = eye == EyeKind.None ? opts.ForegroundGradient : null;

                DrawModule(
                    scanlines,
                    stride,
                    opts.ModuleSize,
                    mx,
                    my,
                    opts.QuietZone,
                    useColor,
                    useGradient,
                    useMask,
                    qrOrigin,
                    qrSizePx);
            }
        }

        if (opts.Logo is not null) {
            ApplyLogo(scanlines, widthPx, heightPx, stride, size * opts.ModuleSize, opts.Logo);
        }

        return scanlines;
    }

    private static void FillBackground(byte[] scanlines, int widthPx, int heightPx, int stride, Rgba32 color) {
        for (var y = 0; y < heightPx; y++) {
            var rowStart = y * (stride + 1);
            scanlines[rowStart] = 0;
            var p = rowStart + 1;
            for (var x = 0; x < widthPx; x++) {
                scanlines[p++] = color.R;
                scanlines[p++] = color.G;
                scanlines[p++] = color.B;
                scanlines[p++] = color.A;
            }
        }
    }

    private static void DrawModule(
        byte[] scanlines,
        int stride,
        int moduleSize,
        int mx,
        int my,
        int quietZone,
        Rgba32 color,
        QrPngGradientOptions? gradient,
        bool[] mask,
        int qrOrigin,
        int qrSizePx) {
        var x0 = (mx + quietZone) * moduleSize;
        var y0 = (my + quietZone) * moduleSize;
        for (var sy = 0; sy < moduleSize; sy++) {
            var rowStart = (y0 + sy) * (stride + 1) + 1 + x0 * 4;
            var maskRow = sy * moduleSize;
            for (var sx = 0; sx < moduleSize; sx++) {
                if (!mask[maskRow + sx]) {
                    rowStart += 4;
                    continue;
                }
                var outColor = gradient is null
                    ? color
                    : GetGradientColor(gradient, x0 + sx, y0 + sy, qrOrigin, qrSizePx);

                if (outColor.A == 255) {
                    scanlines[rowStart + 0] = outColor.R;
                    scanlines[rowStart + 1] = outColor.G;
                    scanlines[rowStart + 2] = outColor.B;
                    scanlines[rowStart + 3] = 255;
                } else {
                    var dr = scanlines[rowStart + 0];
                    var dg = scanlines[rowStart + 1];
                    var db = scanlines[rowStart + 2];
                    var da = scanlines[rowStart + 3];
                    var sa = outColor.A;
                    var inv = 255 - sa;
                    scanlines[rowStart + 0] = (byte)((outColor.R * sa + dr * inv + 127) / 255);
                    scanlines[rowStart + 1] = (byte)((outColor.G * sa + dg * inv + 127) / 255);
                    scanlines[rowStart + 2] = (byte)((outColor.B * sa + db * inv + 127) / 255);
                    scanlines[rowStart + 3] = (byte)((sa + da * inv + 127) / 255);
                }
                rowStart += 4;
            }
        }
    }

    private static bool[] BuildModuleMask(
        int moduleSize,
        QrPngModuleShape shape,
        double scale,
        int cornerRadiusPx) {
        var mask = new bool[moduleSize * moduleSize];
        if (moduleSize <= 0) return mask;

        if (scale < 0.1) scale = 0.1;
        if (scale > 1.0) scale = 1.0;

        var inset = (int)Math.Round((moduleSize - moduleSize * scale) / 2.0);
        if (inset < 0) inset = 0;
        if (inset > moduleSize / 2) inset = moduleSize / 2;
        var inner = moduleSize - inset * 2;
        if (inner <= 0) return mask;

        var radius = cornerRadiusPx;
        if (radius <= 0) radius = inner / 4;
        if (radius > inner / 2) radius = inner / 2;
        var r2 = radius * radius;
        var center = (inner - 1) / 2.0;
        var circleR = inner / 2.0;
        var circleR2 = circleR * circleR;

        for (var y = 0; y < moduleSize; y++) {
            for (var x = 0; x < moduleSize; x++) {
                if (x < inset || x >= inset + inner || y < inset || y >= inset + inner) {
                    mask[y * moduleSize + x] = false;
                    continue;
                }
                var lx = x - inset;
                var ly = y - inset;
                var inside = shape switch {
                    QrPngModuleShape.Square => true,
                    QrPngModuleShape.Circle => InsideCircle(lx, ly, center, circleR2),
                    QrPngModuleShape.Rounded => InsideRoundedLocal(lx, ly, inner, radius, r2),
                    _ => true,
                };
                mask[y * moduleSize + x] = inside;
            }
        }

        return mask;
    }

    private static bool InsideCircle(int x, int y, double center, double radiusSq) {
        var dx = x - center;
        var dy = y - center;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static bool InsideRoundedLocal(int x, int y, int size, int radius, int radiusSq) {
        if (radius <= 0) return true;
        if (x >= radius && x < size - radius) return true;
        if (y >= radius && y < size - radius) return true;

        var cx = x < radius ? radius - 1 : size - radius;
        var cy = y < radius ? radius - 1 : size - radius;
        var dx = x - cx;
        var dy = y - cy;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static Rgba32 GetGradientColor(QrPngGradientOptions gradient, int px, int py, int qrOrigin, int qrSizePx) {
        var size = Math.Max(1, qrSizePx - 1);
        var u = (px - qrOrigin) / (double)size;
        var v = (py - qrOrigin) / (double)size;
        if (u < 0) u = 0;
        if (u > 1) u = 1;
        if (v < 0) v = 0;
        if (v > 1) v = 1;

        double t = gradient.Type switch {
            QrPngGradientType.Horizontal => u,
            QrPngGradientType.Vertical => v,
            QrPngGradientType.DiagonalDown => (u + v) * 0.5,
            QrPngGradientType.DiagonalUp => (u + (1 - v)) * 0.5,
            QrPngGradientType.Radial => GetRadialT(u, v, gradient.CenterX, gradient.CenterY),
            _ => u,
        };

        return Lerp(gradient.StartColor, gradient.EndColor, t);
    }

    private static double GetRadialT(double u, double v, double cx, double cy) {
        var dx = u - cx;
        var dy = v - cy;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var maxDist = 0.0;
        maxDist = Math.Max(maxDist, Distance(0, 0, cx, cy));
        maxDist = Math.Max(maxDist, Distance(1, 0, cx, cy));
        maxDist = Math.Max(maxDist, Distance(0, 1, cx, cy));
        maxDist = Math.Max(maxDist, Distance(1, 1, cx, cy));
        if (maxDist <= 0) return 0;
        var t = dist / maxDist;
        if (t < 0) return 0;
        if (t > 1) return 1;
        return t;
    }

    private static double Distance(double x, double y, double cx, double cy) {
        var dx = x - cx;
        var dy = y - cy;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Rgba32 Lerp(Rgba32 a, Rgba32 b, double t) {
        if (t <= 0) return a;
        if (t >= 1) return b;
        var r = (byte)Math.Round(a.R + (b.R - a.R) * t);
        var g = (byte)Math.Round(a.G + (b.G - a.G) * t);
        var bl = (byte)Math.Round(a.B + (b.B - a.B) * t);
        var al = (byte)Math.Round(a.A + (b.A - a.A) * t);
        return new Rgba32(r, g, bl, al);
    }

    private enum EyeKind {
        None,
        Outer,
        Inner,
    }

    private static EyeKind GetEyeKind(int x, int y, int size) {
        if (IsInEye(x, y, 0, 0)) return GetEyeModuleKind(x, y, 0, 0);
        if (IsInEye(x, y, size - 7, 0)) return GetEyeModuleKind(x, y, size - 7, 0);
        if (IsInEye(x, y, 0, size - 7)) return GetEyeModuleKind(x, y, 0, size - 7);
        return EyeKind.None;
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
