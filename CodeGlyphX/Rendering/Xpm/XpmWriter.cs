using System;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Xpm;

/// <summary>
/// Writes XPM (X Pixmap) images from RGBA buffers.
/// </summary>
public static class XpmWriter {
    /// <summary>
    /// Writes an XPM string from an RGBA buffer.
    /// </summary>
    public static string WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride, string? name = null, Rgba32? foreground = null, Rgba32? background = null) {
        return WriteRgba32Core(width, height, rgba, stride, rowOffset: 0, rowStride: stride, name, foreground, background, nameof(rgba), "RGBA buffer is too small.");
    }

    /// <summary>
    /// Writes an XPM string from a PNG scanline buffer (filter byte per row).
    /// </summary>
    public static string WriteRgba32Scanlines(int width, int height, ReadOnlySpan<byte> scanlines, int stride, string? name = null, Rgba32? foreground = null, Rgba32? background = null) {
        return WriteRgba32Core(width, height, scanlines, stride, rowOffset: 1, rowStride: stride + 1, name, foreground, background, nameof(scanlines), "Scanline buffer is too small.");
    }

    private static string WriteRgba32Core(
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        int stride,
        int rowOffset,
        int rowStride,
        string? name,
        Rgba32? foreground,
        Rgba32? background,
        string bufferName,
        string bufferMessage) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, "XPM output exceeds size limits.");
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rowStride < rowOffset + stride) throw new ArgumentOutOfRangeException(nameof(rowStride));
        if (rgba.Length < (height - 1) * rowStride + rowOffset + width * 4) throw new ArgumentException(bufferMessage, bufferName);

        var fg = foreground ?? new Rgba32(0, 0, 0);
        var bg = background ?? new Rgba32(255, 255, 255);
        var safeName = SanitizeName(string.IsNullOrWhiteSpace(name) ? "codeglyphx" : name!);

        var sb = new StringBuilder();
        sb.Append("/* XPM */\n");
        sb.Append("static const char *").Append(safeName).Append("[] = {\n");
        sb.Append("\"").Append(width).Append(' ').Append(height).Append(" 2 1\",\n");
        sb.Append("\"a c ").Append(ToHex(bg)).Append("\",\n");
        sb.Append("\"b c ").Append(ToHex(fg)).Append("\",\n");

        for (var y = 0; y < height; y++) {
            var srcRow = y * rowStride + rowOffset;
            sb.Append('\"');
            for (var x = 0; x < width; x++) {
                var p = srcRow + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                var a = rgba[p + 3];
                if (a == 0) {
                    sb.Append('a');
                    continue;
                }
                var lum = LumaTables.Luma(r, g, b);
                sb.Append(lum <= 127 ? 'b' : 'a');
            }
            sb.Append("\",");
            sb.Append('\n');
        }

        sb.Append("};\n");
        return sb.ToString();
    }

    private static string ToHex(Rgba32 color) {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static string SanitizeName(string name) {
        var sb = new StringBuilder(name.Length);
        for (var i = 0; i < name.Length; i++) {
            var c = name[i];
            if (char.IsLetterOrDigit(c)) {
                sb.Append(char.ToLowerInvariant(c));
            } else {
                sb.Append('_');
            }
        }
        if (sb.Length == 0 || char.IsDigit(sb[0])) {
            sb.Insert(0, 'x');
        }
        return sb.ToString();
    }
}
