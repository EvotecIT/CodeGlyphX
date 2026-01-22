namespace CodeGlyphX.AustraliaPost;

/// <summary>
/// Australia Post customer barcode formats.
/// </summary>
public enum AustraliaPostFormat {
    /// <summary>
    /// Standard Customer Barcode (37 bars).
    /// </summary>
    Standard,
    /// <summary>
    /// Customer Barcode 2 (52 bars).
    /// </summary>
    Customer2,
    /// <summary>
    /// Customer Barcode 3 (67 bars).
    /// </summary>
    Customer3
}

/// <summary>
/// Customer information encoding tables for Australia Post customer barcodes.
/// </summary>
public enum AustraliaPostCustomerEncodingTable {
    /// <summary>
    /// N Encoding Table (numeric digits).
    /// </summary>
    N,
    /// <summary>
    /// C Encoding Table (alphanumeric + space/#).
    /// </summary>
    C
}
