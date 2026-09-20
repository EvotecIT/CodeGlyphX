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
    private static bool TryDecodeCore(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        decoded = null!;
        if (IsCancelled(cancellationToken, diagnostics)) return false;
        var squareish = IsSquareish(width, height);
        var preferQr = squareish && LooksLikeQr(pixels, width, height, stride, format);

        if (preferBarcode) {
            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcode, out var barcodeDiag)) {
                diagnostics.Barcode = barcodeDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            diagnostics.Barcode = barcodeDiag;

            if (!squareish) {
                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417, out var pdfDiag)) {
                    diagnostics.Pdf417 = pdfDiag;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                    return true;
                }
                diagnostics.Pdf417 = pdfDiag;

                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrix, out var dmDiag)) {
                    diagnostics.DataMatrix = dmDiag;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                    decoded = new CodeGlyphDecoded(dataMatrix);
                    return true;
                }
                diagnostics.DataMatrix = dmDiag;

                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr, out var qrInfo, qrOptions, cancellationToken)) {
                    diagnostics.Qr = qrInfo;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Qr;
                    decoded = new CodeGlyphDecoded(qr);
                    return true;
                }
                diagnostics.Qr = qrInfo;

                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec, out var aztecDiag)) {
                    diagnostics.Aztec = aztecDiag;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                    return true;
                }
                diagnostics.Aztec = aztecDiag;

                SetNoResult(diagnostics);
                return false;
            }

            if (preferQr) {
                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrPrefQr, out var qrInfoPrefQr, qrOptions, cancellationToken)) {
                    diagnostics.Qr = qrInfoPrefQr;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Qr;
                    decoded = new CodeGlyphDecoded(qrPrefQr);
                    return true;
                }
                diagnostics.Qr = qrInfoPrefQr;

                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecPrefQr, out var aztecDiagPrefQr)) {
                    diagnostics.Aztec = aztecDiagPrefQr;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecPrefQr);
                    return true;
                }
                diagnostics.Aztec = aztecDiagPrefQr;

                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefQr, out var dmDiagPrefQr)) {
                    diagnostics.DataMatrix = dmDiagPrefQr;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                    decoded = new CodeGlyphDecoded(dataMatrixPrefQr);
                    return true;
                }
                diagnostics.DataMatrix = dmDiagPrefQr;
            } else {
                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefDm, out var dmDiagPrefDm)) {
                    diagnostics.DataMatrix = dmDiagPrefDm;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                    decoded = new CodeGlyphDecoded(dataMatrixPrefDm);
                    return true;
                }
                diagnostics.DataMatrix = dmDiagPrefDm;

                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecPrefDm, out var aztecDiagPrefDm)) {
                    diagnostics.Aztec = aztecDiagPrefDm;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecPrefDm);
                    return true;
                }
                diagnostics.Aztec = aztecDiagPrefDm;

                if (IsCancelled(cancellationToken, diagnostics)) return false;
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrPrefDm, out var qrInfoPrefDm, qrOptions, cancellationToken)) {
                    diagnostics.Qr = qrInfoPrefDm;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Qr;
                    decoded = new CodeGlyphDecoded(qrPrefDm);
                    return true;
                }
                diagnostics.Qr = qrInfoPrefDm;
            }

            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf4170, out var pdfDiag0)) {
                diagnostics.Pdf417 = pdfDiag0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf4170);
                return true;
            }
            diagnostics.Pdf417 = pdfDiag0;

            SetNoResult(diagnostics);
            return false;
        }

        if (!squareish) {
            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded, out var pdfDiagA)) {
                diagnostics.Pdf417 = pdfDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
                return true;
            }
            diagnostics.Pdf417 = pdfDiagA;

            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded, out var barcodeDiagA)) {
                diagnostics.Barcode = barcodeDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
                decoded = new CodeGlyphDecoded(barcodeDecoded);
                return true;
            }
            diagnostics.Barcode = barcodeDiagA;

            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixDecoded, out var dmDiagA)) {
                diagnostics.DataMatrix = dmDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(dataMatrixDecoded);
                return true;
            }
            diagnostics.DataMatrix = dmDiagA;

            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, out var qrInfoA, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfoA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qrDecoded);
                return true;
            }
            diagnostics.Qr = qrInfoA;

            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecDecoded, out var aztecDiagA)) {
                diagnostics.Aztec = aztecDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
                return true;
            }
            diagnostics.Aztec = aztecDiagA;

            SetNoResult(diagnostics);
            return false;
        }

        if (preferQr) {
            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrNonQr0, out var qrInfoNonQr0, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfoNonQr0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qrNonQr0);
                return true;
            }
            diagnostics.Qr = qrInfoNonQr0;

            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecNonQr0, out var aztecDiagNonQr0)) {
                diagnostics.Aztec = aztecDiagNonQr0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecNonQr0);
                return true;
            }
            diagnostics.Aztec = aztecDiagNonQr0;

            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonQr0, out var dmDiagNonQr0)) {
                diagnostics.DataMatrix = dmDiagNonQr0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(dataMatrixNonQr0);
                return true;
            }
            diagnostics.DataMatrix = dmDiagNonQr0;
        } else {
            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonDm0, out var dmDiagNonDm0)) {
                diagnostics.DataMatrix = dmDiagNonDm0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(dataMatrixNonDm0);
                return true;
            }
            diagnostics.DataMatrix = dmDiagNonDm0;

            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecNonDm0, out var aztecDiagNonDm0)) {
                diagnostics.Aztec = aztecDiagNonDm0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecNonDm0);
                return true;
            }
            diagnostics.Aztec = aztecDiagNonDm0;

            if (IsCancelled(cancellationToken, diagnostics)) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrNonDm0, out var qrInfoNonDm0, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfoNonDm0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qrNonDm0);
                return true;
            }
            diagnostics.Qr = qrInfoNonDm0;
        }

        if (IsCancelled(cancellationToken, diagnostics)) return false;
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded0, out var pdfDiagB)) {
            diagnostics.Pdf417 = pdfDiagB;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded0);
            return true;
        }
        diagnostics.Pdf417 = pdfDiagB;

        if (IsCancelled(cancellationToken, diagnostics)) return false;
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded0, out var barcodeDiagB)) {
            diagnostics.Barcode = barcodeDiagB;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
            decoded = new CodeGlyphDecoded(barcodeDecoded0);
            return true;
        }
        diagnostics.Barcode = barcodeDiagB;
        SetNoResult(diagnostics);
        return false;
    }
}
