namespace CodeGlyphX;

/// <summary>
/// Identifies a physical symbol format independently of a rendering or image format.
/// </summary>
public enum SymbolFormat {
    /// <summary>QR Code Model 2.</summary>
    QrCode,
    /// <summary>Micro QR Code.</summary>
    MicroQrCode,
    /// <summary>Aztec Code.</summary>
    Aztec,
    /// <summary>Code 128.</summary>
    Code128,
    /// <summary>GS1-128.</summary>
    Gs1Code128,
    /// <summary>Code 39.</summary>
    Code39,
    /// <summary>Code 93.</summary>
    Code93,
    /// <summary>EAN family.</summary>
    Ean,
    /// <summary>UPC-A.</summary>
    UpcA,
    /// <summary>UPC-E.</summary>
    UpcE,
    /// <summary>ITF-14.</summary>
    Itf14,
    /// <summary>Interleaved 2 of 5.</summary>
    Itf,
    /// <summary>Industrial 2 of 5.</summary>
    Industrial2Of5,
    /// <summary>Matrix 2 of 5.</summary>
    Matrix2Of5,
    /// <summary>IATA 2 of 5.</summary>
    Iata2Of5,
    /// <summary>Patch Code.</summary>
    PatchCode,
    /// <summary>Codabar.</summary>
    Codabar,
    /// <summary>MSI.</summary>
    Msi,
    /// <summary>Code 11.</summary>
    Code11,
    /// <summary>Plessey.</summary>
    Plessey,
    /// <summary>Telepen.</summary>
    Telepen,
    /// <summary>One-track Pharmacode.</summary>
    Pharmacode,
    /// <summary>Two-track Pharmacode.</summary>
    PharmacodeTwoTrack,
    /// <summary>Code 32.</summary>
    Code32,
    /// <summary>POSTNET.</summary>
    Postnet,
    /// <summary>PLANET.</summary>
    Planet,
    /// <summary>Royal Mail 4-State Customer Code.</summary>
    RoyalMail4State,
    /// <summary>Australia Post customer barcode.</summary>
    AustraliaPost,
    /// <summary>Japan Post barcode.</summary>
    JapanPost,
    /// <summary>GS1 DataBar Truncated.</summary>
    Gs1DataBarTruncated,
    /// <summary>GS1 DataBar Omnidirectional.</summary>
    Gs1DataBarOmnidirectional,
    /// <summary>GS1 DataBar Stacked.</summary>
    Gs1DataBarStacked,
    /// <summary>GS1 DataBar Expanded.</summary>
    Gs1DataBarExpanded,
    /// <summary>GS1 DataBar Expanded Stacked.</summary>
    Gs1DataBarExpandedStacked,
    /// <summary>USPS Intelligent Mail Barcode.</summary>
    UspsIntelligentMail,
    /// <summary>KIX Code.</summary>
    KixCode,
    /// <summary>Data Matrix ECC200.</summary>
    DataMatrix,
    /// <summary>PDF417.</summary>
    Pdf417,
    /// <summary>MicroPDF417.</summary>
    MicroPdf417
}
