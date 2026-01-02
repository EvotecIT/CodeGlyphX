namespace CodeMatrix.Rendering.Html;

/// <summary>
/// Options for <see cref="HtmlQrRenderer"/>.
/// </summary>
public sealed class QrHtmlRenderOptions {
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
    /// When true, produces an email-friendly table (no CSS background shorthand; uses <c>bgcolor</c> + compressed rows).
    /// </summary>
    public bool EmailSafeTable { get; set; }
}
