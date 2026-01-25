using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Decodes QR codes from raw pixel buffers.
/// </summary>

/// <example>
/// <code>
/// using CodeGlyphX;
/// var bytes = File.ReadAllBytes("qr.png");
/// if (QrImageDecoder.TryDecodeImage(bytes, out var decoded)) {
///     Console.WriteLine(decoded.Text);
/// }
/// </code>
/// </example>
public static class QrImageDecoder {
    /// <summary>
    /// Attempts to decode a QR code from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, options, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from a raw pixel buffer, with cancellation.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, options, cancellationToken, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, out decoded);
#else
        decoded = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, options, out decoded);
#else
        decoded = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer, with cancellation.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, options, cancellationToken, out decoded);
#else
        decoded = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with diagnostics and profile options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            info = default;
            return false;
        }
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options);
#else
        decoded = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            info = default;
            return false;
        }
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options, cancellationToken);
#else
        decoded = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return TryDecodeImage(image, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options and cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        return TryDecodeImageCore(image, imageOptions, options, cancellationToken, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, diagnostics, and profile options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodeImage(image, imageOptions, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
#if NET8_0_OR_GREATER
        return TryDecodeImageCore(image, imageOptions, options, cancellationToken, out decoded, out info);
#else
        decoded = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, QrPixelDecodeOptions? options, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, cancellationToken, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) from a span.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) from a span, with diagnostics and profile options.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
#if NET8_0_OR_GREATER
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            info = default;
            return false;
        }
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options);
#else
        decoded = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) from a span, with diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
#if NET8_0_OR_GREATER
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            info = default;
            return false;
        }
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options, cancellationToken);
#else
        decoded = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options from a span.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return TryDecodeImage(image, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options and cancellation from a span.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        return TryDecodeImageCore(image, imageOptions, options, cancellationToken, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, diagnostics, and profile options from a span.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodeImage(image, imageOptions, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, diagnostics, profile options, and cancellation from a span.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
#if NET8_0_OR_GREATER
        return TryDecodeImageCore(image, imageOptions, options, cancellationToken, out decoded, out info);
#else
        decoded = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) from a span with profile options.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, QrPixelDecodeOptions? options, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) from a span, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, cancellationToken, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = Array.Empty<QrDecoded>();
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded);
#else
        decoded = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = Array.Empty<QrDecoded>();
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, options, out decoded);
#else
        decoded = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
        return TryDecodeAllImage(image, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options and cancellation.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        return TryDecodeAllImageCore(image, imageOptions, options, cancellationToken, out decoded);
#else
        decoded = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with diagnostics and profile options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options);
#else
        decoded = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, QrPixelDecodeOptions? options, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return TryDecodeImage(stream, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options and cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeImageCore(data, imageOptions, options, cancellationToken, out decoded);
#else
        decoded = null!;
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, diagnostics, and profile options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodeImage(stream, imageOptions, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeImageCore(data, imageOptions, options, cancellationToken, out decoded, out info);
#else
        decoded = null!;
        info = default;
        return false;
#endif
    }

    /// <summary>
    /// Decodes a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) and returns diagnostics.
    /// </summary>
    public static DecodeResult<QrDecoded> DecodeImageResult(byte[] image, ImageDecodeOptions? imageOptions = null, QrPixelDecodeOptions? options = null, CancellationToken cancellationToken = default) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        return DecodeImageResult((ReadOnlySpan<byte>)image, imageOptions, options, cancellationToken);
#else
        _ = imageOptions;
        _ = options;
        _ = cancellationToken;
        if (image is null) throw new ArgumentNullException(nameof(image));
        return new DecodeResult<QrDecoded>(DecodeFailureReason.PlatformNotSupported, default, TimeSpan.Zero, "QR decoding requires .NET 8 or later.");
#endif
    }

    /// <summary>
    /// Decodes a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) in a span and returns diagnostics.
    /// </summary>
    public static DecodeResult<QrDecoded> DecodeImageResult(ReadOnlySpan<byte> image, ImageDecodeOptions? imageOptions = null, QrPixelDecodeOptions? options = null, CancellationToken cancellationToken = default) {
#if NET8_0_OR_GREATER
        var stopwatch = Stopwatch.StartNew();
        _ = DecodeResultHelpers.TryGetImageInfo(image, out var info, out var formatKnown);
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) {
                return new DecodeResult<QrDecoded>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
            }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
                var imageFailure = DecodeResultHelpers.FailureForImageRead(image, formatKnown, token);
                return new DecodeResult<QrDecoded>(imageFailure, info, stopwatch.Elapsed);
            }

            info = DecodeResultHelpers.EnsureDimensions(info, formatKnown, width, height);

            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) {
                return new DecodeResult<QrDecoded>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
            }
            if (global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, token, out var decoded)) {
                return new DecodeResult<QrDecoded>(decoded, info, stopwatch.Elapsed);
            }
            var failure = DecodeResultHelpers.FailureForDecode(token);
            return new DecodeResult<QrDecoded>(failure, info, stopwatch.Elapsed);
        } catch (Exception ex) {
            return new DecodeResult<QrDecoded>(DecodeFailureReason.Error, info, stopwatch.Elapsed, ex.Message);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
#else
        _ = imageOptions;
        _ = options;
        _ = cancellationToken;
        return new DecodeResult<QrDecoded>(DecodeFailureReason.PlatformNotSupported, default, TimeSpan.Zero, "QR decoding requires .NET 8 or later.");
#endif
    }

    /// <summary>
    /// Decodes a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) and returns diagnostics.
    /// </summary>
    public static DecodeResult<QrDecoded> DecodeImageResult(Stream stream, ImageDecodeOptions? imageOptions = null, QrPixelDecodeOptions? options = null, CancellationToken cancellationToken = default) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeImageResult(buffer.AsSpan(), imageOptions, options, cancellationToken);
        }
        var data = RenderIO.ReadBinary(stream);
        return DecodeImageResult(data, imageOptions, options, cancellationToken);
#else
        _ = imageOptions;
        _ = options;
        _ = cancellationToken;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        return new DecodeResult<QrDecoded>(DecodeFailureReason.PlatformNotSupported, default, TimeSpan.Zero, "QR decoding requires .NET 8 or later.");
#endif
    }

    /// <summary>
    /// Decodes a batch of QR images with shared settings and aggregated diagnostics.
    /// </summary>
    public static DecodeBatchResult<QrDecoded> DecodeImageBatch(IEnumerable<byte[]> images, ImageDecodeOptions? imageOptions = null, QrPixelDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        return DecodeBatchHelpers.Run(images, image => DecodeImageResult(image, imageOptions, options, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Attempts to decode all QR codes from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded);
#else
        decoded = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, options, out decoded);
#else
        decoded = Array.Empty<QrDecoded>();
        return false;
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
        return TryDecodeAllImage(stream, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options and cancellation.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeAllImageCore(data, imageOptions, options, cancellationToken, out decoded);
#else
        decoded = Array.Empty<QrDecoded>();
        return false;
#endif
    }

#if NET8_0_OR_GREATER
    private static bool TryDecodeImageCore(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeImageCore(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded, out QrPixelDecodeInfo info) {
        decoded = null!;
        info = default;
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options, token);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeImageCore(ReadOnlySpan<byte> image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeImageCore(ReadOnlySpan<byte> image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded, out QrPixelDecodeInfo info) {
        decoded = null!;
        info = default;
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options, token);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeAllImageCore(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, options, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }
#endif

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a QR code from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out QrDecoded decoded) {
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, options, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out QrDecoded[] decoded) {
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, options, out decoded);
    }
#endif
}
