using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Bmp;

/// <summary>
/// Decodes BMP images to RGBA buffers (1/4/8-bit palette, 16/24/32-bit, RLE4/RLE8, bitfields).
/// </summary>
public static class BmpReader {
    private const uint BiRgb = 0;
    private const uint BiRle8 = 1;
    private const uint BiRle4 = 2;
    private const uint BiBitFields = 3;
    private const uint BiAlphaBitFields = 6;

    private const string BmpDimensionsLimitMessage = "BMP dimensions exceed size limits.";

    /// <summary>
    /// Decodes a BMP image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> bmp, out int width, out int height) {
        DecodeGuards.EnsurePayloadWithinLimits(bmp.Length, "BMP payload exceeds size limits.");
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
        if (compression != BiRgb && compression != BiRle8 && compression != BiRle4 && compression != BiBitFields && compression != BiAlphaBitFields) {
            throw new FormatException("Unsupported BMP compression.");
        }

        width = w;
        height = h;
        _ = DecodeGuards.EnsurePixelCount(width, height, BmpDimensionsLimitMessage);

        if (dataOffset < 0 || dataOffset >= bmp.Length) throw new FormatException("Invalid BMP pixel data offset.");

        var maskOffset = 14 + 40;
        var hasBitFields = compression == BiBitFields || compression == BiAlphaBitFields;
        uint redMask = 0;
        uint greenMask = 0;
        uint blueMask = 0;
        uint alphaMask = 0;
        if (hasBitFields || headerSize >= 52) {
            if (maskOffset + 12 <= bmp.Length) {
                redMask = ReadUInt32LE(bmp, maskOffset);
                greenMask = ReadUInt32LE(bmp, maskOffset + 4);
                blueMask = ReadUInt32LE(bmp, maskOffset + 8);
                if (maskOffset + 16 <= bmp.Length && (headerSize >= 56 || compression == BiAlphaBitFields)) {
                    alphaMask = ReadUInt32LE(bmp, maskOffset + 12);
                }
            }
        }

        if (bpp == 24) {
            var rowStride = RowStride(width, bpp);
            var required = (long)dataOffset + (long)rowStride * height;
            if (required > bmp.Length) throw new FormatException("Truncated BMP data.");

            var rgba = DecodeGuards.AllocateRgba32(width, height, BmpDimensionsLimitMessage);
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
            var rowStride = RowStride(width, bpp);
            var required = (long)dataOffset + (long)rowStride * height;
            if (required > bmp.Length) throw new FormatException("Truncated BMP data.");

            var rgba = DecodeGuards.AllocateRgba32(width, height, BmpDimensionsLimitMessage);
            for (var y = 0; y < height; y++) {
                var srcRow = topDown ? y : (height - 1 - y);
                var src = dataOffset + srcRow * rowStride;
                var dst = y * width * 4;
                for (var x = 0; x < width; x++) {
                    if (hasBitFields || (redMask != 0 && greenMask != 0 && blueMask != 0)) {
                        var value = ReadUInt32LE(bmp, src + x * 4);
                        rgba[dst++] = ExtractToByte(value, redMask);
                        rgba[dst++] = ExtractToByte(value, greenMask);
                        rgba[dst++] = ExtractToByte(value, blueMask);
                        var a = alphaMask != 0 ? ExtractToByte(value, alphaMask) : (byte)255;
                        rgba[dst++] = a;
                    } else {
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
            }
            return rgba;
        }

        if (bpp == 16) {
            var rowStride = RowStride(width, bpp);
            var required = (long)dataOffset + (long)rowStride * height;
            if (required > bmp.Length) throw new FormatException("Truncated BMP data.");
            if (!hasBitFields && redMask == 0 && greenMask == 0 && blueMask == 0) {
                redMask = 0x7C00;
                greenMask = 0x03E0;
                blueMask = 0x001F;
                alphaMask = 0;
            }

            var rgba = DecodeGuards.AllocateRgba32(width, height, BmpDimensionsLimitMessage);
            for (var y = 0; y < height; y++) {
                var srcRow = topDown ? y : (height - 1 - y);
                var src = dataOffset + srcRow * rowStride;
                var dst = y * width * 4;
                for (var x = 0; x < width; x++) {
                    var value = ReadUInt16LE(bmp, src + x * 2);
                    rgba[dst++] = ExtractToByte(value, redMask);
                    rgba[dst++] = ExtractToByte(value, greenMask);
                    rgba[dst++] = ExtractToByte(value, blueMask);
                    rgba[dst++] = alphaMask != 0 ? ExtractToByte(value, alphaMask) : (byte)255;
                }
            }
            return rgba;
        }

        if (bpp == 1 || bpp == 4 || bpp == 8) {
            var colorsUsed = headerSize >= 44 ? ReadUInt32LE(bmp, 46) : 0;
            var paletteCount = colorsUsed != 0 ? (int)colorsUsed : 1 << bpp;
            if (paletteCount <= 0 || paletteCount > (1 << bpp) || paletteCount > 256) {
                throw new FormatException("Invalid BMP palette.");
            }
            var paletteOffset = 14 + headerSize + (hasBitFields ? 12 : 0);
            var paletteBytes = DecodeGuards.EnsureByteCount((long)paletteCount * 4, "Invalid BMP palette.");
            if ((long)paletteOffset + paletteBytes > bmp.Length) throw new FormatException("Invalid BMP palette.");

            var rgba = DecodeGuards.AllocateRgba32(width, height, BmpDimensionsLimitMessage);
            if (compression == BiRle8 || compression == BiRle4) {
                var indices = DecodeGuards.AllocatePixelBuffer(width, height, BmpDimensionsLimitMessage);
                if (compression == BiRle8) {
                    DecodeRle8(bmp.Slice(dataOffset), indices, width, height, topDown);
                } else {
                    DecodeRle4(bmp.Slice(dataOffset), indices, width, height, topDown);
                }

                for (var y = 0; y < height; y++) {
                    var dst = y * width * 4;
                    var srcIndex = y * width;
                    for (var x = 0; x < width; x++) {
                        var idx = indices[srcIndex + x];
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

            var rowStride = RowStride(width, bpp);
            var required = (long)dataOffset + (long)rowStride * height;
            if (required > bmp.Length) throw new FormatException("Truncated BMP data.");
            for (var y = 0; y < height; y++) {
                var srcRow = topDown ? y : (height - 1 - y);
                var src = dataOffset + srcRow * rowStride;
                var dst = y * width * 4;
                for (var x = 0; x < width; x++) {
                    var idx = ReadPackedIndex(bmp, src, x, bpp);
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

    private static int RowStride(int width, int bpp) {
        var rowBits = (long)width * bpp;
        var stride = ((rowBits + 31) / 32) * 4;
        return DecodeGuards.EnsureByteCount(stride, "BMP row exceeds size limits.");
    }

    private static byte ExtractToByte(uint value, uint mask) {
        if (mask == 0) return 0;
        var shift = 0;
        while (((mask >> shift) & 1) == 0 && shift < 32) shift++;
        var bits = 0;
        while (((mask >> (shift + bits)) & 1) == 1 && (shift + bits) < 32) bits++;
        if (bits == 0) return 0;
        var raw = (value & mask) >> shift;
        var max = (1u << bits) - 1;
        return (byte)((raw * 255 + (max / 2)) / max);
    }

    private static byte ReadPackedIndex(ReadOnlySpan<byte> data, int rowStart, int x, int bpp) {
        if (bpp == 8) return data[rowStart + x];
        if (bpp == 4) {
            var b = data[rowStart + (x >> 1)];
            return (byte)((x & 1) == 0 ? (b >> 4) & 0xF : b & 0xF);
        }
        var bit = (data[rowStart + (x >> 3)] >> (7 - (x & 7))) & 1;
        return (byte)bit;
    }

    private static void DecodeRle8(ReadOnlySpan<byte> data, byte[] indices, int width, int height, bool topDown) {
        var x = 0;
        var y = 0;
        var pos = 0;
        while (pos + 1 < data.Length && y < height) {
            var count = data[pos++];
            var value = data[pos++];
            if (count > 0) {
                for (var i = 0; i < count; i++) {
                    if (x >= width) {
                        x = 0;
                        y++;
                        if (y >= height) break;
                    }
                    var row = topDown ? y : (height - 1 - y);
                    indices[row * width + x] = value;
                    x++;
                }
                continue;
            }

            if (value == 0) {
                x = 0;
                y++;
            } else if (value == 1) {
                break;
            } else if (value == 2) {
                if (pos + 1 >= data.Length) break;
                var dx = data[pos++];
                var dy = data[pos++];
                x += dx;
                y += dy;
            } else {
                var absolute = value;
                for (var i = 0; i < absolute && pos < data.Length; i++) {
                    if (x >= width) {
                        x = 0;
                        y++;
                        if (y >= height) break;
                    }
                    var row = topDown ? y : (height - 1 - y);
                    indices[row * width + x] = data[pos++];
                    x++;
                }
                if ((absolute & 1) == 1) {
                    pos++;
                }
            }
        }
    }

    private static void DecodeRle4(ReadOnlySpan<byte> data, byte[] indices, int width, int height, bool topDown) {
        var x = 0;
        var y = 0;
        var pos = 0;
        while (pos + 1 < data.Length && y < height) {
            var count = data[pos++];
            var value = data[pos++];
            if (count > 0) {
                var hi = (byte)(value >> 4);
                var lo = (byte)(value & 0x0F);
                for (var i = 0; i < count; i++) {
                    if (x >= width) {
                        x = 0;
                        y++;
                        if (y >= height) break;
                    }
                    var idx = (i & 1) == 0 ? hi : lo;
                    var row = topDown ? y : (height - 1 - y);
                    indices[row * width + x] = idx;
                    x++;
                }
                continue;
            }

            if (value == 0) {
                x = 0;
                y++;
            } else if (value == 1) {
                break;
            } else if (value == 2) {
                if (pos + 1 >= data.Length) break;
                var dx = data[pos++];
                var dy = data[pos++];
                x += dx;
                y += dy;
            } else {
                var absolute = value;
                var bytes = (absolute + 1) / 2;
                for (var i = 0; i < absolute && pos < data.Length; i++) {
                    if (x >= width) {
                        x = 0;
                        y++;
                        if (y >= height) break;
                    }
                    var b = data[pos + (i >> 1)];
                    var idx = (i & 1) == 0 ? (byte)(b >> 4) : (byte)(b & 0x0F);
                    var row = topDown ? y : (height - 1 - y);
                    indices[row * width + x] = idx;
                    x++;
                }
                pos += bytes;
                if ((bytes & 1) == 1) {
                    pos++;
                }
            }
        }
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
