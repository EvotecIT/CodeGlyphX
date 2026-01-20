namespace CodeGlyphX.Rendering;

/// <summary>
/// Basic image metadata returned by <see cref="ImageReader.TryReadInfo"/>.
/// </summary>
public readonly struct ImageInfo {
    /// <summary>
    /// Image format.
    /// </summary>
    public ImageFormat Format { get; }

    /// <summary>
    /// Image width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Image height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Indicates whether the info looks valid.
    /// </summary>
    public bool IsValid => Format != ImageFormat.Unknown && Width > 0 && Height > 0;

    /// <summary>
    /// Creates a new <see cref="ImageInfo"/>.
    /// </summary>
    public ImageInfo(ImageFormat format, int width, int height) {
        Format = format;
        Width = width;
        Height = height;
    }
}
