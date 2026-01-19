namespace CodeGlyphX;

/// <summary>
/// Diagnostics for 1D barcode decoding.
/// </summary>
public sealed class BarcodeDecodeDiagnostics {
    /// <summary>
    /// Number of scanline candidates tested.
    /// </summary>
    public int CandidateCount { get; internal set; }

    /// <summary>
    /// Number of decode attempts across transforms.
    /// </summary>
    public int AttemptCount { get; internal set; }

    /// <summary>
    /// True when an inverted candidate was tried.
    /// </summary>
    public bool InvertedTried { get; internal set; }

    /// <summary>
    /// True when a reversed candidate was tried.
    /// </summary>
    public bool ReversedTried { get; internal set; }

    /// <summary>
    /// True when decode succeeded.
    /// </summary>
    public bool Success { get; internal set; }

    /// <summary>
    /// Optional failure message when decoding fails.
    /// </summary>
    public string? Failure { get; internal set; }
}

