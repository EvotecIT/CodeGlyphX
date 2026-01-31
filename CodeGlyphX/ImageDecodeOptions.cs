using CodeGlyphX.Rendering.Jpeg;

namespace CodeGlyphX;

/// <summary>
/// Options for decoding from image sources (non-QR).
/// </summary>
public sealed partial class ImageDecodeOptions {
    /// <summary>
    /// Maximum image dimension (pixels) for decoding. Larger inputs will be downscaled.
    /// Set to 0 to disable.
    /// </summary>
    public int MaxDimension { get; set; } = 0;

    /// <summary>
    /// Maximum pixel count allowed for decoding (width * height). Set to 0 to disable.
    /// </summary>
    public long MaxPixels { get; set; } = 0;

    /// <summary>
    /// Maximum input size in bytes for decoding. Set to 0 to disable.
    /// </summary>
    public int MaxBytes { get; set; } = 0;

    /// <summary>
    /// Maximum milliseconds to spend decoding (best effort). Set to 0 to disable.
    /// </summary>
    public int MaxMilliseconds { get; set; } = 0;

    /// <summary>
    /// Maximum animation frame count allowed for decoding. Set to 0 to use global defaults.
    /// </summary>
    public int MaxAnimationFrames { get; set; } = 0;

    /// <summary>
    /// Maximum total animation duration (milliseconds) allowed for decoding. Set to 0 to use global defaults.
    /// </summary>
    public int MaxAnimationDurationMs { get; set; } = 0;

    /// <summary>
    /// Maximum pixel count allowed per animation frame. Set to 0 to use global defaults.
    /// </summary>
    public long MaxAnimationFramePixels { get; set; } = 0;

    /// <summary>
    /// Optional JPEG decoding options (chroma upsampling, truncated handling).
    /// </summary>
    public JpegDecodeOptions? JpegOptions { get; set; }

    /// <summary>
    /// Screen preset (budgeted decode for UI capture scenarios).
    /// </summary>
    public static ImageDecodeOptions Screen(int maxMilliseconds = 300, int maxDimension = 1200) {
        return new ImageDecodeOptions {
            MaxMilliseconds = maxMilliseconds < 0 ? 0 : maxMilliseconds,
            MaxDimension = maxDimension < 0 ? 0 : maxDimension
        };
    }
}
