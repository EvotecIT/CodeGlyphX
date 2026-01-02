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
}
