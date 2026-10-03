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
