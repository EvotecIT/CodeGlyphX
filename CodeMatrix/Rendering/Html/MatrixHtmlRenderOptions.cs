namespace CodeMatrix.Rendering.Html;

/// <summary>
/// Options for rendering generic 2D matrices to HTML.
/// </summary>
public sealed class MatrixHtmlRenderOptions {
    /// <summary>
    /// Module size in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = 6;

    /// <summary>
    /// Quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = 4;

    /// <summary>
    /// Foreground color (CSS).
    /// </summary>
    public string DarkColor { get; set; } = "#000";

    /// <summary>
    /// Background color (CSS).
    /// </summary>
    public string LightColor { get; set; } = "#fff";

    /// <summary>
    /// When true, renders HTML using email-safe tables.
    /// </summary>
    public bool EmailSafeTable { get; set; }
}
