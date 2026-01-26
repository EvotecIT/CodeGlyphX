using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Palette overrides for specific QR zones.
/// </summary>
public sealed class QrPngPaletteZoneOptions {
    /// <summary>
    /// Palette applied to a centered square zone.
    /// </summary>
    public QrPngPaletteOptions? CenterPalette { get; set; }

    /// <summary>
    /// Center zone size in modules (0 = disabled).
    /// </summary>
    public int CenterSize { get; set; } = 0;

    /// <summary>
    /// Palette applied to the four corners.
    /// </summary>
    public QrPngPaletteOptions? CornerPalette { get; set; }

    /// <summary>
    /// Corner zone size in modules (0 = disabled).
    /// </summary>
    public int CornerSize { get; set; } = 0;

    internal void Validate() {
        if (CenterSize < 0) throw new ArgumentOutOfRangeException(nameof(CenterSize));
        if (CornerSize < 0) throw new ArgumentOutOfRangeException(nameof(CornerSize));
        CenterPalette?.Validate();
        CornerPalette?.Validate();
    }
}
