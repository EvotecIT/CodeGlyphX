namespace CodeMatrix;

/// <summary>
/// Barcode symbologies supported (or planned) by <see cref="BarcodeEncoder"/>.
/// </summary>
public enum BarcodeType {
    /// <summary>
    /// Code 128 (implemented).
    /// </summary>
    Code128,
    /// <summary>
    /// Code 39 (planned).
    /// </summary>
    Code39,
    /// <summary>
    /// Code 93 (planned).
    /// </summary>
    Code93,
    /// <summary>
    /// EAN family (planned).
    /// </summary>
    EAN,
    /// <summary>
    /// UPC-A (planned).
    /// </summary>
    UPCA,
    /// <summary>
    /// UPC-E (planned).
    /// </summary>
    UPCE,
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
