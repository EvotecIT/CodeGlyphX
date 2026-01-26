using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Palette options for multi-color QR module rendering.
/// </summary>
public sealed class QrPngPaletteOptions {
    /// <summary>
    /// Default ring size (in modules) for <see cref="QrPngPaletteMode.Rings"/>.
    /// </summary>
    public const int DefaultRingSize = 2;
    /// <summary>
    /// Gets or sets the palette colors (required).
    /// </summary>
    public Rgba32[] Colors { get; set; } = { Rgba32.Black };

    /// <summary>
    /// Gets or sets the palette selection mode.
    /// </summary>
    public QrPngPaletteMode Mode { get; set; } = QrPngPaletteMode.Cycle;

    /// <summary>
    /// Gets or sets the random seed used by <see cref="QrPngPaletteMode.Random"/>.
    /// </summary>
    public int Seed { get; set; } = 12345;

    /// <summary>
    /// Gets or sets the ring thickness in modules for <see cref="QrPngPaletteMode.Rings"/>.
    /// </summary>
    public int RingSize { get; set; } = DefaultRingSize;

    /// <summary>
    /// When false, finder (eye) modules keep the foreground color.
    /// </summary>
    public bool ApplyToEyes { get; set; } = true;

    internal void Validate() {
        if (Colors is null || Colors.Length == 0) throw new ArgumentException("Palette must include at least one color.", nameof(Colors));
        if (RingSize <= 0) throw new ArgumentOutOfRangeException(nameof(RingSize));
    }
}
