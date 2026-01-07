using CodeMatrix.Rendering;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Rendering.Svg;

/// <summary>
/// Options for <see cref="SvgQrRenderer"/>.
/// </summary>
public sealed class QrSvgRenderOptions {
    /// <summary>
    /// Gets or sets the size of a single QR module in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = 4;

    /// <summary>
    /// Gets or sets the quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = 4;

    /// <summary>
    /// Gets or sets the dark color (CSS value).
    /// </summary>
    public string DarkColor { get; set; } = "#000";

    /// <summary>
    /// Gets or sets the light color (CSS value).
    /// </summary>
    public string LightColor { get; set; } = "#fff";

    /// <summary>
    /// Optional logo overlay (PNG).
    /// </summary>
    public QrLogoOptions? Logo { get; set; }

    /// <summary>
    /// Gets or sets the module shape.
    /// </summary>
    public QrPngModuleShape ModuleShape { get; set; } = QrPngModuleShape.Square;

    /// <summary>
    /// Gets or sets the scale of the module inside its cell (0.1..1.0).
    /// </summary>
    public double ModuleScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the corner radius in pixels for rounded modules.
    /// </summary>
    public int ModuleCornerRadiusPx { get; set; }

    /// <summary>
    /// Optional gradient for the foreground (dark) modules.
    /// </summary>
    public QrPngGradientOptions? ForegroundGradient { get; set; }

    /// <summary>
    /// Optional eye (finder) styling overrides.
    /// </summary>
    public QrPngEyeOptions? Eyes { get; set; }
}
