using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Pgm;

/// <summary>
/// Decodes PGM (P5/P2) images to RGBA buffers.
/// </summary>
public static class PgmReader {
    private const int MaxDimension = 16384;
    /// <summary>
    /// Decodes a PGM image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> pgm, out int width, out int height) {
        DecodeGuards.EnsurePayloadWithinLimits(pgm.Length, "PGM payload exceeds size limits.");
        if (pgm.Length < 2) throw new FormatException("Invalid PGM data.");
        if (pgm[0] != (byte)'P') throw new FormatException("Invalid PGM signature.");
        var format = pgm[1];
        if (format != (byte)'5' && format != (byte)'2') throw new FormatException("Unsupported PGM format.");

        var pos = 2;
        width = ReadIntToken(pgm, ref pos);
        height = ReadIntToken(pgm, ref pos);
        var maxVal = ReadIntToken(pgm, ref pos);
        if (width <= 0 || height <= 0) throw new FormatException("Invalid PGM dimensions.");
        if (width > MaxDimension || height > MaxDimension) throw new FormatException("PGM dimensions are too large.");
        if (maxVal <= 0 || maxVal > 65535) throw new FormatException("Invalid PGM max value.");

        SkipWhitespaceAndComments(pgm, ref pos);

        var pixelCount = DecodeGuards.EnsurePixelCount(width, height, "PGM dimensions exceed size limits.");
        var rgba = DecodeGuards.AllocateRgba32(width, height, "PGM dimensions exceed size limits.");

        if (format == (byte)'2') {
            for (var i = 0; i < pixelCount; i++) {
                var value = ReadIntToken(pgm, ref pos);
                if (value < 0 || value > maxVal) throw new FormatException("Invalid PGM pixel value.");
                var v = ScaleToByte(value, maxVal);
                var dst = i * 4;
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            }
            return rgba;
        }

        var bytesPerSample = maxVal > 255 ? 2 : 1;
        var required = (long)pos + (long)pixelCount * bytesPerSample;
        if (required > pgm.Length) throw new FormatException("Truncated PGM data.");

        if (bytesPerSample == 1) {
            for (var i = 0; i < pixelCount; i++) {
                var v = ScaleToByte(pgm[pos + i], maxVal);
                var dst = i * 4;
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            }
            return rgba;
        }

        var offset = pos;
        for (var i = 0; i < pixelCount; i++) {
            var value = (pgm[offset] << 8) | pgm[offset + 1];
            offset += 2;
            var v = ScaleToByte(value, maxVal);
            var dst = i * 4;
            rgba[dst + 0] = v;
            rgba[dst + 1] = v;
            rgba[dst + 2] = v;
            rgba[dst + 3] = 255;
        }
        return rgba;
    }

    private static byte ScaleToByte(int value, int maxVal) {
        if (maxVal == 255) return (byte)value;
        return (byte)((value * 255 + maxVal / 2) / maxVal);
    }

    private static int ReadIntToken(ReadOnlySpan<byte> data, ref int pos) {
        SkipWhitespaceAndComments(data, ref pos);
        if (pos >= data.Length) throw new FormatException("Unexpected end of PGM header.");

        var value = 0;
        var sawDigit = false;
        while (pos < data.Length) {
            var c = data[pos];
            if (c < (byte)'0' || c > (byte)'9') break;
            sawDigit = true;
            if (value > (int.MaxValue - 9) / 10) throw new FormatException("PGM header value too large.");
            value = value * 10 + (c - (byte)'0');
            pos++;
        }
        if (!sawDigit) throw new FormatException("Invalid PGM header.");
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
