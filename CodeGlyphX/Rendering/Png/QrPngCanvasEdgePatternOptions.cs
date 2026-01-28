using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Edge pattern options for frames and badges.
/// </summary>
public sealed class QrPngCanvasEdgePatternOptions {
    /// <summary>
    /// Pattern type.
    /// </summary>
    public QrPngCanvasEdgePatternType Type { get; set; } = QrPngCanvasEdgePatternType.Dots;

    /// <summary>
    /// Pattern color.
    /// </summary>
    public Rgba32 Color { get; set; } = new(255, 255, 255, 160);

    /// <summary>
    /// Pattern thickness in pixels.
    /// </summary>
    public int ThicknessPx { get; set; } = 2;

    /// <summary>
    /// Spacing between pattern elements in pixels.
    /// </summary>
    public int SpacingPx { get; set; } = 8;

    /// <summary>
    /// Dash length in pixels (used by dashes/stitches).
    /// </summary>
    public int DashPx { get; set; } = 10;

    /// <summary>
    /// Inset from the outer edge in pixels.
    /// </summary>
    public int InsetPx { get; set; } = 1;

    internal void Validate() {
        if (ThicknessPx < 0) throw new ArgumentOutOfRangeException(nameof(ThicknessPx));
        if (SpacingPx < 0) throw new ArgumentOutOfRangeException(nameof(SpacingPx));
        if (DashPx < 0) throw new ArgumentOutOfRangeException(nameof(DashPx));
        if (InsetPx < 0) throw new ArgumentOutOfRangeException(nameof(InsetPx));
    }
}

