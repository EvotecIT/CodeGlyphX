using System;
using System.Globalization;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Vector;

namespace CodeGlyphX.Rendering.Pdf;

/// <summary>
/// Renders QR modules to a PDF image.
/// </summary>
public static class QrPdfRenderer {
    /// <summary>
    /// Renders the QR module matrix to a PDF byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster || QrVectorLayout.ShouldRaster(opts)) {
            var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
            return PdfWriter.WriteRgba32(widthPx, heightPx, pixels, stride, opts.Background);
        }
        var content = BuildVectorContent(modules, opts, out var widthPx2, out var heightPx2);
        return PdfVectorWriter.Write(widthPx2, heightPx2, content);
    }

    /// <summary>
    /// Renders the QR module matrix to a PDF stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster || QrVectorLayout.ShouldRaster(opts)) {
            var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
            PdfWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride, opts.Background);
            return;
        }
        var content = BuildVectorContent(modules, opts, out var widthPx2, out var heightPx2);
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

    private static string BuildVectorContent(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx) {
        QrVectorLayout.GetSize(modules, opts, out widthPx, out heightPx);
        var sb = new StringBuilder();
        var sink = new PdfSink(sb, heightPx);
        QrVectorLayout.Render(modules, opts, sink, out _, out _);
        return sb.ToString();
    }

    private sealed class PdfSink : IQrVectorSink {
        private readonly StringBuilder _sb;
        private readonly int _height;
        private Rgba32 _current;
        private bool _hasCurrent;

        public PdfSink(StringBuilder sb, int height) {
            _sb = sb ?? throw new ArgumentNullException(nameof(sb));
            _height = height;
        }

        public void SetFillColor(Rgba32 color) {
            if (_hasCurrent && _current.R == color.R && _current.G == color.G && _current.B == color.B) return;
            _current = color;
            _hasCurrent = true;
            _sb.Append(ToComponent(color.R)).Append(' ')
                .Append(ToComponent(color.G)).Append(' ')
                .Append(ToComponent(color.B)).Append(" rg\n");
        }

        public void FillRect(int x, int yTop, int width, int height) {
            var y = _height - yTop - height;
            _sb.Append(x).Append(' ')
                .Append(y).Append(' ')
                .Append(width).Append(' ')
                .Append(height).Append(" re f\n");
        }

        public void FillRoundedRect(int x, int yTop, int width, int height, int radius) {
            if (radius <= 0) {
                FillRect(x, yTop, width, height);
                return;
            }
            var r = Math.Min(radius, Math.Min(width, height) / 2);
            if (r <= 0) {
                FillRect(x, yTop, width, height);
                return;
            }

            var y = _height - yTop - height;
            var x0 = x;
            var y0 = y;
            var x1 = x + width;
            var y1 = y + height;
            var c = r * 0.5522847498307936;

            _sb.Append(FormatNumber(x0 + r)).Append(' ').Append(FormatNumber(y0)).Append(" m\n");
            _sb.Append(FormatNumber(x1 - r)).Append(' ').Append(FormatNumber(y0)).Append(" l\n");
            _sb.Append(FormatNumber(x1 - r + c)).Append(' ').Append(FormatNumber(y0)).Append(' ')
                .Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y0 + r - c)).Append(' ')
                .Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y0 + r)).Append(" c\n");
            _sb.Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y1 - r)).Append(" l\n");
            _sb.Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y1 - r + c)).Append(' ')
                .Append(FormatNumber(x1 - r + c)).Append(' ').Append(FormatNumber(y1)).Append(' ')
                .Append(FormatNumber(x1 - r)).Append(' ').Append(FormatNumber(y1)).Append(" c\n");
            _sb.Append(FormatNumber(x0 + r)).Append(' ').Append(FormatNumber(y1)).Append(" l\n");
            _sb.Append(FormatNumber(x0 + r - c)).Append(' ').Append(FormatNumber(y1)).Append(' ')
                .Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y1 - r + c)).Append(' ')
                .Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y1 - r)).Append(" c\n");
            _sb.Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y0 + r)).Append(" l\n");
            _sb.Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y0 + r - c)).Append(' ')
                .Append(FormatNumber(x0 + r - c)).Append(' ').Append(FormatNumber(y0)).Append(' ')
                .Append(FormatNumber(x0 + r)).Append(' ').Append(FormatNumber(y0)).Append(" c\n");
            _sb.Append("h f\n");
        }

        private static string ToComponent(byte value) {
            var scaled = value / 255.0;
            return scaled.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatNumber(double value) {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
