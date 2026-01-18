using System;
using System.IO;
using System.Text;

namespace CodeGlyphX.Rendering.Ppm;

/// <summary>
/// Writes PPM (P6) images from RGBA buffers.
/// </summary>
public static class PpmWriter {
    /// <summary>
    /// Writes a PPM byte array from an RGBA buffer.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a PPM (P6) stream from an RGBA buffer (alpha blended over white).
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var header = Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        stream.Write(header, 0, header.Length);

        var row = new byte[width * 3];
        for (var y = 0; y < height; y++) {
            var srcRow = y * stride;
            var dst = 0;
            for (var x = 0; x < width; x++) {
                var p = srcRow + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                var a = rgba[p + 3];
                if (a != 255) {
                    r = (byte)((r * a + 255 * (255 - a)) / 255);
                    g = (byte)((g * a + 255 * (255 - a)) / 255);
                    b = (byte)((b * a + 255 * (255 - a)) / 255);
                }
                row[dst++] = r;
                row[dst++] = g;
                row[dst++] = b;
            }
            stream.Write(row, 0, row.Length);
        }
    }
}
