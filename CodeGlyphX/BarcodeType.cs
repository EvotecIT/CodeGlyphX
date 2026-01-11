namespace CodeGlyphX;

/// <summary>
/// Barcode symbologies supported (or planned) by <see cref="BarcodeEncoder"/>.
/// </summary>
public enum BarcodeType {
    /// <summary>
    /// Code 128 (implemented).
    /// </summary>
    Code128,
    /// <summary>
    /// GS1-128 (Code 128 with FNC1, implemented).
    /// </summary>
    GS1_128,
    /// <summary>
    /// Code 39 (implemented).
    /// </summary>
    Code39,
    /// <summary>
    /// Code 93 (implemented).
    /// </summary>
    Code93,
    /// <summary>
    /// EAN family (implemented).
    /// </summary>
    EAN,
    /// <summary>
    /// UPC-A (implemented).
    /// </summary>
    UPCA,
    /// <summary>
    /// UPC-E (implemented).
    /// </summary>
    UPCE,
    /// <summary>
    /// ITF-14 (implemented).
    /// </summary>
    ITF14,
    /// <summary>
    /// KIX Code (planned).
    /// </summary>
    KixCode,
    /// <summary>
    /// Data Matrix (planned).
    /// </summary>
    DataMatrix,
    /// <summary>
    /// PDF417 (planned).
    /// </summary>
    PDF417,
}
