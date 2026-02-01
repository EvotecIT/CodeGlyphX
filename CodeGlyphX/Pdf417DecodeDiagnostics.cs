namespace CodeGlyphX;

/// <summary>
/// Diagnostics for PDF417 decoding.
/// </summary>
public sealed class Pdf417DecodeDiagnostics : IDecodeDiagnostics {
    /// <summary>
    /// Number of decode attempts.
    /// </summary>
    public int AttemptCount { get; internal set; }

    /// <summary>
    /// Number of start-pattern candidates detected.
    /// </summary>
    public int StartPatternCandidates { get; internal set; }

    /// <summary>
    /// Number of start-pattern candidates tried.
    /// </summary>
    public int StartPatternAttempts { get; internal set; }

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
