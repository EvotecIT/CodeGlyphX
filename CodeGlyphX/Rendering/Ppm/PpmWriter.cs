using System;
using System.Buffers;
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
        WriteRgba32Core(stream, width, height, rgba, stride, rowOffset: 0, rowStride: stride, nameof(rgba), "RGBA buffer is too small.");
    }

    /// <summary>
    /// Writes a PPM byte array from a PNG scanline buffer (filter byte per row).
    /// </summary>
    public static byte[] WriteRgba32Scanlines(int width, int height, ReadOnlySpan<byte> scanlines, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32Scanlines(ms, width, height, scanlines, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a PPM (P6) stream from a PNG scanline buffer (filter byte per row).
    /// </summary>
    public static void WriteRgba32Scanlines(Stream stream, int width, int height, ReadOnlySpan<byte> scanlines, int stride) {
        WriteRgba32Core(stream, width, height, scanlines, stride, rowOffset: 1, rowStride: stride + 1, nameof(scanlines), "Scanline buffer is too small.");
    }

    private static void WriteRgba32Core(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, int rowOffset, int rowStride, string bufferName, string bufferMessage) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rowStride < rowOffset + stride) throw new ArgumentOutOfRangeException(nameof(rowStride));
        if (rgba.Length < (height - 1) * rowStride + rowOffset + width * 4) throw new ArgumentException(bufferMessage, bufferName);

        var header = Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        stream.Write(header, 0, header.Length);

        var row = ArrayPool<byte>.Shared.Rent(width * 3);
        try {
            for (var y = 0; y < height; y++) {
                var srcRow = y * rowStride + rowOffset;
                var dst = 0;
                for (var x = 0; x < width; x++) {
                    var p = srcRow + x * 4;
                    var r = rgba[p + 0];
                    var g = rgba[p + 1];
                    var b = rgba[p + 2];
                    var a = rgba[p + 3];
                    if (a != 255) {
                        var inv = 255 - a;
                        r = (byte)((r * a + 255 * inv) / 255);
                        g = (byte)((g * a + 255 * inv) / 255);
                        b = (byte)((b * a + 255 * inv) / 255);
                    }
                    row[dst++] = r;
                    row[dst++] = g;
                    row[dst++] = b;
                }
                stream.Write(row, 0, width * 3);
            }
        } finally {
            ArrayPool<byte>.Shared.Return(row);
        }
    }
}
