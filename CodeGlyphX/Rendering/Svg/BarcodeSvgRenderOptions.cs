namespace CodeGlyphX.Rendering.Svg;

/// <summary>
/// Options for <see cref="SvgBarcodeRenderer"/>.
/// </summary>
public sealed class BarcodeSvgRenderOptions {
    /// <summary>
    /// Gets or sets the size of a single module in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = 10;

    /// <summary>
    /// Gets or sets the barcode height in modules.
    /// </summary>
    public int HeightModules { get; set; } = 40;

    /// <summary>
    /// Gets or sets the bar color (CSS value).
    /// </summary>
    public string BarColor { get; set; } = "#000";

    /// <summary>
    /// Gets or sets the background color (CSS value).
    /// </summary>
    public string BackgroundColor { get; set; } = "#fff";
}
