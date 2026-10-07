using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Options for <see cref="QrPngRenderer"/>.
/// </summary>
public sealed partial class QrPngRenderOptions {
    /// <summary>
    /// Minimum allowed background supersample factor.
    /// </summary>
    public const int BackgroundSupersampleMin = 1;

    /// <summary>
    /// Maximum allowed background supersample factor (capped to avoid large memory spikes).
    /// </summary>
    public const int BackgroundSupersampleMax = 4;

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
    public QrGradientOptions? BackgroundGradient { get; set; }

    /// <summary>
    /// Optional pattern overlay for the QR background area.
    /// </summary>
    public QrBackgroundPatternOptions? BackgroundPattern { get; set; }

    /// <summary>
    /// Background supersample factor for gradients/patterns (1 = disabled, max 4).
    /// </summary>
    public int BackgroundSupersample { get; set; } = BackgroundSupersampleMin;

    /// <summary>
    /// Optional gradient for the foreground (dark) modules.
    /// </summary>
    public QrGradientOptions? ForegroundGradient { get; set; }

    /// <summary>
    /// Optional multi-color palette for foreground modules.
    /// </summary>
    public QrPaletteOptions? ForegroundPalette { get; set; }

    /// <summary>
    /// Optional pattern overlay for foreground modules.
    /// </summary>
    public QrForegroundPatternOptions? ForegroundPattern { get; set; }

    /// <summary>
    /// Optional palette overrides for specific zones.
    /// </summary>
    public QrPaletteZoneOptions? ForegroundPaletteZones { get; set; }

    /// <summary>
    /// Optional eye (finder) styling overrides.
    /// </summary>
    public QrEyeOptions? Eyes { get; set; }

    /// <summary>
    /// Gets or sets the module shape.
    /// </summary>
    public QrModuleShape ModuleShape { get; set; } = QrModuleShape.Square;

    /// <summary>
    /// Gets or sets the scale of the module inside its cell (0.1..1.0).
    /// </summary>
    public double ModuleScale { get; set; } = 1.0;

    /// <summary>
    /// Optional per-module scale mapping.
    /// </summary>
    public QrModuleScaleMapOptions? ModuleScaleMap { get; set; }

    /// <summary>
    /// Optional per-module shape mapping.
    /// </summary>
    public QrModuleShapeMapOptions? ModuleShapeMap { get; set; }

    /// <summary>
    /// Optional per-module jitter (organic placement).
    /// </summary>
    public QrModuleJitterOptions? ModuleJitter { get; set; }


    /// <summary>
    /// When true, keeps non-eye functional patterns (timing/alignment/format/version/dark module)
    /// at full scale and a stable foreground color to preserve conventional geometry.
    /// </summary>
    public bool ProtectFunctionalPatterns { get; set; } = true;

    /// <summary>
    /// When true and a background pattern is enabled, skips drawing the pattern inside the quiet zone.
    /// </summary>
    public bool ProtectQuietZone { get; set; } = true;

    /// <summary>
    /// Gets or sets the corner radius in pixels for <see cref="QrModuleShape.Rounded"/>.
    /// </summary>
    public int ModuleCornerRadiusPx { get; set; }

    /// <summary>
    /// Optional logo overlay (centered).
    /// </summary>
    public QrRasterLogoOptions? Logo { get; set; }

    /// <summary>
    /// Optional canvas options for sticker-style output.
    /// </summary>
    public QrCanvasOptions? Canvas { get; set; }

    /// <summary>
    /// Optional debug overlay options.
    /// </summary>
    public QrRasterDebugOptions? Debug { get; set; }

    /// <summary>
    /// PNG compression level (0 = stored/uncompressed, 1-9 = compressed).
    /// </summary>
    public int PngCompressionLevel { get; set; }
}
