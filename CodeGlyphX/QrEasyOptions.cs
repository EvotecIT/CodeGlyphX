using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Simple options for QR rendering.
/// </summary>
public sealed class QrEasyOptions {
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
    /// Optional error correction level override.
    /// </summary>
    public QrErrorCorrectionLevel? ErrorCorrectionLevel { get; set; }

    /// <summary>
    /// Optional text encoding override (when set, emits ECI for non-default encodings).
    /// </summary>
    public QrTextEncoding? TextEncoding { get; set; }

    /// <summary>
    /// When true, emits ECI headers for non-default encodings.
    /// </summary>
    public bool IncludeEci { get; set; } = true;

    /// <summary>
    /// When true, payload defaults may override version/ECC settings.
    /// </summary>
    public bool RespectPayloadDefaults { get; set; } = true;

    /// <summary>
    /// Minimum QR version (1..40).
    /// </summary>
    public int MinVersion { get; set; } = 1;

    /// <summary>
    /// Maximum QR version (1..40).
    /// </summary>
    public int MaxVersion { get; set; } = 40;

    /// <summary>
    /// Optional forced mask pattern (0..7).
    /// </summary>
    public int? ForceMask { get; set; }

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
    /// Style preset for PNG rendering.
    /// </summary>
    public QrRenderStyle Style { get; set; } = QrRenderStyle.Default;

    /// <summary>
    /// High-level QR art options (theme + variant + intensity).
    /// </summary>
    public QrArtOptions? Art { get; set; }

    /// <summary>
    /// When true, applies scan-safety auto-tuning for art-heavy styles.
    /// </summary>
    public bool ArtAutoTune { get; set; } = true;

    /// <summary>
    /// Minimum safety score target (0..100) for art auto-tuning.
    /// </summary>
    public int ArtAutoTuneMinScore { get; set; } = 80;

    /// <summary>
    /// Overrides the module shape (when set).
    /// </summary>
    public QrPngModuleShape? ModuleShape { get; set; }

    /// <summary>
    /// Overrides the module scale (0.1..1.0).
    /// </summary>
    public double? ModuleScale { get; set; }

    /// <summary>
    /// Overrides the module scale map.
    /// </summary>
    public QrPngModuleScaleMapOptions? ModuleScaleMap { get; set; }

    /// <summary>
    /// Overrides the module shape map.
    /// </summary>
    public QrPngModuleShapeMapOptions? ModuleShapeMap { get; set; }

    /// <summary>
    /// Overrides per-module jitter options.
    /// </summary>
    public QrPngModuleJitterOptions? ModuleJitter { get; set; }


    /// <summary>
    /// When true, keeps non-eye functional patterns at a stable, scan-friendly style.
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
    public QrPngGradientOptions? ForegroundGradient { get; set; }

    /// <summary>
    /// Overrides the foreground palette.
    /// </summary>
    public QrPngPaletteOptions? ForegroundPalette { get; set; }

    /// <summary>
    /// Overrides the foreground pattern overlay.
    /// </summary>
    public QrPngForegroundPatternOptions? ForegroundPattern { get; set; }

    /// <summary>
    /// Overrides palette zones.
    /// </summary>
    public QrPngPaletteZoneOptions? ForegroundPaletteZones { get; set; }

    /// <summary>
    /// Optional canvas options for sticker-style output.
    /// </summary>
    public QrPngCanvasOptions? Canvas { get; set; }

    /// <summary>
    /// Optional debug overlay options (PNG only).
    /// </summary>
    public QrPngDebugOptions? Debug { get; set; }

    /// <summary>
    /// Overrides eye (finder) styling.
    /// </summary>
    public QrPngEyeOptions? Eyes { get; set; }

    /// <summary>
    /// Optional logo PNG bytes (embedded for PNG/SVG/HTML).
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
    /// When enabled, the encoder may auto-bump the minimum version for scan safety.
    /// </summary>
    public bool LogoDrawBackground { get; set; } = true;

    /// <summary>
    /// When true, bumps <see cref="MinVersion"/> to a safer minimum for logo background plates.
    /// </summary>
    public bool AutoBumpVersionForLogoBackground { get; set; } = true;

    /// <summary>
    /// Minimum version to use when a logo background plate is enabled (0 = disable auto bump).
    /// </summary>
    public int LogoBackgroundMinVersion { get; set; } = 8;

    /// <summary>
    /// Logo background color (defaults to QR background).
    /// </summary>
    public Rgba32? LogoBackground { get; set; }

    /// <summary>
    /// Logo background corner radius in pixels.
    /// </summary>
    public int LogoCornerRadiusPx { get; set; } = 8;

    /// <summary>
    /// JPEG quality (1..100).
    /// </summary>
    public int JpegQuality { get; set; } = 85;

    /// <summary>
    /// ICO output sizes in pixels (1..256). Defaults to common icon sizes.
    /// </summary>
    public int[]? IcoSizes { get; set; }

    /// <summary>
    /// When true, preserves aspect ratio and pads to square for ICO.
    /// </summary>
    public bool IcoPreserveAspectRatio { get; set; } = true;

    /// <summary>
    /// When true, renders HTML using email-safe tables.
    /// </summary>
    public bool HtmlEmailSafeTable { get; set; }
}
