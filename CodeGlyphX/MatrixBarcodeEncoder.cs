using System;
using CodeGlyphX.AustraliaPost;
using CodeGlyphX.DataBar;
using CodeGlyphX.JapanPost;
using CodeGlyphX.Kix;
using CodeGlyphX.Pharmacode;
using CodeGlyphX.Postal;
using CodeGlyphX.RoyalMail;

namespace CodeGlyphX;

/// <summary>
/// Encodes 2D barcode symbologies into a <see cref="BitMatrix"/>.
/// </summary>
public static class MatrixBarcodeEncoder {
    /// <summary>
    /// Encodes a 2D barcode using the specified <see cref="BarcodeType"/>.
    /// </summary>
    public static BitMatrix Encode(BarcodeType type, string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return type switch {
            BarcodeType.KixCode => KixEncoder.Encode(value),
            BarcodeType.PharmacodeTwoTrack => PharmacodeTwoTrackEncoder.Encode(value),
            BarcodeType.Postnet => PostnetEncoder.Encode(value),
            BarcodeType.Planet => PlanetEncoder.Encode(value),
            BarcodeType.RoyalMail4State => RoyalMailFourStateEncoder.Encode(value, includeHeaders: true),
            BarcodeType.AustraliaPost => AustraliaPostEncoder.Encode(value),
            BarcodeType.JapanPost => JapanPostEncoder.Encode(value),
            BarcodeType.UspsImb => UspsImbEncoder.Encode(value),
            BarcodeType.GS1DataBarOmni => DataBar14Encoder.EncodeOmni(value),
            BarcodeType.GS1DataBarStacked => DataBar14Encoder.EncodeStacked(value),
            BarcodeType.GS1DataBarExpandedStacked => DataBarExpandedEncoder.EncodeExpandedStacked(value),
            BarcodeType.DataMatrix => DataMatrix.DataMatrixEncoder.Encode(value),
            BarcodeType.PDF417 => Pdf417.Pdf417Encoder.Encode(value),
            BarcodeType.MicroPDF417 => Pdf417.MicroPdf417Encoder.Encode(value),
            _ => throw new NotSupportedException($"BarcodeType.{type} is not a 2D matrix barcode.")
        };
    }

    /// <summary>
    /// Encodes a KIX code into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeKix(string value) => KixEncoder.Encode(value);

    /// <summary>
    /// Encodes a Pharmacode (two-track) barcode into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodePharmacodeTwoTrack(string value) => PharmacodeTwoTrackEncoder.Encode(value);

    /// <summary>
    /// Encodes a POSTNET symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodePostnet(string value) => PostnetEncoder.Encode(value);

    /// <summary>
    /// Encodes a PLANET symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodePlanet(string value) => PlanetEncoder.Encode(value);

    /// <summary>
    /// Encodes a Royal Mail 4-State Customer Code (RM4SCC) symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeRoyalMail4State(string value) => RoyalMailFourStateEncoder.Encode(value, includeHeaders: true);

    /// <summary>
    /// Encodes an Australia Post customer barcode into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeAustraliaPost(string value) => AustraliaPostEncoder.Encode(value);

    /// <summary>
    /// Encodes a Japan Post barcode into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeJapanPost(string value) => JapanPostEncoder.Encode(value);

    /// <summary>
    /// Encodes a USPS Intelligent Mail Barcode (IMB) into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeUspsImb(string value) => UspsImbEncoder.Encode(value);

    /// <summary>
    /// Encodes a GS1 DataBar-14 Omnidirectional symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeGs1DataBarOmni(string value) => DataBar14Encoder.EncodeOmni(value);

    /// <summary>
    /// Encodes a GS1 DataBar-14 Stacked symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeGs1DataBarStacked(string value) => DataBar14Encoder.EncodeStacked(value);

    /// <summary>
    /// Encodes a GS1 DataBar Expanded Stacked symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeGs1DataBarExpandedStacked(string value) => DataBarExpandedEncoder.EncodeExpandedStacked(value);

    /// <summary>
    /// Encodes a Data Matrix symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeDataMatrix(string value) => DataMatrix.DataMatrixEncoder.Encode(value);

    /// <summary>
    /// Encodes a PDF417 symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodePdf417(string value) => Pdf417.Pdf417Encoder.Encode(value);

    /// <summary>
    /// Encodes a MicroPDF417 symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeMicroPdf417(string value) => Pdf417.MicroPdf417Encoder.Encode(value);
}
