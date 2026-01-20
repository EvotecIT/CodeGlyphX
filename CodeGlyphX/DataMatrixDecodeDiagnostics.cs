namespace CodeGlyphX;

/// <summary>
/// Diagnostics for Data Matrix decoding.
/// </summary>
public sealed class DataMatrixDecodeDiagnostics {
    /// <summary>
    /// Number of decode attempts (rotations + mirror).
    /// </summary>
    public int AttemptCount { get; internal set; }

    /// <summary>
    /// True when mirrored decode was attempted.
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
}

