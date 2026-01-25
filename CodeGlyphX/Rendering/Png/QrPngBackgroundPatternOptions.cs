using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Background pattern options for canvas fills.
/// </summary>
public sealed class QrPngBackgroundPatternOptions {
    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public QrPngBackgroundPatternType Type { get; set; } = QrPngBackgroundPatternType.Dots;

    /// <summary>
    /// Gets or sets the pattern color (alpha recommended).
    /// </summary>
    public Rgba32 Color { get; set; } = new(255, 255, 255, 32);

    /// <summary>
    /// Gets or sets the pattern cell size in pixels.
    /// </summary>
    public int SizePx { get; set; } = 8;

    /// <summary>
    /// Gets or sets the pattern thickness (dot radius or grid line thickness).
    /// </summary>
    public int ThicknessPx { get; set; } = 1;

    /// <summary>
    /// When true, pattern cell size snaps to a multiple of the QR module size.
    /// </summary>
    public bool SnapToModuleSize { get; set; }

    /// <summary>
    /// Module size multiplier when <see cref="SnapToModuleSize"/> is enabled.
    /// </summary>
    public int ModuleStep { get; set; } = 1;

    internal void Validate() {
        if (SizePx <= 0) throw new ArgumentOutOfRangeException(nameof(SizePx));
        if (ThicknessPx < 0) throw new ArgumentOutOfRangeException(nameof(ThicknessPx));
        if (ModuleStep <= 0) throw new ArgumentOutOfRangeException(nameof(ModuleStep));
    }
}
