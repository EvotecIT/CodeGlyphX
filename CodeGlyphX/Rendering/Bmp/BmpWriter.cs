using System;
using System.IO;

namespace CodeGlyphX.Rendering.Bmp;

/// <summary>
/// Writes BMP images from RGBA buffers.
/// </summary>
public static class BmpWriter {
    /// <summary>
    /// Writes a BMP (32-bit BGRA) byte array from an RGBA buffer.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a BMP (32-bit BGRA) to a stream from an RGBA buffer.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        const int fileHeaderSize = 14;
        const int infoHeaderSize = 108;
        var dataOffset = fileHeaderSize + infoHeaderSize;
        var rowSize = width * 4;
        var imageSize = rowSize * height;
        var fileSize = dataOffset + imageSize;

        var header = new byte[fileHeaderSize + infoHeaderSize];
        header[0] = (byte)'B';
        header[1] = (byte)'M';
        WriteInt32(header, 2, fileSize);
        WriteInt32(header, 10, dataOffset);

        WriteInt32(header, 14, infoHeaderSize);
        WriteInt32(header, 18, width);
        WriteInt32(header, 22, height);
        WriteInt16(header, 26, 1);
        WriteInt16(header, 28, 32);
        WriteInt32(header, 30, 3);
        WriteInt32(header, 34, imageSize);
        WriteInt32(header, 38, 0);
        WriteInt32(header, 42, 0);
        WriteInt32(header, 46, 0);
        WriteInt32(header, 50, 0);
        WriteInt32(header, 54, 0x00FF0000);
        WriteInt32(header, 58, 0x0000FF00);
        WriteInt32(header, 62, 0x000000FF);
        WriteInt32(header, 66, unchecked((int)0xFF000000));
        WriteInt32(header, 70, 0x73524742);

        stream.Write(header, 0, header.Length);

        var row = new byte[rowSize];
        for (var y = 0; y < height; y++) {
            var srcRow = (height - 1 - y) * stride;
            var dst = 0;
            for (var x = 0; x < width; x++) {
                var p = srcRow + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                var a = rgba[p + 3];
                row[dst++] = b;
                row[dst++] = g;
                row[dst++] = r;
                row[dst++] = a;
            }
            stream.Write(row, 0, rowSize);
        }
    }

    /// <summary>
    /// Writes a BMP (24-bit BGR) byte array from an RGBA buffer (alpha discarded).
    /// </summary>
    public static byte[] WriteRgb24(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgb24(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a BMP (24-bit BGR) to a stream from an RGBA buffer (alpha discarded).
    /// </summary>
    public static void WriteRgb24(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        const int fileHeaderSize = 14;
        const int infoHeaderSize = 40;
        var dataOffset = fileHeaderSize + infoHeaderSize;
        var rowSize = ((width * 3 + 3) / 4) * 4;
        var imageSize = rowSize * height;
        var fileSize = dataOffset + imageSize;

        var header = new byte[fileHeaderSize + infoHeaderSize];
        header[0] = (byte)'B';
        header[1] = (byte)'M';
        WriteInt32(header, 2, fileSize);
        WriteInt32(header, 10, dataOffset);

        WriteInt32(header, 14, infoHeaderSize);
        WriteInt32(header, 18, width);
        WriteInt32(header, 22, height);
        WriteInt16(header, 26, 1);
        WriteInt16(header, 28, 24);
        WriteInt32(header, 30, 0);
        WriteInt32(header, 34, imageSize);
        WriteInt32(header, 38, 0);
        WriteInt32(header, 42, 0);
        WriteInt32(header, 46, 0);
        WriteInt32(header, 50, 0);

        stream.Write(header, 0, header.Length);

        var row = new byte[rowSize];
        for (var y = 0; y < height; y++) {
            var srcRow = (height - 1 - y) * stride;
            var dst = 0;
            for (var x = 0; x < width; x++) {
                var p = srcRow + x * 4;
                row[dst++] = rgba[p + 2];
                row[dst++] = rgba[p + 1];
                row[dst++] = rgba[p + 0];
            }
            while (dst < rowSize) row[dst++] = 0;
            stream.Write(row, 0, rowSize);
        }
    }

    private static void WriteInt16(byte[] buffer, int offset, short value) {
        buffer[offset + 0] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteInt32(byte[] buffer, int offset, int value) {
        buffer[offset + 0] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
