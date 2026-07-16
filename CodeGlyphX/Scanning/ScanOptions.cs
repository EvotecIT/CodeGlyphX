using System.Threading;

namespace CodeGlyphX;

/// <summary>
/// Controls unified symbol scanning.
/// </summary>
public sealed class ScanOptions {
    /// <summary>
    /// Gets or sets the formats to scan. A null or empty array selects every image-scannable format.
    /// Module-only requested formats are reported through <see cref="ScanResult.UnsupportedFormats"/>.
    /// </summary>
    public SymbolFormat[]? Formats { get; set; }

    /// <summary>Gets or sets an optional region of interest in source-image coordinates.</summary>
    public ImageRegion? Region { get; set; }

    /// <summary>
    /// Gets or sets the total wall-clock deadline in milliseconds for image decoding, conversion, and recognition.
    /// Zero disables the deadline. Recognition cancellation is cooperative rather than hard real-time.
    /// </summary>
    public int TimeoutMilliseconds { get; set; }

    /// <summary>Gets or sets the maximum number of results. Zero means unlimited.</summary>
    public int MaxSymbols { get; set; } = 32;

    /// <summary>Gets or sets whether equivalent format-and-payload results are deduplicated.</summary>
    public bool Deduplicate { get; set; } = true;

    /// <summary>Gets or sets the scanner speed and accuracy profile.</summary>
    public ScanProfile Profile { get; set; } = ScanProfile.Balanced;

    /// <summary>Gets or sets advanced QR recognition options.</summary>
    public QrPixelDecodeOptions? Qr { get; set; }

    /// <summary>Gets or sets advanced linear-barcode recognition options.</summary>
    public BarcodeDecodeOptions? Barcode { get; set; }

    /// <summary>Gets or sets compressed-image decode limits and codec options.</summary>
    public ImageDecodeOptions? Image { get; set; }

    /// <summary>Gets or sets caller cancellation.</summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>Creates low-latency scan options.</summary>
    public static ScanOptions Fast(int timeoutMilliseconds = 150) {
        return new ScanOptions { Profile = ScanProfile.Fast, TimeoutMilliseconds = Normalize(timeoutMilliseconds) };
    }

    /// <summary>Creates balanced scan options.</summary>
    public static ScanOptions Balanced(int timeoutMilliseconds = 500) {
        return new ScanOptions { Profile = ScanProfile.Balanced, TimeoutMilliseconds = Normalize(timeoutMilliseconds) };
    }

    /// <summary>Creates robust scan options.</summary>
    public static ScanOptions Robust(int timeoutMilliseconds = 1500) {
        return new ScanOptions { Profile = ScanProfile.Robust, TimeoutMilliseconds = Normalize(timeoutMilliseconds) };
    }

    /// <summary>Creates bounded options suitable for screenshot and UI scanning.</summary>
    public static ScanOptions Screen(int timeoutMilliseconds = 300, int maxDimension = 1200) {
        var timeout = Normalize(timeoutMilliseconds);
        var dimension = maxDimension < 0 ? 0 : maxDimension;
        return new ScanOptions {
            Profile = ScanProfile.Screen,
            TimeoutMilliseconds = timeout,
            Qr = QrPixelDecodeOptions.Screen(timeout, dimension),
            Image = ImageDecodeOptions.Screen(timeout, dimension)
        };
    }

    private static int Normalize(int value) => value < 0 ? 0 : value;
}
