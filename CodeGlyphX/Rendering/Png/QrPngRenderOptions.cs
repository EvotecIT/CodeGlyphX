using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Options for <see cref="QrPngRenderer"/>.
/// </summary>
public sealed class QrPngRenderOptions {
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
    /// Optional gradient for the foreground (dark) modules.
    /// </summary>
    public QrPngGradientOptions? ForegroundGradient { get; set; }

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
    /// Gets or sets the corner radius in pixels for <see cref="QrPngModuleShape.Rounded"/>.
    /// </summary>
    public int ModuleCornerRadiusPx { get; set; }

    /// <summary>
    /// Optional logo overlay (centered).
    /// </summary>
    public QrPngLogoOptions? Logo { get; set; }
}
