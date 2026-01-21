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
/// Decodes 2D barcode symbologies from a <see cref="BitMatrix"/>.
/// </summary>
public static class MatrixBarcodeDecoder {
    /// <summary>
    /// Attempts to decode any supported matrix barcode from a <see cref="BitMatrix"/>.
    /// </summary>
    /// <param name="modules">Module matrix to decode.</param>
    /// <param name="decoded">Decoded barcode payload.</param>
    /// <param name="expectedType">Optional expected matrix barcode type to decode.</param>
    /// <returns><c>true</c> when decoding succeeds.</returns>
    public static bool TryDecodeAny(BitMatrix modules, out BarcodeDecoded decoded, BarcodeType? expectedType = null) {
        decoded = null!;
        if (modules is null) return false;

        if (expectedType.HasValue) {
            return TryDecodeExpected(expectedType.Value, modules, out decoded);
        }

        if (DataMatrixDecoder.TryDecode(modules, out var dataMatrix)) {
            decoded = new BarcodeDecoded(BarcodeType.DataMatrix, dataMatrix);
            return true;
        }
        if (Pdf417Decoder.TryDecode(modules, out string pdf417)) {
            decoded = new BarcodeDecoded(BarcodeType.PDF417, pdf417);
            return true;
        }
        if (MicroPdf417Decoder.TryDecode(modules, out var microPdf417)) {
            decoded = new BarcodeDecoded(BarcodeType.MicroPDF417, microPdf417);
            return true;
        }
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
            BarcodeType.GS1DataBarOmni => DataBar14Decoder.TryDecodeOmni(modules, out text),
            BarcodeType.GS1DataBarStacked => DataBar14Decoder.TryDecodeStacked(modules, out text),
            BarcodeType.GS1DataBarExpandedStacked => DataBarExpandedDecoder.TryDecodeExpandedStacked(modules, out text),
            BarcodeType.DataMatrix => DataMatrixDecoder.TryDecode(modules, out text),
            BarcodeType.PDF417 => Pdf417Decoder.TryDecode(modules, out text),
            BarcodeType.MicroPDF417 => MicroPdf417Decoder.TryDecode(modules, out text),
            _ => throw new NotSupportedException($"BarcodeType.{type} is not a 2D matrix barcode.")
        };
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar-14 Omnidirectional symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeGs1DataBarOmni(BitMatrix modules, out string text) => DataBar14Decoder.TryDecodeOmni(modules, out text);

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

    private static bool TryDecodeExpected(BarcodeType type, BitMatrix modules, out BarcodeDecoded decoded) {
        decoded = null!;
        switch (type) {
            case BarcodeType.DataMatrix:
                if (DataMatrixDecoder.TryDecode(modules, out var dataMatrix)) {
                    decoded = new BarcodeDecoded(BarcodeType.DataMatrix, dataMatrix);
                    return true;
                }
                return false;
            case BarcodeType.PDF417:
                if (Pdf417Decoder.TryDecode(modules, out string pdf417)) {
                    decoded = new BarcodeDecoded(BarcodeType.PDF417, pdf417);
                    return true;
                }
                return false;
            case BarcodeType.MicroPDF417:
                if (MicroPdf417Decoder.TryDecode(modules, out var microPdf417)) {
                    decoded = new BarcodeDecoded(BarcodeType.MicroPDF417, microPdf417);
                    return true;
                }
                return false;
            case BarcodeType.GS1DataBarOmni:
                if (DataBar14Decoder.TryDecodeOmni(modules, out var omni)) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1DataBarOmni, omni);
                    return true;
                }
                return false;
            case BarcodeType.GS1DataBarStacked:
                if (DataBar14Decoder.TryDecodeStacked(modules, out var stacked)) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1DataBarStacked, stacked);
                    return true;
                }
                return false;
            case BarcodeType.GS1DataBarExpandedStacked:
                if (DataBarExpandedDecoder.TryDecodeExpandedStacked(modules, out var expandedStacked)) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1DataBarExpandedStacked, expandedStacked);
                    return true;
                }
                return false;
            case BarcodeType.KixCode:
                if (KixDecoder.TryDecode(modules, out var kix)) {
                    decoded = new BarcodeDecoded(BarcodeType.KixCode, kix);
                    return true;
                }
                return false;
            case BarcodeType.PharmacodeTwoTrack:
                if (PharmacodeTwoTrackDecoder.TryDecode(modules, out var pharma)) {
                    decoded = new BarcodeDecoded(BarcodeType.PharmacodeTwoTrack, pharma);
                    return true;
                }
                return false;
            case BarcodeType.Postnet:
                if (PostnetDecoder.TryDecode(modules, out var postnet)) {
                    decoded = new BarcodeDecoded(BarcodeType.Postnet, postnet);
                    return true;
                }
                return false;
            case BarcodeType.Planet:
                if (PlanetDecoder.TryDecode(modules, out var planet)) {
                    decoded = new BarcodeDecoded(BarcodeType.Planet, planet);
                    return true;
                }
                return false;
            case BarcodeType.RoyalMail4State:
                if (RoyalMailFourStateDecoder.TryDecode(modules, out var royalMail)) {
                    decoded = new BarcodeDecoded(BarcodeType.RoyalMail4State, royalMail);
                    return true;
                }
                return false;
            case BarcodeType.AustraliaPost:
                if (AustraliaPostDecoder.TryDecode(modules, out var australiaPost)) {
                    decoded = new BarcodeDecoded(BarcodeType.AustraliaPost, australiaPost);
                    return true;
                }
                return false;
            case BarcodeType.JapanPost:
                if (JapanPostDecoder.TryDecode(modules, out var japanPost)) {
                    decoded = new BarcodeDecoded(BarcodeType.JapanPost, japanPost);
                    return true;
                }
                return false;
            case BarcodeType.UspsImb:
                if (UspsImbDecoder.TryDecode(modules, out var uspsImb)) {
                    decoded = new BarcodeDecoded(BarcodeType.UspsImb, uspsImb);
                    return true;
                }
                return false;
            default:
                return false;
        }
    }
}
