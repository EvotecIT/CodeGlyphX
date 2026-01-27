using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Paint splash/drip options for the canvas background.
/// </summary>
public sealed class QrPngCanvasSplashOptions {
    /// <summary>
    /// Splash color (alpha recommended).
    /// </summary>
    public Rgba32 Color { get; set; } = new(48, 96, 220, 92);

    /// <summary>
    /// Optional multi-color splash palette. When set, a random entry is used per splash.
    /// </summary>
    public Rgba32[]? Colors { get; set; }

    /// <summary>
    /// Number of splash blobs to draw.
    /// </summary>
    public int Count { get; set; } = 10;

    /// <summary>
    /// Minimum splash radius in pixels.
    /// </summary>
    public int MinRadiusPx { get; set; } = 14;

    /// <summary>
    /// Maximum splash radius in pixels.
    /// </summary>
    public int MaxRadiusPx { get; set; } = 40;

    /// <summary>
    /// How far splashes can extend beyond the QR bounds.
    /// </summary>
    public int SpreadPx { get; set; } = 22;

    /// <summary>
    /// Random seed used to place splashes.
    /// </summary>
    public int Seed { get; set; } = 12345;

    /// <summary>
    /// Chance (0..1) that a splash will include a downward drip.
    /// </summary>
    public double DripChance { get; set; } = 0.35;

    /// <summary>
    /// Maximum drip length in pixels.
    /// </summary>
    public int DripLengthPx { get; set; } = 26;

    /// <summary>
    /// Drip width in pixels.
    /// </summary>
    public int DripWidthPx { get; set; } = 8;

    /// <summary>
    /// When true, never draws inside the QR area.
    /// </summary>
    public bool ProtectQrArea { get; set; } = true;

    internal void Validate() {
        if (Count < 0) throw new ArgumentOutOfRangeException(nameof(Count));
        if (MinRadiusPx < 1) throw new ArgumentOutOfRangeException(nameof(MinRadiusPx));
        if (MaxRadiusPx < MinRadiusPx) throw new ArgumentOutOfRangeException(nameof(MaxRadiusPx));
        if (SpreadPx < 0) throw new ArgumentOutOfRangeException(nameof(SpreadPx));
        if (DripChance is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(DripChance));
        if (DripLengthPx < 0) throw new ArgumentOutOfRangeException(nameof(DripLengthPx));
        if (DripWidthPx < 0) throw new ArgumentOutOfRangeException(nameof(DripWidthPx));
        if (Colors is { Length: 0 }) throw new ArgumentOutOfRangeException(nameof(Colors));
    }
}
