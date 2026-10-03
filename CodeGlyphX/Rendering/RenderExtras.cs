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
    /// Optional PNG compression override: zero stores pixels without compression; 1-9 enable compression.
    /// Null preserves the renderer's default. Applies to QR, matrix and linear PNG outputs.
    /// </summary>
    public int? PngCompressionLevel { get; set; }

    internal int ResolvePngCompression(int fallback) {
        if (!PngCompressionLevel.HasValue) return fallback;
        var level = PngCompressionLevel.Value;
        if (level < 0 || level > 9) throw new System.ArgumentOutOfRangeException(nameof(PngCompressionLevel), "PNG compression must be between zero and nine.");
        return level;
    }

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
    /// Console-friendly ASCII options (auto-fit).
    /// </summary>
    public AsciiConsoleOptions? AsciiConsole { get; set; }

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

    /// <summary>
    /// TIFF compression selection for TIFF outputs.
    /// </summary>
    public TiffCompressionMode TiffCompression { get; set; } = TiffCompressionMode.Auto;
}
