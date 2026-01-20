using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Vector;

internal interface IQrVectorSink {
    void SetFillColor(Rgba32 color);
    void FillRect(int x, int yTop, int width, int height);
    void FillRoundedRect(int x, int yTop, int width, int height, int radius);
}

internal static class QrVectorLayout {
    internal static void GetSize(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.ModuleScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleScale));

        var size = modules.Width;
        var outModules = size + opts.QuietZone * 2;
        widthPx = outModules * opts.ModuleSize;
        heightPx = widthPx;
    }

    internal static void Render(BitMatrix modules, QrPngRenderOptions opts, IQrVectorSink sink, out int widthPx, out int heightPx) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (sink is null) throw new ArgumentNullException(nameof(sink));
        opts.Eyes?.Validate();

        GetSize(modules, opts, out widthPx, out heightPx);
        var size = modules.Width;

        var bg = Flatten(opts.Background, Rgba32.White);
        var fg = Flatten(opts.Foreground, bg);

        sink.SetFillColor(bg);
        sink.FillRect(0, 0, widthPx, heightPx);
        sink.SetFillColor(fg);

        var moduleShape = opts.ModuleShape;
        var moduleScale = opts.ModuleScale;
        var moduleRadius = opts.ModuleCornerRadiusPx;
        var eye = opts.Eyes;
        var useFrame = eye is not null && eye.UseFrame;

        for (var my = 0; my < size; my++) {
            for (var mx = 0; mx < size; mx++) {
                if (!modules[mx, my]) continue;
                if (useFrame && IsInEye(mx, my, size)) continue;

                var shape = moduleShape;
                var scale = moduleScale;
                var radius = moduleRadius;
                var color = fg;

                if (eye is not null && TryGetEyeKind(mx, my, size, out var kind)) {
                    if (kind == EyeKind.Outer) {
                        shape = eye.OuterShape;
                        scale = eye.OuterScale;
                        radius = eye.OuterCornerRadiusPx;
                        color = Flatten(eye.OuterColor ?? opts.Foreground, bg);
                    } else if (kind == EyeKind.Inner) {
                        shape = eye.InnerShape;
                        scale = eye.InnerScale;
                        radius = eye.InnerCornerRadiusPx;
                        color = Flatten(eye.InnerColor ?? opts.Foreground, bg);
                    }
                }

                sink.SetFillColor(color);
                DrawModule(sink, mx, my, opts.ModuleSize, opts.QuietZone, shape, scale, radius);
            }
        }

        if (useFrame) {
            DrawEyeFrame(sink, opts, 0, 0, size, bg);
            DrawEyeFrame(sink, opts, size - 7, 0, size, bg);
            DrawEyeFrame(sink, opts, 0, size - 7, size, bg);
        }
    }

    internal static bool ShouldRaster(QrPngRenderOptions opts) {
        if (opts.ForegroundGradient is not null) return true;
        if (opts.Logo is not null) return true;
        if (opts.Eyes is not null && (opts.Eyes.OuterGradient is not null || opts.Eyes.InnerGradient is not null)) return true;
        if (!IsVectorShapeSupported(opts.ModuleShape)) return true;
        if (opts.Eyes is not null && (!IsVectorShapeSupported(opts.Eyes.OuterShape) || !IsVectorShapeSupported(opts.Eyes.InnerShape))) return true;
        return false;
    }

    private static void DrawModule(IQrVectorSink sink, int mx, int my, int moduleSize, int quietZone, QrPngModuleShape shape, double scale, int radius) {
        var scaled = Math.Max(1, (int)Math.Round(moduleSize * scale));
        var offset = (moduleSize - scaled) / 2;
        var x = (mx + quietZone) * moduleSize + offset;
        var y = (my + quietZone) * moduleSize + offset;

        if (shape == QrPngModuleShape.Circle) {
            sink.FillRoundedRect(x, y, scaled, scaled, scaled / 2);
        } else if (shape == QrPngModuleShape.Rounded) {
            sink.FillRoundedRect(x, y, scaled, scaled, radius);
        } else {
            sink.FillRect(x, y, scaled, scaled);
        }
    }

    private static void DrawEyeFrame(IQrVectorSink sink, QrPngRenderOptions opts, int ex, int ey, int size, Rgba32 background) {
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

        var outerColor = Flatten(eye.OuterColor ?? opts.Foreground, background);
        var innerColor = Flatten(eye.InnerColor ?? opts.Foreground, background);

        sink.SetFillColor(outerColor);
        DrawShape(sink, outerX, outerY, outerScaled, outerScaled, eye.OuterShape, eye.OuterCornerRadiusPx);
        sink.SetFillColor(background);
        DrawShape(sink, innerX, innerY, innerScaled, innerScaled, QrPngModuleShape.Rounded, eye.InnerCornerRadiusPx);

        if (dotScaled > 0) {
            sink.SetFillColor(innerColor);
            DrawShape(sink, dotX, dotY, dotScaled, dotScaled, eye.InnerShape, eye.InnerCornerRadiusPx);
        }
    }

    private static void DrawShape(IQrVectorSink sink, int x, int y, int width, int height, QrPngModuleShape shape, int radius) {
        if (shape == QrPngModuleShape.Circle) {
            sink.FillRoundedRect(x, y, width, height, Math.Min(width, height) / 2);
        } else if (shape == QrPngModuleShape.Rounded) {
            sink.FillRoundedRect(x, y, width, height, radius);
        } else {
            sink.FillRect(x, y, width, height);
        }
    }

    private static int ScaleSize(int size, double scale) {
        if (scale < 0.1) scale = 0.1;
        if (scale > 1.0) scale = 1.0;
        return Math.Max(1, (int)Math.Round(size * scale));
    }

    private static bool IsVectorShapeSupported(QrPngModuleShape shape) {
        return shape == QrPngModuleShape.Square ||
               shape == QrPngModuleShape.Circle ||
               shape == QrPngModuleShape.Rounded;
    }

    private static bool IsInEye(int x, int y, int size) {
        return (x < 7 && y < 7) || (x >= size - 7 && y < 7) || (x < 7 && y >= size - 7);
    }

    private static bool TryGetEyeKind(int x, int y, int size, out EyeKind kind) {
        if (x < 7 && y < 7) {
            kind = GetEyeModuleKind(x, y, 0, 0);
            return true;
        }
        if (x >= size - 7 && y < 7) {
            kind = GetEyeModuleKind(x, y, size - 7, 0);
            return true;
        }
        if (x < 7 && y >= size - 7) {
            kind = GetEyeModuleKind(x, y, 0, size - 7);
            return true;
        }
        kind = EyeKind.None;
        return false;
    }

    private static EyeKind GetEyeModuleKind(int x, int y, int ex, int ey) {
        var lx = x - ex;
        var ly = y - ey;
        if (lx >= 2 && lx <= 4 && ly >= 2 && ly <= 4) return EyeKind.Inner;
        return EyeKind.Outer;
    }

    private static Rgba32 Flatten(Rgba32 color, Rgba32 background) {
        if (color.A == 255) return color;
        var inv = 255 - color.A;
        return new Rgba32(
            (byte)((color.R * color.A + background.R * inv + 127) / 255),
            (byte)((color.G * color.A + background.G * inv + 127) / 255),
            (byte)((color.B * color.A + background.B * inv + 127) / 255),
            255);
    }

    private enum EyeKind {
        None,
        Outer,
        Inner
    }
}
