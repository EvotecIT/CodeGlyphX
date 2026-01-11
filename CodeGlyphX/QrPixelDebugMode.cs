namespace CodeGlyphX;

/// <summary>
/// Debug visualization modes for pixel-based QR decoding.
/// </summary>
public enum QrPixelDebugMode {
    /// <summary>
    /// Raw grayscale image.
    /// </summary>
    Grayscale,
    /// <summary>
    /// Threshold map (constant or adaptive).
    /// </summary>
    Threshold,
    /// <summary>
    /// Binarized image (black/white).
    /// </summary>
    Binarized,
    /// <summary>
    /// Heatmap of luminance vs threshold.
    /// </summary>
    Heatmap
}
