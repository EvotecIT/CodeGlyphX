namespace CodeGlyphX.Rendering;

/// <summary>
/// Limit type for image decode guard violations.
/// </summary>
public enum ImageDecodeLimitKind {
    /// <summary>
    /// Input exceeded byte size limit.
    /// </summary>
    MaxBytes,
    /// <summary>
    /// Image dimensions exceeded pixel limit.
    /// </summary>
    MaxPixels,
    /// <summary>
    /// Animation exceeded maximum frame count.
    /// </summary>
    MaxAnimationFrames,
    /// <summary>
    /// Animation exceeded total duration limit.
    /// </summary>
    MaxAnimationDurationMs,
    /// <summary>
    /// Animation frame exceeded pixel limit.
    /// </summary>
    MaxAnimationFramePixels
}

/// <summary>
/// Details about a decode guard violation.
/// </summary>
public readonly struct ImageDecodeLimitViolation {
    /// <summary>
    /// Creates a new limit violation description.
    /// </summary>
    public ImageDecodeLimitViolation(
        ImageDecodeLimitKind kind,
        long limit,
        long actual,
        ImageFormat format = ImageFormat.Unknown,
        int? pageIndex = null) {
        Kind = kind;
        Limit = limit;
        Actual = actual;
        Format = format;
        PageIndex = pageIndex;
    }

    /// <summary>
    /// The limit that was exceeded.
    /// </summary>
    public ImageDecodeLimitKind Kind { get; }

    /// <summary>
    /// The configured limit (0 when unknown).
    /// </summary>
    public long Limit { get; }

    /// <summary>
    /// The observed value (0 when unknown).
    /// </summary>
    public long Actual { get; }

    /// <summary>
    /// Detected image format, if known.
    /// </summary>
    public ImageFormat Format { get; }

    /// <summary>
    /// Image page index when applicable (null when unknown).
    /// </summary>
    public int? PageIndex { get; }
}
