namespace CodeGlyphX;

/// <summary>
/// Options for pixel-based QR decoding.
/// </summary>
public sealed class QrPixelDecodeOptions {
    /// <summary>
    /// Speed/accuracy profile (default: Robust).
    /// </summary>
    public QrDecodeProfile Profile { get; set; } = QrDecodeProfile.Robust;
}
