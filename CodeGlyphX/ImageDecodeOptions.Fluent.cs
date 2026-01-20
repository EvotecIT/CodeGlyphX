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
}
