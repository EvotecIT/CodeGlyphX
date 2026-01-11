using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Simplified rendering options for 2D matrices (Data Matrix, PDF417).
/// </summary>
public sealed class MatrixOptions {
    /// <summary>
    /// Module size in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = RenderDefaults.QrModuleSize;

    /// <summary>
    /// Quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = RenderDefaults.QrQuietZone;

    /// <summary>
    /// Foreground color.
    /// </summary>
    public Rgba32 Foreground { get; set; } = RenderDefaults.QrForeground;

    /// <summary>
    /// Background color.
    /// </summary>
    public Rgba32 Background { get; set; } = RenderDefaults.QrBackground;

    /// <summary>
    /// JPEG quality (1..100).
    /// </summary>
    public int JpegQuality { get; set; } = 85;

    /// <summary>
    /// When true, renders HTML using email-safe tables.
    /// </summary>
    public bool HtmlEmailSafeTable { get; set; }
}
