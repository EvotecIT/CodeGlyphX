namespace CodeGlyphX;

/// <summary>
/// Barcode symbologies supported by <see cref="BarcodeEncoder"/>.
/// </summary>
public enum BarcodeType {
    /// <summary>
    /// Code 128.
    /// </summary>
    Code128,
    /// <summary>
    /// GS1-128 (Code 128 with FNC1).
    /// </summary>
    GS1_128,
    /// <summary>
    /// Code 39.
    /// </summary>
    Code39,
    /// <summary>
    /// Code 93.
    /// </summary>
    Code93,
    /// <summary>
    /// EAN family.
    /// </summary>
    EAN,
    /// <summary>
    /// UPC-A.
    /// </summary>
    UPCA,
    /// <summary>
    /// UPC-E.
    /// </summary>
    UPCE,
    /// <summary>
    /// ITF-14.
    /// </summary>
    ITF14,
    /// <summary>
    /// Codabar.
    /// </summary>
    Codabar,
    /// <summary>
    /// MSI.
    /// </summary>
    MSI,
    /// <summary>
    /// Code 11.
    /// </summary>
    Code11,
    /// <summary>
    /// Plessey.
    /// </summary>
    Plessey,
    /// <summary>
    /// KIX Code (not supported).
    /// </summary>
    KixCode,
    /// <summary>
    /// Data Matrix (not supported in 1D encoder).
    /// </summary>
    DataMatrix,
    /// <summary>
    /// PDF417 (not supported in 1D encoder).
    /// </summary>
    PDF417,
}
