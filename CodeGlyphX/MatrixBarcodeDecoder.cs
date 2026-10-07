using System;
using CodeGlyphX.AustraliaPost;
using CodeGlyphX.DataBar;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.JapanPost;
using CodeGlyphX.Kix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Pharmacode;
using CodeGlyphX.Postal;
using CodeGlyphX.RoyalMail;

namespace CodeGlyphX;

/// <summary>
/// Decodes matrix, stacked, postal, and other multi-height symbologies from a <see cref="BitMatrix"/>.
/// </summary>
public static class MatrixBarcodeDecoder {
    /// <summary>
    /// Attempts to decode a 2D barcode using the specified <see cref="BarcodeType"/>.
    /// </summary>
    public static bool TryDecode(BarcodeType type, BitMatrix modules, out string text) {
        text = string.Empty;
        if (modules is null) return false;
        return type switch {
            BarcodeType.KixCode => KixDecoder.TryDecode(modules, out text),
            BarcodeType.PharmacodeTwoTrack => PharmacodeTwoTrackDecoder.TryDecode(modules, out text),
            BarcodeType.Postnet => PostnetDecoder.TryDecode(modules, out text),
            BarcodeType.Planet => PlanetDecoder.TryDecode(modules, out text),
            BarcodeType.RoyalMail4State => RoyalMailFourStateDecoder.TryDecode(modules, out text),
            BarcodeType.AustraliaPost => AustraliaPostDecoder.TryDecode(modules, out text),
            BarcodeType.JapanPost => JapanPostDecoder.TryDecode(modules, out text),
            BarcodeType.UspsImb => UspsImbDecoder.TryDecode(modules, out text),
            BarcodeType.GS1DataBarOmni => TryDecodeDataBarOmnidirectional(modules, out text),
            BarcodeType.GS1DataBarStacked => DataBar14Decoder.TryDecodeStacked(modules, out text),
            BarcodeType.GS1DataBarStackedOmni => DataBar14Decoder.TryDecodeStackedOmnidirectional(modules, out text),
            BarcodeType.GS1DataBarExpandedStacked => DataBarExpandedDecoder.TryDecodeExpandedStacked(modules, out text),
            BarcodeType.DataMatrix => DataMatrixDecoder.TryDecode(modules, out text),
            BarcodeType.PDF417 => Pdf417Decoder.TryDecode(modules, out text),
            BarcodeType.MicroPDF417 => MicroPdf417Decoder.TryDecode(modules, out text),
            BarcodeType.MaxiCode => MaxiCodeDecoder.TryDecode(modules, out text),
            BarcodeType.DotCode => DotCodeDecoder.TryDecode(modules, out text),
            BarcodeType.HanXin => HanXinDecoder.TryDecode(modules, out text),
            BarcodeType.GS1Composite => TryDecodeGs1CompositeText(modules, out text),
            _ => throw new NotSupportedException($"BarcodeType.{type} is not supported by MatrixBarcodeDecoder.")
        };
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Omnidirectional symbol from a one-row <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeGs1DataBarOmni(BitMatrix modules, out string text) => TryDecodeDataBarOmnidirectional(modules, out text);

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Stacked Omnidirectional symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeGs1DataBarStackedOmnidirectional(BitMatrix modules, out string text) =>
        DataBar14Decoder.TryDecodeStackedOmnidirectional(modules, out text);

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Stacked symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeGs1DataBarStacked(BitMatrix modules, out string text) => DataBar14Decoder.TryDecodeStacked(modules, out text);

    /// <summary>
    /// Attempts to decode a GS1 DataBar Expanded Stacked symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeGs1DataBarExpandedStacked(BitMatrix modules, out string text) => DataBarExpandedDecoder.TryDecodeExpandedStacked(modules, out text);

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeDataMatrix(BitMatrix modules, out string text) => DataMatrixDecoder.TryDecode(modules, out text);

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodePdf417(BitMatrix modules, out string text) => Pdf417Decoder.TryDecode(modules, out text);

    /// <summary>
    /// Attempts to decode a MicroPDF417 symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeMicroPdf417(BitMatrix modules, out string text) => MicroPdf417Decoder.TryDecode(modules, out text);

    /// <summary>Attempts to decode a MaxiCode sampled module grid.</summary>
    public static bool TryDecodeMaxiCode(BitMatrix modules, out string text) => MaxiCodeDecoder.TryDecode(modules, out text);

    /// <summary>Attempts to decode an exact sampled AIM DotCode grid.</summary>
    public static bool TryDecodeDotCode(BitMatrix modules, out string text) => DotCodeDecoder.TryDecode(modules, out text);

    /// <summary>Attempts to decode an exact sampled Han Xin Code grid.</summary>
    public static bool TryDecodeHanXin(BitMatrix modules, out string text) => HanXinDecoder.TryDecode(modules, out text);

    /// <summary>Attempts to decode both GS1 messages from a Composite symbol.</summary>
    public static bool TryDecodeGs1Composite(BitMatrix modules, out Gs1CompositeDecoded decoded) =>
        Gs1CompositeDecoder.TryDecode(modules, out decoded);

    private static bool TryDecodeDataBarOmnidirectional(BitMatrix modules, out string text) {
        text = string.Empty;
        if (modules is null || modules.Height != 1 || modules.Width <= 0) return false;
        var row = new bool[modules.Width];
        for (var x = 0; x < modules.Width; x++) row[x] = modules[x, 0];
        return DataBar14Decoder.TryDecodeOmnidirectional(row, out text);
    }

    private static bool TryDecodeGs1CompositeText(BitMatrix modules, out string text) {
        text = string.Empty;
        if (!Gs1CompositeDecoder.TryDecode(modules, out var decoded)) return false;
        text = decoded.CompositeText;
        return true;
    }
}
