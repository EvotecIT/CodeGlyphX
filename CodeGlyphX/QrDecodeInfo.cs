namespace CodeGlyphX;

/// <summary>
/// High-level failure reasons for QR decoding.
/// </summary>
public enum QrDecodeFailureReason {
    /// <summary>
    /// No failure (decode succeeded).
    /// </summary>
    None = 0,
    /// <summary>
    /// Invalid input (null, non-square, or too small buffers).
    /// </summary>
    InvalidInput = 1,
    /// <summary>
    /// Invalid module size or version.
    /// </summary>
    InvalidSize = 2,
    /// <summary>
    /// Format information could not be recovered.
    /// </summary>
    FormatInfo = 3,
    /// <summary>
    /// Reed-Solomon error correction failed.
    /// </summary>
    ReedSolomon = 4,
    /// <summary>
    /// Payload parsing failed.
    /// </summary>
    Payload = 5,
    /// <summary>
    /// Decoding was cancelled.
    /// </summary>
    Cancelled = 6,
    /// <summary>No decoding attempt has initialized the diagnostics.</summary>
    Uninitialized = 7,
}

/// <summary>
/// Diagnostics returned by QR decoding attempts.
/// </summary>
public readonly struct QrDecodeInfo {
    private readonly bool _initialized;
    private readonly QrDecodeFailureReason _failure;
    /// <summary>
    /// Gets the failure reason.
    /// </summary>
    public QrDecodeFailureReason Failure => _initialized ? _failure : QrDecodeFailureReason.Uninitialized;

    /// <summary>
    /// Gets the decoded QR version (1..40) when available.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the error correction level when available.
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }

    /// <summary>
    /// Gets the mask pattern index when available.
    /// </summary>
    public int Mask { get; }

    /// <summary>
    /// Gets the best distance to a format pattern (lower is better).
    /// </summary>
    public int FormatBestDistance { get; }

    /// <summary>
    /// Gets a value indicating whether decoding succeeded.
    /// </summary>
    public bool IsSuccess => Failure == QrDecodeFailureReason.None;

    /// <summary>
    /// Gets a human-friendly message describing the failure.
    /// </summary>
    public string Message => Failure switch {
        QrDecodeFailureReason.None => $"ok v{Version} {ErrorCorrectionLevel} m{Mask}",
        QrDecodeFailureReason.InvalidInput => "invalid input (expect a square QR matrix)",
        QrDecodeFailureReason.InvalidSize => $"invalid size (v{Version})",
        QrDecodeFailureReason.FormatInfo => $"format info mismatch (best distance {FormatBestDistance})",
        QrDecodeFailureReason.ReedSolomon => $"reed-solomon failed (v{Version}, {ErrorCorrectionLevel}, m{Mask})",
        QrDecodeFailureReason.Payload => $"payload parse failed (v{Version}, {ErrorCorrectionLevel}, m{Mask})",
        QrDecodeFailureReason.Cancelled => "cancelled",
        QrDecodeFailureReason.Uninitialized => "QR decode diagnostics are uninitialized",
        _ => "unknown failure"
    };

    internal QrDecodeInfo(
        QrDecodeFailureReason failure,
        int version,
        QrErrorCorrectionLevel errorCorrectionLevel,
        int mask,
        int formatBestDistance) {
        _failure = failure == QrDecodeFailureReason.None && version is < 1 or > 40
            ? QrDecodeFailureReason.Uninitialized
            : failure;
        _initialized = true;
        Version = version;
        ErrorCorrectionLevel = errorCorrectionLevel;
        Mask = mask;
        FormatBestDistance = formatBestDistance;
    }

    internal static QrDecodeInfo FromInternal(QrDecodeDiagnostics diagnostics) {
        return new QrDecodeInfo(
            MapFailure(diagnostics.Failure),
            diagnostics.Version,
            diagnostics.ErrorCorrectionLevel,
            diagnostics.Mask,
            diagnostics.FormatBestDistance);
    }

    private static QrDecodeFailureReason MapFailure(QrDecodeFailure failure) {
        return failure switch {
            QrDecodeFailure.None => QrDecodeFailureReason.None,
            QrDecodeFailure.InvalidInput => QrDecodeFailureReason.InvalidInput,
            QrDecodeFailure.InvalidSize => QrDecodeFailureReason.InvalidSize,
            QrDecodeFailure.FormatInfo => QrDecodeFailureReason.FormatInfo,
            QrDecodeFailure.ReedSolomon => QrDecodeFailureReason.ReedSolomon,
            QrDecodeFailure.Payload => QrDecodeFailureReason.Payload,
            QrDecodeFailure.Cancelled => QrDecodeFailureReason.Cancelled,
            _ => QrDecodeFailureReason.InvalidInput
        };
    }

    /// <summary>Returns the diagnostic message.</summary>
    public override string ToString() => Message;
}
