namespace CodeGlyphX;

internal enum QrDecodeFailure {
    None = 0,
    InvalidInput = 1,
    InvalidSize = 2,
    FormatInfo = 3,
    ReedSolomon = 4,
    Payload = 5,
    Cancelled = 6,
}

internal readonly struct QrDecodeDiagnostics {
    public QrDecodeFailure Failure { get; }
    public int Version { get; }
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }
    public int Mask { get; }
    public int FormatBestDistance { get; }

    public QrDecodeDiagnostics(
        QrDecodeFailure failure,
        int version = 0,
        QrErrorCorrectionLevel errorCorrectionLevel = default,
        int mask = 0,
        int formatBestDistance = -1) {
        Failure = failure;
        Version = version;
        ErrorCorrectionLevel = errorCorrectionLevel;
        Mask = mask;
        FormatBestDistance = formatBestDistance;
    }

    public override string ToString() {
        return Failure switch {
            QrDecodeFailure.None => $"ok v{Version} {ErrorCorrectionLevel} m{Mask}",
            QrDecodeFailure.InvalidInput => "invalid-input",
            QrDecodeFailure.InvalidSize => $"invalid-size v{Version}",
            QrDecodeFailure.FormatInfo => $"format (v{Version}, d{FormatBestDistance})",
            QrDecodeFailure.ReedSolomon => $"rs (v{Version}, {ErrorCorrectionLevel}, m{Mask})",
            QrDecodeFailure.Cancelled => "cancelled",
            _ => $"payload (v{Version}, {ErrorCorrectionLevel}, m{Mask})",
        };
    }
}
