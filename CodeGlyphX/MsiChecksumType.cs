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
    Mod10Mod10,
    /// <summary>Modulo 11 with IBM weights 2 through 7. A check value of ten is encoded as two decimal digits.</summary>
    Mod11,
    /// <summary>IBM modulo 11 followed by modulo 10 over the data and its first check.</summary>
    Mod11Mod10,
    /// <summary>Modulo 11 with NCR weights 2 through 9. A check value of ten is encoded as two decimal digits.</summary>
    NcrMod11,
    /// <summary>NCR modulo 11 followed by modulo 10 over the data and its first check.</summary>
    NcrMod11Mod10
}
