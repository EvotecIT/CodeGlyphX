using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Styling overrides for QR finder (eye) patterns.
/// </summary>
public sealed class QrPngEyeOptions {
    /// <summary>
    /// Gets or sets whether to draw each eye as a single frame (outer ring + inner dot).
    /// </summary>
    public bool UseFrame { get; set; }

    /// <summary>
    /// Gets or sets the outer (7x7) eye module shape.
    /// </summary>
    public QrPngModuleShape OuterShape { get; set; } = QrPngModuleShape.Square;

    /// <summary>
    /// Gets or sets the inner (3x3) eye module shape.
    /// </summary>
    public QrPngModuleShape InnerShape { get; set; } = QrPngModuleShape.Square;

    /// <summary>
    /// Gets or sets the scale of the outer modules inside their cells (0.1..1.0).
    /// </summary>
    public double OuterScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the scale of the inner modules inside their cells (0.1..1.0).
    /// </summary>
    public double InnerScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the corner radius for outer modules (pixels).
    /// </summary>
    public int OuterCornerRadiusPx { get; set; }

    /// <summary>
    /// Gets or sets the corner radius for inner modules (pixels).
    /// </summary>
    public int InnerCornerRadiusPx { get; set; }

    /// <summary>
    /// Optional outer eye color override.
    /// </summary>
    public Rgba32? OuterColor { get; set; }

    /// <summary>
    /// Optional inner eye color override.
    /// </summary>
    public Rgba32? InnerColor { get; set; }

    /// <summary>
    /// Optional gradient for the outer frame.
    /// </summary>
    public QrPngGradientOptions? OuterGradient { get; set; }

    /// <summary>
    /// Optional gradient for the inner dot.
    /// </summary>
    public QrPngGradientOptions? InnerGradient { get; set; }

    internal void Validate() {
        if (OuterScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(OuterScale));
        if (InnerScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(InnerScale));
        OuterGradient?.Validate();
        InnerGradient?.Validate();
    }
}
