namespace CodeGlyphX.Rendering;

/// <summary>
/// Basic animation metadata returned by ImageReader.TryReadAnimationInfo.
/// </summary>
public readonly struct ImageAnimationInfo {
    /// <summary>
    /// Image format.
    /// </summary>
    public ImageFormat Format { get; }

    /// <summary>
    /// Canvas width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Canvas height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Number of frames in the animation.
    /// </summary>
    public int FrameCount { get; }

    /// <summary>
    /// Animation options such as loop count and background color.
    /// </summary>
    public ImageAnimationOptions Options { get; }

    /// <summary>
    /// Indicates whether the info looks valid.
    /// </summary>
    public bool IsValid => (Format == ImageFormat.Gif || Format == ImageFormat.Webp)
        && Width > 0
        && Height > 0
        && FrameCount > 0;

    /// <summary>
    /// Creates a new <see cref="ImageAnimationInfo"/>.
    /// </summary>
    public ImageAnimationInfo(ImageFormat format, int width, int height, int frameCount, ImageAnimationOptions options) {
        Format = format;
        Width = width;
        Height = height;
        FrameCount = frameCount;
        Options = options;
    }
}
