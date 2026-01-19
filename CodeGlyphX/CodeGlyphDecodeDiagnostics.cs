namespace CodeGlyphX;

/// <summary>
/// Diagnostics for unified QR/barcode decoding.
/// </summary>
public sealed class CodeGlyphDecodeDiagnostics {
    /// <summary>
    /// True when a symbol was decoded.
    /// </summary>
    public bool Success { get; internal set; }

    /// <summary>
    /// Decoded symbol kind (when successful).
    /// </summary>
    public CodeGlyphKind? SuccessKind { get; internal set; }

    /// <summary>
    /// QR diagnostics (when attempted).
    /// </summary>
    public QrPixelDecodeInfo? Qr { get; internal set; }

    /// <summary>
    /// 1D barcode diagnostics (when attempted).
    /// </summary>
    public BarcodeDecodeDiagnostics? Barcode { get; internal set; }

    /// <summary>
    /// Data Matrix diagnostics (when attempted).
    /// </summary>
    public DataMatrixDecodeDiagnostics? DataMatrix { get; internal set; }

    /// <summary>
    /// PDF417 diagnostics (when attempted).
    /// </summary>
    public Pdf417DecodeDiagnostics? Pdf417 { get; internal set; }

    /// <summary>
    /// Aztec diagnostics (when attempted).
    /// </summary>
    public AztecDecodeDiagnostics? Aztec { get; internal set; }

    /// <summary>
    /// Optional failure message when decoding fails.
    /// </summary>
    public string? Failure { get; internal set; }
}
