using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Grain/noise texture options for the canvas background.
/// </summary>
public sealed class QrPngCanvasGrainOptions {
    /// <summary>
    /// Grain color (alpha recommended).
    /// </summary>
    public Rgba32 Color { get; set; } = new(0, 0, 0, 48);

    /// <summary>
    /// Grain density (0..1).
    /// </summary>
    public double Density { get; set; } = 0.22;

    /// <summary>
    /// Grain dot size in pixels.
    /// </summary>
    public int PixelSizePx { get; set; } = 2;

    /// <summary>
    /// Alpha jitter amount (0..1).
    /// </summary>
    public double AlphaJitter { get; set; } = 0.55;

    /// <summary>
    /// Random seed used to place grain. Use 0 to auto-randomize per render.
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// Optional edge band width in pixels. When greater than 0, grain is limited to the canvas edges.
    /// </summary>
    public int BandPx { get; set; }

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
        if (Density is < 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(Density));
        if (PixelSizePx < 1) throw new ArgumentOutOfRangeException(nameof(PixelSizePx));
        if (AlphaJitter is < 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(AlphaJitter));
        if (BandPx < 0) throw new ArgumentOutOfRangeException(nameof(BandPx));
    }
}

