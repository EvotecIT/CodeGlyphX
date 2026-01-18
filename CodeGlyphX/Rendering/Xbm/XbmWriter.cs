using System;
using System.Text;

namespace CodeGlyphX.Rendering.Xbm;

/// <summary>
/// Writes XBM (X Bitmap) images from RGBA buffers.
/// </summary>
public static class XbmWriter {
    /// <summary>
    /// Writes an XBM string from an RGBA buffer.
    /// </summary>
    public static string WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride, string? name = null) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var safeName = SanitizeName(string.IsNullOrWhiteSpace(name) ? "codeglyphx" : name!);
        var rowBytes = (width + 7) / 8;
        var sb = new StringBuilder();
        sb.Append("#define ").Append(safeName).Append("_width ").Append(width).Append('\n');
        sb.Append("#define ").Append(safeName).Append("_height ").Append(height).Append('\n');
        sb.Append("static unsigned char ").Append(safeName).Append("_bits[] = {\n");

        var totalBytes = rowBytes * height;
        var count = 0;
        for (var y = 0; y < height; y++) {
            var srcRow = y * stride;
            for (var bx = 0; bx < rowBytes; bx++) {
                byte value = 0;
                var baseX = bx * 8;
                for (var bit = 0; bit < 8; bit++) {
                    var x = baseX + bit;
                    if (x >= width) break;
                    var p = srcRow + x * 4;
                    var r = rgba[p + 0];
                    var g = rgba[p + 1];
                    var b = rgba[p + 2];
                    var a = rgba[p + 3];
                    if (a == 0) continue;
                    var lum = (r * 299 + g * 587 + b * 114 + 500) / 1000;
                    var isBlack = lum <= 127;
                    if (isBlack) {
                        value |= (byte)(1 << bit);
                    }
                }

                if (count % 12 == 0) sb.Append("  ");
                sb.Append("0x").Append(value.ToString("x2"));
                count++;
                if (count < totalBytes) sb.Append(", ");
                if (count % 12 == 0) sb.Append('\n');
            }
        }
        if (count % 12 != 0) sb.Append('\n');
        sb.Append("};\n");
        return sb.ToString();
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
