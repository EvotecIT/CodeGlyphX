using System.Globalization;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Helpers for converting colors to CSS-friendly strings.
/// </summary>
public static class ColorUtils {
    /// <summary>
    /// Converts an RGBA color to a CSS color string.
    /// </summary>
    public static string ToCss(Rgba32 color) {
        if (color.A == 255) {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        var a = color.A / 255.0;
        return $"rgba({color.R},{color.G},{color.B},{a.ToString("0.###", CultureInfo.InvariantCulture)})";
    }
}
