using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Describes a single RGBA32 page for TIFF encoding.
/// </summary>
public readonly struct TiffRgba32Page {
    /// <summary>
    /// Creates a TIFF page backed by an RGBA32 buffer.
    /// </summary>
    public TiffRgba32Page(byte[] rgba, int width, int height, int stride, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        Rgba = rgba ?? throw new ArgumentNullException(nameof(rgba));
        Width = width;
        Height = height;
        Stride = stride;
        Compression = compression;
        RowsPerStrip = 0;
    }

    /// <summary>
    /// Creates a TIFF page backed by an RGBA32 buffer with explicit rows-per-strip.
    /// </summary>
    public TiffRgba32Page(byte[] rgba, int width, int height, int stride, TiffCompressionMode compression, int rowsPerStrip) {
        Rgba = rgba ?? throw new ArgumentNullException(nameof(rgba));
        Width = width;
        Height = height;
        Stride = stride;
        Compression = compression;
        RowsPerStrip = rowsPerStrip;
    }

    /// <summary>
    /// RGBA32 pixel buffer.
    /// </summary>
    public byte[] Rgba { get; }

    /// <summary>
    /// Page width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Page height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Page stride in bytes.
    /// </summary>
    public int Stride { get; }

    /// <summary>
    /// Compression to use for the page.
    /// </summary>
    public TiffCompressionMode Compression { get; }

    /// <summary>
    /// Rows per strip (0 = single strip).
    /// </summary>
    public int RowsPerStrip { get; }
}
