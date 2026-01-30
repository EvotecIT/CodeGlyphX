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
    /// Maximum milliseconds to spend decoding (best effort). Set to 0 to disable.
    /// </summary>
    public int MaxMilliseconds { get; set; } = 0;

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
