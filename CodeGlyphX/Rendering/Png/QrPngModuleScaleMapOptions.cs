using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Options for per-module scale mapping.
/// </summary>
public sealed class QrPngModuleScaleMapOptions {
    /// <summary>
    /// Default ring size (in modules) for <see cref="QrPngModuleScaleMode.Rings"/>.
    /// </summary>
    public const int DefaultRingSize = 2;
    /// <summary>
    /// Gets or sets the scale map mode.
    /// </summary>
    public QrPngModuleScaleMode Mode { get; set; } = QrPngModuleScaleMode.Rings;

    /// <summary>
    /// Gets or sets the minimum scale factor (0.1..1.0).
    /// </summary>
    public double MinScale { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the maximum scale factor (0.1..1.0).
    /// </summary>
    public double MaxScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the ring size in modules for <see cref="QrPngModuleScaleMode.Rings"/>.
    /// </summary>
    public int RingSize { get; set; } = DefaultRingSize;

    /// <summary>
    /// Gets or sets the random seed for <see cref="QrPngModuleScaleMode.Random"/>.
    /// </summary>
    public int Seed { get; set; } = 12345;

    /// <summary>
    /// When true, applies scale mapping to finder (eye) modules.
    /// </summary>
    public bool ApplyToEyes { get; set; } = false;

    internal void Validate() {
        if (MinScale is < 0.1 or > 1.0) throw new ArgumentOutOfRangeException(nameof(MinScale));
        if (MaxScale is < 0.1 or > 1.0) throw new ArgumentOutOfRangeException(nameof(MaxScale));
        if (MinScale > MaxScale) throw new ArgumentOutOfRangeException(nameof(MinScale));
        if (RingSize <= 0) throw new ArgumentOutOfRangeException(nameof(RingSize));
    }
}
