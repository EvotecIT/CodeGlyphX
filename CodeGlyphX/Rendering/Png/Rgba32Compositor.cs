namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Provides straight-alpha color composition shared by rendering and static render analysis.
/// </summary>
internal static class Rgba32Compositor {
    /// <summary>
    /// Composes <paramref name="top"/> over <paramref name="bottom"/> using Porter-Duff over.
    /// </summary>
    internal static Rgba32 ComposeOver(Rgba32 top, Rgba32 bottom) {
        var topAlpha = top.A;
        if (topAlpha == 0) return bottom;
        if (topAlpha == 255 && bottom.A == 255) return top;

        var bottomAlpha = bottom.A;
        var inverseTopAlpha = 255 - topAlpha;
        var outputAlpha = topAlpha + (bottomAlpha * inverseTopAlpha + 127) / 255;
        if (outputAlpha <= 0) return new Rgba32(0, 0, 0, 0);

        var red = top.R * topAlpha + (int)((bottom.R * bottomAlpha * (long)inverseTopAlpha + 127) / 255);
        var green = top.G * topAlpha + (int)((bottom.G * bottomAlpha * (long)inverseTopAlpha + 127) / 255);
        var blue = top.B * topAlpha + (int)((bottom.B * bottomAlpha * (long)inverseTopAlpha + 127) / 255);

        return new Rgba32(
            (byte)((red + outputAlpha / 2) / outputAlpha),
            (byte)((green + outputAlpha / 2) / outputAlpha),
            (byte)((blue + outputAlpha / 2) / outputAlpha),
            (byte)outputAlpha);
    }
}
