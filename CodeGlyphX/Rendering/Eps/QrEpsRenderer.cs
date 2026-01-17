using System;
using System.Globalization;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Eps;

/// <summary>
/// Renders QR modules to an EPS image.
/// </summary>
public static class QrEpsRenderer {
    /// <summary>
    /// Renders the QR module matrix to an EPS string.
    /// </summary>
    public static string Render(BitMatrix modules, QrPngRenderOptions opts, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster || ShouldRaster(opts)) {
            var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
            return Encoding.ASCII.GetString(EpsWriter.WriteRgba32(widthPx, heightPx, pixels, stride, opts.Background));
        }
        return BuildEps(modules, opts);
    }

    /// <summary>
    /// Renders the QR module matrix to an EPS stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster || ShouldRaster(opts)) {
            var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
            EpsWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride, opts.Background);
            return;
        }
        var eps = BuildEps(modules, opts);
        RenderIO.WriteText(stream, eps, Encoding.ASCII);
    }

    /// <summary>
    /// Renders the QR module matrix to an EPS file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path, RenderMode mode = RenderMode.Vector) {
        var eps = Render(modules, opts, mode);
        return RenderIO.WriteText(path, eps, Encoding.ASCII);
    }

    /// <summary>
    /// Renders the QR module matrix to an EPS file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName, RenderMode mode = RenderMode.Vector) {
        var eps = Render(modules, opts, mode);
        return RenderIO.WriteText(directory, fileName, eps, Encoding.ASCII);
    }

    private static string BuildEps(BitMatrix modules, QrPngRenderOptions opts) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.ModuleScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleScale));
        opts.Eyes?.Validate();

        var size = modules.Width;
        var outModules = size + opts.QuietZone * 2;
        var widthPx = outModules * opts.ModuleSize;
        var heightPx = widthPx;

        var bg = Flatten(opts.Background, Rgba32.White);
        var fg = Flatten(opts.Foreground, bg);
        var current = default(Rgba32);
        var hasCurrent = false;

        var sb = new StringBuilder(outModules * outModules * 3);
        sb.AppendLine("%!PS-Adobe-3.0 EPSF-3.0");
        sb.AppendLine($"%%BoundingBox: 0 0 {widthPx} {heightPx}");
        sb.AppendLine("%%LanguageLevel: 2");
        sb.AppendLine("%%Pages: 1");
        sb.AppendLine("%%EndComments");
        sb.AppendLine("gsave");
        AppendColor(sb, bg, ref current, ref hasCurrent);
        AppendRectTopLeft(sb, 0, 0, widthPx, heightPx, heightPx);
        AppendColor(sb, fg, ref current, ref hasCurrent);

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

                AppendColor(sb, color, ref current, ref hasCurrent);
                DrawModule(sb, mx, my, opts.ModuleSize, opts.QuietZone, heightPx, shape, scale, radius);
            }
        }

        if (useFrame) {
            DrawEyeFrame(sb, opts, 0, 0, size, heightPx, bg, ref current, ref hasCurrent);
            DrawEyeFrame(sb, opts, size - 7, 0, size, heightPx, bg, ref current, ref hasCurrent);
            DrawEyeFrame(sb, opts, 0, size - 7, size, heightPx, bg, ref current, ref hasCurrent);
        }

        sb.AppendLine("grestore");
        sb.AppendLine("showpage");
        sb.AppendLine("%%EOF");
        return sb.ToString();
    }

    private static void AppendColor(StringBuilder sb, Rgba32 color, ref Rgba32 current, ref bool hasCurrent) {
        if (hasCurrent && current.R == color.R && current.G == color.G && current.B == color.B) return;
        current = color;
        hasCurrent = true;
        sb.Append(ToComponent(color.R)).Append(' ')
            .Append(ToComponent(color.G)).Append(' ')
            .Append(ToComponent(color.B)).AppendLine(" setrgbcolor");
    }

    private static string ToComponent(byte value) {
        var scaled = value / 255.0;
        return scaled.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static void AppendRectTopLeft(StringBuilder sb, int x, int yTop, int width, int height, int totalHeight) {
        var y = totalHeight - yTop - height;
        sb.Append(x).Append(' ')
            .Append(y).Append(' ')
            .Append(width).Append(' ')
            .Append(height).AppendLine(" rectfill");
    }

    private static void AppendRoundedRectTopLeft(StringBuilder sb, int x, int yTop, int width, int height, int radius, int totalHeight) {
        if (radius <= 0) {
            AppendRectTopLeft(sb, x, yTop, width, height, totalHeight);
            return;
        }
        var r = Math.Min(radius, Math.Min(width, height) / 2);
        if (r <= 0) {
            AppendRectTopLeft(sb, x, yTop, width, height, totalHeight);
            return;
        }

        var y = totalHeight - yTop - height;
        var x0 = x;
        var y0 = y;
        var x1 = x + width;
        var y1 = y + height;
        var c = r * 0.5522847498307936;

        sb.Append(FormatNumber(x0 + r)).Append(' ').Append(FormatNumber(y0)).AppendLine(" moveto");
        sb.Append(FormatNumber(x1 - r)).Append(' ').Append(FormatNumber(y0)).AppendLine(" lineto");
        sb.Append(FormatNumber(x1 - r + c)).Append(' ').Append(FormatNumber(y0)).Append(' ')
            .Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y0 + r - c)).Append(' ')
            .Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y0 + r)).AppendLine(" curveto");
        sb.Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y1 - r)).AppendLine(" lineto");
        sb.Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y1 - r + c)).Append(' ')
            .Append(FormatNumber(x1 - r + c)).Append(' ').Append(FormatNumber(y1)).Append(' ')
            .Append(FormatNumber(x1 - r)).Append(' ').Append(FormatNumber(y1)).AppendLine(" curveto");
        sb.Append(FormatNumber(x0 + r)).Append(' ').Append(FormatNumber(y1)).AppendLine(" lineto");
        sb.Append(FormatNumber(x0 + r - c)).Append(' ').Append(FormatNumber(y1)).Append(' ')
            .Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y1 - r + c)).Append(' ')
            .Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y1 - r)).AppendLine(" curveto");
        sb.Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y0 + r)).AppendLine(" lineto");
        sb.Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y0 + r - c)).Append(' ')
            .Append(FormatNumber(x0 + r - c)).Append(' ').Append(FormatNumber(y0)).Append(' ')
            .Append(FormatNumber(x0 + r)).Append(' ').Append(FormatNumber(y0)).AppendLine(" curveto");
        sb.AppendLine("closepath fill");
    }

    private static void DrawModule(StringBuilder sb, int mx, int my, int moduleSize, int quietZone, int totalHeight, QrPngModuleShape shape, double scale, int radius) {
        var scaled = Math.Max(1, (int)Math.Round(moduleSize * scale));
        var offset = (moduleSize - scaled) / 2;
        var x = (mx + quietZone) * moduleSize + offset;
        var y = (my + quietZone) * moduleSize + offset;
        switch (shape) {
            case QrPngModuleShape.Circle:
                AppendRoundedRectTopLeft(sb, x, y, scaled, scaled, scaled / 2, totalHeight);
                break;
            case QrPngModuleShape.Rounded:
                AppendRoundedRectTopLeft(sb, x, y, scaled, scaled, radius, totalHeight);
                break;
            default:
                AppendRectTopLeft(sb, x, y, scaled, scaled, totalHeight);
                break;
        }
    }

    private static void DrawEyeFrame(
        StringBuilder sb,
        QrPngRenderOptions opts,
        int ex,
        int ey,
        int size,
        int totalHeight,
        Rgba32 background,
        ref Rgba32 current,
        ref bool hasCurrent) {
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

        AppendColor(sb, outerColor, ref current, ref hasCurrent);
        DrawShape(sb, outerX, outerY, outerScaled, outerScaled, eye.OuterShape, eye.OuterCornerRadiusPx, totalHeight);
        AppendColor(sb, background, ref current, ref hasCurrent);
        DrawShape(sb, innerX, innerY, innerScaled, innerScaled, QrPngModuleShape.Square, eye.InnerCornerRadiusPx, totalHeight);

        if (dotScaled > 0) {
            AppendColor(sb, innerColor, ref current, ref hasCurrent);
            DrawShape(sb, dotX, dotY, dotScaled, dotScaled, eye.InnerShape, eye.InnerCornerRadiusPx, totalHeight);
        }
    }

    private static void DrawShape(StringBuilder sb, int x, int y, int width, int height, QrPngModuleShape shape, int radius, int totalHeight) {
        switch (shape) {
            case QrPngModuleShape.Circle:
                AppendRoundedRectTopLeft(sb, x, y, width, height, Math.Min(width, height) / 2, totalHeight);
                break;
            case QrPngModuleShape.Rounded:
                AppendRoundedRectTopLeft(sb, x, y, width, height, radius, totalHeight);
                break;
            default:
                AppendRectTopLeft(sb, x, y, width, height, totalHeight);
                break;
        }
    }

    private static int ScaleSize(int size, double scale) {
        if (scale < 0.1) scale = 0.1;
        if (scale > 1.0) scale = 1.0;
        return Math.Max(1, (int)Math.Round(size * scale));
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

    private static bool ShouldRaster(QrPngRenderOptions opts) {
        if (opts.ForegroundGradient is not null) return true;
        if (opts.Logo is not null) return true;
        if (opts.Eyes is not null && (opts.Eyes.OuterGradient is not null || opts.Eyes.InnerGradient is not null)) return true;
        return false;
    }

    private static string FormatNumber(double value) {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private enum EyeKind {
        None,
        Outer,
        Inner
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
}
