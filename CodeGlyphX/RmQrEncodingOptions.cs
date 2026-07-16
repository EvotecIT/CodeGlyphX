namespace CodeGlyphX;

/// <summary>
/// Controls rectangular Micro QR (rMQR) encoding.
/// </summary>
public sealed class RmQrEncodingOptions {
    /// <summary>Gets or sets the rMQR error correction level. Only M and H are valid.</summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; set; } = QrErrorCorrectionLevel.M;

    /// <summary>Gets or sets the high-level data mode.</summary>
    public RmQrEncodingMode Mode { get; set; } = RmQrEncodingMode.Auto;

    /// <summary>Gets or sets the text encoding used by byte mode.</summary>
    public QrTextEncoding TextEncoding { get; set; } = QrTextEncoding.Latin1;

    /// <summary>
    /// Gets or sets the ECI assignment number. When null, non-Latin-1 byte encodings emit their standard ECI assignment.
    /// </summary>
    public int? EciAssignmentNumber { get; set; }

    /// <summary>Gets or sets whether FNC1 in first position identifies a GS1 payload.</summary>
    public bool IsGs1 { get; set; }

    /// <summary>Gets or sets the minimum allowed rMQR version (1..32).</summary>
    public int MinimumVersion { get; set; } = 1;

    /// <summary>Gets or sets the maximum allowed rMQR version (1..32).</summary>
    public int MaximumVersion { get; set; } = 32;
}
