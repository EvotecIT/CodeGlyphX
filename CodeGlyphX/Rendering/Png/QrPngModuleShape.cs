namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Module shape used by <see cref="QrPngRenderer"/>.
/// </summary>
public enum QrPngModuleShape {
    /// <summary>
    /// Full square modules.
    /// </summary>
    Square,
    /// <summary>
    /// Circular (dot) modules.
    /// </summary>
    Circle,
    /// <summary>
    /// Rounded-corner square modules.
    /// </summary>
    Rounded,
    /// <summary>
    /// Diamond-shaped modules.
    /// </summary>
    Diamond,
    /// <summary>
    /// Squircle (superellipse) modules.
    /// </summary>
    Squircle,
    /// <summary>
    /// Dot modules (small circles inside the cell).
    /// </summary>
    Dot,
    /// <summary>
    /// Dot grid modules (four small dots per cell).
    /// </summary>
    DotGrid,
}
