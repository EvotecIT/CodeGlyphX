using System;
using System.IO;
using System.Text;

namespace CodeGlyphX.Rendering.Pbm;

/// <summary>
/// Writes PBM (P4) images from RGBA buffers.
/// </summary>
public static class PbmWriter {
    /// <summary>
    /// Writes a PBM byte array from an RGBA buffer.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a PBM (P4) stream from an RGBA buffer (alpha treated as white).
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var header = Encoding.ASCII.GetBytes($"P4\n{width} {height}\n");
        stream.Write(header, 0, header.Length);

        var rowBytes = (width + 7) / 8;
        var row = new byte[rowBytes];
        for (var y = 0; y < height; y++) {
            Array.Clear(row, 0, row.Length);
            var srcRow = y * stride;
            for (var x = 0; x < width; x++) {
                var p = srcRow + x * 4;
                var a = rgba[p + 3];
                if (a == 0) continue;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                var lum = (r * 299 + g * 587 + b * 114 + 500) / 1000;
                var isBlack = lum <= 127;
                if (isBlack) {
                    var byteIndex = x >> 3;
                    var bit = 7 - (x & 7);
                    row[byteIndex] |= (byte)(1 << bit);
                }
            }
            stream.Write(row, 0, row.Length);
        }
    }
}
