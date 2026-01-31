using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Tga;

/// <summary>
/// Writes TGA images from RGBA buffers (uncompressed).
/// </summary>
public static class TgaWriter {
    private const int MaxDimension = 16384;
    /// <summary>
    /// Writes a TGA byte array from an RGBA buffer.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TGA stream from an RGBA buffer (32-bit BGRA).
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (width > MaxDimension) throw new ArgumentOutOfRangeException(nameof(width));
        if (height > MaxDimension) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, "TGA output exceeds size limits.");
        _ = RenderGuards.EnsureOutputBytes((long)width * height * 4, "TGA output exceeds size limits.");
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var header = new byte[18];
        header[2] = 2; // uncompressed true-color image
        header[12] = (byte)(width & 0xFF);
        header[13] = (byte)((width >> 8) & 0xFF);
        header[14] = (byte)(height & 0xFF);
        header[15] = (byte)((height >> 8) & 0xFF);
        header[16] = 32; // bits per pixel
        header[17] = 0x28; // top-left origin, 8 bits alpha
        stream.Write(header, 0, header.Length);

        var rowBytes = RenderGuards.EnsureOutputBytes((long)width * 4, "TGA output exceeds size limits.");
        var row = new byte[rowBytes];
        for (var y = 0; y < height; y++) {
            var srcRow = y * stride;
            var dst = 0;
            for (var x = 0; x < width; x++) {
                var p = srcRow + x * 4;
                row[dst++] = rgba[p + 2];
                row[dst++] = rgba[p + 1];
                row[dst++] = rgba[p + 0];
                row[dst++] = rgba[p + 3];
            }
            stream.Write(row, 0, row.Length);
        }
    }
}
