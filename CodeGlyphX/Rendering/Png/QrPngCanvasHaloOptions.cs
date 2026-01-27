using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Halo/glow options drawn around the QR bounds on the canvas.
/// </summary>
public sealed class QrPngCanvasHaloOptions {
    /// <summary>
    /// Halo color (alpha recommended).
    /// </summary>
    public Rgba32 Color { get; set; } = new(0, 170, 220, 96);

    /// <summary>
    /// Halo radius in pixels.
    /// </summary>
    public int RadiusPx { get; set; } = 32;

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
        if (RadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(RadiusPx));
    }
}

