using System;

namespace CodeGlyphX.Rendering.Tga;

/// <summary>
/// Decodes TGA images to RGBA buffers (uncompressed true-color/grayscale).
/// </summary>
public static class TgaReader {
    /// <summary>
    /// Decodes a TGA image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> tga, out int width, out int height) {
        if (tga.Length < 18) throw new FormatException("Invalid TGA header.");

        var idLength = tga[0];
        var colorMapType = tga[1];
        var imageType = tga[2];

        if (colorMapType != 0) throw new FormatException("Color-mapped TGA not supported.");
        if (imageType != 2 && imageType != 3) throw new FormatException("Unsupported TGA type.");

        width = ReadUInt16LE(tga, 12);
        height = ReadUInt16LE(tga, 14);
        if (width <= 0 || height <= 0) throw new FormatException("Invalid TGA dimensions.");

        var bpp = tga[16];
        if (bpp != 8 && bpp != 24 && bpp != 32) throw new FormatException("Unsupported TGA bit depth.");

        var descriptor = tga[17];
        var originTop = (descriptor & 0x20) != 0;

        var dataOffset = 18 + idLength;
        var bytesPerPixel = bpp / 8;
        var rowStride = width * bytesPerPixel;
        var required = dataOffset + rowStride * height;
        if (required > tga.Length) throw new FormatException("Truncated TGA data.");

        var rgba = new byte[width * height * 4];

        for (var y = 0; y < height; y++) {
            var srcRow = originTop ? y : (height - 1 - y);
            var src = dataOffset + srcRow * rowStride;
            var dst = y * width * 4;
            if (bpp == 8) {
                for (var x = 0; x < width; x++) {
                    var v = tga[src + x];
                    rgba[dst++] = v;
                    rgba[dst++] = v;
                    rgba[dst++] = v;
                    rgba[dst++] = 255;
                }
                continue;
            }

            if (bpp == 24) {
                for (var x = 0; x < width; x++) {
                    var b = tga[src + x * 3 + 0];
                    var g = tga[src + x * 3 + 1];
                    var r = tga[src + x * 3 + 2];
                    rgba[dst++] = r;
                    rgba[dst++] = g;
                    rgba[dst++] = b;
                    rgba[dst++] = 255;
                }
                continue;
            }

            for (var x = 0; x < width; x++) {
                var b = tga[src + x * 4 + 0];
                var g = tga[src + x * 4 + 1];
                var r = tga[src + x * 4 + 2];
                var a = tga[src + x * 4 + 3];
                rgba[dst++] = r;
                rgba[dst++] = g;
                rgba[dst++] = b;
                rgba[dst++] = a;
            }
        }

        return rgba;
    }

    internal static bool LooksLikeTga(ReadOnlySpan<byte> data) {
        if (data.Length < 18) return false;
        var colorMapType = data[1];
        var imageType = data[2];
        if (colorMapType != 0) return false;
        if (imageType != 2 && imageType != 3) return false;
        var w = ReadUInt16LE(data, 12);
        var h = ReadUInt16LE(data, 14);
        if (w == 0 || h == 0) return false;
        var bpp = data[16];
        return bpp is 8 or 24 or 32;
    }

    private static ushort ReadUInt16LE(ReadOnlySpan<byte> data, int offset) {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }
}
