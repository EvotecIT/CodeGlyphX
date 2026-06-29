namespace CodeGlyphX;

/// <summary>
/// Swiss QR bill currency codes.
/// </summary>
public enum SwissQrCurrency {
    /// <summary>Swiss franc.</summary>
    CHF,
    /// <summary>Euro.</summary>
    EUR
}

/// <summary>
/// Swiss QR bill IBAN kind.
/// </summary>
public enum SwissQrIbanType {
    /// <summary>Standard IBAN.</summary>
    Iban,
    /// <summary>QR-IBAN.</summary>
    QrIban
}

/// <summary>
/// Swiss QR bill reference type.
/// </summary>
public enum SwissQrReferenceType {
    /// <summary>QR reference.</summary>
    QRR,
    /// <summary>Creditor reference.</summary>
    SCOR,
    /// <summary>No reference.</summary>
    NON
}

/// <summary>
/// Swiss QR bill contact address format.
/// </summary>
public enum SwissQrAddressType {
    /// <summary>Structured address.</summary>
    StructuredAddress,
    /// <summary>Combined address lines.</summary>
    CombinedAddress
}
