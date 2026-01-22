using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace CodeGlyphX.Tests.TestHelpers;

internal static class PngTestDecoder {
    public static (byte[] rgba, int width, int height, int stride) DecodeRgba32(byte[] png) {
        if (png.Length < 8) throw new FormatException("PNG too small.");
        ReadOnlySpan<byte> sig = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (!png.AsSpan(0, 8).SequenceEqual(sig)) throw new FormatException("Invalid PNG signature.");

        var offset = 8;
        var width = 0;
        var height = 0;
        var idat = new List<byte>();
        byte[]? palette = null;
        byte bitDepth = 0;
        byte colorType = 0;

        while (offset + 8 <= png.Length) {
            var len = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset, 4));
            offset += 4;
            var type = png.AsSpan(offset, 4);
            offset += 4;
            if (offset + len + 4 > png.Length) throw new FormatException("Invalid chunk length.");

            var data = png.AsSpan(offset, (int)len);
            offset += (int)len;
            offset += 4; // CRC

            if (type.SequenceEqual("IHDR"u8)) {
                width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.Slice(0, 4));
                height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4, 4));
                bitDepth = data[8];
                colorType = data[9];
                var compression = data[10];
                var filter = data[11];
                var interlace = data[12];
                if (compression != 0 || filter != 0 || interlace != 0)
                    throw new FormatException("Unexpected IHDR parameters.");
                if (bitDepth != 1 && bitDepth != 8)
                    throw new FormatException("Unexpected IHDR parameters.");
                if (colorType != 0 && colorType != 3 && colorType != 6)
                    throw new FormatException("Unexpected IHDR parameters.");
            } else if (type.SequenceEqual("PLTE"u8)) {
                palette = data.ToArray();
            } else if (type.SequenceEqual("IDAT"u8)) {
                idat.AddRange(data.ToArray());
            } else if (type.SequenceEqual("IEND"u8)) {
                break;
            }
        }

        if (width <= 0 || height <= 0) throw new FormatException("Missing IHDR.");
        if (idat.Count < 6) throw new FormatException("Missing IDAT.");

        var rowBytes = bitDepth == 1 ? (width + 7) / 8 : width * ChannelsFor(colorType);
        var scanlines = InflateZlibStored(CollectionsMarshal.AsSpan(idat), expectedLen: height * (rowBytes + 1));
        var stride = width * 4;
        var rgba = new byte[height * stride];

        for (var y = 0; y < height; y++) {
            var rowStart = y * (rowBytes + 1);
            if (scanlines[rowStart] != 0) throw new FormatException("Unexpected PNG filter.");
            DecodeRow(scanlines.AsSpan(rowStart + 1, rowBytes), rgba, y, width, stride, bitDepth, colorType, palette);
        }

        return (rgba, width, height, stride);
    }

    private static int ChannelsFor(byte colorType) {
        return colorType switch {
            0 => 1,
            3 => 1,
            6 => 4,
            _ => throw new FormatException("Unexpected IHDR parameters.")
        };
    }

    private static void DecodeRow(ReadOnlySpan<byte> packed, byte[] rgba, int row, int width, int stride, byte bitDepth, byte colorType, byte[]? palette) {
        var rowOffset = row * stride;
        if (colorType == 6 && bitDepth == 8) {
            packed.CopyTo(rgba.AsSpan(rowOffset, width * 4));
            return;
        }
        if (colorType == 0 && bitDepth == 8) {
            for (var x = 0; x < width; x++) {
                var v = packed[x];
                var dst = rowOffset + x * 4;
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            }
            return;
        }
        if (colorType == 0 && bitDepth == 1) {
            for (var x = 0; x < width; x++) {
                var b = packed[x >> 3];
                var bit = (b >> (7 - (x & 7))) & 1;
                var v = (byte)(bit == 0 ? 0 : 255);
                var dst = rowOffset + x * 4;
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            }
            return;
        }
        if (colorType == 3 && palette is not null) {
            var entryCount = palette.Length / 3;
            if (bitDepth == 1) {
                for (var x = 0; x < width; x++) {
                    var b = packed[x >> 3];
                    var idx = (b >> (7 - (x & 7))) & 1;
                    if (idx >= entryCount) throw new FormatException("Unexpected IHDR parameters.");
                    var p = idx * 3;
                    var dst = rowOffset + x * 4;
                    rgba[dst + 0] = palette[p + 0];
                    rgba[dst + 1] = palette[p + 1];
                    rgba[dst + 2] = palette[p + 2];
                    rgba[dst + 3] = 255;
                }
                return;
            }
            if (bitDepth == 8) {
                for (var x = 0; x < width; x++) {
                    var idx = packed[x];
                    if (idx >= entryCount) throw new FormatException("Unexpected IHDR parameters.");
                    var p = idx * 3;
                    var dst = rowOffset + x * 4;
                    rgba[dst + 0] = palette[p + 0];
                    rgba[dst + 1] = palette[p + 1];
                    rgba[dst + 2] = palette[p + 2];
                    rgba[dst + 3] = 255;
                }
                return;
            }
        }

        throw new FormatException("Unexpected IHDR parameters.");
    }

    private static byte[] InflateZlibStored(ReadOnlySpan<byte> zlib, int expectedLen) {
        if (zlib.Length < 6) throw new FormatException("Invalid zlib stream.");
        // Minimal: accept common header 0x78 0x01 but don't require it.
        var offset = 2;

        var output = new byte[expectedLen];
        var outPos = 0;

        while (true) {
            if (offset >= zlib.Length) throw new FormatException("Truncated deflate stream.");
            var header = zlib[offset++];
            var bfinal = (header & 1) != 0;
            var btype = (header >> 1) & 0x03;
            if (btype != 0) throw new FormatException("Only stored blocks are supported.");

            if (offset + 4 > zlib.Length) throw new FormatException("Truncated stored block header.");
            var len = zlib[offset] | (zlib[offset + 1] << 8);
            var nlen = zlib[offset + 2] | (zlib[offset + 3] << 8);
            offset += 4;
            if (((len ^ 0xFFFF) & 0xFFFF) != nlen) throw new FormatException("Stored block length mismatch.");

            if (offset + len > zlib.Length) throw new FormatException("Truncated stored block data.");
            zlib.Slice(offset, len).CopyTo(output.AsSpan(outPos));
            offset += len;
            outPos += len;

            if (bfinal) break;
        }

        if (outPos != expectedLen) throw new FormatException("Unexpected decompressed length.");
        return output;
    }
}
