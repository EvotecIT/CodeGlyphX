using System;
using System.IO;
using System.IO.Compression;

namespace CodeGlyphX.Rendering.Png;

internal static class PngDecoder {
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static byte[] DecodeRgba32(byte[] png, out int width, out int height) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        if (png.Length < Signature.Length) throw new FormatException("Invalid PNG signature.");

        for (var i = 0; i < Signature.Length; i++) {
            if (png[i] != Signature[i]) throw new FormatException("Invalid PNG signature.");
        }

        width = 0;
        height = 0;
        var bitDepth = 0;
        var colorType = 0;
        var compression = 0;
        var filter = 0;
        var interlace = 0;

        var idat = new MemoryStream();
        var offset = Signature.Length;

        while (offset + 8 <= png.Length) {
            var len = ReadUInt32BE(png, offset);
            offset += 4;
            if (offset + 4 > png.Length) throw new FormatException("Invalid PNG chunk.");
            var typeOffset = offset;
            offset += 4;
            if (offset + len + 4 > png.Length) throw new FormatException("Invalid PNG chunk length.");
            var dataOffset = offset;
            offset += (int)len;
            offset += 4; // CRC

            if (MatchType(png, typeOffset, "IHDR")) {
                if (len < 13) throw new FormatException("Invalid IHDR chunk.");
                width = (int)ReadUInt32BE(png, dataOffset);
                height = (int)ReadUInt32BE(png, dataOffset + 4);
                bitDepth = png[dataOffset + 8];
                colorType = png[dataOffset + 9];
                compression = png[dataOffset + 10];
                filter = png[dataOffset + 11];
                interlace = png[dataOffset + 12];
            } else if (MatchType(png, typeOffset, "IDAT")) {
                idat.Write(png, dataOffset, (int)len);
            } else if (MatchType(png, typeOffset, "IEND")) {
                break;
            }
        }

        if (width <= 0 || height <= 0) throw new FormatException("Missing IHDR.");
        if (bitDepth != 8) throw new FormatException("Only 8-bit PNGs are supported.");
        if (compression != 0 || filter != 0) throw new FormatException("Unsupported PNG compression/filter method.");
        if (interlace != 0) throw new FormatException("Interlaced PNGs are not supported.");

        var channels = colorType switch {
            2 => 3,
            6 => 4,
            _ => throw new FormatException("Unsupported PNG color type."),
        };

        var rowBytes = checked(width * channels);
        var expected = checked(height * (rowBytes + 1));
        var scanlines = new byte[expected];

        if (idat.Length == 0) throw new FormatException("Missing IDAT.");

        idat.Position = 0;
        using (var z = CreateZLibStream(idat)) {
            ReadExact(z, scanlines);
        }

        var raw = new byte[checked(height * rowBytes)];
        Unfilter(scanlines, raw, width, height, channels);

        if (channels == 4) return raw;

        var rgba = new byte[checked(width * height * 4)];
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

    private static void ReadExact(Stream s, byte[] buffer) {
        var offset = 0;
        while (offset < buffer.Length) {
            var read = s.Read(buffer, offset, buffer.Length - offset);
            if (read <= 0) throw new FormatException("Truncated PNG data.");
            offset += read;
        }
    }

    private static void Unfilter(byte[] scanlines, byte[] raw, int width, int height, int bpp) {
        var rowBytes = width * bpp;
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
