namespace CodeGlyphX;

/// <summary>
/// Identifies a physical symbol format independently of a rendering or image format.
/// </summary>
public enum SymbolFormat {
    /// <summary>QR Code Model 2.</summary>
    QrCode = 0,
    /// <summary>Micro QR Code.</summary>
    MicroQrCode = 1,
    /// <summary>Aztec Code.</summary>
    Aztec = 2,
    /// <summary>Code 128.</summary>
    Code128 = 3,
    /// <summary>GS1-128.</summary>
    Gs1Code128 = 4,
    /// <summary>Code 39.</summary>
    Code39 = 5,
    /// <summary>Code 93.</summary>
    Code93 = 6,
    /// <summary>EAN family.</summary>
    Ean = 7,
    /// <summary>UPC-A.</summary>
    UpcA = 8,
    /// <summary>UPC-E.</summary>
    UpcE = 9,
    /// <summary>ITF-14.</summary>
    Itf14 = 10,
    /// <summary>Interleaved 2 of 5.</summary>
    Itf = 11,
    /// <summary>Industrial 2 of 5.</summary>
    Industrial2Of5 = 12,
    /// <summary>Matrix 2 of 5.</summary>
    Matrix2Of5 = 13,
    /// <summary>IATA 2 of 5.</summary>
    Iata2Of5 = 14,
    /// <summary>Patch Code.</summary>
    PatchCode = 15,
    /// <summary>Codabar.</summary>
    Codabar = 16,
    /// <summary>MSI.</summary>
    Msi = 17,
    /// <summary>Code 11.</summary>
    Code11 = 18,
    /// <summary>Plessey.</summary>
    Plessey = 19,
    /// <summary>Telepen.</summary>
    Telepen = 20,
    /// <summary>One-track Pharmacode.</summary>
    Pharmacode = 21,
    /// <summary>Two-track Pharmacode.</summary>
    PharmacodeTwoTrack = 22,
    /// <summary>Code 32.</summary>
    Code32 = 23,
    /// <summary>POSTNET.</summary>
    Postnet = 24,
    /// <summary>PLANET.</summary>
    Planet = 25,
    /// <summary>Royal Mail 4-State Customer Code.</summary>
    RoyalMail4State = 26,
    /// <summary>Australia Post customer barcode.</summary>
    AustraliaPost = 27,
    /// <summary>Japan Post barcode.</summary>
    JapanPost = 28,
    /// <summary>GS1 DataBar Truncated.</summary>
    Gs1DataBarTruncated = 29,
    /// <summary>GS1 DataBar Omnidirectional.</summary>
    Gs1DataBarOmnidirectional = 30,
    /// <summary>GS1 DataBar Stacked.</summary>
    Gs1DataBarStacked = 31,
    /// <summary>GS1 DataBar Expanded.</summary>
    Gs1DataBarExpanded = 32,
    /// <summary>GS1 DataBar Expanded Stacked.</summary>
    Gs1DataBarExpandedStacked = 33,
    /// <summary>USPS Intelligent Mail Barcode.</summary>
    UspsIntelligentMail = 34,
    /// <summary>KIX Code.</summary>
    KixCode = 35,
    /// <summary>Data Matrix ECC200.</summary>
    DataMatrix = 36,
    /// <summary>PDF417.</summary>
    Pdf417 = 37,
    /// <summary>MicroPDF417.</summary>
    MicroPdf417 = 38,
    /// <summary>Rectangular Micro QR Code (rMQR).</summary>
    RmQrCode = 39,
    /// <summary>GS1 DataBar Limited.</summary>
    Gs1DataBarLimited = 40,
    /// <summary>GS1 DataBar Stacked Omnidirectional.</summary>
    Gs1DataBarStackedOmnidirectional = 41,
    /// <summary>MaxiCode.</summary>
    MaxiCode = 42,
    /// <summary>AIM DotCode.</summary>
    DotCode = 43,
    /// <summary>Han Xin Code.</summary>
    HanXin = 44,
    /// <summary>GS1 Composite with a GS1-128 linear component.</summary>
    Gs1Composite = 45
}
