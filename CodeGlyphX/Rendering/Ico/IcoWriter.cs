using System;
using System.IO;

namespace CodeGlyphX.Rendering.Ico;

/// <summary>
/// Writes ICO files embedding PNG images.
/// </summary>
public static class IcoWriter {
    /// <summary>
    /// Creates an ICO byte array from PNG bytes.
    /// </summary>
    public static byte[] WritePng(byte[] pngBytes) {
        if (pngBytes is null) throw new ArgumentNullException(nameof(pngBytes));
        using var ms = new MemoryStream();
        WritePng(ms, pngBytes);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes an ICO stream from PNG bytes.
    /// </summary>
    public static void WritePng(Stream stream, ReadOnlySpan<byte> pngBytes) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (!TryGetPngSize(pngBytes, out var width, out var height)) {
            throw new FormatException("Invalid PNG data.");
        }

        var w = (byte)(width >= 256 ? 0 : width);
        var h = (byte)(height >= 256 ? 0 : height);

        var header = new byte[6];
        header[0] = 0;
        header[1] = 0;
        header[2] = 1;
        header[3] = 0;
        header[4] = 1;
        header[5] = 0;
        stream.Write(header, 0, header.Length);

        var entry = new byte[16];
        entry[0] = w;
        entry[1] = h;
        entry[2] = 0;
        entry[3] = 0;
        entry[4] = 1;
        entry[5] = 0;
        entry[6] = 32;
        entry[7] = 0;
        var size = pngBytes.Length;
        entry[8] = (byte)(size & 0xFF);
        entry[9] = (byte)((size >> 8) & 0xFF);
        entry[10] = (byte)((size >> 16) & 0xFF);
        entry[11] = (byte)((size >> 24) & 0xFF);
        var offset = 6 + 16;
        entry[12] = (byte)(offset & 0xFF);
        entry[13] = (byte)((offset >> 8) & 0xFF);
        entry[14] = (byte)((offset >> 16) & 0xFF);
        entry[15] = (byte)((offset >> 24) & 0xFF);
        stream.Write(entry, 0, entry.Length);

        WriteBytes(stream, pngBytes);
    }

    /// <summary>
    /// Creates an ICO byte array from multiple PNG images.
    /// </summary>
    public static byte[] WritePngs(byte[][] pngBytes) {
        if (pngBytes is null) throw new ArgumentNullException(nameof(pngBytes));
        using var ms = new MemoryStream();
        WritePngs(ms, pngBytes);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes an ICO stream from multiple PNG images.
    /// </summary>
    public static void WritePngs(Stream stream, byte[][] pngBytes) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (pngBytes is null) throw new ArgumentNullException(nameof(pngBytes));
        if (pngBytes.Length == 0) throw new ArgumentException("At least one PNG is required.", nameof(pngBytes));

        var count = pngBytes.Length;
        var widths = new int[count];
        var heights = new int[count];
        var sizes = new int[count];

        for (var i = 0; i < count; i++) {
            var png = pngBytes[i];
            if (png is null) throw new ArgumentNullException(nameof(pngBytes), "PNG entry cannot be null.");
            if (!TryGetPngSize(png, out var width, out var height)) {
                throw new FormatException("Invalid PNG data.");
            }
            widths[i] = width;
            heights[i] = height;
            sizes[i] = png.Length;
        }

        var header = new byte[6];
        header[0] = 0;
        header[1] = 0;
        header[2] = 1;
        header[3] = 0;
        header[4] = (byte)(count & 0xFF);
        header[5] = (byte)((count >> 8) & 0xFF);
        stream.Write(header, 0, header.Length);

        var offset = 6 + 16 * count;
        for (var i = 0; i < count; i++) {
            var entry = new byte[16];
            entry[0] = (byte)(widths[i] >= 256 ? 0 : widths[i]);
            entry[1] = (byte)(heights[i] >= 256 ? 0 : heights[i]);
            entry[2] = 0;
            entry[3] = 0;
            entry[4] = 1;
            entry[5] = 0;
            entry[6] = 32;
            entry[7] = 0;
            var size = sizes[i];
            entry[8] = (byte)(size & 0xFF);
            entry[9] = (byte)((size >> 8) & 0xFF);
            entry[10] = (byte)((size >> 16) & 0xFF);
            entry[11] = (byte)((size >> 24) & 0xFF);
            entry[12] = (byte)(offset & 0xFF);
            entry[13] = (byte)((offset >> 8) & 0xFF);
            entry[14] = (byte)((offset >> 16) & 0xFF);
            entry[15] = (byte)((offset >> 24) & 0xFF);
            stream.Write(entry, 0, entry.Length);
            offset += size;
        }

        for (var i = 0; i < count; i++) {
            WriteBytes(stream, pngBytes[i]);
        }
    }

    private static bool TryGetPngSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (data.Length < 24) return false;
        if (data[0] != 137 || data[1] != 80 || data[2] != 78 || data[3] != 71) return false;
        if (data[12] != (byte)'I' || data[13] != (byte)'H' || data[14] != (byte)'D' || data[15] != (byte)'R') return false;
        width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
        height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
        return width > 0 && height > 0;
    }

    private static void WriteBytes(Stream stream, ReadOnlySpan<byte> data) {
        var buffer = data.ToArray();
        stream.Write(buffer, 0, buffer.Length);
    }
}
