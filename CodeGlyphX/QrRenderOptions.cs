using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Appearance and layout for a previously encoded QR symbol. Encoding settings belong to <see cref="QrEncodingOptions"/>.
/// </summary>
/// <remarks>
/// Raster outputs support all appearance options. SVG, SVGZ and HTML support layout, colors,
/// basic shapes, foreground gradients, simple finder styling and logos. They reject unsupported
/// effects, including canvas art, palettes, procedural maps, connected shapes and decorative finder effects,
/// with <see cref="System.NotSupportedException"/> instead of silently omitting them.
/// </remarks>
public sealed class QrRenderOptions {
    /// <summary>
    /// Module size in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = RenderDefaults.QrModuleSize;

    /// <summary>
    /// Quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = RenderDefaults.QrQuietZone;

    /// <summary>
    /// Target output size in pixels (0 = disabled). When set, module size is adjusted to fit this target.
    /// </summary>
    public int TargetSizePx { get; set; } = 0;

    /// <summary>
    /// When true, <see cref="TargetSizePx"/> includes the quiet zone.
    /// </summary>
    public bool TargetSizeIncludesQuietZone { get; set; } = true;

    /// <summary>
    /// Foreground color.
    /// </summary>
    public Rgba32 Foreground { get; set; } = RenderDefaults.QrForeground;

    /// <summary>
    /// Background color.
    /// </summary>
    public Rgba32 Background { get; set; } = RenderDefaults.QrBackground;

    /// <summary>
    /// Optional background gradient.
    /// </summary>
    public QrGradientOptions? BackgroundGradient { get; set; }

    /// <summary>
    /// Optional pattern overlay for the QR background area.
    /// </summary>
    public QrBackgroundPatternOptions? BackgroundPattern { get; set; }

    /// <summary>
    /// Background supersample factor for gradients/patterns (1 = disabled).
    /// </summary>
    public int BackgroundSupersample { get; set; } = 1;

    /// <summary>
    /// Style preset for QR rendering.
    /// </summary>
    public QrRenderStyle Style { get; set; } = QrRenderStyle.Default;

    /// <summary>
    /// High-level QR art options (theme + variant + intensity).
    /// </summary>
    public QrArtOptions? Art { get; set; }

    /// <summary>
    /// When true, applies conservative static guardrails for art-heavy styles.
    /// This does not validate the rendered output with a decoder.
    /// </summary>
    public bool ArtGuardrailsEnabled { get; set; } = true;

    /// <summary>
    /// Minimum static heuristic score target (0..100) for art guardrails.
    /// </summary>
    public int ArtGuardrailMinimumScore { get; set; } = 80;

    /// <summary>
    /// Overrides the module shape (when set).
    /// </summary>
    public QrModuleShape? ModuleShape { get; set; }

    /// <summary>
    /// Overrides the module scale (0.1..1.0).
    /// </summary>
    public double? ModuleScale { get; set; }

    /// <summary>
    /// Overrides the module scale map.
    /// </summary>
    public QrModuleScaleMapOptions? ModuleScaleMap { get; set; }

    /// <summary>
    /// Overrides the module shape map.
    /// </summary>
    public QrModuleShapeMapOptions? ModuleShapeMap { get; set; }

    /// <summary>
    /// Overrides per-module jitter options.
    /// </summary>
    public QrModuleJitterOptions? ModuleJitter { get; set; }


    /// <summary>
    /// When true, keeps non-eye functional patterns at a conventional style.
    /// </summary>
    public bool ProtectFunctionalPatterns { get; set; } = true;

    /// <summary>
    /// When true and a background pattern is enabled, preserves a clean quiet zone.
    /// </summary>
    public bool ProtectQuietZone { get; set; } = true;

    /// <summary>
    /// Overrides the module corner radius in pixels.
    /// </summary>
    public int? ModuleCornerRadiusPx { get; set; }

    /// <summary>
    /// Overrides the foreground gradient.
    /// </summary>
    public QrGradientOptions? ForegroundGradient { get; set; }

    /// <summary>
    /// Overrides the foreground palette.
    /// </summary>
    public QrPaletteOptions? ForegroundPalette { get; set; }

    /// <summary>
    /// Overrides the foreground pattern overlay.
    /// </summary>
    public QrForegroundPatternOptions? ForegroundPattern { get; set; }

    /// <summary>
    /// Overrides palette zones.
    /// </summary>
    public QrPaletteZoneOptions? ForegroundPaletteZones { get; set; }

    /// <summary>
    /// Optional canvas options for sticker-style output.
    /// </summary>
    public QrCanvasOptions? Canvas { get; set; }

    /// <summary>
    /// Optional debug overlays for raster outputs.
    /// </summary>
    public QrRasterDebugOptions? Debug { get; set; }

    /// <summary>
    /// Overrides eye (finder) styling.
    /// </summary>
    public QrEyeOptions? Eyes { get; set; }

    /// <summary>
    /// Optional PNG image used as a centered logo in rendered outputs.
    /// </summary>
    public byte[]? LogoPng { get; set; }

    /// <summary>
    /// Logo size relative to the QR area (excluding quiet zone).
    /// </summary>
    public double LogoScale { get; set; } = 0.20;

    /// <summary>
    /// Padding around the logo in pixels.
    /// </summary>
    public int LogoPaddingPx { get; set; } = 4;

    /// <summary>
    /// Whether to draw a background plate behind the logo.
    /// </summary>
    public bool LogoDrawBackground { get; set; } = true;

    /// <summary>
    /// Logo background color (defaults to QR background).
    /// </summary>
    public Rgba32? LogoBackground { get; set; }

    /// <summary>
    /// Logo background corner radius in pixels.
    /// </summary>
    public int LogoCornerRadiusPx { get; set; } = 8;

}
