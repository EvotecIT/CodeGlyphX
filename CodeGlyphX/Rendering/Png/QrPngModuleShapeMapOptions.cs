using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Options for per-module shape mapping.
/// </summary>
public sealed class QrPngModuleShapeMapOptions {
    /// <summary>
    /// Default ring size (in modules) for <see cref="QrPngModuleShapeMapMode.Rings"/>.
    /// </summary>
    public const int DefaultRingSize = 2;

    /// <summary>
    /// Gets or sets the shape map mode.
    /// </summary>
    public QrPngModuleShapeMapMode Mode { get; set; } = QrPngModuleShapeMapMode.Radial;

    /// <summary>
    /// Primary shape (used for the center in radial mode).
    /// </summary>
    public QrPngModuleShape PrimaryShape { get; set; } = QrPngModuleShape.Rounded;

    /// <summary>
    /// Secondary shape (used for the edge in radial mode).
    /// </summary>
    public QrPngModuleShape SecondaryShape { get; set; } = QrPngModuleShape.Dot;

    /// <summary>
    /// Split point (0..1) used by <see cref="QrPngModuleShapeMapMode.Radial"/>.
    /// </summary>
    public double Split { get; set; } = 0.6;

    /// <summary>
    /// Ring size in modules for <see cref="QrPngModuleShapeMapMode.Rings"/>.
    /// </summary>
    public int RingSize { get; set; } = DefaultRingSize;

    /// <summary>
    /// Random seed used by <see cref="QrPngModuleShapeMapMode.Random"/>.
    /// </summary>
    public int Seed { get; set; } = 12345;

    /// <summary>
    /// Probability (0..1) of choosing the secondary shape in random mode.
    /// </summary>
    public double SecondaryChance { get; set; } = 0.5;

    /// <summary>
    /// Corner zone size in modules for <see cref="QrPngModuleShapeMapMode.Corners"/>.
    /// </summary>
    public int CornerSize { get; set; }

    /// <summary>
    /// When true, allows the shape map to affect eye modules.
    /// </summary>
    public bool ApplyToEyes { get; set; }

    /// <summary>
    /// When true, functional patterns keep the base module shape.
    /// </summary>
    public bool ProtectFunctionalPatterns { get; set; } = true;

    internal void Validate() {
        if (Split is < 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(Split));
        if (RingSize <= 0) throw new ArgumentOutOfRangeException(nameof(RingSize));
        if (SecondaryChance is < 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(SecondaryChance));
        if (CornerSize < 0) throw new ArgumentOutOfRangeException(nameof(CornerSize));
    }
}
