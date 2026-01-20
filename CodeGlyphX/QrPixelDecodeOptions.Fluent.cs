using System;

namespace CodeGlyphX;

public sealed partial class QrPixelDecodeOptions {
    /// <summary>
    /// Sets the profile (fast/balanced/robust).
    /// </summary>
    public QrPixelDecodeOptions WithProfile(QrDecodeProfile profile) {
        Profile = profile;
        return this;
    }

    /// <summary>
    /// Sets maximum dimension (pixels) for QR decoding.
    /// </summary>
    public QrPixelDecodeOptions WithMaxDimension(int maxDimension) {
        MaxDimension = maxDimension;
        return this;
    }

    /// <summary>
    /// Sets maximum scale to try (1..8).
    /// </summary>
    public QrPixelDecodeOptions WithMaxScale(int maxScale) {
        MaxScale = maxScale;
        return this;
    }

    /// <summary>
    /// Sets the maximum decode time budget.
    /// </summary>
    public QrPixelDecodeOptions WithMaxMilliseconds(int maxMilliseconds) {
        MaxMilliseconds = maxMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets a hard decode time budget without profile downgrades.
    /// </summary>
    public QrPixelDecodeOptions WithBudgetMilliseconds(int maxMilliseconds) {
        BudgetMilliseconds = maxMilliseconds < 0 ? 0 : maxMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the time+dimension budget in one call.
    /// </summary>
    public QrPixelDecodeOptions WithBudget(int maxMilliseconds, int maxDimension = 0) {
        MaxMilliseconds = Math.Max(0, maxMilliseconds);
        BudgetMilliseconds = MaxMilliseconds;
        if (maxDimension > 0) {
            MaxDimension = maxDimension;
        }
        return this;
    }

    /// <summary>
    /// Disables rotation/mirroring attempts.
    /// </summary>
    public QrPixelDecodeOptions WithoutTransforms() {
        DisableTransforms = true;
        return this;
    }

    /// <summary>
    /// Enables or disables aggressive sampling.
    /// </summary>
    public QrPixelDecodeOptions WithAggressiveSampling(bool enabled = true) {
        AggressiveSampling = enabled;
        return this;
    }
}
