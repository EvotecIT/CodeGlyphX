using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Decorative band drawn outside the QR bounds (often used as a quiet-zone ring).
/// </summary>
public sealed class QrPngCanvasBandOptions {
    /// <summary>
    /// Band thickness in pixels.
    /// </summary>
    public int BandPx { get; set; } = 12;

    /// <summary>
    /// Gap between the QR bounds (including quiet zone) and the band.
    /// </summary>
    public int GapPx { get; set; }

    /// <summary>
    /// Band corner radius in pixels.
    /// </summary>
    public int RadiusPx { get; set; } = 18;

    /// <summary>
    /// Band color.
    /// </summary>
    public Rgba32 Color { get; set; } = new(30, 50, 90, 210);

    /// <summary>
    /// Optional band gradient.
    /// </summary>
    public QrPngGradientOptions? Gradient { get; set; }

    /// <summary>
    /// Optional edge pattern overlay.
    /// </summary>
    public QrPngCanvasEdgePatternOptions? EdgePattern { get; set; }

    internal void Validate() {
        if (BandPx < 0) throw new ArgumentOutOfRangeException(nameof(BandPx));
        if (GapPx < 0) throw new ArgumentOutOfRangeException(nameof(GapPx));
        if (RadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(RadiusPx));
        Gradient?.Validate();
        EdgePattern?.Validate();
    }
}

