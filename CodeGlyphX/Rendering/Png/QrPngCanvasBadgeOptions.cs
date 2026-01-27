using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Decorative badge/tab/ribbon options drawn outside the QR bounds.
/// </summary>
public sealed class QrPngCanvasBadgeOptions {
    /// <summary>
    /// Badge shape.
    /// </summary>
    public QrPngCanvasBadgeShape Shape { get; set; } = QrPngCanvasBadgeShape.Badge;

    /// <summary>
    /// Badge position relative to the QR bounds.
    /// </summary>
    public QrPngCanvasBadgePosition Position { get; set; } = QrPngCanvasBadgePosition.Top;

    /// <summary>
    /// Badge width in pixels.
    /// </summary>
    public int WidthPx { get; set; } = 120;

    /// <summary>
    /// Badge height in pixels.
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
    /// Ribbon tail length in pixels (applies to <see cref="QrPngCanvasBadgeShape.Ribbon"/>).
    /// </summary>
    public int TailPx { get; set; } = 10;

    internal void Validate() {
        if (WidthPx < 0) throw new ArgumentOutOfRangeException(nameof(WidthPx));
        if (HeightPx < 0) throw new ArgumentOutOfRangeException(nameof(HeightPx));
        if (GapPx < 0) throw new ArgumentOutOfRangeException(nameof(GapPx));
        if (CornerRadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(CornerRadiusPx));
        if (TailPx < 0) throw new ArgumentOutOfRangeException(nameof(TailPx));
    }
}

