using System;
using System.Collections.Generic;
using System.Globalization;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Xpm;

/// <summary>
/// Decodes XPM images to RGBA buffers.
/// </summary>
public static class XpmReader {
    private const int MaxDimension = 16384;
    private const int MaxColors = 65536;
    private const int MaxCharsPerPixel = 8;

    /// <summary>
    /// Decodes an XPM image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> xpm, out int width, out int height) {
        DecodeGuards.EnsurePayloadWithinLimits(xpm.Length, "XPM payload exceeds size limits.");
        var text = System.Text.Encoding.ASCII.GetString(xpm.ToArray());
        var strings = ExtractQuotedStrings(text);
        if (strings.Count == 0) throw new FormatException("Invalid XPM data.");

        var headerParts = strings[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (headerParts.Length < 4) throw new FormatException("Invalid XPM header.");
        width = int.Parse(headerParts[0], CultureInfo.InvariantCulture);
        height = int.Parse(headerParts[1], CultureInfo.InvariantCulture);
        var colors = int.Parse(headerParts[2], CultureInfo.InvariantCulture);
        var charsPerPixel = int.Parse(headerParts[3], CultureInfo.InvariantCulture);
        if (width <= 0 || height <= 0 || colors <= 0 || charsPerPixel <= 0) throw new FormatException("Invalid XPM header.");
        if (width > MaxDimension || height > MaxDimension) throw new FormatException("XPM dimensions are too large.");
        if (colors > MaxColors || charsPerPixel > MaxCharsPerPixel) throw new FormatException("XPM color table is too large.");
        _ = DecodeGuards.EnsurePixelCount(width, height, "XPM dimensions exceed size limits.");

        var requiredStrings = 1L + colors + height;
        if (requiredStrings > int.MaxValue || strings.Count < requiredStrings) throw new FormatException("Truncated XPM data.");

        var palette = new Dictionary<string, Rgba32>(StringComparer.Ordinal);
        for (var i = 0; i < colors; i++) {
            var line = strings[1 + i];
            if (line.Length < charsPerPixel) throw new FormatException("Invalid XPM color table.");
            var key = line.Substring(0, charsPerPixel);
            var value = ParseColor(line.Substring(charsPerPixel));
            palette[key] = value;
        }

        var rgba = DecodeGuards.AllocateRgba32(width, height, "XPM dimensions exceed size limits.");
        for (var y = 0; y < height; y++) {
            var line = strings[1 + colors + y];
            if ((long)line.Length < (long)width * charsPerPixel) throw new FormatException("Invalid XPM pixel data.");
            for (var x = 0; x < width; x++) {
                var key = line.Substring(x * charsPerPixel, charsPerPixel);
                if (!palette.TryGetValue(key, out var color)) color = new Rgba32(0, 0, 0, 0);
                var dst = (y * width + x) * 4;
                rgba[dst + 0] = color.R;
                rgba[dst + 1] = color.G;
                rgba[dst + 2] = color.B;
                rgba[dst + 3] = color.A;
            }
        }

        return rgba;
    }

    private static Rgba32 ParseColor(string spec) {
        var parts = spec.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length - 1; i++) {
            if (!parts[i].Equals("c", StringComparison.OrdinalIgnoreCase)) continue;
            var value = parts[i + 1];
            if (value.Equals("None", StringComparison.OrdinalIgnoreCase)) return new Rgba32(0, 0, 0, 0);
            if (value.Length == 7 && value.StartsWith("#", StringComparison.Ordinal)) {
                var r = byte.Parse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var g = byte.Parse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var b = byte.Parse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return new Rgba32(r, g, b, 255);
            }
        }
        return new Rgba32(0, 0, 0, 255);
    }

    private static List<string> ExtractQuotedStrings(string text) {
        var list = new List<string>();
        var sb = new System.Text.StringBuilder();
        var inString = false;
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            if (!inString) {
                if (c == '"') {
                    inString = true;
                    sb.Clear();
                }
                continue;
            }

            if (c == '\\' && i + 1 < text.Length) {
                sb.Append(text[i + 1]);
                i++;
                continue;
            }
            if (c == '"') {
                inString = false;
                list.Add(sb.ToString());
                continue;
            }
            sb.Append(c);
        }
        return list;
    }
}
