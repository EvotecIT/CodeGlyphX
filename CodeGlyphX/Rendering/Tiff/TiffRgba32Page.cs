using System;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Describes a decoded TIFF page backed by an RGBA32 buffer.
/// </summary>
public readonly struct TiffRgba32Page {
    /// <summary>
    /// Creates a TIFF page backed by an RGBA32 buffer.
    /// </summary>
    public TiffRgba32Page(byte[] rgba, int width, int height, int stride) {
        Rgba = rgba ?? throw new ArgumentNullException(nameof(rgba));
        Width = width;
        Height = height;
        Stride = stride;
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
}
