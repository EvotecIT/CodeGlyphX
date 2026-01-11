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
    /// Style preset for PNG rendering.
    /// </summary>
    public QrRenderStyle Style { get; set; } = QrRenderStyle.Default;

    /// <summary>
    /// Overrides the module shape (when set).
    /// </summary>
    public QrPngModuleShape? ModuleShape { get; set; }

    /// <summary>
    /// Overrides the module scale (0.1..1.0).
    /// </summary>
    public double? ModuleScale { get; set; }

    /// <summary>
    /// Overrides the module corner radius in pixels.
    /// </summary>
    public int? ModuleCornerRadiusPx { get; set; }

    /// <summary>
    /// Overrides the foreground gradient.
    /// </summary>
    public QrPngGradientOptions? ForegroundGradient { get; set; }

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

    /// <summary>
    /// JPEG quality (1..100).
    /// </summary>
    public int JpegQuality { get; set; } = 85;

    /// <summary>
    /// When true, renders HTML using email-safe tables.
    /// </summary>
    public bool HtmlEmailSafeTable { get; set; }
}
