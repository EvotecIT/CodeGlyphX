using CodeGlyphX;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Webp;

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

    /// <summary>
    /// Optional GIF animation frames for matrix outputs.
    /// </summary>
    public BitMatrix[]? GifFrames { get; set; }

    /// <summary>
    /// Optional WebP animation frames for matrix outputs.
    /// </summary>
    public BitMatrix[]? WebpFrames { get; set; }

    /// <summary>
    /// Optional GIF animation frames for barcode outputs.
    /// </summary>
    public Barcode1D[]? BarcodeGifFrames { get; set; }

    /// <summary>
    /// Optional WebP animation frames for barcode outputs.
    /// </summary>
    public Barcode1D[]? BarcodeWebpFrames { get; set; }

    /// <summary>
    /// Optional per-frame durations (ms) for GIF/WebP animations.
    /// </summary>
    public int[]? AnimationDurationsMs { get; set; }

    /// <summary>
    /// Optional constant frame duration (ms) for GIF/WebP animations.
    /// </summary>
    public int AnimationDurationMs { get; set; } = 100;

    /// <summary>
    /// Optional GIF animation options.
    /// </summary>
    public GifAnimationOptions GifAnimationOptions { get; set; } = default;

    /// <summary>
    /// Optional WebP animation options.
    /// </summary>
    public WebpAnimationOptions WebpAnimationOptions { get; set; } = default;
}
