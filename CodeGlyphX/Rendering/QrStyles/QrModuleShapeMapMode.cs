namespace CodeGlyphX.Rendering;

/// <summary>
/// Modes for mapping module shapes across a QR matrix.
/// </summary>
public enum QrModuleShapeMapMode {
    /// <summary>
    /// Radial blend from center to edge using <see cref="QrModuleShapeMapOptions.Split"/>.
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
