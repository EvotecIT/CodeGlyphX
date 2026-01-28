namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Modes for mapping module shapes across a QR matrix.
/// </summary>
public enum QrPngModuleShapeMapMode {
    /// <summary>
    /// Radial blend from center to edge using <see cref="QrPngModuleShapeMapOptions.Split"/>.
    /// </summary>
    Radial,
    /// <summary>
    /// Ring-based alternating shapes.
    /// </summary>
    Rings,
    /// <summary>
    /// Checkerboard alternation.
    /// </summary>
    Checker,
    /// <summary>
    /// Random selection between shapes.
    /// </summary>
    Random,
    /// <summary>
    /// Apply the secondary shape to corner zones.
    /// </summary>
    Corners
}
