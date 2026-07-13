using System;

namespace CodeGlyphX;

/// <summary>
/// Options for pixel-based QR decoding.
/// </summary>
public sealed partial class QrPixelDecodeOptions {
    /// <summary>
    /// Speed/accuracy profile (default: Robust).
    /// </summary>
    public QrDecodeProfile Profile { get; set; } = QrDecodeProfile.Robust;

    /// <summary>
    /// Maximum dimension (pixels) for decoding. Larger inputs will be downscaled by sampling.
    /// Set to 0 to disable.
    /// </summary>
    public int MaxDimension { get; set; } = 0;

    /// <summary>
    /// Maximum scale to try when decoding (1..8). Set to 0 to use profile defaults.
    /// </summary>
    public int MaxScale { get; set; } = 0;

    /// <summary>
    /// Cooperative time budget in milliseconds for one public decode call. Set to 0 to disable.
    /// The decoder checks this budget between recognition passes; it is not a hard real-time limit.
    /// </summary>
    public int BudgetMilliseconds { get; set; } = 0;

    /// <summary>
    /// Attempts to auto-crop likely QR regions before decoding (useful for screenshots).
    /// </summary>
    public bool AutoCrop { get; set; } = false;

    /// <summary>
    /// Enables tile-based scanning for multiple QR codes in one image.
    /// </summary>
    public bool EnableTileScan { get; set; } = false;

    /// <summary>
    /// Tile grid size for multi-scan (0 = auto, 2..4 recommended).
    /// </summary>
    public int TileGrid { get; set; } = 0;

    /// <summary>
    /// Disable rotation/mirroring attempts even in robust profiles.
    /// </summary>
    public bool DisableTransforms { get; set; } = false;

    /// <summary>
    /// Enables extra thresholding/sampling passes for stylized or noisy QR codes (slower).
    /// </summary>
    public bool AggressiveSampling { get; set; } = false;

    /// <summary>
    /// Enables stylized sampling tuned for QR art (opt-in, slower).
    /// </summary>
    public bool StylizedSampling { get; set; } = false;

    /// <summary>
    /// Fast preset (lower accuracy, fewer transforms).
    /// </summary>
    public static QrPixelDecodeOptions Fast() {
        return new QrPixelDecodeOptions { Profile = QrDecodeProfile.Fast };
    }

    /// <summary>
    /// Balanced preset (good default for most images).
    /// </summary>
    public static QrPixelDecodeOptions Balanced() {
        return new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced };
    }

    /// <summary>
    /// Robust preset (best accuracy, slower).
    /// </summary>
    public static QrPixelDecodeOptions Robust() {
        return new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust };
    }

    /// <summary>
    /// Stylized preset (adds aggressive sampling for QR art).
    /// </summary>
    public static QrPixelDecodeOptions Stylized() {
        return new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust, AggressiveSampling = true, StylizedSampling = true };
    }

    /// <summary>
    /// Screen preset (budgeted decode for UI capture scenarios).
    /// </summary>
    public static QrPixelDecodeOptions Screen(int budgetMilliseconds = 300, int maxDimension = 1200) {
        var budget = Math.Max(0, budgetMilliseconds);
        return new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Balanced,
            BudgetMilliseconds = budget,
            MaxDimension = Math.Max(0, maxDimension),
            AutoCrop = true
        };
    }
}
