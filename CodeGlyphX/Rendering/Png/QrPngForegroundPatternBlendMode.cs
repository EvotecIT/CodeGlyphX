namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Blend mode for foreground patterns.
/// </summary>
public enum QrPngForegroundPatternBlendMode {
    /// <summary>
    /// Overlay the pattern color on top of the module color.
    /// </summary>
    Overlay,
    /// <summary>
    /// Mask the module so only pattern pixels are drawn (bead/cluster look).
    /// </summary>
    Mask,
    /// <summary>
    /// Replace the module color with the pattern color at pattern pixels.
    /// </summary>
    Replace
}
