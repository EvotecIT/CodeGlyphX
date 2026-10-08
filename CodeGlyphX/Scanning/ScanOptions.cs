using System.Threading;

namespace CodeGlyphX;

/// <summary>
/// Controls unified symbol scanning.
/// </summary>
public sealed class ScanOptions {
    /// <summary>
    /// Gets or sets the formats to scan. A null or empty array selects formats whose
    /// <see cref="SymbolCapability.IsDefaultScanFormat"/> is true.
    /// Module-only requested formats are reported through <see cref="ScanResult.UnsupportedFormats"/>.
    /// </summary>
    public SymbolFormat[]? Formats { get; set; }

    /// <summary>Gets or sets an optional region of interest in source-image coordinates.</summary>
    public ImageRegion? Region { get; set; }

    /// <summary>
    /// Gets or sets the total wall-clock deadline in milliseconds for image decoding, conversion, and recognition.
    /// The default is 500 milliseconds, matching <see cref="Balanced(int)"/>. Zero explicitly disables
    /// the deadline. Recognition cancellation is cooperative rather than hard real-time.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 500;

    /// <summary>Gets or sets the maximum number of results. Zero means unlimited.</summary>
    public int MaxSymbols { get; set; } = 32;

    /// <summary>Gets or sets whether equivalent format-and-payload results are deduplicated.</summary>
    /// <remarks>When false, repeated recognition observations are retained. This does not count physical instances: retries can observe the same symbol more than once.</remarks>
    public bool Deduplicate { get; set; } = true;

    /// <summary>Gets or sets whether bounded tile retries search for additional symbols.</summary>
    /// <remarks>Explicit QR or barcode options retain their own tile settings.</remarks>
    public bool EnableTileScan { get; set; } = true;

    /// <summary>Gets or sets the tile grid: zero selects two or three tiles per axis; explicit values are 2..4.</summary>
    public int TileGrid { get; set; }

    /// <summary>Gets or sets the scanner speed and accuracy profile.</summary>
    public ScanProfile Profile { get; set; } = ScanProfile.Balanced;

    /// <summary>Gets or sets advanced QR recognition options.</summary>
    public QrPixelDecodeOptions? Qr { get; set; }

    /// <summary>Gets or sets advanced linear-barcode recognition options.</summary>
    public BarcodeDecodeOptions? Barcode { get; set; }

    /// <summary>Gets or sets opt-in direct-part-mark preprocessing for Data Matrix images.</summary>
    public DirectPartMarkOptions? DirectPartMarking { get; set; }

    /// <summary>Gets or sets compressed-image decode limits and codec options.</summary>
    public ImageDecodeOptions? Image { get; set; }

    /// <summary>Gets or sets caller cancellation.</summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>Creates low-latency scan options.</summary>
    public static ScanOptions Fast(int timeoutMilliseconds = 150) {
        return new ScanOptions { Profile = ScanProfile.Fast, TimeoutMilliseconds = Normalize(timeoutMilliseconds), EnableTileScan = false };
    }

    /// <summary>Creates balanced scan options.</summary>
    public static ScanOptions Balanced(int timeoutMilliseconds = 500) {
        return new ScanOptions { Profile = ScanProfile.Balanced, TimeoutMilliseconds = Normalize(timeoutMilliseconds) };
    }

    /// <summary>Creates robust scan options.</summary>
    public static ScanOptions Robust(int timeoutMilliseconds = 1500) {
        return new ScanOptions {
            Profile = ScanProfile.Robust,
            TimeoutMilliseconds = Normalize(timeoutMilliseconds),
            DirectPartMarking = DirectPartMarkOptions.Auto()
        };
    }

    /// <summary>Creates bounded options suitable for screenshot and UI scanning.</summary>
    public static ScanOptions Screen(int timeoutMilliseconds = 300, int maxDimension = 1200) {
        var timeout = Normalize(timeoutMilliseconds);
        if (maxDimension < 0) throw new System.ArgumentOutOfRangeException(nameof(maxDimension));
        var dimension = maxDimension;
        return new ScanOptions {
            Profile = ScanProfile.Screen,
            TimeoutMilliseconds = timeout,
            Qr = QrPixelDecodeOptions.Screen(timeout, dimension),
            Image = ImageDecodeOptions.Screen(timeout, dimension)
        };
    }

    private static int Normalize(int value) {
        if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
        return value;
    }
}
