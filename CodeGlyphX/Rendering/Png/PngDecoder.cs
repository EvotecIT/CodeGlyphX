using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;

namespace CodeGlyphX.Rendering.Png;

internal static class PngDecoder {
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static byte[] DecodeRgba32(byte[] png, out int width, out int height) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        return DecodeRgba32(png, 0, png.Length, out width, out height);
    }

    public static byte[] DecodeRgba32(byte[] png, int offset, int length, out int width, out int height) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        if (offset < 0 || length < 0 || offset + length > png.Length) throw new ArgumentOutOfRangeException(nameof(length));
        if (length < Signature.Length) throw new FormatException("Invalid PNG signature.");

        for (var i = 0; i < Signature.Length; i++) {
            if (png[offset + i] != Signature[i]) throw new FormatException("Invalid PNG signature.");
        }

        width = 0;
        height = 0;
        var bitDepth = 0;
        var colorType = 0;
        var compression = 0;
        var filter = 0;
        var interlace = 0;

        var idat = new MemoryStream();
        byte[]? palette = null;
        var localOffset = Signature.Length;
        var end = length;

        while (localOffset + 8 <= end) {
            var len = ReadUInt32BE(png, offset + localOffset);
            localOffset += 4;
            if (localOffset + 4 > end) throw new FormatException("Invalid PNG chunk.");
            var typeOffset = offset + localOffset;
            localOffset += 4;
            if (localOffset + len + 4 > end) throw new FormatException("Invalid PNG chunk length.");
            var dataOffset = offset + localOffset;
            localOffset += (int)len;
            localOffset += 4; // CRC

            if (MatchType(png, typeOffset, "IHDR")) {
                if (len < 13) throw new FormatException("Invalid IHDR chunk.");
                width = (int)ReadUInt32BE(png, dataOffset);
                height = (int)ReadUInt32BE(png, dataOffset + 4);
                bitDepth = png[dataOffset + 8];
                colorType = png[dataOffset + 9];
                compression = png[dataOffset + 10];
                filter = png[dataOffset + 11];
                interlace = png[dataOffset + 12];
            } else if (MatchType(png, typeOffset, "PLTE")) {
                palette = new byte[len];
                Buffer.BlockCopy(png, dataOffset, palette, 0, (int)len);
            } else if (MatchType(png, typeOffset, "IDAT")) {
                idat.Write(png, dataOffset, (int)len);
            } else if (MatchType(png, typeOffset, "IEND")) {
                break;
            }
        }

        if (width <= 0 || height <= 0) throw new FormatException("Missing IHDR.");
        if (bitDepth != 8 && bitDepth != 1) throw new FormatException("Only 1-bit or 8-bit PNGs are supported.");
        if (compression != 0 || filter != 0) throw new FormatException("Unsupported PNG compression/filter method.");
        if (interlace != 0) throw new FormatException("Interlaced PNGs are not supported.");

        var channels = colorType switch {
            0 => 1,
            2 => 3,
            3 => 1,
            6 => 4,
            _ => throw new FormatException("Unsupported PNG color type."),
        };
        if (colorType == 3 && (palette is null || palette.Length < 3)) {
            throw new FormatException("Missing PNG palette.");
        }

        var rowBytes = bitDepth == 1 ? (width + 7) / 8 : checked(width * channels);
        var bytesPerPixel = (bitDepth * channels + 7) / 8;
        var expected = checked(height * (rowBytes + 1));
        var scanlines = ArrayPool<byte>.Shared.Rent(expected);

        if (idat.Length == 0) throw new FormatException("Missing IDAT.");

        try {
            idat.Position = 0;
            using (var z = CreateZLibStream(idat)) {
                ReadExact(z, scanlines, expected);
            }

            var raw = new byte[checked(height * rowBytes)];
            Unfilter(scanlines.AsSpan(0, expected), raw, rowBytes, height, bytesPerPixel);

            return ExpandToRgba(raw, width, height, colorType, bitDepth, palette);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static void ReadExact(Stream s, byte[] buffer, int length) {
        var offset = 0;
        while (offset < length) {
            var read = s.Read(buffer, offset, length - offset);
            if (read <= 0) throw new FormatException("Truncated PNG data.");
            offset += read;
        }
    }

    private static void Unfilter(ReadOnlySpan<byte> scanlines, byte[] raw, int rowBytes, int height, int bpp) {
        var src = 0;
        var dst = 0;

        for (var y = 0; y < height; y++) {
            var filter = scanlines[src++];
            for (var x = 0; x < rowBytes; x++) {
                var cur = scanlines[src++];
                var a = x >= bpp ? raw[dst + x - bpp] : 0;
                var b = y > 0 ? raw[dst - rowBytes + x] : 0;
                var c = y > 0 && x >= bpp ? raw[dst - rowBytes + x - bpp] : 0;
                int val = filter switch {
                    0 => cur,
                    1 => cur + a,
                    2 => cur + b,
                    3 => cur + ((a + b) >> 1),
                    4 => cur + Paeth(a, b, c),
                    _ => throw new FormatException("Unsupported PNG filter."),
                };
                raw[dst + x] = (byte)(val & 0xFF);
            }
            dst += rowBytes;
        }
    }

    private static byte[] ExpandToRgba(byte[] raw, int width, int height, int colorType, int bitDepth, byte[]? palette) {
        if (colorType == 6 && bitDepth == 8) return raw;

        var rgba = new byte[checked(width * height * 4)];
        if (colorType == 2 && bitDepth == 8) {
            for (var i = 0; i < width * height; i++) {
                var src = i * 3;
                var dst = i * 4;
                rgba[dst + 0] = raw[src + 0];
                rgba[dst + 1] = raw[src + 1];
                rgba[dst + 2] = raw[src + 2];
                rgba[dst + 3] = 255;
            }
            return rgba;
        }

        if (colorType == 0 && bitDepth == 8) {
            for (var i = 0; i < width * height; i++) {
                var v = raw[i];
                var dst = i * 4;
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            }
            return rgba;
        }

        if (colorType == 0 && bitDepth == 1) {
            for (var y = 0; y < height; y++) {
                var rowStart = y * ((width + 7) / 8);
                for (var x = 0; x < width; x++) {
                    var b = raw[rowStart + (x >> 3)];
                    var bit = (b >> (7 - (x & 7))) & 1;
                    var v = (byte)(bit == 0 ? 0 : 255);
                    var dst = (y * width + x) * 4;
                    rgba[dst + 0] = v;
                    rgba[dst + 1] = v;
                    rgba[dst + 2] = v;
                    rgba[dst + 3] = 255;
                }
            }
            return rgba;
        }

        if (colorType == 3 && palette is not null) {
            var entryCount = palette.Length / 3;
            if (bitDepth == 8) {
                for (var i = 0; i < width * height; i++) {
                    var idx = raw[i];
                    if (idx >= entryCount) throw new FormatException("Palette index out of range.");
                    var p = idx * 3;
                    var dst = i * 4;
                    rgba[dst + 0] = palette[p + 0];
                    rgba[dst + 1] = palette[p + 1];
                    rgba[dst + 2] = palette[p + 2];
                    rgba[dst + 3] = 255;
                }
                return rgba;
            }
            if (bitDepth == 1) {
                for (var y = 0; y < height; y++) {
                    var rowStart = y * ((width + 7) / 8);
                    for (var x = 0; x < width; x++) {
                        var b = raw[rowStart + (x >> 3)];
                        var idx = (b >> (7 - (x & 7))) & 1;
                        if (idx >= entryCount) throw new FormatException("Palette index out of range.");
                        var p = idx * 3;
                        var dst = (y * width + x) * 4;
                        rgba[dst + 0] = palette[p + 0];
                        rgba[dst + 1] = palette[p + 1];
                        rgba[dst + 2] = palette[p + 2];
                        rgba[dst + 3] = 255;
                    }
                }
                return rgba;
            }
        }

        throw new FormatException("Unsupported PNG color type.");
    }

    private static int Paeth(int a, int b, int c) {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        return pb <= pc ? b : c;
    }

    private static uint ReadUInt32BE(byte[] buffer, int offset) {
        return ((uint)buffer[offset] << 24) |
               ((uint)buffer[offset + 1] << 16) |
               ((uint)buffer[offset + 2] << 8) |
               buffer[offset + 3];
    }

    private static bool MatchType(byte[] buffer, int offset, string type) {
        return offset + 4 <= buffer.Length
               && buffer[offset] == (byte)type[0]
               && buffer[offset + 1] == (byte)type[1]
               && buffer[offset + 2] == (byte)type[2]
               && buffer[offset + 3] == (byte)type[3];
    }

    private static Stream CreateZLibStream(Stream source) {
#if NET8_0_OR_GREATER
        return new ZLibStream(source, CompressionMode.Decompress, leaveOpen: true);
#else
        var data = source is MemoryStream ms ? ms.ToArray() : ReadAllBytes(source);
        if (data.Length < 6) throw new FormatException("Invalid zlib stream.");
        return new DeflateStream(new MemoryStream(data, 2, data.Length - 6, writable: false), CompressionMode.Decompress, leaveOpen: true);
#endif
    }

    private static byte[] ReadAllBytes(Stream source) {
        using var ms = new MemoryStream();
        source.CopyTo(ms);
        return ms.ToArray();
    }
}
