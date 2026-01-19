using System;
using System.Text;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX;

/// <summary>
/// Decodes QR codes from a module grid or from raw pixels (clean images).
/// </summary>
/// <remarks>
/// Current scope: byte + kanji payloads on clean/generated images and clean on-screen QR codes.
/// </remarks>
public static partial class QrDecoder {
    /// <summary>
    /// Attempts to decode a QR code from an exact module grid (no quiet zone).
    /// </summary>
    /// <param name="modules">Square matrix of QR modules (dark = <c>true</c>).</param>
    /// <param name="result">Decoded payload.</param>
    /// <returns><c>true</c> when decoding succeeded; otherwise <c>false</c>.</returns>
    public static bool TryDecode(BitMatrix modules, out QrDecoded result) {
        return TryDecodeInternal(modules, out result, out QrDecodeDiagnostics _);
    }

    /// <summary>
    /// Attempts to decode a QR code from an exact module grid (no quiet zone), with diagnostics.
    /// </summary>
    /// <param name="modules">Square matrix of QR modules (dark = <c>true</c>).</param>
    /// <param name="result">Decoded payload.</param>
    /// <param name="info">Decode diagnostics.</param>
    /// <returns><c>true</c> when decoding succeeded; otherwise <c>false</c>.</returns>
    public static bool TryDecode(BitMatrix modules, out QrDecoded result, out QrDecodeInfo info) {
        var ok = TryDecodeInternal(modules, out result, out QrDecodeDiagnostics diagnostics);
        info = QrDecodeInfo.FromInternal(diagnostics);
        return ok;
    }

    /// <summary>
    /// Attempts to decode a QR code from an exact module grid, with cancellation.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out QrDecoded result, CancellationToken cancellationToken) {
        Func<bool>? shouldStop = cancellationToken.CanBeCanceled ? () => cancellationToken.IsCancellationRequested : null;
        return TryDecodeInternal(modules, shouldStop, out result, out _);
    }

    /// <summary>
    /// Attempts to decode a QR code from an exact module grid, with diagnostics and cancellation.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out QrDecoded result, out QrDecodeInfo info, CancellationToken cancellationToken) {
        Func<bool>? shouldStop = cancellationToken.CanBeCanceled ? () => cancellationToken.IsCancellationRequested : null;
        var ok = TryDecodeInternal(modules, shouldStop, out result, out QrDecodeDiagnostics diagnostics);
        info = QrDecodeInfo.FromInternal(diagnostics);
        return ok;
    }

    internal static bool TryDecodeInternal(BitMatrix modules, out QrDecoded result, out QrDecodeDiagnostics diagnostics) {
        return TryDecodeInternal(modules, shouldStop: null, out result, out diagnostics);
    }

    internal static bool TryDecodeInternal(BitMatrix modules, Func<bool>? shouldStop, out QrDecoded result, out QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (shouldStop?.Invoke() == true) {
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled);
            return false;
        }

        if (modules is null || modules.Width != modules.Height) {
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.InvalidInput);
            return false;
        }

        var size = modules.Width;
        var version = (size - 17) / 4;
        if (version is < 1 or > 40 || size != version * 4 + 17) {
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.InvalidSize, version);
            return false;
        }

        if (shouldStop?.Invoke() == true) {
            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled, version);
            return false;
        }

        if (!TryDecodeFormat(modules, out var formatCandidates, out var formatBestDistance, out var formatBitsA, out var formatBitsB)) {
            // If format info is too noisy, attempt a bounded brute-force over all masks.
            if (formatBestDistance <= 7) {
                var allCandidates = BuildAllFormatCandidates(formatBitsA, formatBitsB);
                if (TryDecodeWithCandidates(modules, version, allCandidates, formatBestDistance, requireBothWithin: false, shouldStop, out result, out diagnostics)) {
                    return true;
                }
            }

            diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.FormatInfo, version, formatBestDistance: formatBestDistance);
            return false;
        }

        if (formatCandidates.Length > 0 && HasBothWithin(formatCandidates)) {
            if (TryDecodeWithCandidates(modules, version, formatCandidates, formatBestDistance, requireBothWithin: true, shouldStop, out result, out diagnostics)) {
                return true;
            }
        }

        if (TryDecodeWithCandidates(modules, version, formatCandidates, formatBestDistance, requireBothWithin: false, shouldStop, out result, out diagnostics)) {
            return true;
        }

        // If format bits are noisy, retry all masks (RS + payload validation will reject wrong masks).
        if (formatBestDistance > 3) {
            var allCandidates = BuildAllFormatCandidates(formatBitsA, formatBitsB);
            if (TryDecodeWithCandidates(modules, version, allCandidates, formatBestDistance, requireBothWithin: false, shouldStop, out result, out diagnostics)) {
                return true;
            }
        }

        return false;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs).
    /// </summary>
    /// <param name="pixels">Pixel buffer.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="stride">Bytes per row.</param>
    /// <param name="fmt">Pixel format (4 bytes per pixel).</param>
    /// <param name="result">Decoded payload.</param>
    /// <returns><c>true</c> when decoding succeeded; otherwise <c>false</c>.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result) {
        return QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, out result);
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs) using the specified profile.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, QrPixelDecodeOptions? options) {
        return QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, options, out result);
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs) using the specified profile, with cancellation.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        return QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, options, cancellationToken, out result);
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs), with diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeInfo info) {
        var ok = QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, out result, out var diagnostics);
        info = QrPixelDecodeInfo.FromInternal(diagnostics);
        return ok;
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs), with diagnostics and profile options.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options) {
        var ok = QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, options, out result, out var diagnostics);
        info = QrPixelDecodeInfo.FromInternal(diagnostics);
        return ok;
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs), with diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        var ok = QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, options, cancellationToken, out result, out var diagnostics);
        info = QrPixelDecodeInfo.FromInternal(diagnostics);
        return ok;
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs).
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results) {
        return QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, fmt, out results);
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs) using the specified profile.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, QrPixelDecodeOptions? options) {
        return QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, fmt, options, out results);
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs) using the specified profile, with cancellation.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        return QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, fmt, options, cancellationToken, out results);
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs), with diagnostics.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, out QrPixelDecodeInfo info) {
        var ok = QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, fmt, out results, out var diagnostics);
        info = QrPixelDecodeInfo.FromInternal(diagnostics);
        return ok;
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs), with diagnostics and profile options.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options) {
        var ok = QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, fmt, options, out results, out var diagnostics);
        info = QrPixelDecodeInfo.FromInternal(diagnostics);
        return ok;
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs), with diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        var ok = QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, fmt, options, cancellationToken, out results, out var diagnostics);
        info = QrPixelDecodeInfo.FromInternal(diagnostics);
        return ok;
    }

    internal static bool TryDecodePixelsInternal(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        return QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, out result, out diagnostics);
    }
#endif

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs).
    /// </summary>
    /// <param name="pixels">Pixel buffer.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="stride">Bytes per row.</param>
    /// <param name="fmt">Pixel format (4 bytes per pixel).</param>
    /// <param name="result">Decoded payload.</param>
    /// <returns><c>true</c> when decoding succeeded; otherwise <c>false</c>.</returns>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result) {
        if (pixels is null) {
            result = null!;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out result);
#else
        result = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs) using the specified profile.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, QrPixelDecodeOptions? options) {
        if (pixels is null) {
            result = null!;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out result, options);
#else
        result = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs) using the specified profile, with cancellation.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        if (pixels is null) {
            result = null!;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out result, options, cancellationToken);
#else
        result = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs), with diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeInfo info) {
        if (pixels is null) {
            result = null!;
            info = default;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out result, out info);
#else
        result = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs), with diagnostics and profile options.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options) {
        if (pixels is null) {
            result = null!;
            info = default;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out result, out info, options);
#else
        result = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from raw pixel data (best-effort, clean inputs), with diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        if (pixels is null) {
            result = null!;
            info = default;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out result, out info, options, cancellationToken);
#else
        result = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs).
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results) {
        if (pixels is null) {
            results = Array.Empty<QrDecoded>();
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out results);
#else
        results = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs) using the specified profile.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, QrPixelDecodeOptions? options) {
        if (pixels is null) {
            results = Array.Empty<QrDecoded>();
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out results, options);
#else
        results = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs) using the specified profile, with cancellation.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        if (pixels is null) {
            results = Array.Empty<QrDecoded>();
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out results, options, cancellationToken);
#else
        results = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs), with diagnostics.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, out QrPixelDecodeInfo info) {
        if (pixels is null) {
            results = Array.Empty<QrDecoded>();
            info = default;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out results, out info);
#else
        results = Array.Empty<QrDecoded>();
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs), with diagnostics and profile options.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options) {
        if (pixels is null) {
            results = Array.Empty<QrDecoded>();
            info = default;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out results, out info, options);
#else
        results = Array.Empty<QrDecoded>();
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from raw pixel data (best-effort, clean inputs), with diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        if (pixels is null) {
            results = Array.Empty<QrDecoded>();
            info = default;
            return false;
        }

#if NET8_0_OR_GREATER
        return TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out results, out info, options, cancellationToken);
#else
        results = Array.Empty<QrDecoded>();
        info = default;
        return false;
#endif
    }

#if NET8_0_OR_GREATER
    internal static bool TryDecodePixelsInternal(byte[] pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        if (pixels is null) {
            result = null!;
            diagnostics = default;
            return false;
        }

        return TryDecodePixelsInternal((ReadOnlySpan<byte>)pixels, width, height, stride, fmt, out result, out diagnostics);
    }
#endif
}
