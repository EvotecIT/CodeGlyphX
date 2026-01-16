using System;
using System.Globalization;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pdf;

/// <summary>
/// Renders 1D barcodes to a PDF image.
/// </summary>
public static class BarcodePdfRenderer {
    /// <summary>
    /// Renders the barcode to a PDF byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var content = BuildContent(barcode, opts, out var widthPx, out var heightPx);
        return PdfVectorWriter.Write(widthPx, heightPx, content);
    }

    /// <summary>
    /// Renders the barcode to a PDF stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var content = BuildContent(barcode, opts, out var widthPx, out var heightPx);
        PdfVectorWriter.Write(stream, widthPx, heightPx, content);
    }

    /// <summary>
    /// Renders the barcode to a PDF file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var pdf = Render(barcode, opts);
        return RenderIO.WriteBinary(path, pdf);
    }

    /// <summary>
    /// Renders the barcode to a PDF file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var pdf = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, pdf);
    }

    private static string BuildContent(Barcode1D barcode, BarcodePngRenderOptions opts, out int widthPx, out int heightPx) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.HeightModules <= 0) throw new ArgumentOutOfRangeException(nameof(opts.HeightModules));

        var outModules = barcode.TotalModules + opts.QuietZone * 2;
        widthPx = outModules * opts.ModuleSize;
        var barHeightPx = opts.HeightModules * opts.ModuleSize;

        var labelText = BarcodeLabelText.Normalize(opts.LabelText);
        var hasLabel = !string.IsNullOrEmpty(labelText);
        var labelFontPx = Math.Max(1, opts.LabelFontSize);
        var labelMarginPx = Math.Max(0, opts.LabelMargin);
        var labelScale = Math.Max(1, (int)Math.Round(labelFontPx / (double)BarcodeLabelFont.GlyphHeight));
        var labelHeightPx = hasLabel ? labelMarginPx + BarcodeLabelFont.GlyphHeight * labelScale : 0;

        heightPx = barHeightPx + labelHeightPx;

        var bg = Flatten(opts.Background, Rgba32.White);
        var fg = Flatten(opts.Foreground, bg);
        var labelColor = Flatten(opts.LabelColor, bg);

        var sb = new StringBuilder(widthPx * 2);
        AppendColor(sb, bg);
        AppendRectTopLeft(sb, 0, 0, widthPx, heightPx, heightPx);

        AppendColor(sb, fg);
        var xModules = opts.QuietZone;
        for (var i = 0; i < barcode.Segments.Count; i++) {
            var seg = barcode.Segments[i];
            if (seg.IsBar) {
                var x0 = xModules * opts.ModuleSize;
                var w = seg.Modules * opts.ModuleSize;
                AppendRectTopLeft(sb, x0, 0, w, barHeightPx, heightPx);
            }
            xModules += seg.Modules;
        }

        if (hasLabel) {
            AppendColor(sb, labelColor);
            var spacing = labelScale;
            var textWidth = BarcodeLabelFont.MeasureTextWidth(labelText, labelScale, spacing);
            var xStart = (widthPx - textWidth) / 2;
            if (xStart < 0) xStart = 0;
            var yStart = barHeightPx + labelMarginPx;
            DrawLabel(sb, heightPx, xStart, yStart, labelText, labelScale, spacing);
        }

        return sb.ToString();
    }

    private static void DrawLabel(StringBuilder sb, int heightPx, int xStart, int yStart, string text, int scale, int spacing) {
        var x = xStart;
        for (var i = 0; i < text.Length; i++) {
            var glyph = BarcodeLabelFont.GetGlyph(text[i]);
            for (var row = 0; row < BarcodeLabelFont.GlyphHeight; row++) {
                var bits = glyph[row];
                for (var col = 0; col < BarcodeLabelFont.GlyphWidth; col++) {
                    if ((bits & (1 << (BarcodeLabelFont.GlyphWidth - 1 - col))) == 0) continue;
                    var px = x + col * scale;
                    var py = yStart + row * scale;
                    AppendRectTopLeft(sb, px, py, scale, scale, heightPx);
                }
            }
            x += BarcodeLabelFont.GlyphWidth * scale + spacing;
        }
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
