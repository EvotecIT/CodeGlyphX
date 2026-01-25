using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Options for <see cref="QrPngRenderer"/>.
/// </summary>
public sealed partial class QrPngRenderOptions {
    /// <summary>
    /// Gets or sets the size of a single QR module in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = RenderDefaults.QrModuleSize;

    /// <summary>
    /// Gets or sets the quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = RenderDefaults.QrQuietZone;

    /// <summary>
    /// Gets or sets the foreground (dark) color.
    /// </summary>
    public Rgba32 Foreground { get; set; } = RenderDefaults.QrForeground;

    /// <summary>
    /// Gets or sets the background (light) color.
    /// </summary>
    public Rgba32 Background { get; set; } = RenderDefaults.QrBackground;

    /// <summary>
    /// Optional gradient for the background.
    /// </summary>
    public QrPngGradientOptions? BackgroundGradient { get; set; }

    /// <summary>
    /// Optional pattern overlay for the QR background area.
    /// </summary>
    public QrPngBackgroundPatternOptions? BackgroundPattern { get; set; }

    /// <summary>
    /// Background supersample factor for gradients/patterns (1 = disabled).
    /// </summary>
    public int BackgroundSupersample { get; set; } = 1;

    /// <summary>
    /// Optional gradient for the foreground (dark) modules.
    /// </summary>
    public QrPngGradientOptions? ForegroundGradient { get; set; }

    /// <summary>
    /// Optional multi-color palette for foreground modules.
    /// </summary>
    public QrPngPaletteOptions? ForegroundPalette { get; set; }

    /// <summary>
    /// Optional palette overrides for specific zones.
    /// </summary>
    public QrPngPaletteZoneOptions? ForegroundPaletteZones { get; set; }

    /// <summary>
    /// Optional eye (finder) styling overrides.
    /// </summary>
    public QrPngEyeOptions? Eyes { get; set; }

    /// <summary>
    /// Gets or sets the module shape.
    /// </summary>
    public QrPngModuleShape ModuleShape { get; set; } = QrPngModuleShape.Square;

    /// <summary>
    /// Gets or sets the scale of the module inside its cell (0.1..1.0).
    /// </summary>
    public double ModuleScale { get; set; } = 1.0;

    /// <summary>
    /// Optional per-module scale mapping.
    /// </summary>
    public QrPngModuleScaleMapOptions? ModuleScaleMap { get; set; }

    /// <summary>
    /// Gets or sets the corner radius in pixels for <see cref="QrPngModuleShape.Rounded"/>.
    /// </summary>
    public int ModuleCornerRadiusPx { get; set; }

    /// <summary>
    /// Optional logo overlay (centered).
    /// </summary>
    public QrPngLogoOptions? Logo { get; set; }

    /// <summary>
    /// Optional canvas options for sticker-style output.
    /// </summary>
    public QrPngCanvasOptions? Canvas { get; set; }

    /// <summary>
    /// Optional debug overlay options.
    /// </summary>
    public QrPngDebugOptions? Debug { get; set; }
}
