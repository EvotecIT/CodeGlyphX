using System;
using System.Globalization;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Eps;

/// <summary>
/// Renders generic 2D matrices to an EPS image.
/// </summary>
public static class MatrixEpsRenderer {
    /// <summary>
    /// Renders the matrix to an EPS string.
    /// </summary>
    public static string Render(BitMatrix modules, MatrixPngRenderOptions opts, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster) {
            var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
            return Encoding.ASCII.GetString(EpsWriter.WriteRgba32(widthPx, heightPx, pixels, stride, opts.Background));
        }
        return BuildEps(modules, opts);
    }

    /// <summary>
    /// Renders the matrix to an EPS stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster) {
            var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
            EpsWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride, opts.Background);
            return;
        }
        var eps = BuildEps(modules, opts);
        RenderIO.WriteText(stream, eps, Encoding.ASCII);
    }

    /// <summary>
    /// Renders the matrix to an EPS file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path, RenderMode mode = RenderMode.Vector) {
        var eps = Render(modules, opts, mode);
        return RenderIO.WriteText(path, eps, Encoding.ASCII);
    }

    /// <summary>
    /// Renders the matrix to an EPS file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName, RenderMode mode = RenderMode.Vector) {
        var eps = Render(modules, opts, mode);
        return RenderIO.WriteText(directory, fileName, eps, Encoding.ASCII);
    }

    private static string BuildEps(BitMatrix modules, MatrixPngRenderOptions opts) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var outWidthModules = modules.Width + opts.QuietZone * 2;
        var outHeightModules = modules.Height + opts.QuietZone * 2;
        var widthPx = outWidthModules * opts.ModuleSize;
        var heightPx = outHeightModules * opts.ModuleSize;

        var bg = Flatten(opts.Background, Rgba32.White);
        var fg = Flatten(opts.Foreground, bg);

        var sb = new StringBuilder(outWidthModules * outHeightModules * 3);
        sb.AppendLine("%!PS-Adobe-3.0 EPSF-3.0");
        sb.AppendLine($"%%BoundingBox: 0 0 {widthPx} {heightPx}");
        sb.AppendLine("%%LanguageLevel: 2");
        sb.AppendLine("%%Pages: 1");
        sb.AppendLine("%%EndComments");
        sb.AppendLine("gsave");
        AppendColor(sb, bg);
        AppendRectTopLeft(sb, 0, 0, widthPx, heightPx, heightPx);
        AppendColor(sb, fg);

        for (var my = 0; my < modules.Height; my++) {
            for (var mx = 0; mx < modules.Width; mx++) {
                if (!modules[mx, my]) continue;
                var x = (mx + opts.QuietZone) * opts.ModuleSize;
                var y = (my + opts.QuietZone) * opts.ModuleSize;
                AppendRectTopLeft(sb, x, y, opts.ModuleSize, opts.ModuleSize, heightPx);
            }
        }

        sb.AppendLine("grestore");
        sb.AppendLine("showpage");
        sb.AppendLine("%%EOF");
        return sb.ToString();
    }

    private static void AppendColor(StringBuilder sb, Rgba32 color) {
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
