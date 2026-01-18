using System;
using System.Text;
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
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

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
            var srcRow = y * stride;
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
                var lum = (r * 299 + g * 587 + b * 114 + 500) / 1000;
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
