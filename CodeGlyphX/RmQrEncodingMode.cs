namespace CodeGlyphX;

/// <summary>
/// High-level data modes supported by rectangular Micro QR (rMQR).
/// </summary>
public enum RmQrEncodingMode {
    /// <summary>Select the smallest whole-payload mode that can represent the input.</summary>
    Auto,
    /// <summary>Encode decimal digits in numeric mode.</summary>
    Numeric,
    /// <summary>Encode the QR alphanumeric character set.</summary>
    Alphanumeric,
    /// <summary>Encode bytes using the selected text encoding.</summary>
    Byte,
    /// <summary>Encode Shift-JIS double-byte characters in Kanji mode.</summary>
    Kanji
}
