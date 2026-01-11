using System;

namespace CodeGlyphX.Rendering.Ppm;

/// <summary>
/// Decodes PPM/PGM (P6/P5) images to RGBA buffers.
/// </summary>
public static class PpmReader {
    /// <summary>
    /// Decodes a PPM/PGM image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> ppm, out int width, out int height) {
        if (ppm.Length < 2) throw new FormatException("Invalid PPM data.");
        if (ppm[0] != (byte)'P') throw new FormatException("Invalid PPM signature.");

        var format = ppm[1];
        if (format != (byte)'6' && format != (byte)'5') throw new FormatException("Unsupported PPM format.");
        var isColor = format == (byte)'6';

        var pos = 2;
        width = ReadIntToken(ppm, ref pos);
        height = ReadIntToken(ppm, ref pos);
        var maxVal = ReadIntToken(ppm, ref pos);

        if (width <= 0 || height <= 0) throw new FormatException("Invalid PPM dimensions.");
        if (maxVal <= 0 || maxVal > 255) throw new FormatException("Unsupported PPM max value.");

        SkipWhitespaceAndComments(ppm, ref pos);
        var pixelCount = width * height;

        if (isColor) {
            var required = pos + pixelCount * 3;
            if (required > ppm.Length) throw new FormatException("Truncated PPM data.");
            var rgba = new byte[pixelCount * 4];
            var src = pos;
            var dst = 0;
            for (var i = 0; i < pixelCount; i++) {
                var r = ppm[src++];
                var g = ppm[src++];
                var b = ppm[src++];
                rgba[dst++] = r;
                rgba[dst++] = g;
                rgba[dst++] = b;
                rgba[dst++] = 255;
            }
            return rgba;
        }

        var requiredGray = pos + pixelCount;
        if (requiredGray > ppm.Length) throw new FormatException("Truncated PGM data.");
        var rgbaGray = new byte[pixelCount * 4];
        var srcGray = pos;
        var dstGray = 0;
        for (var i = 0; i < pixelCount; i++) {
            var v = ppm[srcGray++];
            rgbaGray[dstGray++] = v;
            rgbaGray[dstGray++] = v;
            rgbaGray[dstGray++] = v;
            rgbaGray[dstGray++] = 255;
        }
        return rgbaGray;
    }

    private static int ReadIntToken(ReadOnlySpan<byte> data, ref int pos) {
        SkipWhitespaceAndComments(data, ref pos);
        if (pos >= data.Length) throw new FormatException("Unexpected end of PPM header.");

        var value = 0;
        var sawDigit = false;
        while (pos < data.Length) {
            var c = data[pos];
            if (c < (byte)'0' || c > (byte)'9') break;
            sawDigit = true;
            value = value * 10 + (c - (byte)'0');
            pos++;
        }
        if (!sawDigit) throw new FormatException("Invalid PPM header.");
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
            if (c <= 32) {
                pos++;
                continue;
            }
            break;
        }
    }
}
