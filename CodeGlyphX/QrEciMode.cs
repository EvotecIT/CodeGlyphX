namespace CodeGlyphX;

/// <summary>
/// Controls when a QR byte segment declares its character encoding with ECI.
/// </summary>
public enum QrEciMode {
    /// <summary>
    /// Emits ECI only when it is required to preserve non-ASCII text.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Always emits ECI when the selected character encoding has an assignment number.
    /// </summary>
    Always = 1,

    /// <summary>
    /// Never emits ECI. Use only when the reader already knows the byte encoding.
    /// </summary>
    Never = 2
}
