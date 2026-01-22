using System;
using CodeGlyphX.AustraliaPost;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataBar;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.JapanPost;
using CodeGlyphX.Kix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Pharmacode;
using CodeGlyphX.Qr;
using CodeGlyphX.Postal;
using CodeGlyphX.RoyalMail;

namespace CodeGlyphX;

public static partial class CodeGlyph {
    private delegate bool MatrixSymbolDecoder(BitMatrix modules, out CodeGlyphDecoded decoded);
    private delegate bool MatrixBarcodeDecoder(BitMatrix modules, out BarcodeDecoded decoded);

    private static readonly MatrixSymbolDecoder[] MatrixSymbolDecoders = {
        TryDecodeQr,
        TryDecodeAztec,
        TryDecodeDataMatrix,
        TryDecodePdf417,
        TryDecodeMicroPdf417
    };

    private static readonly MatrixBarcodeDecoder[] MatrixBarcodeDecoders = {
        TryDecodeDataBarOmni,
        TryDecodeDataBarStacked,
        TryDecodeExpandedStacked,
        TryDecodeKix,
        TryDecodePharmacodeTwoTrack,
        TryDecodePostnet,
        TryDecodePlanet,
        TryDecodeRoyalMailFourState,
        TryDecodeAustraliaPost,
        TryDecodeJapanPost,
        TryDecodeUspsImb
    };

    /// <summary>
    /// Attempts to decode a QR or barcode from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        decoded = null!;
        if (modules is null) throw new ArgumentNullException(nameof(modules));

        if (expectedBarcode.HasValue) {
            return TryDecodeExpectedMatrix(expectedBarcode.Value, modules, out decoded);
        }

        if (preferBarcode && TryDecodeMatrixBarcode(modules, out var barcodePreferred)) {
            decoded = new CodeGlyphDecoded(barcodePreferred);
            return true;
        }

        if (TryDecodeMatrixSymbols(modules, out decoded)) return true;

        if (!preferBarcode && TryDecodeMatrixBarcode(modules, out var barcodeFallback)) {
            decoded = new CodeGlyphDecoded(barcodeFallback);
            return true;
        }

        return false;
    }

    private static bool TryDecodeExpectedMatrix(BarcodeType type, BitMatrix modules, out CodeGlyphDecoded decoded) {
        decoded = null!;
        return type switch {
            BarcodeType.DataMatrix => TryDecodeDataMatrix(modules, out decoded),
            BarcodeType.PDF417 => TryDecodePdf417(modules, out decoded),
            BarcodeType.MicroPDF417 => TryDecodeMicroPdf417(modules, out decoded),
            BarcodeType.GS1DataBarOmni => TryDecodeExpectedBarcode(TryDecodeDataBarOmni, modules, out decoded),
            BarcodeType.GS1DataBarStacked => TryDecodeExpectedBarcode(TryDecodeDataBarStacked, modules, out decoded),
            BarcodeType.GS1DataBarExpandedStacked => TryDecodeExpectedBarcode(TryDecodeExpandedStacked, modules, out decoded),
            BarcodeType.KixCode => TryDecodeExpectedBarcode(TryDecodeKix, modules, out decoded),
            BarcodeType.PharmacodeTwoTrack => TryDecodeExpectedBarcode(TryDecodePharmacodeTwoTrack, modules, out decoded),
            BarcodeType.Postnet => TryDecodeExpectedBarcode(TryDecodePostnet, modules, out decoded),
            BarcodeType.Planet => TryDecodeExpectedBarcode(TryDecodePlanet, modules, out decoded),
            BarcodeType.RoyalMail4State => TryDecodeExpectedBarcode(TryDecodeRoyalMailFourState, modules, out decoded),
            BarcodeType.AustraliaPost => TryDecodeExpectedBarcode(TryDecodeAustraliaPost, modules, out decoded),
            BarcodeType.JapanPost => TryDecodeExpectedBarcode(TryDecodeJapanPost, modules, out decoded),
            BarcodeType.UspsImb => TryDecodeExpectedBarcode(TryDecodeUspsImb, modules, out decoded),
            _ => false
        };
    }

    private static bool TryDecodeMatrixSymbols(BitMatrix modules, out CodeGlyphDecoded decoded) {
        for (var i = 0; i < MatrixSymbolDecoders.Length; i++) {
            if (MatrixSymbolDecoders[i](modules, out decoded)) return true;
        }
        decoded = null!;
        return false;
    }

    private static bool TryDecodeMatrixBarcode(BitMatrix modules, out BarcodeDecoded decoded) {
        for (var i = 0; i < MatrixBarcodeDecoders.Length; i++) {
            if (MatrixBarcodeDecoders[i](modules, out decoded)) return true;
        }
        decoded = null!;
        return false;
    }

    private static bool TryDecodeExpectedBarcode(MatrixBarcodeDecoder decoder, BitMatrix modules, out CodeGlyphDecoded decoded) {
        decoded = null!;
        if (!decoder(modules, out var barcode)) return false;
        decoded = new CodeGlyphDecoded(barcode);
        return true;
    }

    private static bool TryDecodeQr(BitMatrix modules, out CodeGlyphDecoded decoded) {
        decoded = null!;
        if (!QrDecoder.TryDecode(modules, out var qr)) return false;
        decoded = new CodeGlyphDecoded(qr);
        return true;
    }

    private static bool TryDecodeAztec(BitMatrix modules, out CodeGlyphDecoded decoded) {
        decoded = null!;
        if (!AztecDecoder.TryDecode(modules, out var aztec)) return false;
        decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
        return true;
    }

    private static bool TryDecodeDataMatrix(BitMatrix modules, out CodeGlyphDecoded decoded) {
        decoded = null!;
        if (!DataMatrixDecoder.TryDecode(modules, out var dataMatrix)) return false;
        decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
        return true;
    }

    private static bool TryDecodePdf417(BitMatrix modules, out CodeGlyphDecoded decoded) {
        decoded = null!;
        if (!Pdf417Decoder.TryDecode(modules, out Pdf417Decoded pdf417)) return false;
        decoded = new CodeGlyphDecoded(pdf417);
        return true;
    }

    private static bool TryDecodeMicroPdf417(BitMatrix modules, out CodeGlyphDecoded decoded) {
        decoded = null!;
        if (!MicroPdf417Decoder.TryDecode(modules, out var microPdf417)) return false;
        decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, microPdf417);
        return true;
    }

    private static bool TryDecodeDataBarOmni(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!DataBar14Decoder.TryDecodeOmni(modules, out var omni)) return false;
        decoded = new BarcodeDecoded(BarcodeType.GS1DataBarOmni, omni);
        return true;
    }

    private static bool TryDecodeDataBarStacked(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!DataBar14Decoder.TryDecodeStacked(modules, out var stacked)) return false;
        decoded = new BarcodeDecoded(BarcodeType.GS1DataBarStacked, stacked);
        return true;
    }

    private static bool TryDecodeExpandedStacked(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!DataBarExpandedDecoder.TryDecodeExpandedStacked(modules, out var expandedStacked)) return false;
        decoded = new BarcodeDecoded(BarcodeType.GS1DataBarExpandedStacked, expandedStacked);
        return true;
    }

    private static bool TryDecodeKix(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!KixDecoder.TryDecode(modules, out var kix)) return false;
        decoded = new BarcodeDecoded(BarcodeType.KixCode, kix);
        return true;
    }

    private static bool TryDecodePharmacodeTwoTrack(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!PharmacodeTwoTrackDecoder.TryDecode(modules, out var pharma)) return false;
        decoded = new BarcodeDecoded(BarcodeType.PharmacodeTwoTrack, pharma);
        return true;
    }

    private static bool TryDecodePostnet(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!PostnetDecoder.TryDecode(modules, out var postnet)) return false;
        decoded = new BarcodeDecoded(BarcodeType.Postnet, postnet);
        return true;
    }

    private static bool TryDecodePlanet(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!PlanetDecoder.TryDecode(modules, out var planet)) return false;
        decoded = new BarcodeDecoded(BarcodeType.Planet, planet);
        return true;
    }

    private static bool TryDecodeRoyalMailFourState(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!RoyalMailFourStateDecoder.TryDecode(modules, out var royalMail)) return false;
        decoded = new BarcodeDecoded(BarcodeType.RoyalMail4State, royalMail);
        return true;
    }

    private static bool TryDecodeAustraliaPost(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!AustraliaPostDecoder.TryDecode(modules, out var australiaPost)) return false;
        decoded = new BarcodeDecoded(BarcodeType.AustraliaPost, australiaPost);
        return true;
    }

    private static bool TryDecodeJapanPost(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!JapanPostDecoder.TryDecode(modules, out var japanPost)) return false;
        decoded = new BarcodeDecoded(BarcodeType.JapanPost, japanPost);
        return true;
    }

    private static bool TryDecodeUspsImb(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (!UspsImbDecoder.TryDecode(modules, out var uspsImb)) return false;
        decoded = new BarcodeDecoded(BarcodeType.UspsImb, uspsImb);
        return true;
    }
}
