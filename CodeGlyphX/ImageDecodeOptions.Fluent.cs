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
    /// Sets the cooperative symbol-recognition budget. Raster image decoding itself is not timed.
    /// </summary>
    public ImageDecodeOptions WithRecognitionBudget(int milliseconds) {
        RecognitionBudgetMilliseconds = milliseconds < 0 ? 0 : milliseconds;
        return this;
    }

    /// <summary>
    /// Sets the symbol-recognition time budget and maximum output dimension in one call.
    /// </summary>
    public ImageDecodeOptions WithRecognitionBudget(int milliseconds, int maxDimension) {
        RecognitionBudgetMilliseconds = milliseconds < 0 ? 0 : milliseconds;
        if (maxDimension > 0) {
            MaxDimension = maxDimension;
        }
        return this;
    }

    /// <summary>
    /// Sets the maximum pixel count allowed for decoding.
    /// </summary>
    public ImageDecodeOptions WithMaxPixels(long maxPixels) {
        MaxPixels = maxPixels;
        return this;
    }

    /// <summary>
    /// Sets the maximum input size in bytes for decoding.
    /// </summary>
    public ImageDecodeOptions WithMaxBytes(int maxBytes) {
        MaxBytes = maxBytes;
        return this;
    }

    /// <summary>
    /// Sets the maximum animation frame count allowed for decoding.
    /// </summary>
    public ImageDecodeOptions WithMaxAnimationFrames(int maxFrames) {
        MaxAnimationFrames = maxFrames;
        return this;
    }

    /// <summary>
    /// Sets the maximum total animation duration (milliseconds) allowed for decoding.
    /// </summary>
    public ImageDecodeOptions WithMaxAnimationDurationMs(int maxDurationMs) {
        MaxAnimationDurationMs = maxDurationMs;
        return this;
    }

    /// <summary>
    /// Sets the maximum pixel count allowed per animation frame.
    /// </summary>
    public ImageDecodeOptions WithMaxAnimationFramePixels(long maxPixels) {
        MaxAnimationFramePixels = maxPixels;
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
