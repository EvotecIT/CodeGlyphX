using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    internal static int GetScanlineLength(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
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
        return heightPx * (stride + 1);
    }

    internal static byte[] RenderScanlines(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        return RenderScanlines(modules, opts, out widthPx, out heightPx, out stride, scanlines: null);
    }

    internal static byte[] RenderScanlines(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx, out int stride, byte[]? scanlines) {
        var length = GetScanlineLength(modules, opts, out widthPx, out heightPx, out stride);
        var buffer = scanlines ?? new byte[length];
        if (buffer.Length < length) throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlines));
        PngRenderHelpers.FillBackground(buffer, widthPx, heightPx, stride, opts.Background);

        var size = modules.Width;
        var mask = BuildModuleMask(opts.ModuleSize, opts.ModuleShape, opts.ModuleScale, opts.ModuleCornerRadiusPx);
        var maskSolid = IsSolidMask(mask);
        var eyeOuterMask = opts.Eyes is null
            ? mask
            : BuildModuleMask(opts.ModuleSize, opts.Eyes.OuterShape, opts.Eyes.OuterScale, opts.Eyes.OuterCornerRadiusPx);
        var eyeOuterSolid = eyeOuterMask == mask ? maskSolid : IsSolidMask(eyeOuterMask);
        var eyeInnerMask = opts.Eyes is null
            ? mask
            : BuildModuleMask(opts.ModuleSize, opts.Eyes.InnerShape, opts.Eyes.InnerScale, opts.Eyes.InnerCornerRadiusPx);
        var eyeInnerSolid = eyeInnerMask == mask ? maskSolid : IsSolidMask(eyeInnerMask);
        var qrOrigin = opts.QuietZone * opts.ModuleSize;
        var qrSizePx = size * opts.ModuleSize;
        var useFrame = opts.Eyes is not null && opts.Eyes.UseFrame;
        var background = opts.Background;
        var gradientInfo = opts.ForegroundGradient is null
            ? (GradientInfo?)null
            : new GradientInfo(opts.ForegroundGradient, qrSizePx - 1, qrSizePx - 1);
        var eyeOuterGradient = opts.Eyes?.OuterGradient;
        var eyeInnerGradient = opts.Eyes?.InnerGradient;
        var eyeOuterGradientInfo = eyeOuterGradient is null ? (GradientInfo?)null : new GradientInfo(eyeOuterGradient, 7 * opts.ModuleSize - 1, 7 * opts.ModuleSize - 1);
        var eyeInnerGradientInfo = eyeInnerGradient is null ? (GradientInfo?)null : new GradientInfo(eyeInnerGradient, 3 * opts.ModuleSize - 1, 3 * opts.ModuleSize - 1);

        for (var my = 0; my < size; my++) {
            for (var mx = 0; mx < size; mx++) {
                if (!modules[mx, my]) continue;
                var eyeKind = EyeKind.None;
                var eyeX = 0;
                var eyeY = 0;
                if (opts.Eyes is not null && TryGetEye(mx, my, size, out eyeX, out eyeY, out var kind)) {
                    eyeKind = kind;
                }

                if (useFrame && eyeKind != EyeKind.None) continue;

                var useMask = eyeKind == EyeKind.Outer ? eyeOuterMask : eyeKind == EyeKind.Inner ? eyeInnerMask : mask;
                var useMaskSolid = eyeKind == EyeKind.Outer ? eyeOuterSolid : eyeKind == EyeKind.Inner ? eyeInnerSolid : maskSolid;
                var useColor = eyeKind switch {
                    EyeKind.Outer => opts.Eyes!.OuterColor ?? opts.Foreground,
                    EyeKind.Inner => opts.Eyes!.InnerColor ?? opts.Foreground,
                    _ => opts.Foreground,
                };

                if (!useFrame && eyeKind != EyeKind.None) {
                    var eyeGrad = eyeKind == EyeKind.Outer ? eyeOuterGradientInfo : eyeInnerGradientInfo;
                    if (eyeGrad is not null) {
                        var eyeSizeModules = eyeKind == EyeKind.Outer ? 7 : 3;
                        var boxX = (eyeX + opts.QuietZone) * opts.ModuleSize;
                        var boxY = (eyeY + opts.QuietZone) * opts.ModuleSize;
                        DrawModuleInBox(
                            buffer,
                            stride,
                            opts.ModuleSize,
                            mx,
                            my,
                            opts.QuietZone,
                            eyeGrad.Value,
                            useMask,
                            boxX,
                            boxY);
                        continue;
                    }
                }

                var useGradient = eyeKind == EyeKind.None ? gradientInfo : null;
                if (useGradient is null && useMaskSolid) {
                    var solid = useColor.A == 255 ? useColor : CompositeColor(useColor, background);
                    DrawModuleSolid(buffer, stride, opts.ModuleSize, mx, my, opts.QuietZone, solid);
                    continue;
                }

                DrawModule(
                    buffer,
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

        if (useFrame && opts.Eyes is not null) {
            DrawEyeFrame(buffer, widthPx, heightPx, stride, opts, 0, 0);
            DrawEyeFrame(buffer, widthPx, heightPx, stride, opts, size - 7, 0);
            DrawEyeFrame(buffer, widthPx, heightPx, stride, opts, 0, size - 7);
        }

        if (opts.Logo is not null) {
            ApplyLogo(buffer, widthPx, heightPx, stride, size * opts.ModuleSize, opts.Logo);
        }

        return buffer;
    }

    private static void DrawModuleSolid(byte[] scanlines, int stride, int moduleSize, int mx, int my, int quietZone, Rgba32 color) {
        var x0 = (mx + quietZone) * moduleSize;
        var y0 = (my + quietZone) * moduleSize;
        for (var sy = 0; sy < moduleSize; sy++) {
            var rowStart = (y0 + sy) * (stride + 1) + 1 + x0 * 4;
            PngRenderHelpers.FillRowPixels(scanlines, rowStart, moduleSize, color);
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
        GradientInfo? gradient,
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
                    : GetGradientColor(gradient.Value, x0 + sx, y0 + sy, qrOrigin, qrOrigin);

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

    private static void DrawModuleInBox(
        byte[] scanlines,
        int stride,
        int moduleSize,
        int mx,
        int my,
        int quietZone,
        GradientInfo gradient,
        bool[] mask,
        int boxX,
        int boxY) {
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
                var color = GetGradientColorInBox(gradient, x0 + sx, y0 + sy, boxX, boxY);
                scanlines[rowStart + 0] = color.R;
                scanlines[rowStart + 1] = color.G;
                scanlines[rowStart + 2] = color.B;
                scanlines[rowStart + 3] = color.A;
                rowStart += 4;
            }
        }
    }

    private static bool IsSolidMask(bool[] mask) {
        for (var i = 0; i < mask.Length; i++) {
            if (!mask[i]) return false;
        }
        return true;
    }

    private static Rgba32 CompositeColor(Rgba32 foreground, Rgba32 background) {
        if (foreground.A == 255) return foreground;
        var sa = foreground.A;
        var inv = 255 - sa;
        var r = (byte)((foreground.R * sa + background.R * inv + 127) / 255);
        var g = (byte)((foreground.G * sa + background.G * inv + 127) / 255);
        var b = (byte)((foreground.B * sa + background.B * inv + 127) / 255);
        var a = (byte)((sa + background.A * inv + 127) / 255);
        return new Rgba32(r, g, b, a);
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
        if (shape == QrPngModuleShape.Dot) scale *= QrPngShapeDefaults.DotScale;
        if (shape == QrPngModuleShape.DotGrid) scale *= QrPngShapeDefaults.DotGridScale;

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

        var dotGridCenter0 = 0.0;
        var dotGridCenter1 = 0.0;
        var dotGridRadius = 0.0;
        var dotGridRadiusSq = 0.0;
        if (shape == QrPngModuleShape.DotGrid) {
            dotGridCenter0 = (inner - 1) * QrPngShapeDefaults.DotGridCenterFactor;
            dotGridCenter1 = (inner - 1) * (1.0 - QrPngShapeDefaults.DotGridCenterFactor);
            dotGridRadius = Math.Max(QrPngShapeDefaults.DotGridMinRadius, inner * QrPngShapeDefaults.DotGridRadiusFactor);
            dotGridRadiusSq = dotGridRadius * dotGridRadius;
        }

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
                    QrPngModuleShape.Diamond => InsideDiamond(lx, ly, center, circleR),
                    QrPngModuleShape.Squircle => InsideSquircle(lx, ly, center, circleR),
                    QrPngModuleShape.Dot => InsideCircle(lx, ly, center, circleR2),
                    QrPngModuleShape.DotGrid => InsideDotGrid(lx, ly, dotGridCenter0, dotGridCenter1, dotGridRadiusSq),
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

    private static bool InsideDiamond(int x, int y, double center, double radius) {
        var dx = Math.Abs(x - center);
        var dy = Math.Abs(y - center);
        return dx + dy <= radius;
    }

    private static bool InsideSquircle(int x, int y, double center, double radius) {
        if (radius <= 0) return false;
        var dx = Math.Abs(x - center) / radius;
        var dy = Math.Abs(y - center) / radius;
        var dx2 = dx * dx;
        var dy2 = dy * dy;
        return dx2 * dx2 + dy2 * dy2 <= 1.0;
    }

    private static bool InsideDotGrid(int x, int y, double c0, double c1, double radiusSq) {
        return InsideCircle(x, y, c0, radiusSq) ||
               InsideCircle(x, y, c1, radiusSq) ||
               InsideCircle(x, y, c0, radiusSq, c1) ||
               InsideCircle(x, y, c1, radiusSq, c0);
    }

    private static bool InsideCircle(int x, int y, double cx, double radiusSq, double cy) {
        var dx = x - cx;
        var dy = y - cy;
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

    private static Rgba32 GetGradientColor(GradientInfo gradient, int px, int py, int originX, int originY) {
        var u = (px - originX) * gradient.InvSizeX;
        var v = (py - originY) * gradient.InvSizeY;
        if (u < 0) u = 0;
        if (u > 1) u = 1;
        if (v < 0) v = 0;
        if (v > 1) v = 1;

        double t = gradient.Type switch {
            QrPngGradientType.Horizontal => u,
            QrPngGradientType.Vertical => v,
            QrPngGradientType.DiagonalDown => (u + v) * 0.5,
            QrPngGradientType.DiagonalUp => (u + (1 - v)) * 0.5,
            QrPngGradientType.Radial => GetRadialT(u, v, gradient.CenterX, gradient.CenterY, gradient.MaxDist),
            _ => u,
        };

        return Lerp(gradient, t);
    }

    private static Rgba32 GetGradientColorInBox(GradientInfo gradient, int px, int py, int x, int y) {
        var u = (px - x) * gradient.InvSizeX;
        var v = (py - y) * gradient.InvSizeY;
        if (u < 0) u = 0;
        if (u > 1) u = 1;
        if (v < 0) v = 0;
        if (v > 1) v = 1;

        double t = gradient.Type switch {
            QrPngGradientType.Horizontal => u,
            QrPngGradientType.Vertical => v,
            QrPngGradientType.DiagonalDown => (u + v) * 0.5,
            QrPngGradientType.DiagonalUp => (u + (1 - v)) * 0.5,
            QrPngGradientType.Radial => GetRadialT(u, v, gradient.CenterX, gradient.CenterY, gradient.MaxDist),
            _ => u,
        };

        return Lerp(gradient, t);
    }

    private static double GetRadialT(double u, double v, double cx, double cy, double maxDist) {
        var dx = u - cx;
        var dy = v - cy;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (maxDist <= 0) return 0;
        var t = dist / maxDist;
        if (t < 0) return 0;
        if (t > 1) return 1;
        return t;
    }

    private static Rgba32 Lerp(GradientInfo gradient, double t) {
        if (t <= 0) return gradient.StartColor;
        if (t >= 1) return gradient.EndColor;
        var r = (byte)Math.Round(gradient.StartColor.R + gradient.Dr * t);
        var g = (byte)Math.Round(gradient.StartColor.G + gradient.Dg * t);
        var b = (byte)Math.Round(gradient.StartColor.B + gradient.Db * t);
        var a = (byte)Math.Round(gradient.StartColor.A + gradient.Da * t);
        return new Rgba32(r, g, b, a);
    }

    private readonly struct GradientInfo {
        public QrPngGradientType Type { get; }
        public Rgba32 StartColor { get; }
        public Rgba32 EndColor { get; }
        public double CenterX { get; }
        public double CenterY { get; }
        public double InvSizeX { get; }
        public double InvSizeY { get; }
        public double MaxDist { get; }
        public int Dr { get; }
        public int Dg { get; }
        public int Db { get; }
        public int Da { get; }

        public GradientInfo(QrPngGradientOptions gradient, int sizeX, int sizeY) {
            Type = gradient.Type;
            StartColor = gradient.StartColor;
            EndColor = gradient.EndColor;
            CenterX = gradient.CenterX;
            CenterY = gradient.CenterY;
            var sx = Math.Max(1, sizeX);
            var sy = Math.Max(1, sizeY);
            InvSizeX = 1.0 / sx;
            InvSizeY = 1.0 / sy;
            Dr = EndColor.R - StartColor.R;
            Dg = EndColor.G - StartColor.G;
            Db = EndColor.B - StartColor.B;
            Da = EndColor.A - StartColor.A;
            MaxDist = Type == QrPngGradientType.Radial ? ComputeMaxDist(CenterX, CenterY) : 0.0;
        }
    }

    private static double ComputeMaxDist(double cx, double cy) {
        var maxDist = 0.0;
        maxDist = Math.Max(maxDist, Distance(0, 0, cx, cy));
        maxDist = Math.Max(maxDist, Distance(1, 0, cx, cy));
        maxDist = Math.Max(maxDist, Distance(0, 1, cx, cy));
        maxDist = Math.Max(maxDist, Distance(1, 1, cx, cy));
        return maxDist;
    }

    private static double Distance(double x, double y, double cx, double cy) {
        var dx = x - cx;
        var dy = y - cy;
        return Math.Sqrt(dx * dx + dy * dy);
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

}
