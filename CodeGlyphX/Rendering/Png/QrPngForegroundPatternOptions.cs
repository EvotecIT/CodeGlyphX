using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Foreground pattern options for styling dark modules.
/// </summary>
public sealed class QrPngForegroundPatternOptions {
    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public QrPngForegroundPatternType Type { get; set; } = QrPngForegroundPatternType.StippleDots;

    /// <summary>
    /// Gets or sets the pattern color (alpha recommended).
    /// </summary>
    public Rgba32 Color { get; set; } = new(0, 0, 0, 48);

    /// <summary>
    /// Gets or sets the pattern blend mode.
    /// </summary>
    public QrPngForegroundPatternBlendMode BlendMode { get; set; } = QrPngForegroundPatternBlendMode.Overlay;


    /// <summary>
    /// Gets or sets the pattern cell size in pixels.
    /// </summary>
    public int SizePx { get; set; } = 6;

    /// <summary>
    /// Gets or sets the pattern thickness (dot radius or stripe thickness).
    /// </summary>
    public int ThicknessPx { get; set; } = 1;

    /// <summary>
    /// Random seed used by jittered pattern variants such as <see cref="QrPngForegroundPatternType.SpeckleDots"/>.
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// Variation amount (0..1) used by jittered pattern variants.
    /// </summary>
    public double Variation { get; set; } = 0.65;

    /// <summary>
    /// Dot density (0..1) used by jittered pattern variants.
    /// </summary>
    public double Density { get; set; } = 0.9;

    /// <summary>
    /// When true, pattern cell size snaps to a multiple of the QR module size.
    /// </summary>
    public bool SnapToModuleSize { get; set; }

    /// <summary>
    /// Module size multiplier when <see cref="SnapToModuleSize"/> is enabled.
    /// </summary>
    public int ModuleStep { get; set; } = 1;

    /// <summary>
    /// When true, applies the pattern to non-eye modules.
    /// </summary>
    public bool ApplyToModules { get; set; } = true;

    /// <summary>
    /// When true, applies the pattern to eye (finder) modules.
    /// </summary>
    public bool ApplyToEyes { get; set; }

    internal void Validate() {
        if (SizePx <= 0) throw new ArgumentOutOfRangeException(nameof(SizePx));
        if (ThicknessPx < 0) throw new ArgumentOutOfRangeException(nameof(ThicknessPx));
        if (ModuleStep <= 0) throw new ArgumentOutOfRangeException(nameof(ModuleStep));
        if (Variation is < 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(Variation));
        if (Density is < 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(Density));
    }
}
