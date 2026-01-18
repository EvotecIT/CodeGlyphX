using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CodeGlyphX.Rendering.Xbm;

/// <summary>
/// Decodes XBM images to RGBA buffers.
/// </summary>
public static class XbmReader {
    /// <summary>
    /// Decodes an XBM image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> xbm, out int width, out int height) {
        var text = System.Text.Encoding.ASCII.GetString(xbm.ToArray());
        width = ExtractDefineValue(text, "_width");
        height = ExtractDefineValue(text, "_height");
        if (width <= 0 || height <= 0) throw new FormatException("Invalid XBM dimensions.");
        var pixelCount = (long)width * height;
        if (pixelCount > int.MaxValue / 4) throw new FormatException("XBM dimensions are too large.");

        var bytes = ExtractByteArray(text);
        var rowBytes = (width + 7) / 8;
        if ((long)rowBytes * height > int.MaxValue) throw new FormatException("XBM dimensions are too large.");
        if (bytes.Count < rowBytes * height) throw new FormatException("Truncated XBM data.");

        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            var rowStart = y * rowBytes;
            for (var x = 0; x < width; x++) {
                var b = bytes[rowStart + (x >> 3)];
                var bit = (b >> (x & 7)) & 1;
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

    private static int ExtractDefineValue(string text, string suffix) {
        var idx = text.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
        while (idx >= 0) {
            var lineStart = text.LastIndexOf('\n', idx);
            if (lineStart < 0) lineStart = 0;
            var lineEnd = text.IndexOf('\n', idx);
            if (lineEnd < 0) lineEnd = text.Length;
            var line = text.Substring(lineStart, lineEnd - lineStart);
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && parts[0].Equals("#define", StringComparison.OrdinalIgnoreCase)) {
                if (int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) {
                    return value;
                }
            }
            idx = text.IndexOf(suffix, idx + suffix.Length, StringComparison.OrdinalIgnoreCase);
        }
        return 0;
    }

    private static List<byte> ExtractByteArray(string text) {
        var start = text.IndexOf('{');
        var end = text.IndexOf('}', start + 1);
        if (start < 0 || end < 0 || end <= start) throw new FormatException("Invalid XBM data.");

        var list = new List<byte>();
        var span = text.AsSpan(start + 1, end - start - 1);
        var token = new StringBuilder();
        for (var i = 0; i < span.Length; i++) {
            var c = span[i];
            if (char.IsLetterOrDigit(c) || c == 'x' || c == 'X') {
                token.Append(c);
                continue;
            }
            if (token.Length != 0) {
                list.Add(ParseByte(token.ToString()));
                token.Clear();
            }
        }
        if (token.Length != 0) list.Add(ParseByte(token.ToString()));
        return list;
    }

    private static byte ParseByte(string token) {
        token = token.Trim();
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
            return byte.Parse(token.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
        return byte.Parse(token, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}
