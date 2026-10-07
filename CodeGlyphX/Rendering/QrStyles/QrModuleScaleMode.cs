namespace CodeGlyphX.Rendering;

/// <summary>
/// Module scale map modes.
/// </summary>
public enum QrModuleScaleMode {
    /// <summary>
    /// Concentric bands from the center.
    /// </summary>
    Rings,
    /// <summary>
    /// Smooth radial gradient from the center.
    /// </summary>
    Radial,
    /// <summary>
    /// Pseudo-random per-module scale.
    /// </summary>
    Random,
    /// <summary>
    /// Alternating pattern (checkerboard).
    /// </summary>
    Checker,
}
