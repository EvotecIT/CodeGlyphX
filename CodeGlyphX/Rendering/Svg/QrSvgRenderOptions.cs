using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Svg;

/// <summary>
/// Options for <see cref="SvgQrRenderer"/>.
/// </summary>
public sealed partial class QrSvgRenderOptions {
    /// <summary>
    /// Gets or sets the size of a single QR module in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = RenderDefaults.QrModuleSize;

    /// <summary>
    /// Gets or sets the quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = RenderDefaults.QrQuietZone;

    /// <summary>
    /// Gets or sets the dark color (CSS value).
    /// </summary>
    public string DarkColor { get; set; } = RenderDefaults.QrForegroundCss;

    /// <summary>
    /// Gets or sets the light color (CSS value).
    /// </summary>
    public string LightColor { get; set; } = RenderDefaults.QrBackgroundCss;

    /// <summary>
    /// Optional logo overlay (PNG).
    /// </summary>
    public QrLogoOptions? Logo { get; set; }

    /// <summary>
    /// Gets or sets the module shape.
    /// </summary>
    /// <remarks>Connected module shapes require raster output and are rejected by the SVG renderer.</remarks>
    public QrModuleShape ModuleShape { get; set; } = QrModuleShape.Square;

    /// <summary>
    /// Gets or sets the scale of the module inside its cell (0.1..1.0).
    /// </summary>
    public double ModuleScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether non-eye functional patterns retain full square modules and the solid dark color.
    /// </summary>
    /// <remarks>Applies to timing, alignment, format, version and fixed dark modules in standard QR matrices.</remarks>
    public bool ProtectFunctionalPatterns { get; set; } = true;

    /// <summary>
    /// Gets or sets the corner radius in pixels for rounded modules.
    /// </summary>
    public int ModuleCornerRadiusPx { get; set; }

    /// <summary>
    /// Optional gradient for the foreground (dark) modules.
    /// </summary>
    public QrGradientOptions? ForegroundGradient { get; set; }

    /// <summary>
    /// Optional eye (finder) styling overrides.
    /// </summary>
    /// <remarks>Raster frame effects, per-eye arrays, accents and connected shapes are rejected rather than silently omitted.</remarks>
    public QrEyeOptions? Eyes { get; set; }
}
