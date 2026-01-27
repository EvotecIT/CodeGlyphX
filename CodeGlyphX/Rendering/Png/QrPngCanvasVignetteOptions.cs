using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Edge vignette options for the canvas background.
/// </summary>
public sealed class QrPngCanvasVignetteOptions {
    /// <summary>
    /// Vignette color (alpha recommended).
    /// </summary>
    public Rgba32 Color { get; set; } = new(0, 0, 0, 92);

    /// <summary>
    /// Vignette band width in pixels.
    /// </summary>
    public int BandPx { get; set; } = 68;

    /// <summary>
    /// Vignette strength multiplier (0..2).
    /// </summary>
    public double Strength { get; set; } = 1.0;

    /// <summary>
    /// When true, never draws inside the QR area.
    /// </summary>
    public bool ProtectQrArea { get; set; } = true;

    /// <summary>
    /// Maximum alpha allowed inside the QR area when <see cref="ProtectQrArea"/> is false.
    /// Set to 0 to apply no alpha limit.
    /// </summary>
    public byte QrAreaAlphaMax { get; set; }

    internal void Validate() {
        if (BandPx < 0) throw new ArgumentOutOfRangeException(nameof(BandPx));
        if (Strength is < 0 or > 2.0) throw new ArgumentOutOfRangeException(nameof(Strength));
    }
}

