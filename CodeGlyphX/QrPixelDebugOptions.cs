namespace CodeGlyphX;

/// <summary>
/// Options for QR pixel debug rendering.
/// </summary>
public sealed class QrPixelDebugOptions {
    /// <summary>
    /// Downscale factor applied before analysis (1..8).
    /// </summary>
    public int Scale { get; set; } = 1;

    /// <summary>
    /// Upscale factor for the output visualization (default 1).
    /// </summary>
    public int OutputScale { get; set; } = 1;

    /// <summary>
    /// Apply contrast stretch before thresholding.
    /// </summary>
    public bool ContrastStretch { get; set; }

    /// <summary>
    /// Minimum contrast range for stretch (default 40).
    /// </summary>
    public int ContrastStretchMinRange { get; set; } = 40;

    /// <summary>
    /// Apply a box blur before thresholding (radius in pixels).
    /// </summary>
    public int BoxBlurRadius { get; set; }

    /// <summary>
    /// Normalize background gradients using local mean subtraction.
    /// </summary>
    public bool NormalizeBackground { get; set; }

    /// <summary>
    /// Window size for background normalization (odd >= 3).
    /// </summary>
    public int NormalizeWindowSize { get; set; } = 33;

    /// <summary>
    /// Use adaptive thresholding (local window).
    /// </summary>
    public bool AdaptiveThreshold { get; set; }

    /// <summary>
    /// Window size for adaptive thresholding (odd >= 3).
    /// </summary>
    public int AdaptiveWindowSize { get; set; } = 15;

    /// <summary>
    /// Offset subtracted from the local mean (0..255).
    /// </summary>
    public int AdaptiveOffset { get; set; } = 8;

    /// <summary>
    /// Invert black/white classification for binarized/heatmap outputs.
    /// </summary>
    public bool Invert { get; set; }
}
