using System;

namespace CodeGlyphX.Rendering.Pam;

/// <summary>
/// Decodes PAM (P7) images to RGBA buffers.
/// </summary>
public static class PamReader {
    /// <summary>
    /// Decodes a PAM image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> pam, out int width, out int height) {
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
        if (maxVal <= 0 || maxVal > 255) throw new FormatException("Unsupported PAM max value.");
        if (depth is not (3 or 4)) throw new FormatException("Unsupported PAM depth.");
        if (tupleType is null) throw new FormatException("Missing PAM tuple type.");

        var pixelCount = width * height;
        var rgba = new byte[pixelCount * 4];
        var required = pos + pixelCount * depth;
        if (required > pam.Length) throw new FormatException("Truncated PAM data.");

        var src = pos;
        for (var i = 0; i < pixelCount; i++) {
            var dst = i * 4;
            rgba[dst + 0] = pam[src++];
            rgba[dst + 1] = pam[src++];
            rgba[dst + 2] = pam[src++];
            rgba[dst + 3] = depth == 4 ? pam[src++] : (byte)255;
        }

        return rgba;
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
