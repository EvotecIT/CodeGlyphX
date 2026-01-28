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
    /// WebP quality (0..100). A value of 100 uses lossless VP8L.
    /// </summary>
    public int WebpQuality { get; set; } = 100;

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
