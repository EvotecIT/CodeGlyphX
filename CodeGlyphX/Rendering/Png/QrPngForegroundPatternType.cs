namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Foreground pattern types applied inside dark modules.
/// </summary>
public enum QrPngForegroundPatternType {
    /// <summary>
    /// Small stipple dots.
    /// </summary>
    StippleDots,
    /// <summary>
    /// Diagonal stripes.
    /// </summary>
    DiagonalStripes,
    /// <summary>
    /// Two diagonal stripe sets forming a crosshatch.
    /// </summary>
    Crosshatch,
    /// <summary>
    /// Eight-direction starburst rays (cardinal + diagonal).
    /// </summary>
    Starburst,
}
