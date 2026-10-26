using System;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Options for a foreground gradient fill.
/// </summary>
public sealed class QrGradientOptions {
    /// <summary>Colour space used between stops. OKLCH preserves vivid hue; sRGB is available for legacy artwork.</summary>
    public QrGradientInterpolation Interpolation { get; set; } = QrGradientInterpolation.Oklch;

    /// <summary>
    /// Gets or sets the gradient type.
    /// </summary>
    public QrGradientType Type { get; set; } = QrGradientType.Horizontal;

    /// <summary>
    /// Gets or sets the start color.
    /// </summary>
    public Rgba32 StartColor { get; set; } = Rgba32.Black;

    /// <summary>
    /// Gets or sets the end color.
    /// </summary>
    public Rgba32 EndColor { get; set; } = Rgba32.Black;

    /// <summary>
    /// Radial gradient center (0..1) along X.
    /// </summary>
    public double CenterX { get; set; } = 0.5;

    /// <summary>
    /// Radial gradient center (0..1) along Y.
    /// </summary>
    public double CenterY { get; set; } = 0.5;

    internal void Validate() {
        if (!Enum.IsDefined(typeof(QrGradientInterpolation), Interpolation)) throw new ArgumentOutOfRangeException(nameof(Interpolation));
        if (CenterX is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(CenterX));
        if (CenterY is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(CenterY));
    }
}
