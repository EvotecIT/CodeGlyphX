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
    /// Sets the cooperative time budget for one public decode call.
    /// </summary>
    public QrPixelDecodeOptions WithBudgetMilliseconds(int budgetMilliseconds) {
        BudgetMilliseconds = budgetMilliseconds < 0 ? 0 : budgetMilliseconds;
        return this;
    }

    /// <summary>
    /// Enables or disables auto-crop for QR decoding.
    /// </summary>
    public QrPixelDecodeOptions WithAutoCrop(bool enabled = true) {
        AutoCrop = enabled;
        return this;
    }

    /// <summary>
    /// Sets the time+dimension budget in one call.
    /// </summary>
    public QrPixelDecodeOptions WithBudget(int budgetMilliseconds, int maxDimension = 0) {
        BudgetMilliseconds = Math.Max(0, budgetMilliseconds);
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

    /// <summary>
    /// Enables or disables stylized sampling for QR art.
    /// </summary>
    public QrPixelDecodeOptions WithStylizedSampling(bool enabled = true) {
        StylizedSampling = enabled;
        return this;
    }

    /// <summary>
    /// Enables tile-based scanning for multiple QR codes.
    /// </summary>
    public QrPixelDecodeOptions WithTileScan(bool enabled = true, int tileGrid = 0) {
        EnableTileScan = enabled;
        TileGrid = tileGrid;
        return this;
    }
}
