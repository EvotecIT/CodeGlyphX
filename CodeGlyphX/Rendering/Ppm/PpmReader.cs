using System;

namespace CodeGlyphX.Rendering.Ppm;

/// <summary>
/// Decodes PPM/PGM (P6/P5) images to RGBA buffers.
/// </summary>
public static class PpmReader {
    private const int MaxDimension = 16384;
    /// <summary>
    /// Decodes a PPM/PGM image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> ppm, out int width, out int height) {
        if (ppm.Length < 2) throw new FormatException("Invalid PPM data.");
        if (ppm[0] != (byte)'P') throw new FormatException("Invalid PPM signature.");

        var format = ppm[1];
        if (format != (byte)'6' && format != (byte)'5' && format != (byte)'3' && format != (byte)'2') {
            throw new FormatException("Unsupported PPM format.");
        }
        var isColor = format == (byte)'6' || format == (byte)'3';
        var isAscii = format == (byte)'3' || format == (byte)'2';

        var pos = 2;
        width = ReadIntToken(ppm, ref pos);
        height = ReadIntToken(ppm, ref pos);
        var maxVal = ReadIntToken(ppm, ref pos);

        if (width <= 0 || height <= 0) throw new FormatException("Invalid PPM dimensions.");
        if (width > MaxDimension || height > MaxDimension) throw new FormatException("PPM dimensions are too large.");
        if (maxVal <= 0 || maxVal > 65535) throw new FormatException("Unsupported PPM max value.");

        SkipWhitespaceAndComments(ppm, ref pos);
        var pixelCount = width * height;

        if (isAscii) {
            var rgba = new byte[pixelCount * 4];
            var channels = isColor ? 3 : 1;
            for (var i = 0; i < pixelCount; i++) {
                var dst = i * 4;
                if (channels == 3) {
                    var r = ReadIntToken(ppm, ref pos);
                    var g = ReadIntToken(ppm, ref pos);
                    var b = ReadIntToken(ppm, ref pos);
                    rgba[dst + 0] = ScaleToByte(r, maxVal);
                    rgba[dst + 1] = ScaleToByte(g, maxVal);
                    rgba[dst + 2] = ScaleToByte(b, maxVal);
                } else {
                    var v = ReadIntToken(ppm, ref pos);
                    var b = ScaleToByte(v, maxVal);
                    rgba[dst + 0] = b;
                    rgba[dst + 1] = b;
                    rgba[dst + 2] = b;
                }
                rgba[dst + 3] = 255;
            }
            return rgba;
        }

        var bytesPerSample = maxVal > 255 ? 2 : 1;
        var channelsPerPixel = isColor ? 3 : 1;
        var required = (long)pos + (long)pixelCount * channelsPerPixel * bytesPerSample;
        if (required > ppm.Length) throw new FormatException("Truncated PPM data.");
        var rgbaRaw = new byte[pixelCount * 4];
        var srcRaw = pos;
        for (var i = 0; i < pixelCount; i++) {
            var dst = i * 4;
            if (isColor) {
                var r = ReadSample(ppm, ref srcRaw, bytesPerSample);
                var g = ReadSample(ppm, ref srcRaw, bytesPerSample);
                var b = ReadSample(ppm, ref srcRaw, bytesPerSample);
                rgbaRaw[dst + 0] = ScaleToByte(r, maxVal);
                rgbaRaw[dst + 1] = ScaleToByte(g, maxVal);
                rgbaRaw[dst + 2] = ScaleToByte(b, maxVal);
            } else {
                var v = ReadSample(ppm, ref srcRaw, bytesPerSample);
                var b = ScaleToByte(v, maxVal);
                rgbaRaw[dst + 0] = b;
                rgbaRaw[dst + 1] = b;
                rgbaRaw[dst + 2] = b;
            }
            rgbaRaw[dst + 3] = 255;
        }
        return rgbaRaw;
    }

    private static int ReadSample(ReadOnlySpan<byte> data, ref int pos, int bytesPerSample) {
        if (bytesPerSample == 1) {
            return data[pos++];
        }
        var value = (data[pos] << 8) | data[pos + 1];
        pos += 2;
        return value;
    }

    private static byte ScaleToByte(int value, int maxVal) {
        if (value < 0) return 0;
        if (maxVal == 255) return (byte)value;
        return (byte)((value * 255 + maxVal / 2) / maxVal);
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
