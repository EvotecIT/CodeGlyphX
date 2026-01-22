using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Options for <see cref="BarcodePngRenderer"/>.
/// </summary>
public sealed partial class BarcodePngRenderOptions {
    /// <summary>
    /// Gets or sets the size of a single module in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = RenderDefaults.BarcodeModuleSize;

    /// <summary>
    /// Gets or sets the quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = RenderDefaults.BarcodeQuietZone;

    /// <summary>
    /// Gets or sets the barcode height in modules.
    /// </summary>
    public int HeightModules { get; set; } = RenderDefaults.BarcodeHeightModules;

    /// <summary>
    /// Gets or sets the foreground (bar) color.
    /// </summary>
    public Rgba32 Foreground { get; set; } = RenderDefaults.BarcodeForeground;

    /// <summary>
    /// Gets or sets the background (space) color.
    /// </summary>
    public Rgba32 Background { get; set; } = RenderDefaults.BarcodeBackground;

    /// <summary>
    /// Optional label text rendered under the bars.
    /// </summary>
    public string? LabelText { get; set; }

    /// <summary>
    /// Label font size in pixels.
    /// </summary>
    public int LabelFontSize { get; set; } = RenderDefaults.BarcodeLabelFontSize;

    /// <summary>
    /// Vertical margin between bars and label in pixels.
    /// </summary>
    public int LabelMargin { get; set; } = RenderDefaults.BarcodeLabelMargin;

    /// <summary>
    /// Label color.
    /// </summary>
    public Rgba32 LabelColor { get; set; } = RenderDefaults.BarcodeForeground;
}
