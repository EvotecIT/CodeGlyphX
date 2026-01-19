namespace CodeGlyphX;

/// <summary>
/// Options for pixel-based QR decoding.
/// </summary>
public sealed class QrPixelDecodeOptions {
    /// <summary>
    /// Speed/accuracy profile (default: Robust).
    /// </summary>
    public QrDecodeProfile Profile { get; set; } = QrDecodeProfile.Robust;

    /// <summary>
    /// Maximum dimension (pixels) for decoding. Larger inputs will be downscaled by sampling.
    /// Set to 0 to disable.
    /// </summary>
    public int MaxDimension { get; set; } = 0;

    /// <summary>
    /// Maximum scale to try when decoding (1..8). Set to 0 to use profile defaults.
    /// </summary>
    public int MaxScale { get; set; } = 0;

    /// <summary>
    /// Maximum milliseconds to spend decoding (best effort). Set to 0 to disable.
    /// </summary>
    public int MaxMilliseconds { get; set; } = 0;

    /// <summary>
    /// Disable rotation/mirroring attempts even in robust profiles.
    /// </summary>
    public bool DisableTransforms { get; set; } = false;

    /// <summary>
    /// Enables extra thresholding/sampling passes for stylized or noisy QR codes (slower).
    /// </summary>
    public bool AggressiveSampling { get; set; } = false;
}
