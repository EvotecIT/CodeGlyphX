namespace CodeGlyphX;

/// <summary>
/// Speed/accuracy profile for pixel-based QR decoding.
/// </summary>
public enum QrDecodeProfile {
    /// <summary>
    /// Fastest path (minimal thresholds, no transforms).
    /// </summary>
    Fast,
    /// <summary>
    /// Balanced speed/robustness.
    /// </summary>
    Balanced,
    /// <summary>
    /// Most robust path (more thresholds, transforms, and scales).
    /// </summary>
    Robust
}
