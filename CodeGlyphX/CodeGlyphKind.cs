namespace CodeGlyphX;

/// <summary>
/// Kind of decoded symbol.
/// </summary>
public enum CodeGlyphKind {
    /// <summary>QR code.</summary>
    Qr,
    /// <summary>1D barcode.</summary>
    Barcode1D,
    /// <summary>Data Matrix (ECC200).</summary>
    DataMatrix,
    /// <summary>PDF417.</summary>
    Pdf417,
    /// <summary>Aztec.</summary>
    Aztec,
    /// <summary>Micro QR code.</summary>
    MicroQr,
    /// <summary>Rectangular Micro QR code.</summary>
    RmQr,
    /// <summary>MaxiCode.</summary>
    MaxiCode,
    /// <summary>AIM DotCode.</summary>
    DotCode,
    /// <summary>Han Xin Code.</summary>
    HanXin,
    /// <summary>GS1 Composite symbol.</summary>
    Gs1Composite
}
