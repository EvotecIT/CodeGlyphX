using System.Threading;

namespace CodeGlyphX;

/// <summary>
/// Options for unified QR/barcode decoding.
/// </summary>
public sealed class CodeGlyphDecodeOptions {
    /// <summary>
    /// Expected barcode type (optional hint for 1D decoding).
    /// </summary>
    public BarcodeType? ExpectedBarcode { get; set; }

    /// <summary>
    /// Prefer trying barcodes before 2D codes.
    /// </summary>
    public bool PreferBarcode { get; set; }

    /// <summary>
    /// Include 1D barcode results when decoding multiple symbols.
    /// </summary>
    public bool IncludeBarcode { get; set; } = true;

    /// <summary>
    /// QR decode options (profile, budget, etc.).
    /// </summary>
    public QrPixelDecodeOptions? Qr { get; set; }

    /// <summary>
    /// Speed/accuracy profile for QR pixel decoding (default: Robust).
    /// </summary>
    public QrDecodeProfile Profile {
        get => Qr?.Profile ?? QrDecodeProfile.Robust;
        set => EnsureQr().Profile = value;
    }

    /// <summary>
    /// Maximum dimension (pixels) for QR decoding. Larger inputs will be downscaled by sampling.
    /// Set to 0 to disable.
    /// </summary>
    public int MaxDimension {
        get => Qr?.MaxDimension ?? 0;
        set => EnsureQr().MaxDimension = value;
    }

    /// <summary>
    /// Maximum scale to try when decoding QR (1..8). Set to 0 to use profile defaults.
    /// </summary>
    public int MaxScale {
        get => Qr?.MaxScale ?? 0;
        set => EnsureQr().MaxScale = value;
    }

    /// <summary>
    /// Maximum milliseconds to spend decoding QR (best effort). Set to 0 to disable.
    /// </summary>
    public int MaxMilliseconds {
        get => Qr?.MaxMilliseconds ?? 0;
        set => EnsureQr().MaxMilliseconds = value;
    }

    /// <summary>
    /// Disable rotation/mirroring attempts for QR decoding, even in robust profiles.
    /// </summary>
    public bool DisableTransforms {
        get => Qr?.DisableTransforms ?? false;
        set => EnsureQr().DisableTransforms = value;
    }

    /// <summary>
    /// Enables extra thresholding/sampling passes for stylized or noisy QR codes (slower).
    /// </summary>
    public bool AggressiveSampling {
        get => Qr?.AggressiveSampling ?? false;
        set => EnsureQr().AggressiveSampling = value;
    }

    /// <summary>
    /// Cancellation token for decoding.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Screen preset (budgeted decode for UI capture scenarios).
    /// </summary>
    public static CodeGlyphDecodeOptions Screen(int maxMilliseconds = 300, int maxDimension = 1200) {
        return new CodeGlyphDecodeOptions {
            Qr = QrPixelDecodeOptions.Screen(maxMilliseconds, maxDimension)
        };
    }

    private QrPixelDecodeOptions EnsureQr() {
        return Qr ??= new QrPixelDecodeOptions();
    }
}
