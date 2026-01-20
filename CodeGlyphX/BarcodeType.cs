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
    /// Interleaved 2 of 5 (ITF).
    /// </summary>
    ITF,
    /// <summary>
    /// Matrix (Standard) 2 of 5.
    /// </summary>
    Matrix2of5,
    /// <summary>
    /// IATA 2 of 5.
    /// </summary>
    IATA2of5,
    /// <summary>
    /// Patch Code.
    /// </summary>
    PatchCode,
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
    /// Telepen.
    /// </summary>
    Telepen,
    /// <summary>
    /// Pharmacode (one-track).
    /// </summary>
    Pharmacode,
    /// <summary>
    /// Pharmacode (two-track).
    /// </summary>
    PharmacodeTwoTrack,
    /// <summary>
    /// Code 32 (Italian Pharmacode).
    /// </summary>
    Code32,
    /// <summary>
    /// POSTNET.
    /// </summary>
    Postnet,
    /// <summary>
    /// PLANET.
    /// </summary>
    Planet,
    /// <summary>
    /// Royal Mail 4-State Customer Code (RM4SCC).
    /// </summary>
    RoyalMail4State,
    /// <summary>
    /// Australia Post customer barcode.
    /// </summary>
    AustraliaPost,
    /// <summary>
    /// Japan Post barcode.
    /// </summary>
    JapanPost,
    /// <summary>
    /// GS1 DataBar-14 Truncated.
    /// </summary>
    GS1DataBarTruncated,
    /// <summary>
    /// GS1 DataBar-14 Omnidirectional.
    /// </summary>
    GS1DataBarOmni,
    /// <summary>
    /// GS1 DataBar-14 Stacked.
    /// </summary>
    GS1DataBarStacked,
    /// <summary>
    /// GS1 DataBar Expanded.
    /// </summary>
    GS1DataBarExpanded,
    /// <summary>
    /// GS1 DataBar Expanded Stacked.
    /// </summary>
    GS1DataBarExpandedStacked,
    /// <summary>
    /// USPS Intelligent Mail Barcode (IMB).
    /// </summary>
    UspsImb,
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
    /// <summary>
    /// MicroPDF417 (not supported in 1D encoder).
    /// </summary>
    MicroPDF417,
}
