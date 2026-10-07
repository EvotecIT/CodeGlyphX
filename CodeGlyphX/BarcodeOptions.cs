using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Simplified rendering options for 1D barcodes.
/// </summary>
public sealed class BarcodeOptions {
    /// <summary>
    /// Module size in pixels.
    /// </summary>
    public int ModuleSize { get; set; } = RenderDefaults.BarcodeModuleSize;

    /// <summary>
    /// Quiet zone size in modules.
    /// </summary>
    public int QuietZone { get; set; } = RenderDefaults.BarcodeQuietZone;

    /// <summary>
    /// Barcode height in modules.
    /// </summary>
    public int HeightModules { get; set; } = RenderDefaults.BarcodeHeightModules;

    /// <summary>
    /// Foreground color.
    /// </summary>
    public Rgba32 Foreground { get; set; } = RenderDefaults.BarcodeForeground;

    /// <summary>
    /// Background color.
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
    /// Label text color.
    /// </summary>
    public Rgba32 LabelColor { get; set; } = RenderDefaults.BarcodeForeground;

    /// <summary>
    /// Label font family (SVG/HTML).
    /// </summary>
    public string LabelFontFamily { get; set; } = RenderDefaults.BarcodeLabelFontFamily;

    internal BarcodeOptions Clone() => (BarcodeOptions)MemberwiseClone();
}
