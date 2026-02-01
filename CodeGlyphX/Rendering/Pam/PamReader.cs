using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Pam;

/// <summary>
/// Decodes PAM (P7) images to RGBA buffers.
/// </summary>
public static class PamReader {
    private const int MaxDimension = 16384;

    /// <summary>
    /// Decodes a PAM image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> pam, out int width, out int height) {
        DecodeGuards.EnsurePayloadWithinLimits(pam.Length, "PAM payload exceeds size limits.");
        if (pam.Length < 2) throw new FormatException("Invalid PAM data.");
        if (pam[0] != (byte)'P' || pam[1] != (byte)'7') throw new FormatException("Invalid PAM signature.");

        var pos = 2;
        width = 0;
        height = 0;
        var depth = 0;
        var maxVal = 0;
        string? tupleType = null;

        while (true) {
            SkipWhitespaceAndComments(pam, ref pos);
            if (pos >= pam.Length) throw new FormatException("Invalid PAM header.");
            if (IsToken(pam, pos, "ENDHDR")) {
                pos += 6;
                while (pos < pam.Length) {
                    var c = pam[pos];
                    if (c == (byte)'\n' || c == (byte)'\r' || c == (byte)' ' || c == (byte)'\t') {
                        pos++;
                        continue;
                    }
                    break;
                }
                break;
            }

            var key = ReadToken(pam, ref pos);
            if (string.Equals(key, "WIDTH", StringComparison.OrdinalIgnoreCase)) {
                width = ReadIntToken(pam, ref pos);
            } else if (string.Equals(key, "HEIGHT", StringComparison.OrdinalIgnoreCase)) {
                height = ReadIntToken(pam, ref pos);
            } else if (string.Equals(key, "DEPTH", StringComparison.OrdinalIgnoreCase)) {
                depth = ReadIntToken(pam, ref pos);
            } else if (string.Equals(key, "MAXVAL", StringComparison.OrdinalIgnoreCase)) {
                maxVal = ReadIntToken(pam, ref pos);
            } else if (string.Equals(key, "TUPLTYPE", StringComparison.OrdinalIgnoreCase)) {
                tupleType = ReadToken(pam, ref pos);
            } else {
                _ = ReadToken(pam, ref pos);
            }
        }

        if (width <= 0 || height <= 0) throw new FormatException("Invalid PAM dimensions.");
        if (width > MaxDimension || height > MaxDimension) throw new FormatException("PAM dimensions are too large.");
        if (maxVal == 0) throw new FormatException("Missing PAM max value.");
        if (maxVal > 65535) throw new FormatException("Unsupported PAM max value.");
        if (depth is not (1 or 2 or 3 or 4)) throw new FormatException("Unsupported PAM depth.");
        if (tupleType is null) throw new FormatException("Missing PAM tuple type.");

        var pixelCount = DecodeGuards.EnsurePixelCount(width, height, "PAM dimensions exceed size limits.");
        var rgba = DecodeGuards.AllocateRgba32(width, height, "PAM dimensions exceed size limits.");
        var bytesPerSample = maxVal > 255 ? 2 : 1;
        var required = (long)pos + (long)pixelCount * depth * bytesPerSample;
        if (required > pam.Length) throw new FormatException("Truncated PAM data.");

        var src = pos;
        for (var i = 0; i < pixelCount; i++) {
            var dst = i * 4;
            if (depth == 1) {
                var v = ReadSample(pam, ref src, bytesPerSample);
                var b = ScaleToByte(v, maxVal);
                rgba[dst + 0] = b;
                rgba[dst + 1] = b;
                rgba[dst + 2] = b;
                rgba[dst + 3] = 255;
            } else if (depth == 2) {
                var v = ReadSample(pam, ref src, bytesPerSample);
                var a = ReadSample(pam, ref src, bytesPerSample);
                var b = ScaleToByte(v, maxVal);
                rgba[dst + 0] = b;
                rgba[dst + 1] = b;
                rgba[dst + 2] = b;
                rgba[dst + 3] = ScaleToByte(a, maxVal);
            } else if (depth == 3) {
                var r = ReadSample(pam, ref src, bytesPerSample);
                var g = ReadSample(pam, ref src, bytesPerSample);
                var b = ReadSample(pam, ref src, bytesPerSample);
                rgba[dst + 0] = ScaleToByte(r, maxVal);
                rgba[dst + 1] = ScaleToByte(g, maxVal);
                rgba[dst + 2] = ScaleToByte(b, maxVal);
                rgba[dst + 3] = 255;
            } else {
                var r = ReadSample(pam, ref src, bytesPerSample);
                var g = ReadSample(pam, ref src, bytesPerSample);
                var b = ReadSample(pam, ref src, bytesPerSample);
                var a = ReadSample(pam, ref src, bytesPerSample);
                rgba[dst + 0] = ScaleToByte(r, maxVal);
                rgba[dst + 1] = ScaleToByte(g, maxVal);
                rgba[dst + 2] = ScaleToByte(b, maxVal);
                rgba[dst + 3] = ScaleToByte(a, maxVal);
            }
        }

        return rgba;
    }

    private static int ReadSample(ReadOnlySpan<byte> data, ref int pos, int bytesPerSample) {
        if (bytesPerSample == 1) return data[pos++];
        var value = (data[pos] << 8) | data[pos + 1];
        pos += 2;
        return value;
    }

    private static byte ScaleToByte(int value, int maxVal) {
        if (value < 0) return 0;
        if (maxVal == 255) return (byte)value;
        return (byte)((value * 255 + maxVal / 2) / maxVal);
    }

    private static bool IsToken(ReadOnlySpan<byte> data, int pos, string token) {
        if (pos + token.Length > data.Length) return false;
        for (var i = 0; i < token.Length; i++) {
            var c = data[pos + i];
            if (c == token[i] || c == token[i] - 32) continue;
            return false;
        }
        return true;
    }

    private static string ReadToken(ReadOnlySpan<byte> data, ref int pos) {
        SkipWhitespaceAndComments(data, ref pos);
        if (pos >= data.Length) throw new FormatException("Unexpected end of PAM header.");

        var start = pos;
        while (pos < data.Length) {
            var c = data[pos];
            if (c <= 32) break;
            pos++;
        }
        if (pos == start) throw new FormatException("Invalid PAM header.");
        return System.Text.Encoding.ASCII.GetString(data.Slice(start, pos - start).ToArray());
    }

    private static int ReadIntToken(ReadOnlySpan<byte> data, ref int pos) {
        SkipWhitespaceAndComments(data, ref pos);
        if (pos >= data.Length) throw new FormatException("Unexpected end of PAM header.");

        var value = 0;
        var sawDigit = false;
        while (pos < data.Length) {
            var c = data[pos];
            if (c < (byte)'0' || c > (byte)'9') break;
            sawDigit = true;
            if (value > (int.MaxValue - 9) / 10) throw new FormatException("PAM header value too large.");
            value = value * 10 + (c - (byte)'0');
            pos++;
        }
        if (!sawDigit) throw new FormatException("Invalid PAM header.");
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
