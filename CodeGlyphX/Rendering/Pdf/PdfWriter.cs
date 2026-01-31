using System;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pdf;

/// <summary>
/// Minimal PDF writer for embedding RGB images.
/// </summary>
public static class PdfWriter {
    /// <summary>
    /// Writes a single-page PDF containing the supplied RGBA image.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride, Rgba32? background = null) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride, background);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a single-page PDF containing the supplied RGBA image.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, Rgba32? background = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, "PDF output exceeds size limits.");
        _ = RenderGuards.EnsureOutputBytes((long)width * height * 3, "PDF output exceeds size limits.");
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var rgb = ToRgb(width, height, rgba, stride, background ?? Rgba32.White);
        WriteRgb24(stream, width, height, rgb);
    }

    /// <summary>
    /// Writes a single-page PDF containing the supplied RGB image.
    /// </summary>
    public static byte[] WriteRgb24(int width, int height, byte[] rgb) {
        using var ms = new MemoryStream();
        WriteRgb24(ms, width, height, rgb);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a single-page PDF containing the supplied RGB image.
    /// </summary>
    public static void WriteRgb24(Stream stream, int width, int height, byte[] rgb) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, "PDF output exceeds size limits.");
        _ = RenderGuards.EnsureOutputBytes((long)width * height * 3, "PDF output exceeds size limits.");
        if (rgb is null) throw new ArgumentNullException(nameof(rgb));
        if (rgb.Length < width * height * 3) throw new ArgumentException("RGB buffer is too small.", nameof(rgb));

        var offsets = new long[6];
        var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true);

        writer.WriteLine("%PDF-1.4");
        writer.Flush();

        offsets[1] = stream.Position;
        writer.WriteLine("1 0 obj");
        writer.WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
        writer.WriteLine("endobj");
        writer.Flush();

        offsets[2] = stream.Position;
        writer.WriteLine("2 0 obj");
        writer.WriteLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        writer.WriteLine("endobj");
        writer.Flush();

        offsets[3] = stream.Position;
        writer.WriteLine("3 0 obj");
        writer.WriteLine($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}]");
        writer.WriteLine("   /Resources << /XObject << /Im0 5 0 R >> >>");
        writer.WriteLine("   /Contents 4 0 R >>");
        writer.WriteLine("endobj");
        writer.Flush();

        var content = $"q\n{width} 0 0 {height} 0 0 cm\n/Im0 Do\nQ\n";
        var contentBytes = Encoding.ASCII.GetBytes(content);

        offsets[4] = stream.Position;
        writer.WriteLine("4 0 obj");
        writer.WriteLine($"<< /Length {contentBytes.Length} >>");
        writer.WriteLine("stream");
        writer.Flush();
        stream.Write(contentBytes, 0, contentBytes.Length);
        writer.WriteLine();
        writer.WriteLine("endstream");
        writer.WriteLine("endobj");
        writer.Flush();

        offsets[5] = stream.Position;
        writer.WriteLine("5 0 obj");
        writer.WriteLine($"<< /Type /XObject /Subtype /Image /Width {width} /Height {height}");
        writer.WriteLine("   /ColorSpace /DeviceRGB /BitsPerComponent 8");
        writer.WriteLine($"   /Length {width * height * 3} >>");
        writer.WriteLine("stream");
        writer.Flush();
        stream.Write(rgb, 0, width * height * 3);
        writer.WriteLine();
        writer.WriteLine("endstream");
        writer.WriteLine("endobj");
        writer.Flush();

        var xrefStart = stream.Position;
        writer.WriteLine("xref");
        writer.WriteLine("0 6");
        writer.WriteLine("0000000000 65535 f ");
        for (var i = 1; i <= 5; i++) {
            writer.WriteLine($"{offsets[i]:0000000000} 00000 n ");
        }
        writer.WriteLine("trailer");
        writer.WriteLine("<< /Size 6 /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefStart);
        writer.WriteLine("%%EOF");
        writer.Flush();
    }

    private static byte[] ToRgb(int width, int height, ReadOnlySpan<byte> rgba, int stride, Rgba32 background) {
        var rgb = new byte[width * height * 3];
        var bgR = background.R;
        var bgG = background.G;
        var bgB = background.B;

        var dst = 0;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var p = row + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                var a = rgba[p + 3];

                if (a == 255) {
                    rgb[dst++] = r;
                    rgb[dst++] = g;
                    rgb[dst++] = b;
                } else if (a == 0) {
                    rgb[dst++] = bgR;
                    rgb[dst++] = bgG;
                    rgb[dst++] = bgB;
                } else {
                    var inv = 255 - a;
                    rgb[dst++] = (byte)((r * a + bgR * inv + 127) / 255);
                    rgb[dst++] = (byte)((g * a + bgG * inv + 127) / 255);
                    rgb[dst++] = (byte)((b * a + bgB * inv + 127) / 255);
                }
            }
        }

        return rgb;
    }
}
