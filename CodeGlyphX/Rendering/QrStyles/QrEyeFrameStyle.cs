namespace CodeGlyphX.Rendering;

/// <summary>
/// Eye (finder) frame styles.
/// </summary>
public enum QrEyeFrameStyle {
    /// <summary>
    /// Single outer frame with inner dot.
    /// </summary>
    Single,
    /// <summary>
    /// Outer ring with a fine separating groove and a retained dark finder ball.
    /// </summary>
    DoubleRing,
    /// <summary>
    /// Target-style outer ring with two fine grooves and a dark finder ball.
    /// </summary>
    Target,
    /// <summary>
    /// Bold corner brackets joined by a continuous inner finder stroke.
    /// </summary>
    Bracket,
    /// <summary>
    /// Bold squircle badge silhouette with a retained dark finder ball.
    /// </summary>
    Badge,
    /// <summary>
    /// Single frame with a soft glow halo behind it.
    /// </summary>
    Glow,
    /// <summary>
    /// Outer ring with a fine inset groove and a retained dark finder ball.
    /// </summary>
    InsetRing,
    /// <summary>
    /// Square frame with corners cut away for a tech-like look.
    /// </summary>
    CutCorner,
}
