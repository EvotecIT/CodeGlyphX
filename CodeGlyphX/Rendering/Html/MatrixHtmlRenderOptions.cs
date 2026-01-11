using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Html;

/// <summary>
/// Options for rendering generic 2D matrices to HTML.
/// </summary>
public sealed class MatrixHtmlRenderOptions {
    /// <summary>
    /// Module size in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = RenderDefaults.QrModuleSize;

    /// <summary>
    /// Quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = RenderDefaults.QrQuietZone;

    /// <summary>
    /// Foreground color (CSS).
    /// </summary>
    public string DarkColor { get; set; } = RenderDefaults.QrForegroundCss;

    /// <summary>
    /// Background color (CSS).
    /// </summary>
    public string LightColor { get; set; } = RenderDefaults.QrBackgroundCss;

    /// <summary>
    /// When true, renders HTML using email-safe tables.
    /// </summary>
    public bool EmailSafeTable { get; set; }
}
