using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Html;

/// <summary>
/// Options for <see cref="HtmlBarcodeRenderer"/>.
/// </summary>
public sealed partial class BarcodeHtmlRenderOptions {
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
    /// Gets or sets the bar color (CSS value).
    /// </summary>
    public string BarColor { get; set; } = RenderDefaults.BarcodeForegroundCss;

    /// <summary>
    /// Gets or sets the background color (CSS value).
    /// </summary>
    public string BackgroundColor { get; set; } = RenderDefaults.BarcodeBackgroundCss;

    /// <summary>
    /// When true, produces an email-friendly table (uses <c>bgcolor</c>).
    /// </summary>
    public bool EmailSafeTable { get; set; }

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
    /// Label color (CSS value).
    /// </summary>
    public string LabelColor { get; set; } = RenderDefaults.BarcodeForegroundCss;

    /// <summary>
    /// Label font family.
    /// </summary>
    public string LabelFontFamily { get; set; } = RenderDefaults.BarcodeLabelFontFamily;
}
