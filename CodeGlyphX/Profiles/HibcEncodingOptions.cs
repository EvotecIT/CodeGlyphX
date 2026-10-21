using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;

namespace CodeGlyphX;

/// <summary>Input interpretation and existing carrier settings for a HIBC supplier symbol.</summary>
public sealed class HibcEncodingOptions {
    /// <summary>Gets or sets whether the input is data or a complete flag-prefixed, checked HIBC payload.</summary>
    public HibcInputMode InputMode { get; set; }
    /// <summary>Gets or sets validation of primary, secondary and supplemental supplier fields in addition to the check.</summary>
    public bool ValidateStructure { get; set; }
    /// <summary>Gets or sets QR carrier settings, including error correction and version bounds.</summary>
    public QrEncodingOptions? Qr { get; set; }
    /// <summary>Gets or sets Aztec carrier settings. Null retains HIBC's 23 percent error-correction default.</summary>
    public AztecEncodeOptions? Aztec { get; set; }
    /// <summary>Gets or sets Data Matrix size and shape settings, including rectangular models.</summary>
    public DataMatrixEncodingOptions? DataMatrix { get; set; }
    /// <summary>Gets or sets PDF417 carrier settings, including columns and error correction.</summary>
    public Pdf417EncodeOptions? Pdf417 { get; set; }
    /// <summary>Gets or sets MicroPDF417 carrier settings, including its column count.</summary>
    public MicroPdf417EncodeOptions? MicroPdf417 { get; set; }
    /// <summary>Gets or sets Codablock F data columns, or null for its existing default of eight.</summary>
    public int? CodablockFColumns { get; set; }
}

/// <summary>Distinguishes supplier data from a supplied checked HIBC message without guessing.</summary>
public enum HibcInputMode {
    /// <summary>Compute the flag and check for the supplied data.</summary>
    Data,
    /// <summary>Validate a complete '+'-prefixed payload, keeping its supplied check.</summary>
    CheckedPayload
}
