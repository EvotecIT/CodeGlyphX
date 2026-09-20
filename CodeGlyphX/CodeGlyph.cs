using System;
using System.Threading;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Unified decode helpers (QR + 1D + 2D barcodes).
/// </summary>
public static partial class CodeGlyph {
    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecodeCore(pixels, width, height, stride, format, out decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecodeCore(pixels, width, height, stride, format, out decoded, out diagnostics, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from raw pixels.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecodeAllCore(pixels, width, height, stride, format, out decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    private static bool IsSquareish(int width, int height) {
        if (width <= 0 || height <= 0) return false;
        var min = width < height ? width : height;
        var max = width > height ? width : height;
        return (double)max / min <= 1.35d;
    }

    private static QrPixelDecodeOptions ResolveMultiQrOptions(QrPixelDecodeOptions? options) {
        return options ?? new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Balanced,
            BudgetMilliseconds = 800
        };
    }

    private static bool IsCancelled(CancellationToken token, CodeGlyphDecodeDiagnostics diagnostics) {
        if (!token.IsCancellationRequested) return false;
        diagnostics.Failure ??= "Cancelled.";
        diagnostics.FailureReason = DecodeFailureReason.Cancelled;
        return true;
    }

    private static void SetNoResult(CodeGlyphDecodeDiagnostics diagnostics) {
        diagnostics.Failure ??= "No symbol decoded.";
        diagnostics.FailureReason = DecodeFailureReason.NoResult;
    }

    private static void SetFailure(CodeGlyphDecodeDiagnostics diagnostics, DecodeFailureReason reason, string message) {
        diagnostics.Failure ??= message;
        diagnostics.FailureReason = reason;
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
        return TryDecodeCore(pixels, width, height, stride, format, out decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        return TryDecodeCore(pixels, width, height, stride, format, out decoded, out diagnostics, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from raw pixels.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        return TryDecodeAllCore(pixels, width, height, stride, format, out decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

#endif
}
