namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Eye (finder) frame styles.
/// </summary>
public enum QrPngEyeFrameStyle {
    /// <summary>
    /// Single outer frame with inner dot.
    /// </summary>
    Single,
    /// <summary>
    /// Two concentric rings (no center dot).
    /// </summary>
    DoubleRing,
    /// <summary>
    /// Two concentric rings with a center dot.
    /// </summary>
    Target,
    /// <summary>
    /// Corner brackets (L-shapes) around the eye.
    /// </summary>
    Bracket,
    /// <summary>
    /// Bold badge frame with thicker border.
    /// </summary>
    Badge,
    /// <summary>
    /// Single frame with a soft glow halo behind it.
    /// </summary>
    Glow,
    /// <summary>
    /// Outer ring with an inset inner ring and a clear center.
    /// </summary>
    InsetRing,
}
