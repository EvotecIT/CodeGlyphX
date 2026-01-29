using CodeGlyphX.Rendering.Ascii;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Optional render extras for format-specific output.
/// </summary>
public sealed class RenderExtras {
    /// <summary>
    /// Vector or raster output for PDF/EPS.
    /// </summary>
    public RenderMode VectorMode { get; set; } = RenderMode.Vector;

    /// <summary>
    /// Optional HTML title (wraps HTML output).
    /// </summary>
    public string? HtmlTitle { get; set; }

    /// <summary>
    /// ASCII render options for matrix codes.
    /// </summary>
    public MatrixAsciiRenderOptions? MatrixAscii { get; set; }

    /// <summary>
    /// ASCII render options for barcodes.
    /// </summary>
    public BarcodeAsciiRenderOptions? BarcodeAscii { get; set; }
}
