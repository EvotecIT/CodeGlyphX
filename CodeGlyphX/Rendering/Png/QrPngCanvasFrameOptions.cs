using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Decorative frame options drawn around the QR bounds (outside the QR area).
/// </summary>
public sealed class QrPngCanvasFrameOptions {
    /// <summary>
    /// Frame thickness in pixels.
    /// </summary>
    public int ThicknessPx { get; set; } = 12;

    /// <summary>
    /// Gap between the QR bounds (including quiet zone) and the frame.
    /// </summary>
    public int GapPx { get; set; } = 8;

    /// <summary>
    /// Frame corner radius in pixels.
    /// </summary>
    public int RadiusPx { get; set; } = 20;

    /// <summary>
    /// Frame color.
    /// </summary>
    public Rgba32 Color { get; set; } = new(20, 24, 40, 220);

    /// <summary>
    /// Optional frame gradient (overrides <see cref="Color"/> when set).
    /// </summary>
    public QrPngGradientOptions? Gradient { get; set; }

    /// <summary>
    /// Optional edge pattern overlay.
    /// </summary>
    public QrPngCanvasEdgePatternOptions? EdgePattern { get; set; }

    /// <summary>
    /// Optional inner frame thickness in pixels (0 = disabled).
    /// </summary>
    public int InnerThicknessPx { get; set; }

    /// <summary>
    /// Gap from the QR bounds to the inner frame.
    /// </summary>
    public int InnerGapPx { get; set; } = 3;

    /// <summary>
    /// Optional inner frame color. When null, <see cref="Color"/> is used.
    /// </summary>
    public Rgba32? InnerColor { get; set; }

    /// <summary>
    /// Optional inner frame gradient (overrides <see cref="InnerColor"/> when set).
    /// </summary>
    public QrPngGradientOptions? InnerGradient { get; set; }

    /// <summary>
    /// Optional inner edge pattern overlay.
    /// </summary>
    public QrPngCanvasEdgePatternOptions? InnerEdgePattern { get; set; }

    internal void Validate() {
        if (ThicknessPx < 0) throw new ArgumentOutOfRangeException(nameof(ThicknessPx));
        if (GapPx < 0) throw new ArgumentOutOfRangeException(nameof(GapPx));
        if (RadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(RadiusPx));
        if (InnerThicknessPx < 0) throw new ArgumentOutOfRangeException(nameof(InnerThicknessPx));
        if (InnerGapPx < 0) throw new ArgumentOutOfRangeException(nameof(InnerGapPx));
        Gradient?.Validate();
        InnerGradient?.Validate();
        EdgePattern?.Validate();
        InnerEdgePattern?.Validate();
    }
}
