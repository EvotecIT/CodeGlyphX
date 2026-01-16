using System;
using System.Globalization;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pdf;

/// <summary>
/// Renders generic 2D matrices to a PDF image.
/// </summary>
public static class MatrixPdfRenderer {
    /// <summary>
    /// Renders the matrix to a PDF byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        var content = BuildContent(modules, opts, out var widthPx, out var heightPx);
        return PdfVectorWriter.Write(widthPx, heightPx, content);
    }

    /// <summary>
    /// Renders the matrix to a PDF stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        var content = BuildContent(modules, opts, out var widthPx, out var heightPx);
        PdfVectorWriter.Write(stream, widthPx, heightPx, content);
    }

    /// <summary>
    /// Renders the matrix to a PDF file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path) {
        var pdf = Render(modules, opts);
        return RenderIO.WriteBinary(path, pdf);
    }

    /// <summary>
    /// Renders the matrix to a PDF file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName) {
        var pdf = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, pdf);
    }

    private static string BuildContent(BitMatrix modules, MatrixPngRenderOptions opts, out int widthPx, out int heightPx) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var outWidthModules = modules.Width + opts.QuietZone * 2;
        var outHeightModules = modules.Height + opts.QuietZone * 2;
        widthPx = outWidthModules * opts.ModuleSize;
        heightPx = outHeightModules * opts.ModuleSize;

        var bg = Flatten(opts.Background, Rgba32.White);
        var fg = Flatten(opts.Foreground, bg);

        var sb = new StringBuilder(outWidthModules * outHeightModules * 4);
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

        return sb.ToString();
    }

    private static void AppendColor(StringBuilder sb, Rgba32 color) {
        sb.Append(ToComponent(color.R)).Append(' ')
            .Append(ToComponent(color.G)).Append(' ')
            .Append(ToComponent(color.B)).Append(" rg\n");
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
            .Append(height).Append(" re f\n");
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
