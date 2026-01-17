using System;
using System.Globalization;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Vector;

namespace CodeGlyphX.Rendering.Eps;

/// <summary>
/// Renders QR modules to an EPS image.
/// </summary>
public static class QrEpsRenderer {
    /// <summary>
    /// Renders the QR module matrix to an EPS string.
    /// </summary>
    public static string Render(BitMatrix modules, QrPngRenderOptions opts, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster || QrVectorLayout.ShouldRaster(opts)) {
            var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
            return Encoding.ASCII.GetString(EpsWriter.WriteRgba32(widthPx, heightPx, pixels, stride, opts.Background));
        }
        return BuildEps(modules, opts);
    }

    /// <summary>
    /// Renders the QR module matrix to an EPS stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, RenderMode mode = RenderMode.Vector) {
        if (mode == RenderMode.Raster || QrVectorLayout.ShouldRaster(opts)) {
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
        QrVectorLayout.GetSize(modules, opts, out var widthPx, out var heightPx);
        var outModules = modules.Width + opts.QuietZone * 2;
        var sb = new StringBuilder(outModules * outModules * 3);
        sb.AppendLine("%!PS-Adobe-3.0 EPSF-3.0");
        sb.AppendLine($"%%BoundingBox: 0 0 {widthPx} {heightPx}");
        sb.AppendLine("%%LanguageLevel: 2");
        sb.AppendLine("%%Pages: 1");
        sb.AppendLine("%%EndComments");
        sb.AppendLine("gsave");
        var sink = new EpsSink(sb, heightPx);
        QrVectorLayout.Render(modules, opts, sink, out _, out _);

        sb.AppendLine("grestore");
        sb.AppendLine("showpage");
        sb.AppendLine("%%EOF");
        return sb.ToString();
    }

    private sealed class EpsSink : IQrVectorSink {
        private readonly StringBuilder _sb;
        private readonly int _height;
        private Rgba32 _current;
        private bool _hasCurrent;

        public EpsSink(StringBuilder sb, int height) {
            _sb = sb ?? throw new ArgumentNullException(nameof(sb));
            _height = height;
        }

        public void SetFillColor(Rgba32 color) {
            if (_hasCurrent && _current.R == color.R && _current.G == color.G && _current.B == color.B) return;
            _current = color;
            _hasCurrent = true;
            _sb.Append(ToComponent(color.R)).Append(' ')
                .Append(ToComponent(color.G)).Append(' ')
                .Append(ToComponent(color.B)).AppendLine(" setrgbcolor");
        }

        public void FillRect(int x, int yTop, int width, int height) {
            var y = _height - yTop - height;
            _sb.Append(x).Append(' ')
                .Append(y).Append(' ')
                .Append(width).Append(' ')
                .Append(height).AppendLine(" rectfill");
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

            _sb.Append(FormatNumber(x0 + r)).Append(' ').Append(FormatNumber(y0)).AppendLine(" moveto");
            _sb.Append(FormatNumber(x1 - r)).Append(' ').Append(FormatNumber(y0)).AppendLine(" lineto");
            _sb.Append(FormatNumber(x1 - r + c)).Append(' ').Append(FormatNumber(y0)).Append(' ')
                .Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y0 + r - c)).Append(' ')
                .Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y0 + r)).AppendLine(" curveto");
            _sb.Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y1 - r)).AppendLine(" lineto");
            _sb.Append(FormatNumber(x1)).Append(' ').Append(FormatNumber(y1 - r + c)).Append(' ')
                .Append(FormatNumber(x1 - r + c)).Append(' ').Append(FormatNumber(y1)).Append(' ')
                .Append(FormatNumber(x1 - r)).Append(' ').Append(FormatNumber(y1)).AppendLine(" curveto");
            _sb.Append(FormatNumber(x0 + r)).Append(' ').Append(FormatNumber(y1)).AppendLine(" lineto");
            _sb.Append(FormatNumber(x0 + r - c)).Append(' ').Append(FormatNumber(y1)).Append(' ')
                .Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y1 - r + c)).Append(' ')
                .Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y1 - r)).AppendLine(" curveto");
            _sb.Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y0 + r)).AppendLine(" lineto");
            _sb.Append(FormatNumber(x0)).Append(' ').Append(FormatNumber(y0 + r - c)).Append(' ')
                .Append(FormatNumber(x0 + r - c)).Append(' ').Append(FormatNumber(y0)).Append(' ')
                .Append(FormatNumber(x0 + r)).Append(' ').Append(FormatNumber(y0)).AppendLine(" curveto");
            _sb.AppendLine("closepath fill");
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
