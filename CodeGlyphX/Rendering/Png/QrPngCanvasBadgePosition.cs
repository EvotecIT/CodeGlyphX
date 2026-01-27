namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Badge anchor position relative to the QR bounds.
/// </summary>
public enum QrPngCanvasBadgePosition {
    /// <summary>
    /// Above the QR (centered).
    /// </summary>
    Top,
    /// <summary>
    /// Below the QR (centered).
    /// </summary>
    Bottom,
    /// <summary>
    /// Left of the QR (centered).
    /// </summary>
    Left,
    /// <summary>
    /// Right of the QR (centered).
    /// </summary>
    Right,
    /// <summary>
    /// Above-left of the QR.
    /// </summary>
    TopLeft,
    /// <summary>
    /// Above-right of the QR.
    /// </summary>
    TopRight,
    /// <summary>
    /// Below-left of the QR.
    /// </summary>
    BottomLeft,
    /// <summary>
    /// Below-right of the QR.
    /// </summary>
    BottomRight
}
