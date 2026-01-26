using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Debug overlay options for QR PNG rendering.
/// </summary>
public sealed class QrPngDebugOptions {
    /// <summary>
    /// Draws the quiet zone boundary.
    /// </summary>
    public bool ShowQuietZone { get; set; } = true;

    /// <summary>
    /// Draws the QR module area boundary.
    /// </summary>
    public bool ShowQrBounds { get; set; } = true;

    /// <summary>
    /// Draws finder (eye) bounds.
    /// </summary>
    public bool ShowEyeBounds { get; set; }

    /// <summary>
    /// Draws logo bounds (including background padding when enabled).
    /// </summary>
    public bool ShowLogoBounds { get; set; } = true;

    /// <summary>
    /// Stroke thickness in pixels.
    /// </summary>
    public int StrokePx { get; set; } = 1;

    /// <summary>
    /// Color used for quiet zone bounds.
    /// </summary>
    public Rgba32 QuietZoneColor { get; set; } = new Rgba32(255, 0, 128);

    /// <summary>
    /// Color used for QR bounds.
    /// </summary>
    public Rgba32 QrBoundsColor { get; set; } = new Rgba32(0, 200, 255);

    /// <summary>
    /// Color used for eye bounds.
    /// </summary>
    public Rgba32 EyeBoundsColor { get; set; } = new Rgba32(255, 196, 0);

    /// <summary>
    /// Color used for logo bounds.
    /// </summary>
    public Rgba32 LogoBoundsColor { get; set; } = new Rgba32(0, 255, 160);

    internal bool HasOverlay => ShowQuietZone || ShowQrBounds || ShowEyeBounds || ShowLogoBounds;

    internal void Validate() {
        if (StrokePx < 1 || StrokePx > 16) {
            throw new ArgumentOutOfRangeException(nameof(StrokePx), "StrokePx must be between 1 and 16.");
        }
    }
}
