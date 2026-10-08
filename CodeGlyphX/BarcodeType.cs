namespace CodeGlyphX;

/// <summary>
/// Barcode and matrix symbologies recognized by CodeGlyphX facades.
/// </summary>
public enum BarcodeType {
    /// <summary>
    /// Code 128.
    /// </summary>
    Code128 = 0,
    /// <summary>
    /// GS1-128 (Code 128 with FNC1).
    /// </summary>
    GS1_128 = 1,
    /// <summary>
    /// Code 39.
    /// </summary>
    Code39 = 2,
    /// <summary>
    /// Code 93.
    /// </summary>
    Code93 = 3,
    /// <summary>
    /// EAN family.
    /// </summary>
    EAN = 4,
    /// <summary>
    /// UPC-A.
    /// </summary>
    UPCA = 5,
    /// <summary>
    /// UPC-E.
    /// </summary>
    UPCE = 6,
    /// <summary>
    /// ITF-14.
    /// </summary>
    ITF14 = 7,
    /// <summary>
    /// Interleaved 2 of 5 (ITF).
    /// </summary>
    ITF = 8,
    /// <summary>
    /// Industrial (Discrete) 2 of 5.
    /// </summary>
    Industrial2of5 = 9,
    /// <summary>
    /// Matrix (Standard) 2 of 5.
    /// </summary>
    Matrix2of5 = 10,
    /// <summary>
    /// IATA 2 of 5.
    /// </summary>
    IATA2of5 = 11,
    /// <summary>
    /// Patch Code.
    /// </summary>
    PatchCode = 12,
    /// <summary>
    /// Codabar.
    /// </summary>
    Codabar = 13,
    /// <summary>
    /// MSI.
    /// </summary>
    MSI = 14,
    /// <summary>
    /// Code 11.
    /// </summary>
    Code11 = 15,
    /// <summary>
    /// Plessey.
    /// </summary>
    Plessey = 16,
    /// <summary>
    /// Telepen.
    /// </summary>
    Telepen = 17,
    /// <summary>
    /// Pharmacode (one-track).
    /// </summary>
    Pharmacode = 18,
    /// <summary>
    /// Pharmacode (two-track).
    /// </summary>
    PharmacodeTwoTrack = 19,
    /// <summary>
    /// Code 32 (Italian Pharmacode).
    /// </summary>
    Code32 = 20,
    /// <summary>
    /// POSTNET.
    /// </summary>
    Postnet = 21,
    /// <summary>
    /// PLANET.
    /// </summary>
    Planet = 22,
    /// <summary>
    /// Royal Mail 4-State Customer Code (RM4SCC).
    /// </summary>
    RoyalMail4State = 23,
    /// <summary>
    /// Australia Post customer barcode.
    /// </summary>
    AustraliaPost = 24,
    /// <summary>
    /// Japan Post barcode.
    /// </summary>
    JapanPost = 25,
    /// <summary>
    /// GS1 DataBar-14 Truncated.
    /// </summary>
    GS1DataBarTruncated = 26,
    /// <summary>
    /// GS1 DataBar-14 Omnidirectional.
    /// </summary>
    GS1DataBarOmni = 27,
    /// <summary>
    /// GS1 DataBar-14 Stacked.
    /// </summary>
    GS1DataBarStacked = 28,
    /// <summary>
    /// GS1 DataBar Expanded.
    /// </summary>
    GS1DataBarExpanded = 29,
    /// <summary>
    /// GS1 DataBar Expanded Stacked.
    /// </summary>
    GS1DataBarExpandedStacked = 30,
    /// <summary>
    /// USPS Intelligent Mail Barcode (IMB).
    /// </summary>
    UspsImb = 31,
    /// <summary>
    /// KIX Code (matrix-symbol pipeline).
    /// </summary>
    KixCode = 32,
    /// <summary>
    /// Data Matrix (matrix-symbol pipeline).
    /// </summary>
    DataMatrix = 33,
    /// <summary>
    /// PDF417 (matrix-symbol pipeline).
    /// </summary>
    PDF417 = 34,
    /// <summary>
    /// MicroPDF417 (matrix-symbol pipeline).
    /// </summary>
    MicroPDF417 = 35,
    /// <summary>
    /// GS1 DataBar Limited.
    /// </summary>
    GS1DataBarLimited = 36,
    /// <summary>
    /// GS1 DataBar Stacked Omnidirectional.
    /// </summary>
    GS1DataBarStackedOmni = 37,
    /// <summary>
    /// MaxiCode.
    /// </summary>
    MaxiCode = 38,
    /// <summary>
    /// AIM DotCode.
    /// </summary>
    DotCode = 39,
    /// <summary>
    /// Han Xin Code.
    /// </summary>
    HanXin = 40,
    /// <summary>
    /// GS1 Composite with a standards-linked GS1-128 carrier.
    /// </summary>
    GS1Composite = 41,
}
