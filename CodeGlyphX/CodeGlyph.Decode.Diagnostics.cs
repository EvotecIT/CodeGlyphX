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
    // One selection sequence serves raw, diagnostic and image-budgeted single-result calls.
    private static bool TryDecodeCore(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics,
        BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null,
        CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null,
        ImageDecodeOptions? imageOptions = null) {
        decoded = null!;
        diagnostics = new CodeGlyphDecodeDiagnostics();
        if (IsCancelled(cancellationToken, diagnostics)) return false;

        if (preferBarcode) {
            using var budget = ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, imageOptions, out var token);
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out var barcode, out var trace)) {
                diagnostics.Barcode = trace;
                decoded = new CodeGlyphDecoded(barcode);
                return CompleteDecode(decoded, diagnostics);
            }
            diagnostics.Barcode = trace;
        }

        if (IsCancelled(cancellationToken, diagnostics)) return false;
        // A frame's aspect ratio does not constrain the shapes of symbols inside it.
        var resolvedQr = qrOptions ?? new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust, BudgetMilliseconds = 800 };
        if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr, out var qrTrace, resolvedQr, cancellationToken)) {
            diagnostics.Qr = qrTrace;
            decoded = new CodeGlyphDecoded(qr);
            return CompleteDecode(decoded, diagnostics);
        }
        diagnostics.Qr = qrTrace;

        // Geometry orders fallback work; it never excludes QR detection.
        var rectangular = width > height * 1.35 || height > width * 1.35;
        if (rectangular && TryDecodeFallback(CodeGlyphKind.Pdf417, pixels, width, height, stride, format, out decoded, diagnostics, expectedBarcode, barcodeOptions, cancellationToken, imageOptions)) return true;
        if (rectangular && !preferBarcode && TryDecodeFallback(CodeGlyphKind.Barcode1D, pixels, width, height, stride, format, out decoded, diagnostics, expectedBarcode, barcodeOptions, cancellationToken, imageOptions)) return true;
        if (TryDecodeFallback(CodeGlyphKind.DataMatrix, pixels, width, height, stride, format, out decoded, diagnostics, expectedBarcode, barcodeOptions, cancellationToken, imageOptions)) return true;
        if (TryDecodeFallback(CodeGlyphKind.Aztec, pixels, width, height, stride, format, out decoded, diagnostics, expectedBarcode, barcodeOptions, cancellationToken, imageOptions)) return true;
        if (!rectangular && TryDecodeFallback(CodeGlyphKind.Pdf417, pixels, width, height, stride, format, out decoded, diagnostics, expectedBarcode, barcodeOptions, cancellationToken, imageOptions)) return true;
        if (!rectangular && !preferBarcode && TryDecodeFallback(CodeGlyphKind.Barcode1D, pixels, width, height, stride, format, out decoded, diagnostics, expectedBarcode, barcodeOptions, cancellationToken, imageOptions)) return true;
        if (IsCancelled(cancellationToken, diagnostics)) return false;
        SetNoResult(diagnostics);
        return false;
    }

    private static bool TryDecodeFallback(CodeGlyphKind kind, PixelSpan pixels, int width, int height, int stride,
        PixelFormat format, out CodeGlyphDecoded decoded, CodeGlyphDecodeDiagnostics diagnostics,
        BarcodeType? expectedBarcode, BarcodeDecodeOptions? barcodeOptions, CancellationToken cancellationToken,
        ImageDecodeOptions? imageOptions) {
        decoded = null!;
        if (IsCancelled(cancellationToken, diagnostics)) return false;
        using var budget = ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, imageOptions, out var token);
        switch (kind) {
            case CodeGlyphKind.Pdf417:
                if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, token, out string pdf, out var pdfTrace)) {
                    diagnostics.Pdf417 = pdfTrace;
                    decoded = new CodeGlyphDecoded(new Pdf417Decoded(pdf, pdfTrace.Macro));
                    return CompleteDecode(decoded, diagnostics);
                }
                diagnostics.Pdf417 = pdfTrace;
                break;
            case CodeGlyphKind.DataMatrix:
                if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, token, out var matrix, out var matrixTrace)) {
                    diagnostics.DataMatrix = matrixTrace;
                    decoded = new CodeGlyphDecoded(matrix);
                    return CompleteDecode(decoded, diagnostics);
                }
                diagnostics.DataMatrix = matrixTrace;
                break;
            case CodeGlyphKind.Aztec:
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, token, out var aztec, out var aztecTrace)) {
                    diagnostics.Aztec = aztecTrace;
                    decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                    return CompleteDecode(decoded, diagnostics);
                }
                diagnostics.Aztec = aztecTrace;
                break;
            case CodeGlyphKind.Barcode1D:
                if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out var barcode, out var barcodeTrace)) {
                    diagnostics.Barcode = barcodeTrace;
                    decoded = new CodeGlyphDecoded(barcode);
                    return CompleteDecode(decoded, diagnostics);
                }
                diagnostics.Barcode = barcodeTrace;
                break;
        }
        return false;
    }

    private static bool CompleteDecode(CodeGlyphDecoded decoded, CodeGlyphDecodeDiagnostics diagnostics) {
        diagnostics.Success = true;
        diagnostics.SuccessKind = decoded.Kind;
        return true;
    }
}
