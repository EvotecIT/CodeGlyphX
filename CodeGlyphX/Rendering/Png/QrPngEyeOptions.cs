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
    /// Gets or sets the eye frame style when <see cref="UseFrame"/> is enabled.
    /// </summary>
    public QrPngEyeFrameStyle FrameStyle { get; set; } = QrPngEyeFrameStyle.Single;

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
    /// Optional per-eye outer colors (TopLeft, TopRight, BottomLeft).
    /// </summary>
    public Rgba32[]? OuterColors { get; set; }

    /// <summary>
    /// Optional inner eye color override.
    /// </summary>
    public Rgba32? InnerColor { get; set; }

    /// <summary>
    /// Optional per-eye inner colors (TopLeft, TopRight, BottomLeft).
    /// </summary>
    public Rgba32[]? InnerColors { get; set; }

    /// <summary>
    /// Optional gradient for the outer frame.
    /// </summary>
    public QrPngGradientOptions? OuterGradient { get; set; }

    /// <summary>
    /// Optional per-eye outer gradients (TopLeft, TopRight, BottomLeft).
    /// </summary>
    public QrPngGradientOptions[]? OuterGradients { get; set; }

    /// <summary>
    /// Optional gradient for the inner dot.
    /// </summary>
    public QrPngGradientOptions? InnerGradient { get; set; }

    /// <summary>
    /// Optional per-eye inner gradients (TopLeft, TopRight, BottomLeft).
    /// </summary>
    public QrPngGradientOptions[]? InnerGradients { get; set; }

    /// <summary>
    /// Optional glow radius in pixels when <see cref="FrameStyle"/> is <see cref="QrPngEyeFrameStyle.Glow"/>.
    /// A value of 0 uses a reasonable default based on module size.
    /// </summary>
    public int GlowRadiusPx { get; set; }

    /// <summary>
    /// Optional glow color override. Defaults to the outer eye color.
    /// </summary>
    public Rgba32? GlowColor { get; set; }

    /// <summary>
    /// Maximum glow alpha (0..255) when <see cref="FrameStyle"/> is <see cref="QrPngEyeFrameStyle.Glow"/>.
    /// </summary>
    public byte GlowAlpha { get; set; } = 110;

    /// <summary>
    /// Optional sparkle count drawn around the eyes on the canvas.
    /// </summary>
    public int SparkleCount { get; set; }

    /// <summary>
    /// Sparkle radius in pixels.
    /// </summary>
    public int SparkleRadiusPx { get; set; } = 3;

    /// <summary>
    /// How far sparkles can extend beyond the eye ring (in pixels).
    /// </summary>
    public int SparkleSpreadPx { get; set; } = 20;

    /// <summary>
    /// Sparkle color override. Defaults to the outer eye color.
    /// </summary>
    public Rgba32? SparkleColor { get; set; }

    /// <summary>
    /// Random seed for sparkle placement. Use 0 to auto-randomize per render.
    /// </summary>
    public int SparkleSeed { get; set; }

    /// <summary>
    /// When true (default), sparkles do not draw inside the QR area.
    /// </summary>
    public bool SparkleProtectQrArea { get; set; } = true;

    /// <summary>
    /// When false (default), sparkles render only when a canvas is enabled.
    /// </summary>
    public bool SparkleAllowOnQrBackground { get; set; }

    /// <summary>
    /// Optional accent ring count drawn around the eyes on the canvas.
    /// </summary>
    public int AccentRingCount { get; set; }

    /// <summary>
    /// Accent ring stroke thickness in pixels.
    /// </summary>
    public int AccentRingThicknessPx { get; set; } = 4;

    /// <summary>
    /// How far accent rings can extend beyond the eye ring (in pixels).
    /// </summary>
    public int AccentRingSpreadPx { get; set; } = 28;

    /// <summary>
    /// Small jitter applied to ring radius (in pixels).
    /// </summary>
    public int AccentRingJitterPx { get; set; } = 6;

    /// <summary>
    /// Accent ring color override. Defaults to the outer eye color with a softer alpha.
    /// </summary>
    public Rgba32? AccentRingColor { get; set; }

    /// <summary>
    /// Random seed for accent ring placement. Use 0 to auto-randomize per render.
    /// </summary>
    public int AccentRingSeed { get; set; }

    /// <summary>
    /// When true (default), accent rings do not draw inside the QR area.
    /// </summary>
    public bool AccentRingProtectQrArea { get; set; } = true;

    /// <summary>
    /// When false (default), accent rings render only when a canvas is enabled.
    /// </summary>
    public bool AccentRingAllowOnQrBackground { get; set; }

    internal void Validate() {
        if (OuterScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(OuterScale));
        if (InnerScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(InnerScale));
        if (GlowRadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(GlowRadiusPx));
        if (SparkleCount < 0) throw new ArgumentOutOfRangeException(nameof(SparkleCount));
        if (SparkleRadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(SparkleRadiusPx));
        if (SparkleSpreadPx < 0) throw new ArgumentOutOfRangeException(nameof(SparkleSpreadPx));
        if (AccentRingCount < 0) throw new ArgumentOutOfRangeException(nameof(AccentRingCount));
        if (AccentRingThicknessPx < 0) throw new ArgumentOutOfRangeException(nameof(AccentRingThicknessPx));
        if (AccentRingSpreadPx < 0) throw new ArgumentOutOfRangeException(nameof(AccentRingSpreadPx));
        if (AccentRingJitterPx < 0) throw new ArgumentOutOfRangeException(nameof(AccentRingJitterPx));
        if (OuterColors is { Length: not 3 }) throw new ArgumentOutOfRangeException(nameof(OuterColors), "OuterColors must have exactly 3 entries.");
        if (InnerColors is { Length: not 3 }) throw new ArgumentOutOfRangeException(nameof(InnerColors), "InnerColors must have exactly 3 entries.");
        if (OuterGradients is { Length: not 3 }) throw new ArgumentOutOfRangeException(nameof(OuterGradients), "OuterGradients must have exactly 3 entries.");
        if (InnerGradients is { Length: not 3 }) throw new ArgumentOutOfRangeException(nameof(InnerGradients), "InnerGradients must have exactly 3 entries.");
        OuterGradient?.Validate();
        InnerGradient?.Validate();
        if (OuterGradients is { Length: 3 }) {
            foreach (var gradient in OuterGradients) {
                gradient?.Validate();
            }
        }
        if (InnerGradients is { Length: 3 }) {
            foreach (var gradient in InnerGradients) {
                gradient?.Validate();
            }
        }
    }
}
