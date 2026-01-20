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

        if (preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcode)) {
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr, qrOptions, cancellationToken)) {
                decoded = new CodeGlyphDecoded(qr);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                return true;
            }
            return false;
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecDecoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, qrOptions, cancellationToken)) {
            decoded = new CodeGlyphDecoded(qrDecoded);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixDecoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
            return true;
        }
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

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec, out var aztecDiag)) {
                diagnostics.Aztec = aztecDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                return true;
            }
            diagnostics.Aztec = aztecDiag;

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
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix, out var dmDiag)) {
                diagnostics.DataMatrix = dmDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                return true;
            }
            diagnostics.DataMatrix = dmDiag;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417, out var pdfDiag)) {
                diagnostics.Pdf417 = pdfDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                return true;
            }
            diagnostics.Pdf417 = pdfDiag;

            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecDecoded, out var aztecDiag0)) {
            diagnostics.Aztec = aztecDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Aztec;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
            return true;
        }
        diagnostics.Aztec = aztecDiag0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, out var qrInfo0, qrOptions, cancellationToken)) {
            diagnostics.Qr = qrInfo0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Qr;
            decoded = new CodeGlyphDecoded(qrDecoded);
            return true;
        }
        diagnostics.Qr = qrInfo0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixDecoded, out var dmDiag0)) {
            diagnostics.DataMatrix = dmDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
            return true;
        }
        diagnostics.DataMatrix = dmDiag0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded, out var pdfDiag0)) {
            diagnostics.Pdf417 = pdfDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
            return true;
        }
        diagnostics.Pdf417 = pdfDiag0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded, out var barcodeDiag0)) {
            diagnostics.Barcode = barcodeDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
            decoded = new CodeGlyphDecoded(barcodeDecoded);
            return true;
        }
        diagnostics.Barcode = barcodeDiag0;
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

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
            }
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, qrOptions, cancellationToken)) {
            for (var i = 0; i < qrResults.Length; i++) {
                list.Add(new CodeGlyphDecoded(qrResults[i]));
            }
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
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

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, out var qrInfo, qrOptions, cancellationToken)) {
            diagnostics.Qr = qrInfo;
            for (var i = 0; i < qrResults.Length; i++) {
                list.Add(new CodeGlyphDecoded(qrResults[i]));
            }
        } else {
            diagnostics.Qr = qrInfo;
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec, out var aztecDiag)) {
            diagnostics.Aztec = aztecDiag;
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
        } else {
            diagnostics.Aztec = aztecDiag;
        }

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

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        if (preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcode)) {
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr, qrOptions, cancellationToken)) {
                decoded = new CodeGlyphDecoded(qr);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                return true;
            }
            return false;
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecDecoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, qrOptions, cancellationToken)) {
            decoded = new CodeGlyphDecoded(qrDecoded);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixDecoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
            return true;
        }
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
        return false;
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }

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

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec, out var aztecDiag)) {
                diagnostics.Aztec = aztecDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                return true;
            }
            diagnostics.Aztec = aztecDiag;

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
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix, out var dmDiag)) {
                diagnostics.DataMatrix = dmDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                return true;
            }
            diagnostics.DataMatrix = dmDiag;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417, out var pdfDiag)) {
                diagnostics.Pdf417 = pdfDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                return true;
            }
            diagnostics.Pdf417 = pdfDiag;

            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztecDecoded, out var aztecDiag0)) {
            diagnostics.Aztec = aztecDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Aztec;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
            return true;
        }
        diagnostics.Aztec = aztecDiag0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, out var qrInfo0, qrOptions, cancellationToken)) {
            diagnostics.Qr = qrInfo0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Qr;
            decoded = new CodeGlyphDecoded(qrDecoded);
            return true;
        }
        diagnostics.Qr = qrInfo0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrixDecoded, out var dmDiag0)) {
            diagnostics.DataMatrix = dmDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
            return true;
        }
        diagnostics.DataMatrix = dmDiag0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417Decoded, out var pdfDiag0)) {
            diagnostics.Pdf417 = pdfDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
            return true;
        }
        diagnostics.Pdf417 = pdfDiag0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, cancellationToken, out var barcodeDecoded, out var barcodeDiag0)) {
            diagnostics.Barcode = barcodeDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
            decoded = new CodeGlyphDecoded(barcodeDecoded);
            return true;
        }
        diagnostics.Barcode = barcodeDiag0;
        diagnostics.Failure ??= "No symbol decoded.";
        return false;
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from raw pixels.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (cancellationToken.IsCancellationRequested) return false;

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
            }
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, qrOptions, cancellationToken)) {
            for (var i = 0; i < qrResults.Length; i++) {
                list.Add(new CodeGlyphDecoded(qrResults[i]));
            }
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var dataMatrix)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var pdf417)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
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

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, out var qrInfo, qrOptions, cancellationToken)) {
            diagnostics.Qr = qrInfo;
            for (var i = 0; i < qrResults.Length; i++) {
                list.Add(new CodeGlyphDecoded(qrResults[i]));
            }
        } else {
            diagnostics.Qr = qrInfo;
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec, out var aztecDiag)) {
            diagnostics.Aztec = aztecDiag;
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
        } else {
            diagnostics.Aztec = aztecDiag;
        }

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
