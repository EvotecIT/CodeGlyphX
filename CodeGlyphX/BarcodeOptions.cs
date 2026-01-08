using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Simplified rendering options for 1D barcodes.
/// </summary>
public sealed class BarcodeOptions {
    /// <summary>
    /// Module size in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = 2;

    /// <summary>
    /// Quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = 10;

    /// <summary>
    /// Barcode height in modules.
    /// </summary>
    public int HeightModules { get; set; } = 40;

    /// <summary>
    /// Foreground color.
    /// </summary>
    public Rgba32 Foreground { get; set; } = Rgba32.Black;

    /// <summary>
    /// Background color.
    /// </summary>
    public Rgba32 Background { get; set; } = Rgba32.White;

    /// <summary>
    /// JPEG quality (1..100).
    /// </summary>
    public int JpegQuality { get; set; } = 90;

    /// <summary>
    /// When true, renders HTML using email-safe tables.
    /// </summary>
    public bool HtmlEmailSafeTable { get; set; }
}
