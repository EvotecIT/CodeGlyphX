using System;
using System.Globalization;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pdf;

/// <summary>
/// Renders QR modules to a PDF image.
/// </summary>
public static class QrPdfRenderer {
    /// <summary>
    /// Renders the QR module matrix to a PDF byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster) {
            var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
            return PdfWriter.WriteRgba32(widthPx, heightPx, pixels, stride, opts.Background);
        }
        var content = BuildContent(modules, opts, out var widthPx2, out var heightPx2);
        return PdfVectorWriter.Write(widthPx2, heightPx2, content);
    }

    /// <summary>
    /// Renders the QR module matrix to a PDF stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster) {
            var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
            PdfWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride, opts.Background);
            return;
        }
        var content = BuildContent(modules, opts, out var widthPx2, out var heightPx2);
        PdfVectorWriter.Write(stream, widthPx2, heightPx2, content);
    }

    /// <summary>
    /// Renders the QR module matrix to a PDF file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path, RenderMode mode = RenderMode.Vector) {
        var pdf = Render(modules, opts, mode);
        return RenderIO.WriteBinary(path, pdf);
    }

    /// <summary>
    /// Renders the QR module matrix to a PDF file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName, RenderMode mode = RenderMode.Vector) {
        var pdf = Render(modules, opts, mode);
        return RenderIO.WriteBinary(directory, fileName, pdf);
    }

    private static string BuildContent(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var size = modules.Width;
        var outModules = size + opts.QuietZone * 2;
        widthPx = outModules * opts.ModuleSize;
        heightPx = widthPx;

        var bg = Flatten(opts.Background, Rgba32.White);
        var fg = Flatten(opts.Foreground, bg);

        var sb = new StringBuilder(outModules * outModules * 4);
        AppendColor(sb, bg);
        AppendRectTopLeft(sb, 0, 0, widthPx, heightPx, heightPx);
        AppendColor(sb, fg);

        for (var my = 0; my < size; my++) {
            for (var mx = 0; mx < size; mx++) {
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
