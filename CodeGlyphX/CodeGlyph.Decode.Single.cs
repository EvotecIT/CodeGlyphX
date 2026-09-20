using System;
using System.Threading;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX;

public static partial class CodeGlyph {
    private static bool TryDecodeCore(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        var squareish = IsSquareish(width, height);
        var preferQr = squareish && LooksLikeQr(pixels, width, height, stride, format);

        if (preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcode)) {
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            if (!squareish) {
                if (cancellationToken.IsCancellationRequested) return false;
                if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out Pdf417Decoded pdf417Pref)) {
                    decoded = new CodeGlyphDecoded(pdf417Pref);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPref)) {
                    decoded = new CodeGlyphDecoded(dataMatrixPref);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrPref, qrOptions, cancellationToken)) {
                    decoded = new CodeGlyphDecoded(qrPref);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecPref)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecPref);
                    return true;
                }
                return false;
            }
            if (preferQr) {
                if (cancellationToken.IsCancellationRequested) return false;
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrPrefQr, qrOptions, cancellationToken)) {
                    decoded = new CodeGlyphDecoded(qrPrefQr);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecPrefQr)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecPrefQr);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefQr)) {
                    decoded = new CodeGlyphDecoded(dataMatrixPrefQr);
                    return true;
                }
            } else {
                if (cancellationToken.IsCancellationRequested) return false;
                if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefDm)) {
                    decoded = new CodeGlyphDecoded(dataMatrixPrefDm);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecPrefDm)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecPrefDm);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrPrefDm, qrOptions, cancellationToken)) {
                    decoded = new CodeGlyphDecoded(qrPrefDm);
                    return true;
                }
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out Pdf417Decoded pdf417Pref0)) {
                decoded = new CodeGlyphDecoded(pdf417Pref0);
                return true;
            }
            return false;
        }

        if (!squareish) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out Pdf417Decoded pdf417Non)) {
                decoded = new CodeGlyphDecoded(pdf417Non);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeNon)) {
                decoded = new CodeGlyphDecoded(barcodeNon);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNon)) {
                decoded = new CodeGlyphDecoded(dataMatrixNon);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrNon, qrOptions, cancellationToken)) {
                decoded = new CodeGlyphDecoded(qrNon);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecNon)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecNon);
                return true;
            }
            return false;
        }

        if (preferQr) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrNonQr, qrOptions, cancellationToken)) {
                decoded = new CodeGlyphDecoded(qrNonQr);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecNonQr)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecNonQr);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonQr)) {
                decoded = new CodeGlyphDecoded(dataMatrixNonQr);
                return true;
            }
        } else {
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonDm)) {
                decoded = new CodeGlyphDecoded(dataMatrixNonDm);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecNonDm)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecNonDm);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrNonDm, qrOptions, cancellationToken)) {
                decoded = new CodeGlyphDecoded(qrNonDm);
                return true;
            }
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out Pdf417Decoded pdf417Non0)) {
            decoded = new CodeGlyphDecoded(pdf417Non0);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeNon0)) {
            decoded = new CodeGlyphDecoded(barcodeNon0);
            return true;
        }
        return false;
    }
}
