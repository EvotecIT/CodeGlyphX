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
    /// Organic stipple dots with jittered centers and varied sizes.
    /// </summary>
    SpeckleDots,
    /// <summary>
    /// Radial halftone dots that are stronger near the center and lighter near edges.
    /// </summary>
    HalftoneDots,
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
