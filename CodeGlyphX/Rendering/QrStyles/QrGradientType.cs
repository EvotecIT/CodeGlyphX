namespace CodeGlyphX.Rendering;

/// <summary>
/// Gradient types for QR rendering.
/// </summary>
public enum QrGradientType {
    /// <summary>
    /// Horizontal (left to right) gradient.
    /// </summary>
    Horizontal,
    /// <summary>
    /// Vertical (top to bottom) gradient.
    /// </summary>
    Vertical,
    /// <summary>
    /// Diagonal gradient (top-left to bottom-right).
    /// </summary>
    DiagonalDown,
    /// <summary>
    /// Diagonal gradient (bottom-left to top-right).
    /// </summary>
    DiagonalUp,
    /// <summary>
    /// Radial gradient from a center point.
    /// </summary>
    Radial,
}
