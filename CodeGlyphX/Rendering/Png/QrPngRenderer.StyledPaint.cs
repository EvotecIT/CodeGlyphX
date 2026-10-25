namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static PaletteInfo? ResolveStyledPalette(PaletteInfo? palette, PaletteZoneInfo? zones, int mx, int my, bool isEye) {
        if (zones.HasValue && zones.Value.TryGetPalette(mx, my, out var zone) && (!isEye || zone.ApplyToEyes)) return zone;
        return palette.HasValue && (!isEye || palette.Value.ApplyToEyes) ? palette : null;
    }

    // Mask selects coverage and deliberately does not change the original ink colour.
    private static bool ApplyStyledPattern(QrForegroundPatternOptions? pattern, int moduleSize, int px, int py,
        int originX, int originY, int qrSize, ref Rgba32 ink) {
        if (pattern is null || pattern.ThicknessPx <= 0
            || (pattern.Color.A == 0 && pattern.BlendMode != QrForegroundPatternBlendMode.Mask)) return true;
        var selected = ShouldDrawForegroundPattern(pattern, moduleSize, px, py, originX, originY, qrSize);
        if (pattern.BlendMode == QrForegroundPatternBlendMode.Mask) return selected;
        if (selected) ink = pattern.BlendMode == QrForegroundPatternBlendMode.Replace ? pattern.Color
            : Rgba32Compositor.ComposeOver(pattern.Color, ink);
        return true;
    }
}
