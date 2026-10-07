namespace CodeGlyphX.Rendering;

/// <summary>
/// Palette selection modes for QR modules.
/// </summary>
public enum QrPaletteMode {
    /// <summary>
    /// Cycles through colors using (x + y) ordering.
    /// </summary>
    Cycle,
    /// <summary>
    /// Alternates colors in a checkerboard pattern.
    /// </summary>
    Checker,
    /// <summary>
    /// Picks colors using a deterministic hash of (x, y, seed).
    /// </summary>
    Random,
    /// <summary>
    /// Assigns colors by ring distance from the center.
    /// </summary>
    Rings,
}
