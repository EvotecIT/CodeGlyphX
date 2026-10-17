using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Simplified rendering options for 2D matrices (Data Matrix, PDF417).
/// </summary>
public sealed class MatrixOptions {
    /// <summary>Gets or sets optional human-readable text below the matrix, outside its quiet zone.</summary>
    /// <remarks>Uses the library's portable 5-by-7 label font in raster and vector output.</remarks>
    public string? LabelText { get; set; }

    /// <summary>Gets or sets the requested label height in pixels, rounded up to whole module cells.</summary>
    public int LabelFontSize { get; set; } = RenderDefaults.BarcodeLabelFontSize;

    /// <summary>Gets or sets the extra gap in pixels between the matrix quiet zone and its label.</summary>
    public int LabelMargin { get; set; } = RenderDefaults.BarcodeLabelMargin;

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

    internal MatrixOptions Clone() => (MatrixOptions)MemberwiseClone();
}
