namespace CodeGlyphX;

/// <summary>
/// Indicates presence of FNC1 mode in a QR payload.
/// </summary>
public enum QrFnc1Mode {
    /// <summary>
    /// No FNC1 mode present.
    /// </summary>
    None = 0,
    /// <summary>
    /// FNC1 in first position (GS1).
    /// </summary>
    FirstPosition = 1,
    /// <summary>
    /// FNC1 in second position.
    /// </summary>
    SecondPosition = 2
}
