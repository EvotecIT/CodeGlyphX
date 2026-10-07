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
/// Encodes matrix, stacked, postal, and other multi-height symbologies into a <see cref="BitMatrix"/>.
/// </summary>
public static class MatrixBarcodeEncoder {
    /// <summary>Encodes an orthogonal matrix symbol while retaining its format and family metadata.</summary>
    /// <exception cref="NotSupportedException">The format needs a specialized geometry renderer or multiple payloads.</exception>
    public static MatrixSymbol EncodeSymbol(SymbolFormat format, string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return format switch {
            SymbolFormat.MicroQrCode => MicroQrCodeEncoder.EncodeText(value),
            SymbolFormat.RmQrCode => RmQrCodeEncoder.EncodeText(value),
            SymbolFormat.Aztec => Aztec.AztecEncoder.EncodeSymbol(value),
            SymbolFormat.DataMatrix => DataMatrix.DataMatrixEncoder.EncodeSymbol(value),
            SymbolFormat.Pdf417 => Pdf417.Pdf417Encoder.EncodeSymbol(value),
            SymbolFormat.DotCode => DotCodeEncoder.EncodeText(value),
            SymbolFormat.HanXin => HanXinEncoder.EncodeText(value),
            SymbolFormat.MaxiCode => throw new NotSupportedException("MaxiCode uses a hexagonal grid and central bullseye. Use MaxiCodeCode.Encode to retain its specialized geometry."),
            SymbolFormat.Gs1Composite => throw new NotSupportedException("GS1 Composite requires separate linear and composite payloads. Use Gs1CompositeEncoder.Encode."),
            _ => new MatrixSymbol(format, Encode(SymbolCapabilities.GetBarcodeType(format), value))
        };
    }

    /// <summary>
    /// Encodes a matrix, stacked, postal, or multi-height symbol using the specified <see cref="BarcodeType"/>.
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
            BarcodeType.GS1DataBarOmni => ToSingleRowMatrix(DataBar14Encoder.EncodeOmnidirectional(value)),
            BarcodeType.GS1DataBarStacked => DataBar14Encoder.EncodeStacked(value),
            BarcodeType.GS1DataBarStackedOmni => DataBar14Encoder.EncodeStackedOmnidirectional(value),
            BarcodeType.GS1DataBarExpandedStacked => DataBarExpandedEncoder.EncodeExpandedStacked(value),
            BarcodeType.DataMatrix => DataMatrix.DataMatrixEncoder.Encode(value),
            BarcodeType.PDF417 => Pdf417.Pdf417Encoder.Encode(value),
            BarcodeType.MicroPDF417 => Pdf417.MicroPdf417Encoder.Encode(value),
            BarcodeType.MaxiCode => MaxiCodeEncoder.EncodeText(value).Modules.Clone(),
            BarcodeType.DotCode => DotCodeEncoder.EncodeText(value).Modules.Clone(),
            BarcodeType.HanXin => HanXinEncoder.EncodeText(value).Modules.Clone(),
            _ => throw new NotSupportedException($"BarcodeType.{type} is not supported by MatrixBarcodeEncoder.")
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
    /// Encodes a GS1 DataBar-14 Omnidirectional symbol as a one-row <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeGs1DataBarOmni(string value) => ToSingleRowMatrix(DataBar14Encoder.EncodeOmnidirectional(value));

    /// <summary>
    /// Encodes a GS1 DataBar-14 Stacked Omnidirectional symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeGs1DataBarStackedOmnidirectional(string value) => DataBar14Encoder.EncodeStackedOmnidirectional(value);

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

    /// <summary>
    /// Encodes a MaxiCode symbol into its fixed sampled module grid.
    /// </summary>
    public static BitMatrix EncodeMaxiCode(string value, MaxiCodeEncodingOptions? options = null) =>
        MaxiCodeEncoder.EncodeText(value, options ?? new MaxiCodeEncodingOptions()).Modules.Clone();

    /// <summary>Encodes an AIM DotCode symbol.</summary>
    public static BitMatrix EncodeDotCode(string value, DotCodeEncodingOptions? options = null) =>
        DotCodeEncoder.EncodeText(value, options ?? new DotCodeEncodingOptions()).Modules.Clone();

    /// <summary>Encodes a Han Xin Code symbol.</summary>
    public static BitMatrix EncodeHanXin(string value, HanXinEncodingOptions? options = null) =>
        HanXinEncoder.EncodeText(value, options ?? new HanXinEncodingOptions()).Modules.Clone();

    /// <summary>Encodes a standards-linked GS1-128 Composite symbol.</summary>
    public static BitMatrix EncodeGs1Composite(string linearText, string compositeText,
        Gs1CompositeEncodingOptions? options = null) =>
        Gs1CompositeEncoder.Encode(linearText, compositeText, options).Modules.Clone();

    private static BitMatrix ToSingleRowMatrix(Barcode1D barcode) {
        var matrix = new BitMatrix(barcode.TotalModules, 1);
        var x = 0;
        for (var i = 0; i < barcode.Segments.Count; i++) {
            var segment = barcode.Segments[i];
            if (segment.IsBar) {
                for (var j = 0; j < segment.Modules.Clone(); j++) matrix[x + j, 0] = true;
            }
            x += segment.Modules.Clone();
        }
        return matrix;
    }
}
