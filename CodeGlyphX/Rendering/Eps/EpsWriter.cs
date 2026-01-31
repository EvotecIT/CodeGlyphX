using System;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Eps;

/// <summary>
/// Minimal EPS writer for embedding RGB images.
/// </summary>
public static class EpsWriter {
    private static readonly char[] Hex = "0123456789ABCDEF".ToCharArray();

    /// <summary>
    /// Writes a single-page EPS containing the supplied RGBA image.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride, Rgba32? background = null) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride, background);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a single-page EPS containing the supplied RGBA image.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, Rgba32? background = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, "EPS output exceeds size limits.");
        _ = RenderGuards.EnsureOutputBytes((long)width * height * 3, "EPS output exceeds size limits.");
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var rgb = ToRgb(width, height, rgba, stride, background ?? Rgba32.White);
        WriteRgb24(stream, width, height, rgb);
    }

    /// <summary>
    /// Writes a single-page EPS containing the supplied RGB image.
    /// </summary>
    public static byte[] WriteRgb24(int width, int height, byte[] rgb) {
        using var ms = new MemoryStream();
        WriteRgb24(ms, width, height, rgb);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a single-page EPS containing the supplied RGB image.
    /// </summary>
    public static void WriteRgb24(Stream stream, int width, int height, byte[] rgb) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, "EPS output exceeds size limits.");
        _ = RenderGuards.EnsureOutputBytes((long)width * height * 3, "EPS output exceeds size limits.");
        if (rgb is null) throw new ArgumentNullException(nameof(rgb));
        if (rgb.Length < width * height * 3) throw new ArgumentException("RGB buffer is too small.", nameof(rgb));

        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true);
        writer.WriteLine("%!PS-Adobe-3.0 EPSF-3.0");
        writer.WriteLine($"%%BoundingBox: 0 0 {width} {height}");
        writer.WriteLine("%%LanguageLevel: 2");
        writer.WriteLine("%%Pages: 1");
        writer.WriteLine("%%EndComments");
        writer.WriteLine("gsave");
        writer.WriteLine("/DeviceRGB setcolorspace");
        writer.WriteLine($"{width} {height} 8");
        writer.WriteLine($"[{width} 0 0 -{height} 0 {height}]");
        writer.WriteLine("{currentfile /ASCIIHexDecode filter} false 3 colorimage");
        writer.Flush();

        WriteHex(writer, rgb);

        writer.WriteLine("grestore");
        writer.WriteLine("showpage");
        writer.WriteLine("%%EOF");
        writer.Flush();
    }

    private static void WriteHex(StreamWriter writer, byte[] data) {
        const int lineLength = 96;
        var sb = new StringBuilder(lineLength + 2);
        for (var i = 0; i < data.Length; i++) {
            var b = data[i];
            sb.Append(Hex[b >> 4]);
            sb.Append(Hex[b & 0xF]);
            if (sb.Length >= lineLength) {
                writer.WriteLine(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0) {
            writer.WriteLine(sb.ToString());
        }
        writer.WriteLine(">");
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
