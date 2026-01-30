using CodeGlyphX.Rendering.Jpeg;

namespace CodeGlyphX;

public sealed partial class ImageDecodeOptions {
    /// <summary>
    /// Sets maximum image dimension (pixels) for decoding.
    /// </summary>
    public ImageDecodeOptions WithMaxDimension(int maxDimension) {
        MaxDimension = maxDimension;
        return this;
    }

    /// <summary>
    /// Sets the maximum decode time budget (milliseconds).
    /// </summary>
    public ImageDecodeOptions WithMaxMilliseconds(int maxMilliseconds) {
        MaxMilliseconds = maxMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the time+dimension budget in one call.
    /// </summary>
    public ImageDecodeOptions WithBudget(int maxMilliseconds, int maxDimension = 0) {
        MaxMilliseconds = maxMilliseconds;
        if (maxDimension > 0) {
            MaxDimension = maxDimension;
        }
        return this;
    }

    /// <summary>
    /// Sets JPEG decoding options.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new ImageDecodeOptions()
    ///     .WithJpegOptions(highQualityChroma: true, allowTruncated: true);
    /// </code>
    /// </example>
    public ImageDecodeOptions WithJpegOptions(JpegDecodeOptions options) {
        JpegOptions = options;
        return this;
    }

    /// <summary>
    /// Sets JPEG decoding options.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new ImageDecodeOptions()
    ///     .WithJpegOptions(highQualityChroma: true);
    /// </code>
    /// </example>
    public ImageDecodeOptions WithJpegOptions(bool highQualityChroma = false, bool allowTruncated = false) {
        JpegOptions = new JpegDecodeOptions(highQualityChroma, allowTruncated);
        return this;
    }
}
