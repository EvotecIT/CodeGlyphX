using System;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Canvas options for sticker-style QR output.
/// </summary>
public sealed class QrCanvasOptions {
    // Curated dark compositions keep paper shadows on the dark field, inside the canvas.
    internal bool ShadowOnPanelsOnly { get; set; }
    /// <summary>Filled caption band's colour; null uses the frame accent.</summary>
    public Rgba32? BandColor { get; set; }
    /// <summary>Frame stroke colour; null uses Frame.Color or a contrasting default.</summary>
    public Rgba32? StrokeColor { get; set; }
    /// <summary>Frame stroke in pixels; zero chooses a width relative to the code.</summary>
    public int StrokeWidthPx { get; set; }
    /// <summary>Optional speech tail on ScanMePill. SpeechBubble retains this treatment as an alias.</summary>
    public bool CaptionTail { get; set; }
    /// <summary>Optional PNG logo displayed beside the band caption, outside the code.</summary>
    public byte[]? BandLogoPng { get; set; }

    /// <summary>Radius of the light QR panel. Restricted to the quiet-zone corners.</summary>
    public int QrCornerRadiusPx { get; set; }
    /// <summary>Soft shadow extent in pixels. The layout reserves space on all sides.</summary>
    public int ShadowBlurPx { get; set; }
    /// <summary>Offset of a second paper panel behind the QR panel; zero disables it.</summary>
    public int StackedPanelOffsetPx { get; set; }
    /// <summary>Colour of the second paper panel.</summary>
    public Rgba32 StackedPanelColor { get; set; } = new(235, 226, 210);
    /// <summary>Optional bloom colour around the light QR panel.</summary>
    public Rgba32? PanelGlowColor { get; set; }
    /// <summary>Bloom extent outside the QR panel, in pixels.</summary>
    public int PanelGlowRadiusPx { get; set; }
    /// <summary>Optional wider bloom behind the primary panel glow.</summary>
    public Rgba32? SecondaryPanelGlowColor { get; set; }
    /// <summary>Extent of the secondary panel bloom in pixels.</summary>
    public int SecondaryPanelGlowRadiusPx { get; set; }

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
    public QrGradientOptions? BackgroundGradient { get; set; }

    /// <summary>
    /// Optional background pattern overlay.
    /// </summary>
    public QrBackgroundPatternOptions? Pattern { get; set; }

    /// <summary>
    /// Optional paint splash/drip overlay (kept outside the QR area).
    /// </summary>
    public QrCanvasSplashOptions? Splash { get; set; }

    /// <summary>
    /// Optional halo/glow drawn around the QR bounds (outside by default).
    /// </summary>
    public QrCanvasHaloOptions? Halo { get; set; }

    /// <summary>
    /// Optional edge vignette drawn outside the QR area by default.
    /// </summary>
    public QrCanvasVignetteOptions? Vignette { get; set; }

    /// <summary>
    /// Optional grain/noise texture drawn outside the QR area by default.
    /// </summary>
    public QrCanvasGrainOptions? Grain { get; set; }

    /// <summary>
    /// Optional decorative frame drawn around the QR bounds (kept outside the QR area).
    /// </summary>
    public QrCanvasFrameOptions? Frame { get; set; }

    /// <summary>
    /// Optional decorative band drawn outside the QR bounds (quiet-zone ring).
    /// </summary>
    public QrCanvasBandOptions? Band { get; set; }

    /// <summary>
    /// Optional badge/tab/ribbon drawn outside the QR bounds.
    /// </summary>
    public QrCanvasBadgeOptions? Badge { get; set; }

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

    /// <summary>Optional single-line caption, rendered with portable font outlines.</summary>
    public QrCanvasCaptionOptions? Caption { get; set; }

    /// <summary>Optional call-to-action frame. Replaces the decorative Frame, Band, Badge and Border treatments.</summary>
    public QrCanvasTemplate Template { get; set; }

    internal void Validate() {
        if (!Enum.IsDefined(typeof(QrCanvasTemplate), Template)) throw new ArgumentOutOfRangeException(nameof(Template));
        Caption?.Validate();
        if (StrokeWidthPx < 0) throw new ArgumentOutOfRangeException(nameof(StrokeWidthPx));
        if (QrCornerRadiusPx < 0 || ShadowBlurPx < 0 || StackedPanelOffsetPx < 0 || PanelGlowRadiusPx < 0 || SecondaryPanelGlowRadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(QrCornerRadiusPx));
        if (PaddingPx < 0) throw new ArgumentOutOfRangeException(nameof(PaddingPx));
        if (CornerRadiusPx < 0) throw new ArgumentOutOfRangeException(nameof(CornerRadiusPx));
        if (BorderPx < 0) throw new ArgumentOutOfRangeException(nameof(BorderPx));
        BackgroundGradient?.Validate();
        Pattern?.Validate();
        Splash?.Validate();
        Halo?.Validate();
        Vignette?.Validate();
        Grain?.Validate();
        Frame?.Validate();
        Band?.Validate();
        Badge?.Validate();
    }
}
