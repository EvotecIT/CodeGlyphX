namespace CodeGlyphX;

/// <summary>
/// Describes why decoding failed.
/// </summary>
public enum DecodeFailureReason {
    /// <summary>
    /// No failure (success).
    /// </summary>
    None,
    /// <summary>
    /// Input was invalid.
    /// </summary>
    InvalidInput,
    /// <summary>
    /// Image format or data was not supported.
    /// </summary>
    UnsupportedFormat,
    /// <summary>
    /// Feature is not supported on this target framework.
    /// </summary>
    PlatformNotSupported,
    /// <summary>
    /// Operation was cancelled.
    /// </summary>
    Cancelled,
    /// <summary>
    /// Decoding completed but no symbols were found.
    /// </summary>
    NoResult,
    /// <summary>
    /// An unexpected error occurred.
    /// </summary>
    Error
}
