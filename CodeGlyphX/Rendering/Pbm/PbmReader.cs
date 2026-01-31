using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Pbm;

/// <summary>
/// Decodes PBM (P4/P1) images to RGBA buffers.
/// </summary>
public static class PbmReader {
    private const int MaxDimension = 16384;

    /// <summary>
    /// Decodes a PBM image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> pbm, out int width, out int height) {
        DecodeGuards.EnsurePayloadWithinLimits(pbm.Length, "PBM payload exceeds size limits.");
        if (pbm.Length < 2) throw new FormatException("Invalid PBM data.");
        if (pbm[0] != (byte)'P') throw new FormatException("Invalid PBM signature.");
        var format = pbm[1];
        if (format != (byte)'4' && format != (byte)'1') throw new FormatException("Unsupported PBM format.");

        var pos = 2;
        width = ReadIntToken(pbm, ref pos);
        height = ReadIntToken(pbm, ref pos);
        if (width <= 0 || height <= 0) throw new FormatException("Invalid PBM dimensions.");
        if (width > MaxDimension || height > MaxDimension) throw new FormatException("PBM dimensions are too large.");

        SkipWhitespaceAndComments(pbm, ref pos);

        var rgba = DecodeGuards.AllocateRgba32(width, height, "PBM dimensions exceed size limits.");

        if (format == (byte)'1') {
            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width; x++) {
                    var bit = ReadBitAscii(pbm, ref pos) != 0;
                    var color = bit ? (byte)0 : (byte)255;
                    var dst = (y * width + x) * 4;
                    rgba[dst + 0] = color;
                    rgba[dst + 1] = color;
                    rgba[dst + 2] = color;
                    rgba[dst + 3] = 255;
                }
            }
            return rgba;
        }

        var rowBytes = (width + 7) / 8;
        var required = (long)pos + (long)rowBytes * height;
        if (required > pbm.Length) throw new FormatException("Truncated PBM data.");

        for (var y = 0; y < height; y++) {
            var rowStart = pos + y * rowBytes;
            for (var x = 0; x < width; x++) {
                var b = pbm[rowStart + (x >> 3)];
                var bit = (b >> (7 - (x & 7))) & 1;
                var color = bit == 1 ? (byte)0 : (byte)255;
                var dst = (y * width + x) * 4;
                rgba[dst + 0] = color;
                rgba[dst + 1] = color;
                rgba[dst + 2] = color;
                rgba[dst + 3] = 255;
            }
        }
        return rgba;
    }

    private static int ReadBitAscii(ReadOnlySpan<byte> data, ref int pos) {
        SkipWhitespaceAndComments(data, ref pos);
        if (pos >= data.Length) throw new FormatException("Unexpected end of PBM data.");
        var c = data[pos++];
        if (c == (byte)'0') return 0;
        if (c == (byte)'1') return 1;
        throw new FormatException("Invalid PBM data.");
    }

    private static int ReadIntToken(ReadOnlySpan<byte> data, ref int pos) {
        SkipWhitespaceAndComments(data, ref pos);
        if (pos >= data.Length) throw new FormatException("Unexpected end of PBM header.");

        var value = 0;
        var sawDigit = false;
        while (pos < data.Length) {
            var c = data[pos];
            if (c < (byte)'0' || c > (byte)'9') break;
            sawDigit = true;
            if (value > (int.MaxValue - 9) / 10) throw new FormatException("PBM header value too large.");
            value = value * 10 + (c - (byte)'0');
            pos++;
        }
        if (!sawDigit) throw new FormatException("Invalid PBM header.");
        return value;
    }

    private static void SkipWhitespaceAndComments(ReadOnlySpan<byte> data, ref int pos) {
        while (pos < data.Length) {
            var c = data[pos];
            if (c == (byte)'#') {
                pos++;
                while (pos < data.Length && data[pos] != (byte)'\n' && data[pos] != (byte)'\r') pos++;
                continue;
            }
            if (IsAsciiWhitespace(c)) {
                pos++;
                continue;
            }
            break;
        }
    }

    private static bool IsAsciiWhitespace(byte c) {
        return c == (byte)' ' || c == (byte)'\t' || c == (byte)'\r' || c == (byte)'\n' || c == (byte)'\f';
    }
}
