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
    /// <summary>
    /// Attempts to decode a QR or barcode from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        decoded = null!;
        if (modules is null) throw new ArgumentNullException(nameof(modules));

        if (expectedBarcode.HasValue) {
            if (TryDecodeExpectedMatrix(expectedBarcode.Value, modules, out decoded)) return true;
            return false;
        }

        if (preferBarcode && TryDecodeMatrixBarcode(modules, out var barcodePreferred)) {
            decoded = new CodeGlyphDecoded(barcodePreferred);
            return true;
        }

        if (QrDecoder.TryDecode(modules, out var qr)) {
            decoded = new CodeGlyphDecoded(qr);
            return true;
        }
        if (AztecDecoder.TryDecode(modules, out var aztec)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
            return true;
        }
        if (DataMatrixDecoder.TryDecode(modules, out var dataMatrix)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
            return true;
        }
        if (Pdf417Decoder.TryDecode(modules, out Pdf417Decoded pdf417)) {
            decoded = new CodeGlyphDecoded(pdf417);
            return true;
        }
        if (MicroPdf417Decoder.TryDecode(modules, out var microPdf417)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, microPdf417);
            return true;
        }

        if (!preferBarcode && TryDecodeMatrixBarcode(modules, out var barcodeFallback)) {
            decoded = new CodeGlyphDecoded(barcodeFallback);
            return true;
        }

        return false;
    }

    private static bool TryDecodeExpectedMatrix(BarcodeType type, BitMatrix modules, out CodeGlyphDecoded decoded) {
        decoded = null!;
        switch (type) {
            case BarcodeType.DataMatrix:
                if (DataMatrixDecoder.TryDecode(modules, out var dataMatrix)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                    return true;
                }
                return false;
            case BarcodeType.PDF417:
                if (Pdf417Decoder.TryDecode(modules, out Pdf417Decoded pdf417)) {
                    decoded = new CodeGlyphDecoded(pdf417);
                    return true;
                }
                return false;
            case BarcodeType.MicroPDF417:
                if (MicroPdf417Decoder.TryDecode(modules, out var microPdf417)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, microPdf417);
                    return true;
                }
                return false;
            case BarcodeType.GS1DataBarOmni:
                if (DataBar14Decoder.TryDecodeOmni(modules, out var omni)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.GS1DataBarOmni, omni));
                    return true;
                }
                return false;
            case BarcodeType.GS1DataBarStacked:
                if (DataBar14Decoder.TryDecodeStacked(modules, out var stacked)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.GS1DataBarStacked, stacked));
                    return true;
                }
                return false;
            case BarcodeType.GS1DataBarExpandedStacked:
                if (DataBarExpandedDecoder.TryDecodeExpandedStacked(modules, out var expandedStacked)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.GS1DataBarExpandedStacked, expandedStacked));
                    return true;
                }
                return false;
            case BarcodeType.KixCode:
                if (KixDecoder.TryDecode(modules, out var kix)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.KixCode, kix));
                    return true;
                }
                return false;
            case BarcodeType.PharmacodeTwoTrack:
                if (PharmacodeTwoTrackDecoder.TryDecode(modules, out var pharma)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.PharmacodeTwoTrack, pharma));
                    return true;
                }
                return false;
            case BarcodeType.Postnet:
                if (PostnetDecoder.TryDecode(modules, out var postnet)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.Postnet, postnet));
                    return true;
                }
                return false;
            case BarcodeType.Planet:
                if (PlanetDecoder.TryDecode(modules, out var planet)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.Planet, planet));
                    return true;
                }
                return false;
            case BarcodeType.RoyalMail4State:
                if (RoyalMailFourStateDecoder.TryDecode(modules, out var royalMail)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.RoyalMail4State, royalMail));
                    return true;
                }
                return false;
            case BarcodeType.AustraliaPost:
                if (AustraliaPostDecoder.TryDecode(modules, out var australiaPost)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.AustraliaPost, australiaPost));
                    return true;
                }
                return false;
            case BarcodeType.JapanPost:
                if (JapanPostDecoder.TryDecode(modules, out var japanPost)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.JapanPost, japanPost));
                    return true;
                }
                return false;
            case BarcodeType.UspsImb:
                if (UspsImbDecoder.TryDecode(modules, out var uspsImb)) {
                    decoded = new CodeGlyphDecoded(new BarcodeDecoded(BarcodeType.UspsImb, uspsImb));
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    private static bool TryDecodeMatrixBarcode(BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        if (DataBar14Decoder.TryDecodeOmni(modules, out var omni)) {
            decoded = new BarcodeDecoded(BarcodeType.GS1DataBarOmni, omni);
            return true;
        }
        if (DataBar14Decoder.TryDecodeStacked(modules, out var stacked)) {
            decoded = new BarcodeDecoded(BarcodeType.GS1DataBarStacked, stacked);
            return true;
        }
        if (DataBarExpandedDecoder.TryDecodeExpandedStacked(modules, out var expandedStacked)) {
            decoded = new BarcodeDecoded(BarcodeType.GS1DataBarExpandedStacked, expandedStacked);
            return true;
        }
        if (KixDecoder.TryDecode(modules, out var kix)) {
            decoded = new BarcodeDecoded(BarcodeType.KixCode, kix);
            return true;
        }
        if (PharmacodeTwoTrackDecoder.TryDecode(modules, out var pharma)) {
            decoded = new BarcodeDecoded(BarcodeType.PharmacodeTwoTrack, pharma);
            return true;
        }
        if (PostnetDecoder.TryDecode(modules, out var postnet)) {
            decoded = new BarcodeDecoded(BarcodeType.Postnet, postnet);
            return true;
        }
        if (PlanetDecoder.TryDecode(modules, out var planet)) {
            decoded = new BarcodeDecoded(BarcodeType.Planet, planet);
            return true;
        }
        if (RoyalMailFourStateDecoder.TryDecode(modules, out var royalMail)) {
            decoded = new BarcodeDecoded(BarcodeType.RoyalMail4State, royalMail);
            return true;
        }
        if (AustraliaPostDecoder.TryDecode(modules, out var australiaPost)) {
            decoded = new BarcodeDecoded(BarcodeType.AustraliaPost, australiaPost);
            return true;
        }
        if (JapanPostDecoder.TryDecode(modules, out var japanPost)) {
            decoded = new BarcodeDecoded(BarcodeType.JapanPost, japanPost);
            return true;
        }
        if (UspsImbDecoder.TryDecode(modules, out var uspsImb)) {
            decoded = new BarcodeDecoded(BarcodeType.UspsImb, uspsImb);
            return true;
        }
        return false;
    }
}
