using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Canvas options for sticker-style QR output.
/// </summary>
public sealed class QrPngCanvasOptions {
    /// <summary>
    /// Padding around the QR (in pixels).
    /// </summary>
    public int PaddingPx { get; set; } = 24;

    /// <summary>
    /// Canvas corner radius in pixels.
    /// </summary>
    public int CornerRadiusPx { get; set; } = 24;

    /// <summary>
    /// Canvas background color.
    /// </summary>
    public Rgba32 Background { get; set; } = RenderDefaults.QrBackground;

    /// <summary>
    /// Optional canvas background gradient.
    /// </summary>
    public QrPngGradientOptions? BackgroundGradient { get; set; }

    /// <summary>
    /// Optional background pattern overlay.
    /// </summary>
    public QrPngBackgroundPatternOptions? Pattern { get; set; }

    /// <summary>
    /// Optional paint splash/drip overlay (kept outside the QR area).
    /// </summary>
    public QrPngCanvasSplashOptions? Splash { get; set; }

    /// <summary>
    /// Optional halo/glow drawn around the QR bounds (outside by default).
    /// </summary>
    public QrPngCanvasHaloOptions? Halo { get; set; }

    /// <summary>
    /// Optional edge vignette drawn on the canvas (QR-safe by default).
    /// </summary>
    public QrPngCanvasVignetteOptions? Vignette { get; set; }

    /// <summary>
    /// Optional grain/noise texture drawn on the canvas (QR-safe by default).
    /// </summary>
    public QrPngCanvasGrainOptions? Grain { get; set; }

    /// <summary>
    /// Border thickness in pixels (0 = no border).
    /// </summary>
    public int BorderPx { get; set; }

    /// <summary>
    /// Border color (defaults to background).
    /// </summary>
    public Rgba32? BorderColor { get; set; }

    /// <summary>
    /// Shadow offset in pixels (X).
    /// </summary>
    public int ShadowOffsetX { get; set; }

    /// <summary>
    /// Shadow offset in pixels (Y).
    /// </summary>
    public int ShadowOffsetY { get; set; }

    /// <summary>
    /// Shadow color (alpha recommended).
    /// </summary>
    public Rgba32 ShadowColor { get; set; } = new(0, 0, 0, 48);

    internal void Validate() {
        if (PaddingPx < 0) throw new ArgumentOutOfRangeException(nameof(PaddingPx));
        if (CornerRadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(CornerRadiusPx));
        if (BorderPx < 0) throw new ArgumentOutOfRangeException(nameof(BorderPx));
        BackgroundGradient?.Validate();
        Pattern?.Validate();
        Splash?.Validate();
        Halo?.Validate();
        Vignette?.Validate();
        Grain?.Validate();
    }
}
