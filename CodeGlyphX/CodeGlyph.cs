using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Unified decode helpers (QR + 1D + 2D barcodes).
/// </summary>
public static partial class CodeGlyph {
    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = null!;
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
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
                if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Pref)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Pref);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPref)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixPref);
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
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefQr)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixPrefQr);
                    return true;
                }
            } else {
                if (cancellationToken.IsCancellationRequested) return false;
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefDm)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixPrefDm);
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
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Pref0)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Pref0);
                return true;
            }
            return false;
        }

        if (!squareish) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Non)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Non);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeNon)) {
                decoded = new CodeGlyphDecoded(barcodeNon);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNon)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixNon);
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
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonQr)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixNonQr);
                return true;
            }
        } else {
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonDm)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixNonDm);
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
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Non0)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Non0);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeNon0)) {
            decoded = new CodeGlyphDecoded(barcodeNon0);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        decoded = null!;
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var squareish = IsSquareish(width, height);
        var preferQr = squareish && LooksLikeQr(pixels, width, height, stride, format);

        if (preferBarcode) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcode, out var barcodeDiag)) {
                diagnostics.Barcode = barcodeDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            diagnostics.Barcode = barcodeDiag;

            if (!squareish) {
                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417, out var pdfDiag)) {
                    diagnostics.Pdf417 = pdfDiag;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                    return true;
                }
                diagnostics.Pdf417 = pdfDiag;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix, out var dmDiag)) {
                    diagnostics.DataMatrix = dmDiag;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                    return true;
                }
                diagnostics.DataMatrix = dmDiag;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr, out var qrInfo, qrOptions, cancellationToken)) {
                    diagnostics.Qr = qrInfo;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Qr;
                    decoded = new CodeGlyphDecoded(qr);
                    return true;
                }
                diagnostics.Qr = qrInfo;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec, out var aztecDiag)) {
                    diagnostics.Aztec = aztecDiag;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                    return true;
                }
                diagnostics.Aztec = aztecDiag;

                diagnostics.Failure ??= "No symbol decoded.";
                return false;
            }

            if (preferQr) {
                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrPrefQr, out var qrInfoPrefQr, qrOptions, cancellationToken)) {
                    diagnostics.Qr = qrInfoPrefQr;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Qr;
                    decoded = new CodeGlyphDecoded(qrPrefQr);
                    return true;
                }
                diagnostics.Qr = qrInfoPrefQr;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecPrefQr, out var aztecDiagPrefQr)) {
                    diagnostics.Aztec = aztecDiagPrefQr;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecPrefQr);
                    return true;
                }
                diagnostics.Aztec = aztecDiagPrefQr;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefQr, out var dmDiagPrefQr)) {
                    diagnostics.DataMatrix = dmDiagPrefQr;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixPrefQr);
                    return true;
                }
                diagnostics.DataMatrix = dmDiagPrefQr;
            } else {
                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefDm, out var dmDiagPrefDm)) {
                    diagnostics.DataMatrix = dmDiagPrefDm;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixPrefDm);
                    return true;
                }
                diagnostics.DataMatrix = dmDiagPrefDm;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecPrefDm, out var aztecDiagPrefDm)) {
                    diagnostics.Aztec = aztecDiagPrefDm;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecPrefDm);
                    return true;
                }
                diagnostics.Aztec = aztecDiagPrefDm;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrPrefDm, out var qrInfoPrefDm, qrOptions, cancellationToken)) {
                    diagnostics.Qr = qrInfoPrefDm;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Qr;
                    decoded = new CodeGlyphDecoded(qrPrefDm);
                    return true;
                }
                diagnostics.Qr = qrInfoPrefDm;
            }

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf4170, out var pdfDiag0)) {
                diagnostics.Pdf417 = pdfDiag0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf4170);
                return true;
            }
            diagnostics.Pdf417 = pdfDiag0;

            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }

        if (!squareish) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded, out var pdfDiagA)) {
                diagnostics.Pdf417 = pdfDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
                return true;
            }
            diagnostics.Pdf417 = pdfDiagA;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded, out var barcodeDiagA)) {
                diagnostics.Barcode = barcodeDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
                decoded = new CodeGlyphDecoded(barcodeDecoded);
                return true;
            }
            diagnostics.Barcode = barcodeDiagA;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixDecoded, out var dmDiagA)) {
                diagnostics.DataMatrix = dmDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
                return true;
            }
            diagnostics.DataMatrix = dmDiagA;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, out var qrInfoA, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfoA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qrDecoded);
                return true;
            }
            diagnostics.Qr = qrInfoA;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecDecoded, out var aztecDiagA)) {
                diagnostics.Aztec = aztecDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
                return true;
            }
            diagnostics.Aztec = aztecDiagA;

            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }

        if (preferQr) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrNonQr0, out var qrInfoNonQr0, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfoNonQr0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qrNonQr0);
                return true;
            }
            diagnostics.Qr = qrInfoNonQr0;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecNonQr0, out var aztecDiagNonQr0)) {
                diagnostics.Aztec = aztecDiagNonQr0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecNonQr0);
                return true;
            }
            diagnostics.Aztec = aztecDiagNonQr0;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonQr0, out var dmDiagNonQr0)) {
                diagnostics.DataMatrix = dmDiagNonQr0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixNonQr0);
                return true;
            }
            diagnostics.DataMatrix = dmDiagNonQr0;
        } else {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonDm0, out var dmDiagNonDm0)) {
                diagnostics.DataMatrix = dmDiagNonDm0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixNonDm0);
                return true;
            }
            diagnostics.DataMatrix = dmDiagNonDm0;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecNonDm0, out var aztecDiagNonDm0)) {
                diagnostics.Aztec = aztecDiagNonDm0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecNonDm0);
                return true;
            }
            diagnostics.Aztec = aztecDiagNonDm0;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrNonDm0, out var qrInfoNonDm0, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfoNonDm0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qrNonDm0);
                return true;
            }
            diagnostics.Qr = qrInfoNonDm0;
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded0, out var pdfDiagB)) {
            diagnostics.Pdf417 = pdfDiagB;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded0);
            return true;
        }
        diagnostics.Pdf417 = pdfDiagB;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded0, out var barcodeDiagB)) {
            diagnostics.Barcode = barcodeDiagB;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
            decoded = new CodeGlyphDecoded(barcodeDecoded0);
            return true;
        }
        diagnostics.Barcode = barcodeDiagB;
        diagnostics.Failure ??= "No symbol decoded.";
        return false;
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from raw pixels.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (cancellationToken.IsCancellationRequested) return false;
        var squareish = IsSquareish(width, height);
        var qrOptionsLocal = qrOptions ?? new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Balanced,
            BudgetMilliseconds = 800,
            MaxMilliseconds = 800
        };
        var preferQr = squareish && LooksLikeQr(pixels, width, height, stride, format);
        var foundQr = false;

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
            }
        }

        if (squareish) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, qrOptionsLocal, cancellationToken)) {
                for (var i = 0; i < qrResults.Length; i++) {
                    list.Add(new CodeGlyphDecoded(qrResults[i]));
                }
                foundQr = qrResults.Length > 0;
            }

            if (!preferQr || !foundQr) {
                if (cancellationToken.IsCancellationRequested) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec)) {
                    list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
                }
            }
        }

        if (!preferQr || !foundQr) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix)) {
                list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
            }

            if (cancellationToken.IsCancellationRequested) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417)) {
                list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
            }
        }

        if (includeBarcode && !preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
            }
        }

        if (list.Count == 0) return false;
        decoded = list.ToArray();
        return true;
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from raw pixels, with diagnostics.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, out CodeGlyphDecodeDiagnostics diagnostics, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var squareish = IsSquareish(width, height);
        var qrOptionsLocal = qrOptions ?? new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Balanced,
            BudgetMilliseconds = 800,
            MaxMilliseconds = 800
        };
        var preferQr = squareish && LooksLikeQr(pixels, width, height, stride, format);
        var foundQr = false;

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
                diagnostics.Barcode = new BarcodeDecodeDiagnostics { Success = true, CandidateCount = barcodes.Length };
            }
        }

        if (squareish) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, out var qrInfo, qrOptionsLocal, cancellationToken)) {
                diagnostics.Qr = qrInfo;
                for (var i = 0; i < qrResults.Length; i++) {
                    list.Add(new CodeGlyphDecoded(qrResults[i]));
                }
                foundQr = qrResults.Length > 0;
            } else {
                diagnostics.Qr = qrInfo;
            }

            if (!preferQr || !foundQr) {
                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec, out var aztecDiag)) {
                    diagnostics.Aztec = aztecDiag;
                    list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
                } else {
                    diagnostics.Aztec = aztecDiag;
                }
            }
        }

        if (!preferQr || !foundQr) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix, out var dmDiag)) {
                diagnostics.DataMatrix = dmDiag;
                list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
            } else {
                diagnostics.DataMatrix = dmDiag;
            }

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417, out var pdfDiag)) {
                diagnostics.Pdf417 = pdfDiag;
                list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
            } else {
                diagnostics.Pdf417 = pdfDiag;
            }
        }

        if (includeBarcode && !preferBarcode) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
                diagnostics.Barcode = new BarcodeDecodeDiagnostics { Success = true, CandidateCount = barcodes.Length };
            }
        }

        if (list.Count == 0) {
            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }
        diagnostics.Success = true;
        diagnostics.SuccessKind = list.Count == 1 ? list[0].Kind : null;
        decoded = list.ToArray();
        return true;
    }

    private static bool IsSquareish(int width, int height) {
        if (width <= 0 || height <= 0) return false;
        var min = width < height ? width : height;
        var max = width > height ? width : height;
        return (double)max / min <= 1.35d;
    }

    private static bool LooksLikeQr(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format) {
#if NET8_0_OR_GREATER
        if (LooksLikeQrAtScale(pixels, width, height, stride, format, scale: 2)) return true;
        if (LooksLikeQrAtScale(pixels, width, height, stride, format, scale: 1)) return true;
#endif
        return false;
    }

#if NET8_0_OR_GREATER
    private static bool LooksLikeQrAtScale(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, int scale) {
        if (!CodeGlyphX.Qr.QrGrayImage.TryCreate(pixels, width, height, stride, format, scale, out var image)) return false;
        if (CodeGlyphX.Qr.QrFinderPatternDetector.TryFind(image, invert: false, out _, out _, out _)) return true;
        return CodeGlyphX.Qr.QrFinderPatternDetector.TryFind(image, invert: true, out _, out _, out _);
    }
#endif

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
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
                if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr, qrOptions, cancellationToken)) {
                    decoded = new CodeGlyphDecoded(qr);
                    return true;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
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
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefQr)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixPrefQr);
                    return true;
                }
            } else {
                if (cancellationToken.IsCancellationRequested) return false;
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefDm)) {
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixPrefDm);
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
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf4170)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf4170);
                return true;
            }
            return false;
        }

        if (!squareish) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded)) {
                decoded = new CodeGlyphDecoded(barcodeDecoded);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixDecoded)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, qrOptions, cancellationToken)) {
                decoded = new CodeGlyphDecoded(qrDecoded);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecDecoded)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
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
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonQr)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixNonQr);
                return true;
            }
        } else {
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonDm)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixNonDm);
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
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded0)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded0);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded0)) {
            decoded = new CodeGlyphDecoded(barcodeDecoded0);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var squareish = IsSquareish(width, height);
        var preferQr = squareish && LooksLikeQr(pixels, width, height, stride, format);

        if (preferBarcode) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcode, out var barcodeDiag)) {
                diagnostics.Barcode = barcodeDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            diagnostics.Barcode = barcodeDiag;

            if (!squareish) {
                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417, out var pdfDiag)) {
                    diagnostics.Pdf417 = pdfDiag;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                    return true;
                }
                diagnostics.Pdf417 = pdfDiag;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix, out var dmDiag)) {
                    diagnostics.DataMatrix = dmDiag;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                    return true;
                }
                diagnostics.DataMatrix = dmDiag;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr, out var qrInfo, qrOptions, cancellationToken)) {
                    diagnostics.Qr = qrInfo;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Qr;
                    decoded = new CodeGlyphDecoded(qr);
                    return true;
                }
                diagnostics.Qr = qrInfo;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec, out var aztecDiag)) {
                    diagnostics.Aztec = aztecDiag;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                    return true;
                }
                diagnostics.Aztec = aztecDiag;

                diagnostics.Failure ??= "No symbol decoded.";
                return false;
            }

            if (preferQr) {
                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrPrefQr, out var qrInfoPrefQr, qrOptions, cancellationToken)) {
                    diagnostics.Qr = qrInfoPrefQr;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Qr;
                    decoded = new CodeGlyphDecoded(qrPrefQr);
                    return true;
                }
                diagnostics.Qr = qrInfoPrefQr;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecPrefQr, out var aztecDiagPrefQr)) {
                    diagnostics.Aztec = aztecDiagPrefQr;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecPrefQr);
                    return true;
                }
                diagnostics.Aztec = aztecDiagPrefQr;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefQr, out var dmDiagPrefQr)) {
                    diagnostics.DataMatrix = dmDiagPrefQr;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixPrefQr);
                    return true;
                }
                diagnostics.DataMatrix = dmDiagPrefQr;
            } else {
                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixPrefDm, out var dmDiagPrefDm)) {
                    diagnostics.DataMatrix = dmDiagPrefDm;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixPrefDm);
                    return true;
                }
                diagnostics.DataMatrix = dmDiagPrefDm;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecPrefDm, out var aztecDiagPrefDm)) {
                    diagnostics.Aztec = aztecDiagPrefDm;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecPrefDm);
                    return true;
                }
                diagnostics.Aztec = aztecDiagPrefDm;

                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrPrefDm, out var qrInfoPrefDm, qrOptions, cancellationToken)) {
                    diagnostics.Qr = qrInfoPrefDm;
                    diagnostics.Success = true;
                    diagnostics.SuccessKind = CodeGlyphKind.Qr;
                    decoded = new CodeGlyphDecoded(qrPrefDm);
                    return true;
                }
                diagnostics.Qr = qrInfoPrefDm;
            }

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf4170, out var pdfDiag0)) {
                diagnostics.Pdf417 = pdfDiag0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf4170);
                return true;
            }
            diagnostics.Pdf417 = pdfDiag0;

            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }

        if (!squareish) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded, out var pdfDiagA)) {
                diagnostics.Pdf417 = pdfDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
                return true;
            }
            diagnostics.Pdf417 = pdfDiagA;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded, out var barcodeDiagA)) {
                diagnostics.Barcode = barcodeDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
                decoded = new CodeGlyphDecoded(barcodeDecoded);
                return true;
            }
            diagnostics.Barcode = barcodeDiagA;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixDecoded, out var dmDiagA)) {
                diagnostics.DataMatrix = dmDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
                return true;
            }
            diagnostics.DataMatrix = dmDiagA;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, out var qrInfoA, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfoA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qrDecoded);
                return true;
            }
            diagnostics.Qr = qrInfoA;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecDecoded, out var aztecDiagA)) {
                diagnostics.Aztec = aztecDiagA;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
                return true;
            }
            diagnostics.Aztec = aztecDiagA;

            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }

        if (preferQr) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrNonQr0, out var qrInfoNonQr0, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfoNonQr0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qrNonQr0);
                return true;
            }
            diagnostics.Qr = qrInfoNonQr0;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecNonQr0, out var aztecDiagNonQr0)) {
                diagnostics.Aztec = aztecDiagNonQr0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecNonQr0);
                return true;
            }
            diagnostics.Aztec = aztecDiagNonQr0;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonQr0, out var dmDiagNonQr0)) {
                diagnostics.DataMatrix = dmDiagNonQr0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixNonQr0);
                return true;
            }
            diagnostics.DataMatrix = dmDiagNonQr0;
        } else {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixNonDm0, out var dmDiagNonDm0)) {
                diagnostics.DataMatrix = dmDiagNonDm0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixNonDm0);
                return true;
            }
            diagnostics.DataMatrix = dmDiagNonDm0;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecNonDm0, out var aztecDiagNonDm0)) {
                diagnostics.Aztec = aztecDiagNonDm0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecNonDm0);
                return true;
            }
            diagnostics.Aztec = aztecDiagNonDm0;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrNonDm0, out var qrInfoNonDm0, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfoNonDm0;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qrNonDm0);
                return true;
            }
            diagnostics.Qr = qrInfoNonDm0;
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded0, out var pdfDiagB)) {
            diagnostics.Pdf417 = pdfDiagB;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded0);
            return true;
        }
        diagnostics.Pdf417 = pdfDiagB;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded0, out var barcodeDiagB)) {
            diagnostics.Barcode = barcodeDiagB;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
            decoded = new CodeGlyphDecoded(barcodeDecoded0);
            return true;
        }
        diagnostics.Barcode = barcodeDiagB;
        diagnostics.Failure ??= "No symbol decoded.";
        return false;
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from raw pixels.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (cancellationToken.IsCancellationRequested) return false;
        var squareish = IsSquareish(width, height);
        var qrOptionsLocal = qrOptions ?? new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Balanced,
            BudgetMilliseconds = 800,
            MaxMilliseconds = 800
        };
        var preferQr = squareish && LooksLikeQr(pixels, width, height, stride, format);
        var foundQr = false;

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
            }
        }

        if (squareish) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, qrOptionsLocal, cancellationToken)) {
                for (var i = 0; i < qrResults.Length; i++) {
                    list.Add(new CodeGlyphDecoded(qrResults[i]));
                }
                foundQr = qrResults.Length > 0;
            }

            if (!preferQr || !foundQr) {
                if (cancellationToken.IsCancellationRequested) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec)) {
                    list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
                }
            }
        }

        if (!preferQr || !foundQr) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix)) {
                list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
            }

            if (cancellationToken.IsCancellationRequested) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417)) {
                list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
            }
        }

        if (includeBarcode && !preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
            }
        }

        if (list.Count == 0) return false;
        decoded = list.ToArray();
        return true;
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from raw pixels, with diagnostics.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, out CodeGlyphDecodeDiagnostics diagnostics, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var squareish = IsSquareish(width, height);
        var qrOptionsLocal = qrOptions ?? new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Balanced,
            BudgetMilliseconds = 800,
            MaxMilliseconds = 800
        };
        var preferQr = squareish && LooksLikeQr(pixels, width, height, stride, format);
        var foundQr = false;

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
                diagnostics.Barcode = new BarcodeDecodeDiagnostics { Success = true, CandidateCount = barcodes.Length };
            }
        }

        if (squareish) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, out var qrInfo, qrOptionsLocal, cancellationToken)) {
                diagnostics.Qr = qrInfo;
                for (var i = 0; i < qrResults.Length; i++) {
                    list.Add(new CodeGlyphDecoded(qrResults[i]));
                }
                foundQr = qrResults.Length > 0;
            } else {
                diagnostics.Qr = qrInfo;
            }

            if (!preferQr || !foundQr) {
                if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec, out var aztecDiag)) {
                    diagnostics.Aztec = aztecDiag;
                    list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
                } else {
                    diagnostics.Aztec = aztecDiag;
                }
            }
        }

        if (!preferQr || !foundQr) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix, out var dmDiag)) {
                diagnostics.DataMatrix = dmDiag;
                list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
            } else {
                diagnostics.DataMatrix = dmDiag;
            }

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417, out var pdfDiag)) {
                diagnostics.Pdf417 = pdfDiag;
                list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
            } else {
                diagnostics.Pdf417 = pdfDiag;
            }
        }

        if (includeBarcode && !preferBarcode) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
                diagnostics.Barcode = new BarcodeDecodeDiagnostics { Success = true, CandidateCount = barcodes.Length };
            }
        }

        if (list.Count == 0) {
            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }
        diagnostics.Success = true;
        diagnostics.SuccessKind = list.Count == 1 ? list[0].Kind : null;
        decoded = list.ToArray();
        return true;
    }
#endif
}
