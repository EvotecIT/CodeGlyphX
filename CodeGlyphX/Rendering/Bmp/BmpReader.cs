using System;

namespace CodeGlyphX.Rendering.Bmp;

/// <summary>
/// Decodes BMP images to RGBA buffers (uncompressed 24/32/8-bit).
/// </summary>
public static class BmpReader {
    /// <summary>
    /// Decodes a BMP image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> bmp, out int width, out int height) {
        if (bmp.Length < 54) throw new FormatException("Invalid BMP header.");
        if (bmp[0] != (byte)'B' || bmp[1] != (byte)'M') throw new FormatException("Invalid BMP signature.");

        var dataOffset = (int)ReadUInt32LE(bmp, 10);
        var headerSize = (int)ReadUInt32LE(bmp, 14);
        if (headerSize < 40) throw new FormatException("Unsupported BMP header.");

        var w = ReadInt32LE(bmp, 18);
        var h = ReadInt32LE(bmp, 22);
        if (w == 0 || h == 0) throw new FormatException("Invalid BMP dimensions.");

        var topDown = h < 0;
        if (topDown) h = -h;

        if (w < 0 || h < 0) throw new FormatException("Invalid BMP dimensions.");

        var planes = ReadUInt16LE(bmp, 26);
        if (planes != 1) throw new FormatException("Unsupported BMP planes.");

        var bpp = ReadUInt16LE(bmp, 28);
        var compression = ReadUInt32LE(bmp, 30);
        if (compression != 0 && !(compression == 3 && bpp == 32)) {
            throw new FormatException("Unsupported BMP compression.");
        }

        width = w;
        height = h;

        if (dataOffset < 0 || dataOffset >= bmp.Length) throw new FormatException("Invalid BMP pixel data offset.");

        if (bpp == 24) {
            var rowStride = ((width * 3 + 3) / 4) * 4;
            var required = dataOffset + rowStride * height;
            if (required > bmp.Length) throw new FormatException("Truncated BMP data.");

            var rgba = new byte[width * height * 4];
            for (var y = 0; y < height; y++) {
                var srcRow = topDown ? y : (height - 1 - y);
                var src = dataOffset + srcRow * rowStride;
                var dst = y * width * 4;
                for (var x = 0; x < width; x++) {
                    var b = bmp[src + x * 3 + 0];
                    var g = bmp[src + x * 3 + 1];
                    var r = bmp[src + x * 3 + 2];
                    rgba[dst++] = r;
                    rgba[dst++] = g;
                    rgba[dst++] = b;
                    rgba[dst++] = 255;
                }
            }
            return rgba;
        }

        if (bpp == 32) {
            var rowStride = width * 4;
            var required = dataOffset + rowStride * height;
            if (required > bmp.Length) throw new FormatException("Truncated BMP data.");

            var rgba = new byte[width * height * 4];
            for (var y = 0; y < height; y++) {
                var srcRow = topDown ? y : (height - 1 - y);
                var src = dataOffset + srcRow * rowStride;
                var dst = y * width * 4;
                for (var x = 0; x < width; x++) {
                    var b = bmp[src + x * 4 + 0];
                    var g = bmp[src + x * 4 + 1];
                    var r = bmp[src + x * 4 + 2];
                    var a = bmp[src + x * 4 + 3];
                    rgba[dst++] = r;
                    rgba[dst++] = g;
                    rgba[dst++] = b;
                    rgba[dst++] = a;
                }
            }
            return rgba;
        }

        if (bpp == 8) {
            var colorsUsed = headerSize >= 44 ? ReadUInt32LE(bmp, 46) : 0;
            var paletteCount = colorsUsed != 0 ? (int)colorsUsed : 256;
            var paletteOffset = 14 + headerSize;
            var paletteBytes = paletteCount * 4;
            if (paletteOffset + paletteBytes > bmp.Length) throw new FormatException("Invalid BMP palette.");

            var rowStride = ((width + 3) / 4) * 4;
            var required = dataOffset + rowStride * height;
            if (required > bmp.Length) throw new FormatException("Truncated BMP data.");

            var rgba = new byte[width * height * 4];
            for (var y = 0; y < height; y++) {
                var srcRow = topDown ? y : (height - 1 - y);
                var src = dataOffset + srcRow * rowStride;
                var dst = y * width * 4;
                for (var x = 0; x < width; x++) {
                    var idx = bmp[src + x];
                    var pal = paletteOffset + idx * 4;
                    var b = bmp[pal + 0];
                    var g = bmp[pal + 1];
                    var r = bmp[pal + 2];
                    rgba[dst++] = r;
                    rgba[dst++] = g;
                    rgba[dst++] = b;
                    rgba[dst++] = 255;
                }
            }
            return rgba;
        }

        throw new FormatException("Unsupported BMP bit depth.");
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
}
