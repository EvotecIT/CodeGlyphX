namespace CodeMatrix;

/// <summary>
/// QR Code error correction level.
/// </summary>
public enum QrErrorCorrectionLevel {
    /// <summary>
    /// Low (~7%).
    /// </summary>
    L = 0,
    /// <summary>
    /// Medium (~15%).
    /// </summary>
    M = 1,
    /// <summary>
    /// Quartile (~25%).
    /// </summary>
    Q = 2,
    /// <summary>
    /// High (~30%).
    /// </summary>
    H = 3,
}
