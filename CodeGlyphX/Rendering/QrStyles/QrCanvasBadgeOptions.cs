using System;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Decorative badge/tab/ribbon options drawn outside the QR bounds.
/// </summary>
public sealed class QrCanvasBadgeOptions {
    /// <summary>
    /// Badge shape.
    /// </summary>
    public QrCanvasBadgeShape Shape { get; set; } = QrCanvasBadgeShape.Badge;

    /// <summary>
    /// Badge position relative to the QR bounds.
    /// </summary>
    public QrCanvasBadgePosition Position { get; set; } = QrCanvasBadgePosition.Top;

    /// <summary>
    /// Badge width in pixels (0 disables drawing).
    /// </summary>
    public int WidthPx { get; set; } = 120;

    /// <summary>
    /// Badge height in pixels (0 disables drawing).
    /// </summary>
    public int HeightPx { get; set; } = 32;

    /// <summary>
    /// Gap between the QR bounds and the badge.
    /// </summary>
    public int GapPx { get; set; } = 8;

    /// <summary>
    /// Offset along the edge (horizontal for top/bottom, vertical for left/right).
    /// </summary>
    public int OffsetPx { get; set; }

    /// <summary>
    /// Badge corner radius in pixels.
    /// </summary>
    public int CornerRadiusPx { get; set; } = 12;

    /// <summary>
    /// Badge color.
    /// </summary>
    public Rgba32 Color { get; set; } = new(30, 40, 80, 220);

    /// <summary>
    /// Optional badge gradient (overrides <see cref="Color"/> when set).
    /// </summary>
    public QrGradientOptions? Gradient { get; set; }

    /// <summary>
    /// Optional edge pattern overlay.
    /// </summary>
    public QrCanvasEdgePatternOptions? EdgePattern { get; set; }

    /// <summary>
    /// Ribbon tail length in pixels (applies to <see cref="QrCanvasBadgeShape.Ribbon"/>).
    /// </summary>
    public int TailPx { get; set; } = 10;

    internal void Validate() {
        if (WidthPx < 0) throw new ArgumentOutOfRangeException(nameof(WidthPx));
        if (HeightPx < 0) throw new ArgumentOutOfRangeException(nameof(HeightPx));
        if (GapPx < 0) throw new ArgumentOutOfRangeException(nameof(GapPx));
        if (CornerRadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(CornerRadiusPx));
        if (TailPx < 0) throw new ArgumentOutOfRangeException(nameof(TailPx));
        Gradient?.Validate();
        EdgePattern?.Validate();
    }
}
