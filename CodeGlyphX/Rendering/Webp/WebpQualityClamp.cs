namespace CodeGlyphX.Rendering.Webp;

internal static class WebpQualityClamp {
    internal static int Clamp(int value) {
        if (value < 0) return 0;
        if (value > 100) return 100;
        return value;
    }
}
