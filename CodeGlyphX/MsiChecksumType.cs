namespace CodeGlyphX;

/// <summary>
/// Checksum variants for MSI barcodes.
/// </summary>
public enum MsiChecksumType {
    /// <summary>
    /// No checksum digit.
    /// </summary>
    None,
    /// <summary>
    /// Mod 10 (Luhn-style) checksum.
    /// </summary>
    Mod10,
    /// <summary>
    /// Two consecutive Mod 10 checksum digits.
    /// </summary>
    Mod10Mod10
}
