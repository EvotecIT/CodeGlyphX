namespace CodeGlyphX;

/// <summary>
/// Diagnostics for Aztec decoding.
/// </summary>
public sealed class AztecDecodeDiagnostics : IDecodeDiagnostics {
    /// <summary>
    /// Number of decode attempts (threshold/orientation combinations).
    /// </summary>
    public int AttemptCount { get; internal set; }

    /// <summary>
    /// True when inverted decoding was attempted.
    /// </summary>
    public bool InvertedTried { get; internal set; }

    /// <summary>
    /// True when mirrored decoding was attempted.
    /// </summary>
    public bool MirroredTried { get; internal set; }

    /// <summary>
    /// True when decode succeeded.
    /// </summary>
    public bool Success { get; internal set; }

    /// <summary>
    /// Optional failure message when decoding fails.
    /// </summary>
    public string? Failure { get; internal set; }

    void IDecodeDiagnostics.SetFailure(string? value) => Failure = value;
}
