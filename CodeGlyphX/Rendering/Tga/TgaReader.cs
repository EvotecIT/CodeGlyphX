using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Tga;

/// <summary>
/// Decodes TGA images to RGBA buffers (uncompressed + RLE, true-color/grayscale/color-mapped).
/// </summary>
public static class TgaReader {
    /// <summary>
    /// Decodes a TGA image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> tga, out int width, out int height) {
        DecodeGuards.EnsurePayloadWithinLimits(tga.Length, "TGA payload exceeds size limits.");
        if (tga.Length < 18) throw new FormatException("Invalid TGA header.");

        var idLength = tga[0];
        var colorMapType = tga[1];
        var imageType = tga[2];

        var isRle = imageType is 9 or 10 or 11;
        var isColorMapped = imageType is 1 or 9;
        var isTrueColor = imageType is 2 or 10;
        var isGrayscale = imageType is 3 or 11;

        if (!isColorMapped && !isTrueColor && !isGrayscale) {
            throw new FormatException("Unsupported TGA type.");
        }

        width = ReadUInt16LE(tga, 12);
        height = ReadUInt16LE(tga, 14);
        if (width <= 0 || height <= 0) throw new FormatException("Invalid TGA dimensions.");
        _ = DecodeGuards.EnsurePixelCount(width, height, "TGA dimensions exceed size limits.");

        var bpp = tga[16];
        if (isColorMapped) {
            if (bpp != 8 && bpp != 16) throw new FormatException("Unsupported TGA color-mapped index depth.");
        } else if (isGrayscale) {
            if (bpp != 8 && bpp != 16) throw new FormatException("Unsupported TGA grayscale depth.");
        } else {
            if (bpp != 16 && bpp != 24 && bpp != 32) throw new FormatException("Unsupported TGA bit depth.");
        }

        var descriptor = tga[17];
        var originTop = (descriptor & 0x20) != 0;

        var colorMapFirst = ReadUInt16LE(tga, 3);
        var colorMapLength = ReadUInt16LE(tga, 5);
        var colorMapDepth = tga[7];
        if (colorMapType != 0 && colorMapDepth is not (16 or 24 or 32)) {
            throw new FormatException("Unsupported TGA color map depth.");
        }
        var colorMapBytes = colorMapType != 0
            ? DecodeGuards.EnsureByteCount((long)colorMapLength * (colorMapDepth / 8), "Invalid TGA color map.")
            : 0;

        var dataOffset = 18 + idLength + colorMapBytes;
        if (dataOffset > tga.Length) throw new FormatException("Truncated TGA data.");

        var bytesPerPixel = bpp / 8;
        var rowStride = DecodeGuards.EnsureByteCount((long)width * bytesPerPixel, "TGA row exceeds size limits.");
        if (!isRle) {
            var required = (long)dataOffset + (long)rowStride * height;
            if (required > tga.Length) throw new FormatException("Truncated TGA data.");
        }

        var rgba = DecodeGuards.AllocateRgba32(width, height, "TGA dimensions exceed size limits.");

        if (isRle) {
            DecodeRle(tga.Slice(dataOffset), rgba, width, height, originTop, bytesPerPixel, bpp, isColorMapped, isGrayscale, tga, colorMapType, colorMapFirst, colorMapLength, colorMapDepth);
            return rgba;
        }

        for (var y = 0; y < height; y++) {
            var srcRow = originTop ? y : (height - 1 - y);
            var src = dataOffset + srcRow * rowStride;
            var dst = y * width * 4;
            for (var x = 0; x < width; x++) {
                WritePixel(rgba, dst, tga, src + x * bytesPerPixel, bpp, isColorMapped, isGrayscale, tga, colorMapType, colorMapFirst, colorMapLength, colorMapDepth);
                dst += 4;
            }
        }

        return rgba;
    }

    internal static bool LooksLikeTga(ReadOnlySpan<byte> data) {
        if (data.Length < 18) return false;
        var colorMapType = data[1];
        var imageType = data[2];
        if (imageType != 1 && imageType != 2 && imageType != 3 && imageType != 9 && imageType != 10 && imageType != 11) return false;
        if (colorMapType != 0 && colorMapType != 1) return false;
        var w = ReadUInt16LE(data, 12);
        var h = ReadUInt16LE(data, 14);
        if (w == 0 || h == 0) return false;
        var bpp = data[16];
        return bpp is 8 or 16 or 24 or 32;
    }

    private static void DecodeRle(
        ReadOnlySpan<byte> data,
        byte[] rgba,
        int width,
        int height,
        bool originTop,
        int bytesPerPixel,
        int bpp,
        bool isColorMapped,
        bool isGrayscale,
        ReadOnlySpan<byte> fullData,
        int colorMapType,
        int colorMapFirst,
        int colorMapLength,
        int colorMapDepth) {
        var pixelCount = DecodeGuards.EnsurePixelCount(width, height, "TGA dimensions exceed size limits.");
        var outIndex = 0;
        var pos = 0;
        while (outIndex < pixelCount && pos < data.Length) {
            var header = data[pos++];
            var runLength = (header & 0x7F) + 1;
            if ((header & 0x80) != 0) {
                if (pos + bytesPerPixel > data.Length) throw new FormatException("Truncated TGA RLE data.");
                var pixelOffset = pos;
                pos += bytesPerPixel;
                for (var i = 0; i < runLength && outIndex < pixelCount; i++) {
                    WriteRlePixel(rgba, outIndex++, width, height, originTop, data, pixelOffset, bpp, isColorMapped, isGrayscale, fullData, colorMapType, colorMapFirst, colorMapLength, colorMapDepth);
                }
            } else {
                var total = runLength * bytesPerPixel;
                if (pos + total > data.Length) throw new FormatException("Truncated TGA RLE data.");
                for (var i = 0; i < runLength && outIndex < pixelCount; i++) {
                    var pixelOffset = pos + i * bytesPerPixel;
                    WriteRlePixel(rgba, outIndex++, width, height, originTop, data, pixelOffset, bpp, isColorMapped, isGrayscale, fullData, colorMapType, colorMapFirst, colorMapLength, colorMapDepth);
                }
                pos += total;
            }
        }
        if (outIndex != pixelCount) throw new FormatException("Truncated TGA RLE data.");
    }

    private static void WriteRlePixel(
        byte[] rgba,
        int index,
        int width,
        int height,
        bool originTop,
        ReadOnlySpan<byte> data,
        int pixelOffset,
        int bpp,
        bool isColorMapped,
        bool isGrayscale,
        ReadOnlySpan<byte> fullData,
        int colorMapType,
        int colorMapFirst,
        int colorMapLength,
        int colorMapDepth) {
        var y = index / width;
        var x = index - y * width;
        var dstY = originTop ? y : (height - 1 - y);
        var dst = (dstY * width + x) * 4;
        WritePixel(rgba, dst, data, pixelOffset, bpp, isColorMapped, isGrayscale, fullData, colorMapType, colorMapFirst, colorMapLength, colorMapDepth);
    }

    private static void WritePixel(
        byte[] rgba,
        int dst,
        ReadOnlySpan<byte> data,
        int src,
        int bpp,
        bool isColorMapped,
        bool isGrayscale,
        ReadOnlySpan<byte> fullData,
        int colorMapType,
        int colorMapFirst,
        int colorMapLength,
        int colorMapDepth) {
        if (isColorMapped) {
            if (colorMapType == 0) throw new FormatException("Missing TGA color map.");
            var idx = bpp == 16 ? ReadUInt16LE(data, src) : data[src];
            var entry = idx - colorMapFirst;
            if ((uint)entry >= (uint)colorMapLength) {
                rgba[dst + 3] = 255;
                return;
            }
            var entryBytes = colorMapDepth / 8;
            var mapOffset = 18 + fullData[0] + entry * entryBytes;
            if ((long)mapOffset + entryBytes > fullData.Length) throw new FormatException("Invalid TGA color map.");
            if (colorMapDepth == 24) {
                rgba[dst + 0] = fullData[mapOffset + 2];
                rgba[dst + 1] = fullData[mapOffset + 1];
                rgba[dst + 2] = fullData[mapOffset + 0];
                rgba[dst + 3] = 255;
            } else if (colorMapDepth == 32) {
                rgba[dst + 0] = fullData[mapOffset + 2];
                rgba[dst + 1] = fullData[mapOffset + 1];
                rgba[dst + 2] = fullData[mapOffset + 0];
                rgba[dst + 3] = fullData[mapOffset + 3];
            } else if (colorMapDepth == 16) {
                var value = ReadUInt16LE(fullData, mapOffset);
                rgba[dst + 0] = Extract5(value >> 10);
                rgba[dst + 1] = Extract5(value >> 5);
                rgba[dst + 2] = Extract5(value);
                rgba[dst + 3] = (value & 0x8000) != 0 ? (byte)255 : (byte)0;
            } else {
                throw new FormatException("Unsupported TGA color map depth.");
            }
            return;
        }

        if (isGrayscale) {
            if (bpp == 8) {
                var v = data[src];
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            } else {
                var v = data[src];
                var a = data[src + 1];
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = a;
            }
            return;
        }

        if (bpp == 24) {
            rgba[dst + 0] = data[src + 2];
            rgba[dst + 1] = data[src + 1];
            rgba[dst + 2] = data[src + 0];
            rgba[dst + 3] = 255;
            return;
        }

        if (bpp == 32) {
            rgba[dst + 0] = data[src + 2];
            rgba[dst + 1] = data[src + 1];
            rgba[dst + 2] = data[src + 0];
            rgba[dst + 3] = data[src + 3];
            return;
        }

        var value16 = ReadUInt16LE(data, src);
        rgba[dst + 0] = Extract5(value16 >> 10);
        rgba[dst + 1] = Extract5(value16 >> 5);
        rgba[dst + 2] = Extract5(value16);
        rgba[dst + 3] = (value16 & 0x8000) != 0 ? (byte)255 : (byte)0;
    }

    private static byte Extract5(int value) {
        var v = value & 0x1F;
        return (byte)((v * 255 + 15) / 31);
    }

    private static ushort ReadUInt16LE(ReadOnlySpan<byte> data, int offset) {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }
}
