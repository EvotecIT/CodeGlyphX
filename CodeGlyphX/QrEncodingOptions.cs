namespace CodeGlyphX;

/// <summary>
/// Controls standards-aware QR text encoding.
/// </summary>
public sealed class QrEncodingOptions {
    /// <summary>
    /// Gets or sets the error correction level.
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; set; } = QrErrorCorrectionLevel.M;

    /// <summary>
    /// Gets or sets the minimum allowed QR version (1..40).
    /// </summary>
    public int MinVersion { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum allowed QR version (1..40).
    /// </summary>
    public int MaxVersion { get; set; } = 40;

    /// <summary>
    /// Gets or sets an optional forced mask (0..7). A null value selects the best mask.
    /// </summary>
    public int? ForceMask { get; set; }

    /// <summary>
    /// Gets or sets the encoding used by byte-mode segments.
    /// </summary>
    public QrTextEncoding TextEncoding { get; set; } = QrTextEncoding.Utf8;

    /// <summary>
    /// Gets or sets when byte-mode segments declare their encoding with ECI.
    /// </summary>
    public QrEciMode EciMode { get; set; } = QrEciMode.Auto;

    /// <summary>
    /// Gets or sets whether numeric, alphanumeric, byte, and Kanji segments are selected automatically.
    /// </summary>
    public bool OptimizeSegments { get; set; } = true;

    /// <summary>
    /// Gets or sets the FNC1 mode. First position is used by GS1; second position is industry-specific.
    /// </summary>
    public QrFnc1Mode Fnc1Mode { get; set; }

    /// <summary>
    /// Gets or sets the application indicator (0..255) required by FNC1 second position.
    /// </summary>
    public int? Fnc1ApplicationIndicator { get; set; }

    internal QrEncodingOptions Clone() {
        return new QrEncodingOptions {
            ErrorCorrectionLevel = ErrorCorrectionLevel,
            MinVersion = MinVersion,
            MaxVersion = MaxVersion,
            ForceMask = ForceMask,
            TextEncoding = TextEncoding,
            EciMode = EciMode,
            OptimizeSegments = OptimizeSegments,
            Fnc1Mode = Fnc1Mode,
            Fnc1ApplicationIndicator = Fnc1ApplicationIndicator
        };
    }
}
