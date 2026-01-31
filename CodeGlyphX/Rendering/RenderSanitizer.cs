using System;
using System.Text;

namespace CodeGlyphX.Rendering;

internal static class RenderSanitizer {
    public static string SafeCssColor(string? value, string fallback) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var trimmed = value.Trim();
        if (IsHexColor(trimmed) || IsRgbColor(trimmed) || IsNamedColor(trimmed)) return trimmed;
        return fallback;
    }

    public static string SafeFontFamily(string? value, string fallback) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (char.IsLetterOrDigit(c) || c == ' ' || c == ',' || c == '-' || c == '_') {
                sb.Append(c);
            }
        }
        var result = sb.ToString().Trim();
        return result.Length == 0 ? fallback : result;
    }

    private static bool IsHexColor(string value) {
        if (value.Length is not (4 or 5 or 7 or 9)) return false;
        if (value[0] != '#') return false;
        for (var i = 1; i < value.Length; i++) {
            if (!IsHexDigit(value[i])) return false;
        }
        return true;
    }

    private static bool IsRgbColor(string value) {
        if (!value.EndsWith(")", StringComparison.Ordinal)) return false;
        var lower = value.ToLowerInvariant();
        var start = lower.StartsWith("rgba(") ? 5 : lower.StartsWith("rgb(") ? 4 : 0;
        if (start == 0) return false;
        for (var i = start; i < value.Length - 1; i++) {
            var c = value[i];
            if (!(char.IsDigit(c) || c == ' ' || c == ',' || c == '.' || c == '%' || c == '\t')) {
                return false;
            }
        }
        return true;
    }

    private static bool IsNamedColor(string value) {
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (!char.IsLetter(c) && c != '-') return false;
        }
        return true;
    }

    private static bool IsHexDigit(char c) {
        return (c >= '0' && c <= '9')
            || (c >= 'a' && c <= 'f')
            || (c >= 'A' && c <= 'F');
    }
}
