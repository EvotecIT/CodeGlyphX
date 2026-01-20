using System;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ico;

/// <summary>
/// Decodes ICO/CUR images to RGBA buffers (embedded PNG or BMP/DIB).
/// </summary>
public static class IcoReader {
    /// <summary>
    /// Returns true if the data looks like an ICO/CUR header.
    /// </summary>
    public static bool IsIco(ReadOnlySpan<byte> data) {
        if (data.Length < 6) return false;
        if (ReadUInt16LE(data, 0) != 0) return false;
        var type = ReadUInt16LE(data, 2);
        if (type != 1 && type != 2) return false;
        var count = ReadUInt16LE(data, 4);
        return count > 0;
    }

    /// <summary>
    /// Decodes an ICO/CUR image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> ico, out int width, out int height) {
        if (!IsIco(ico)) throw new FormatException("Invalid ICO header.");
        var count = ReadUInt16LE(ico, 4);
        if (count <= 0) throw new FormatException("ICO contains no images.");
        if (ico.Length < 6 + count * 16) throw new FormatException("Truncated ICO header.");

        var bestOffset = -1;
        var bestSize = 0;
        var bestArea = 0;

        for (var i = 0; i < count; i++) {
            var entry = 6 + i * 16;
            var w = ico[entry + 0];
            var h = ico[entry + 1];
            var size = (int)ReadUInt32LE(ico, entry + 8);
            var offset = (int)ReadUInt32LE(ico, entry + 12);
            var widthPx = w == 0 ? 256 : w;
            var heightPx = h == 0 ? 256 : h;
            if (offset < 0 || size <= 0) continue;
            if (offset + size > ico.Length) continue;

            var area = widthPx * heightPx;
            if (area > bestArea || (area == bestArea && size > bestSize)) {
                bestArea = area;
                bestSize = size;
                bestOffset = offset;
            }
        }

        if (bestOffset < 0) throw new FormatException("ICO image data not found.");

        var imageData = ico.Slice(bestOffset, bestSize);
        if (imageData.Length >= 8 && IsPng(imageData)) {
            return PngReader.DecodeRgba32(imageData, out width, out height);
        }

        var bmp = BuildBmpFromDib(imageData);
        return BmpReader.DecodeRgba32(bmp, out width, out height);
    }

    private static bool IsPng(ReadOnlySpan<byte> data) {
        return data.Length >= 8 &&
               data[0] == 137 &&
               data[1] == 80 &&
               data[2] == 78 &&
               data[3] == 71 &&
               data[4] == 13 &&
               data[5] == 10 &&
               data[6] == 26 &&
               data[7] == 10;
    }

    private static byte[] BuildBmpFromDib(ReadOnlySpan<byte> dib) {
        if (dib.Length < 40) throw new FormatException("Unsupported ICO image format.");
        var headerSize = (int)ReadUInt32LE(dib, 0);
        if (headerSize < 40 || headerSize > dib.Length) throw new FormatException("Unsupported ICO image header.");

        var width = ReadInt32LE(dib, 4);
        var height = ReadInt32LE(dib, 8);
        var bpp = ReadUInt16LE(dib, 14);
        var colorsUsed = headerSize >= 44 ? ReadUInt32LE(dib, 32) : 0;

        var absHeight = Math.Abs(height);
        var newHeight = absHeight / 2;
        if (newHeight <= 0) throw new FormatException("Invalid ICO image height.");
        if (height < 0) newHeight = -newHeight;

        var paletteCount = 0;
        if (bpp <= 8) {
            paletteCount = colorsUsed != 0 ? (int)colorsUsed : 1 << bpp;
        }

        var dataOffset = 14 + headerSize + paletteCount * 4;
        var fileSize = 14 + dib.Length;

        var bmp = new byte[fileSize];
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        WriteUInt32LE(bmp, 2, (uint)fileSize);
        WriteUInt16LE(bmp, 6, 0);
        WriteUInt16LE(bmp, 8, 0);
        WriteUInt32LE(bmp, 10, (uint)dataOffset);

        dib.CopyTo(bmp.AsSpan(14));
        WriteInt32LE(bmp, 14 + 8, newHeight);
        return bmp;
    }

    private static ushort ReadUInt16LE(ReadOnlySpan<byte> data, int offset) {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    private static uint ReadUInt32LE(ReadOnlySpan<byte> data, int offset) {
        return (uint)(data[offset]
                      | (data[offset + 1] << 8)
                      | (data[offset + 2] << 16)
                      | (data[offset + 3] << 24));
    }

    private static int ReadInt32LE(ReadOnlySpan<byte> data, int offset) {
        return (int)ReadUInt32LE(data, offset);
    }

    private static void WriteUInt16LE(Span<byte> data, int offset, ushort value) {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    private static void WriteUInt32LE(Span<byte> data, int offset, uint value) {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteInt32LE(Span<byte> data, int offset, int value) {
        WriteUInt32LE(data, offset, unchecked((uint)value));
    }
}

