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
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeFallback(pixels, width, height, stride, format, options: null, cancellationToken: default, out decoded, out _);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, out decoded);
#else
        return TryDecodeFallback(pixels, width, height, stride, format, options: null, cancellationToken: default, out decoded, out _);
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeFallback(pixels, width, height, stride, format, options, cancellationToken: default, out decoded, out _);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, options, out decoded);
#else
        return TryDecodeFallback(pixels, width, height, stride, format, options, cancellationToken: default, out decoded, out _);
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from a raw pixel buffer, with cancellation.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeFallback(pixels, width, height, stride, format, options, cancellationToken, out decoded, out _);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, options, cancellationToken, out decoded);
#else
        return TryDecodeFallback(pixels, width, height, stride, format, options, cancellationToken, out decoded, out _);
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeAllFallback(pixels, width, height, stride, format, options: null, cancellationToken: default, out decoded);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, out decoded);
#else
        return TryDecodeAllFallback(pixels, width, height, stride, format, options: null, cancellationToken: default, out decoded);
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeAllFallback(pixels, width, height, stride, format, options, cancellationToken: default, out decoded);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, options, out decoded);
#else
        return TryDecodeAllFallback(pixels, width, height, stride, format, options, cancellationToken: default, out decoded);
#endif
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer, with cancellation.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
#if NET8_0_OR_GREATER
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeAllFallback(pixels, width, height, stride, format, options, cancellationToken, out decoded);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, options, cancellationToken, out decoded);
#else
        return TryDecodeAllFallback(pixels, width, height, stride, format, options, cancellationToken, out decoded);
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeImageFallback(image, options: null, cancellationToken: default, out decoded, out _);
        }
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded);
#else
        return TryDecodeImageFallback(image, options: null, cancellationToken: default, out decoded, out _);
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with diagnostics and profile options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeImageFallback(image, options, cancellationToken: default, out decoded, out info);
        }
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            info = default;
            return false;
        }
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options);
#else
        return TryDecodeImageFallback(image, options, cancellationToken: default, out decoded, out info);
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeImageFallback(image, options, cancellationToken, out decoded, out info);
        }
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            info = default;
            return false;
        }
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options, cancellationToken);
#else
        return TryDecodeImageFallback(image, options, cancellationToken, out decoded, out info);
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
        return TryDecodeImageFallback(image, imageOptions, options, cancellationToken, out decoded, out _);
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
        return TryDecodeImageFallback(image, imageOptions, options, cancellationToken, out decoded, out info);
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, QrPixelDecodeOptions? options, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeImageFallback(image, options, cancellationToken: default, out decoded, out _);
        }
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, out decoded);
#else
        return TryDecodeImageFallback(image, options, cancellationToken: default, out decoded, out _);
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
        return TryDecodeImageFallback(image, options, cancellationToken, out decoded, out _);
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
        return TryDecodeImageFallback(image.ToArray(), options: null, cancellationToken: default, out decoded, out _);
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
        return TryDecodeImageFallback(image.ToArray(), options, cancellationToken: default, out decoded, out info);
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
        return TryDecodeImageFallback(image.ToArray(), options, cancellationToken, out decoded, out info);
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
        return TryDecodeImageFallback(image.ToArray(), imageOptions, options, cancellationToken, out decoded, out _);
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
        return TryDecodeImageFallback(image.ToArray(), imageOptions, options, cancellationToken, out decoded, out info);
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
        return TryDecodeImageFallback(image.ToArray(), options, cancellationToken: default, out decoded, out _);
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
        return TryDecodeImageFallback(image.ToArray(), options, cancellationToken, out decoded, out _);
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
        return TryDecodeAllImageFallback(image, imageOptions: null, options: null, cancellationToken: default, out decoded);
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
        return TryDecodeAllImageFallback(image, imageOptions: null, options, cancellationToken: default, out decoded);
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
        return TryDecodeAllImageFallback(image, imageOptions, options, cancellationToken, out decoded);
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
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeImageFallback(data, options: null, cancellationToken: default, out decoded, out _);
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with diagnostics and profile options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            var data = RenderIO.ReadBinary(stream);
            return TryDecodeImageFallback(data, options, cancellationToken: default, out decoded, out info);
        }
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options);
#else
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeImageFallback(data, options, cancellationToken: default, out decoded, out info);
#endif
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, QrPixelDecodeOptions? options, out QrDecoded decoded) {
#if NET8_0_OR_GREATER
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            var data = RenderIO.ReadBinary(stream);
            return TryDecodeImageFallback(data, options, cancellationToken: default, out decoded, out _);
        }
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, options, out decoded);
#else
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeImageFallback(data, options, cancellationToken: default, out decoded, out _);
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
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeImageFallback(data, imageOptions, options, cancellationToken, out decoded, out _);
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
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeImageFallback(data, imageOptions, options, cancellationToken, out decoded, out info);
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
        if (image is null) throw new ArgumentNullException(nameof(image));
        return DecodeImageResult((ReadOnlySpan<byte>)image, imageOptions, options, cancellationToken);
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

            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) {
                    return new DecodeResult<QrDecoded>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
                }
                var mergedOptions = MergeDecodeOptions(imageOptions, options);
                if (TryDecodeFallback(rgba, width, height, width * 4, PixelFormat.Rgba32, mergedOptions, token, out var decodedFallback, out _)) {
                    return new DecodeResult<QrDecoded>(decodedFallback, info, stopwatch.Elapsed);
                }
                var fallbackFailure = DecodeResultHelpers.FailureForDecode(token);
                return new DecodeResult<QrDecoded>(fallbackFailure, info, stopwatch.Elapsed);
            }

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
        var stopwatch = Stopwatch.StartNew();
        if (cancellationToken.IsCancellationRequested) {
            return new DecodeResult<QrDecoded>(DecodeFailureReason.Cancelled, default, stopwatch.Elapsed);
        }

        ImageFormat format = ImageFormat.Unknown;
        _ = ImageReader.TryDetectFormat(image, out format);

        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            var infoFail = new ImageInfo(format, 0, 0);
            return new DecodeResult<QrDecoded>(DecodeFailureReason.UnsupportedFormat, infoFail, stopwatch.Elapsed);
        }

        var info = new ImageInfo(format, width, height);
        if (TryDecodeFallback(rgba, width, height, width * 4, PixelFormat.Rgba32, options, cancellationToken, out var decoded, out _)) {
            return new DecodeResult<QrDecoded>(decoded, info, stopwatch.Elapsed);
        }

        var failure = cancellationToken.IsCancellationRequested ? DecodeFailureReason.Cancelled : DecodeFailureReason.NoResult;
        return new DecodeResult<QrDecoded>(failure, info, stopwatch.Elapsed);
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
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return DecodeImageResult(data, imageOptions, options, cancellationToken);
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
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeAllImageFallback(data, imageOptions: null, options: null, cancellationToken: default, out decoded);
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
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeAllImageFallback(data, imageOptions: null, options, cancellationToken: default, out decoded);
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
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeAllImageFallback(data, imageOptions, options, cancellationToken, out decoded);
#endif
    }

#if NET8_0_OR_GREATER
    private static bool TryDecodeImageCore(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                return TryDecodeImageFallback(image, imageOptions, options, token, out decoded, out _);
            }
            if (!ImageReader.TryDecodeRgba32(image, imageOptions, out var rgba, out var width, out var height)) return false;
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
            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                return TryDecodeImageFallback(image, imageOptions, options, token, out decoded, out info);
            }
            if (!ImageReader.TryDecodeRgba32(image, imageOptions, out var rgba, out var width, out var height)) return false;
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
            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                return TryDecodeImageFallback(image.ToArray(), imageOptions, options, token, out decoded, out _);
            }
            if (!ImageReader.TryDecodeRgba32(image, imageOptions, out var rgba, out var width, out var height)) return false;
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
            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                return TryDecodeImageFallback(image.ToArray(), imageOptions, options, token, out decoded, out info);
            }
            if (!ImageReader.TryDecodeRgba32(image, imageOptions, out var rgba, out var width, out var height)) return false;
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
            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                if (!TryDecodeImageFallback(image, imageOptions, options, token, out var single, out _)) return false;
                decoded = new[] { single };
                return true;
            }
            if (!ImageReader.TryDecodeRgba32(image, imageOptions, out var rgba, out var width, out var height)) return false;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, options, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }
#endif

    private static bool TryDecodeImageFallback(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded, out QrPixelDecodeInfo info) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        var mergedOptions = MergeDecodeOptions(imageOptions, options);
        var token = ApplyBudget(cancellationToken, imageOptions, mergedOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) {
                decoded = null!;
                info = default;
                return false;
            }
            return TryDecodeImageFallback(image, mergedOptions, token, out decoded, out info);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeAllImageFallback(byte[] image, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        var mergedOptions = MergeDecodeOptions(imageOptions, options);
        var token = ApplyBudget(cancellationToken, imageOptions, mergedOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) {
                decoded = Array.Empty<QrDecoded>();
                return false;
            }
            if (!ImageReader.TryDecodeRgba32(image, imageOptions, out var rgba, out var width, out var height)) {
                decoded = Array.Empty<QrDecoded>();
                return false;
            }
            var stride = width * 4;
            return TryDecodeAllFallback(rgba, width, height, stride, PixelFormat.Rgba32, mergedOptions, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static QrPixelDecodeOptions? MergeDecodeOptions(ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options) {
        if (imageOptions is null) return options;
        if (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0) return options;

        var merged = options is null ? new QrPixelDecodeOptions() : CloneOptions(options);
        if (imageOptions.MaxDimension > 0 && (merged.MaxDimension <= 0 || merged.MaxDimension > imageOptions.MaxDimension)) {
            merged.MaxDimension = imageOptions.MaxDimension;
        }
        if (imageOptions.MaxDimension <= 0 && merged.MaxDimension < 0) {
            merged.MaxDimension = 0;
        }
        if (imageOptions.MaxMilliseconds > 0 && merged.MaxMilliseconds <= 0) {
            merged.MaxMilliseconds = imageOptions.MaxMilliseconds;
        }
        return merged;
    }

    private static QrPixelDecodeOptions CloneOptions(QrPixelDecodeOptions options) {
        return new QrPixelDecodeOptions {
            Profile = options.Profile,
            MaxDimension = options.MaxDimension,
            MaxScale = options.MaxScale,
            MaxMilliseconds = options.MaxMilliseconds,
            BudgetMilliseconds = options.BudgetMilliseconds,
            AutoCrop = options.AutoCrop,
            EnableTileScan = options.EnableTileScan,
            TileGrid = options.TileGrid,
            DisableTransforms = options.DisableTransforms,
            AggressiveSampling = options.AggressiveSampling,
            StylizedSampling = options.StylizedSampling
        };
    }

    private static CancellationToken ApplyBudget(CancellationToken cancellationToken, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out CancellationTokenSource? budgetCts, out IDisposable? budgetScope) {
        budgetCts = null;
        budgetScope = null;
        var budgetMs = GetBudgetMs(imageOptions, options);
        if (budgetMs <= 0) return cancellationToken;

        budgetScope = Internal.DecodeBudget.Begin(budgetMs);
        if (cancellationToken.CanBeCanceled) {
            budgetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            budgetCts.CancelAfter(budgetMs);
            return budgetCts.Token;
        }

        budgetCts = new CancellationTokenSource(budgetMs);
        return budgetCts.Token;
    }

    private static int GetBudgetMs(ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options) {
        var budgetMs = 0;
        Consider(imageOptions?.MaxMilliseconds ?? 0, ref budgetMs);
        Consider(options?.BudgetMilliseconds ?? 0, ref budgetMs);
        Consider(options?.MaxMilliseconds ?? 0, ref budgetMs);
        return budgetMs;
    }

    private static void Consider(int value, ref int budgetMs) {
        if (value <= 0) return;
        budgetMs = budgetMs <= 0 ? value : Math.Min(budgetMs, value);
    }

    private static bool TryDecodeImageFallback(byte[] image, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded, out QrPixelDecodeInfo info) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        decoded = null!;
        info = default;
        if (cancellationToken.IsCancellationRequested) return false;

        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            return false;
        }

        var stride = width * 4;
        return TryDecodeFallback(rgba, width, height, stride, PixelFormat.Rgba32, options, cancellationToken, out decoded, out info);
    }

    private static bool TryDecodeAllFallback(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (options?.EnableTileScan == true) {
            if (TryDecodeAllTilesFallback(pixels, width, height, stride, format, options, cancellationToken, out decoded)) {
                return decoded.Length > 0;
            }
        }
        if (!TryDecodeFallback(pixels, width, height, stride, format, options, cancellationToken, out var single, out _)) {
            return false;
        }
        decoded = new[] { single };
        return true;
    }

    private static bool TryDecodeAllTilesFallback(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (width <= 0 || height <= 0 || stride < width * 4) return false;
        if (cancellationToken.IsCancellationRequested) return false;

        var rgba = format == PixelFormat.Rgba32 ? pixels : ConvertBgraToRgba(pixels, width, height, stride);
        ApplyMaxDimension(ref rgba, ref width, ref height, ref stride, options);
        if (cancellationToken.IsCancellationRequested) return false;

        var tileGrid = options.TileGrid;
        if (tileGrid < 2 || tileGrid > 4) {
            var maxSide = width > height ? width : height;
            tileGrid = maxSide <= 800 ? 2 : 3;
        }

        var tileOptions = CloneOptions(options);
        tileOptions.MaxDimension = 0;
        tileOptions.EnableTileScan = false;
        tileOptions.TileGrid = 0;

        var overlap = Math.Max(16, Math.Min(width, height) / 40);
        var tileWidth = Math.Max(1, width / tileGrid);
        var tileHeight = Math.Max(1, height / tileGrid);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<QrDecoded>(tileGrid * tileGrid);

        for (var ty = 0; ty < tileGrid; ty++) {
            var baseY0 = ty * tileHeight;
            var baseY1 = ty == tileGrid - 1 ? height : (ty + 1) * tileHeight;
            var y0 = baseY0 > overlap ? baseY0 - overlap : 0;
            var y1 = baseY1 + overlap;
            if (y1 > height) y1 = height;
            var th = y1 - y0;
            if (th <= 0) continue;

            for (var tx = 0; tx < tileGrid; tx++) {
                if (cancellationToken.IsCancellationRequested) {
                    decoded = Array.Empty<QrDecoded>();
                    return false;
                }

                var baseX0 = tx * tileWidth;
                var baseX1 = tx == tileGrid - 1 ? width : (tx + 1) * tileWidth;
                var x0 = baseX0 > overlap ? baseX0 - overlap : 0;
                var x1 = baseX1 + overlap;
                if (x1 > width) x1 = width;
                var tw = x1 - x0;
                if (tw <= 0) continue;

                var tile = CropRgba(rgba, width, height, stride, x0, y0, tw, th);
                if (!TryDecodeFallback(tile, tw, th, tw * 4, PixelFormat.Rgba32, tileOptions, cancellationToken, out var result, out _)) {
                    continue;
                }

                var text = result.Text;
                if (string.IsNullOrEmpty(text) || !seen.Add(text)) continue;
                list.Add(result);
            }
        }

        decoded = list.ToArray();
        return decoded.Length > 0;
    }

    private static byte[] CropRgba(byte[] rgba, int width, int height, int stride, int x, int y, int cropWidth, int cropHeight) {
        if (cropWidth <= 0 || cropHeight <= 0) return Array.Empty<byte>();
        var x0 = ClampInt(x, 0, width - 1);
        var y0 = ClampInt(y, 0, height - 1);
        var x1 = ClampInt(x0 + cropWidth, x0 + 1, width);
        var y1 = ClampInt(y0 + cropHeight, y0 + 1, height);
        var w = x1 - x0;
        var h = y1 - y0;
        var destStride = w * 4;
        var dest = new byte[h * destStride];

        for (var row = 0; row < h; row++) {
            var srcIndex = (y0 + row) * stride + (x0 * 4);
            var dstIndex = row * destStride;
            Buffer.BlockCopy(rgba, srcIndex, dest, dstIndex, destStride);
        }

        return dest;
    }

    private static bool TryDecodeFallback(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded, out QrPixelDecodeInfo info) {
        decoded = null!;
        info = default;
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (width <= 0 || height <= 0 || stride < width * 4) return false;
        if (cancellationToken.IsCancellationRequested) return false;

        var rgba = format == PixelFormat.Rgba32 ? pixels : ConvertBgraToRgba(pixels, width, height, stride);
        ApplyMaxDimension(ref rgba, ref width, ref height, ref stride, options);
        if (cancellationToken.IsCancellationRequested) return false;

        var grayscale = BuildGrayscale(rgba, width, height, stride);
        var mean = ComputeMean(grayscale);
        var thresholds = BuildThresholds(mean);
        for (var t = 0; t < thresholds.Length; t++) {
            if (TryDecodeWithThreshold(grayscale, width, height, thresholds[t], options, cancellationToken, out decoded, out info)) {
                return true;
            }
        }

        return false;
    }

    private static void ApplyMaxDimension(ref byte[] rgba, ref int width, ref int height, ref int stride, QrPixelDecodeOptions? options) {
        var maxDim = options?.MaxDimension ?? 0;
        if (maxDim <= 0) return;

        var currentMax = width > height ? width : height;
        if (currentMax <= maxDim) return;

        var scale = maxDim / (double)currentMax;
        var dstWidth = Math.Max(1, (int)Math.Round(width * scale));
        var dstHeight = Math.Max(1, (int)Math.Round(height * scale));
        var background = new CodeGlyphX.Rendering.Png.Rgba32(255, 255, 255, 255);
        rgba = ImageScaler.ResizeToFitNearest(rgba, width, height, stride, dstWidth, dstHeight, background, preserveAspectRatio: true);
        width = dstWidth;
        height = dstHeight;
        stride = width * 4;
    }

    private static byte[] ConvertBgraToRgba(byte[] pixels, int width, int height, int stride) {
        var rgba = new byte[checked(width * height * 4)];
        var dst = 0;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var i = row + (x * 4);
                rgba[dst + 0] = pixels[i + 2];
                rgba[dst + 1] = pixels[i + 1];
                rgba[dst + 2] = pixels[i + 0];
                rgba[dst + 3] = pixels[i + 3];
                dst += 4;
            }
        }
        return rgba;
    }

    private static byte[] BuildGrayscale(byte[] rgba, int width, int height, int stride) {
        var gray = new byte[checked(width * height)];
        var dst = 0;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var i = row + (x * 4);
                var r = rgba[i + 0];
                var g = rgba[i + 1];
                var b = rgba[i + 2];
                var lum = (299 * r) + (587 * g) + (114 * b);
                gray[dst++] = (byte)(lum / 1000);
            }
        }
        return gray;
    }

    private static byte[] InvertGrayscale(byte[] gray) {
        var inverted = new byte[gray.Length];
        for (var i = 0; i < gray.Length; i++) {
            inverted[i] = (byte)(255 - gray[i]);
        }
        return inverted;
    }

    private static int ComputeMean(byte[] gray) {
        long sum = 0;
        for (var i = 0; i < gray.Length; i++) sum += gray[i];
        return (int)(sum / Math.Max(1, gray.Length));
    }

    private static byte[] BuildThresholds(int mean) {
        var thresholds = new List<byte>(3);
        AddThreshold(thresholds, mean - 12);
        AddThreshold(thresholds, mean - 4);
        AddThreshold(thresholds, mean + 4);
        if (thresholds.Count == 0) thresholds.Add((byte)ClampByte(mean));
        return thresholds.ToArray();
    }

    private static void AddThreshold(List<byte> thresholds, int value) {
        var clamped = (byte)ClampByte(value);
        for (var i = 0; i < thresholds.Count; i++) {
            if (thresholds[i] == clamped) return;
        }
        thresholds.Add(clamped);
    }

    private static bool TryFindDarkBounds(byte[] gray, int width, int height, byte threshold, out int minX, out int minY, out int maxX, out int maxY) {
        minX = width;
        minY = height;
        maxX = -1;
        maxY = -1;

        var idx = 0;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++, idx++) {
                if (gray[idx] >= threshold) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        return maxX >= minX && maxY >= minY;
    }

    private static bool TryDecodeWithThreshold(byte[] gray, int width, int height, byte threshold, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded, out QrPixelDecodeInfo info) {
        decoded = null!;
        info = default;

        if (TryDecodeWithThresholdCore(gray, width, height, threshold, options, cancellationToken, out decoded, out info)) {
            return true;
        }

        var inverted = InvertGrayscale(gray);
        var invertedThreshold = (byte)ClampByte(255 - threshold);
        return TryDecodeWithThresholdCore(inverted, width, height, invertedThreshold, options, cancellationToken, out decoded, out info);
    }

    private static bool TryDecodeWithThresholdCore(byte[] gray, int width, int height, byte threshold, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded, out QrPixelDecodeInfo info) {
        decoded = null!;
        info = default;

        if (!TryFindDarkBounds(gray, width, height, threshold, out var minX, out var minY, out var maxX, out var maxY)) {
            return false;
        }

        var dimensionEstimate = EstimateDimension(gray, width, minX, minY, maxX, maxY, threshold);
        var versionEstimate = dimensionEstimate >= 21 ? ClampInt((dimensionEstimate - 17) / 4, 1, 40) : 1;
        var moduleSize = EstimateModuleSize(gray, width, height, minX, minY, maxX, maxY, threshold);
        if (moduleSize <= 0) moduleSize = 1;

        var padHalf = moduleSize / 2;
        var pads = new[] { 0, padHalf, moduleSize, moduleSize * 2 };
        var versionList = new List<int>(13) { versionEstimate };
        for (var offset = 1; offset <= 6; offset++) {
            var lower = versionEstimate - offset;
            var upper = versionEstimate + offset;
            if (lower >= 1) versionList.Add(lower);
            if (upper <= 40) versionList.Add(upper);
        }
        var versions = versionList.ToArray();

        foreach (var pad in pads) {
            var adjMinX = ClampInt(minX - pad, 0, width - 1);
            var adjMinY = ClampInt(minY - pad, 0, height - 1);
            var adjMaxX = ClampInt(maxX + pad, 0, width - 1);
            var adjMaxY = ClampInt(maxY + pad, 0, height - 1);

            foreach (var version in versions) {
                var dimension = (version * 4) + 17;
                var modules = SampleModules(gray, width, height, adjMinX, adjMinY, adjMaxX, adjMaxY, threshold, dimension);
                if (cancellationToken.IsCancellationRequested) return false;

                if (TryDecodeModules(modules, options, cancellationToken, out decoded, out var moduleInfo)) {
                    info = new QrPixelDecodeInfo(1, threshold, invert: false, candidateCount: 0, candidateTriplesTried: 0, dimension, moduleInfo);
                    return true;
                }

                var inverted = modules.Clone();
                inverted.Invert();
                if (TryDecodeModules(inverted, options, cancellationToken, out decoded, out moduleInfo)) {
                    info = new QrPixelDecodeInfo(1, threshold, invert: true, candidateCount: 0, candidateTriplesTried: 0, dimension, moduleInfo);
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryDecodeModules(BitMatrix modules, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded, out QrDecodeInfo moduleInfo) {
        if (cancellationToken.IsCancellationRequested) {
            decoded = null!;
            moduleInfo = default;
            return false;
        }

        if (QrDecoder.TryDecode(modules, out decoded, out moduleInfo)) {
            return true;
        }

        if (options?.DisableTransforms == true) {
            return false;
        }

        var rot90 = Rotate90(modules);
        if (QrDecoder.TryDecode(rot90, out decoded, out moduleInfo)) return true;
        var rot180 = Rotate180(modules);
        if (QrDecoder.TryDecode(rot180, out decoded, out moduleInfo)) return true;
        var rot270 = Rotate270(modules);
        if (QrDecoder.TryDecode(rot270, out decoded, out moduleInfo)) return true;

        var mirror = MirrorX(modules);
        if (QrDecoder.TryDecode(mirror, out decoded, out moduleInfo)) return true;
        var mirror90 = Rotate90(mirror);
        if (QrDecoder.TryDecode(mirror90, out decoded, out moduleInfo)) return true;
        var mirror180 = Rotate180(mirror);
        if (QrDecoder.TryDecode(mirror180, out decoded, out moduleInfo)) return true;
        var mirror270 = Rotate270(mirror);
        if (QrDecoder.TryDecode(mirror270, out decoded, out moduleInfo)) return true;

        return false;
    }

    private static BitMatrix Rotate90(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Height - 1 - y, x] = matrix[x, y];
            }
        }
        return result;
    }

    private static BitMatrix Rotate180(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Width, matrix.Height);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Width - 1 - x, matrix.Height - 1 - y] = matrix[x, y];
            }
        }
        return result;
    }

    private static BitMatrix Rotate270(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[y, matrix.Width - 1 - x] = matrix[x, y];
            }
        }
        return result;
    }

    private static BitMatrix MirrorX(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Width, matrix.Height);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Width - 1 - x, y] = matrix[x, y];
            }
        }
        return result;
    }

    private static int EstimateDimension(byte[] gray, int width, int minX, int minY, int maxX, int maxY, byte threshold) {
        var boxWidth = (maxX - minX) + 1;
        var boxHeight = (maxY - minY) + 1;
        if (boxWidth < 21 || boxHeight < 21) return 0;

        var midY = minY + (boxHeight / 2);
        var rowStart = (midY * width) + minX;
        var rowEnd = rowStart + boxWidth;
        var runs = new List<int>(64);
        var lastDark = gray[rowStart] < threshold;
        var run = 0;
        for (var i = rowStart; i < rowEnd; i++) {
            var dark = gray[i] < threshold;
            if (dark == lastDark) {
                run++;
                continue;
            }
            if (run > 0) runs.Add(run);
            run = 1;
            lastDark = dark;
        }
        if (run > 0) runs.Add(run);
        if (runs.Count == 0) return 0;

        runs.Sort();
        var take = runs.Count < 12 ? runs.Count : 12;
        var smallSum = 0;
        for (var i = 0; i < take; i++) smallSum += runs[i];
        var moduleSize = Math.Max(1, smallSum / Math.Max(1, take));

        var rawDim = (int)Math.Round(boxWidth / (double)moduleSize);
        var version = ClampInt((int)Math.Round((rawDim - 17) / 4.0), 1, 40);
        return (version * 4) + 17;
    }

    private static int EstimateModuleSize(byte[] gray, int width, int height, int minX, int minY, int maxX, int maxY, byte threshold) {
        var boxWidth = (maxX - minX) + 1;
        var boxHeight = (maxY - minY) + 1;
        if (boxWidth <= 0 || boxHeight <= 0) return 1;

        var midY = minY + (boxHeight / 2);
        if (midY < 0) midY = 0;
        if (midY >= height) midY = height - 1;

        var rowStart = midY * width;
        var runs = new List<int>(128);
        var lastDark = gray[rowStart] < threshold;
        var run = 0;
        for (var x = 0; x < width; x++) {
            var dark = gray[rowStart + x] < threshold;
            if (dark == lastDark) {
                run++;
                continue;
            }
            if (run > 0) runs.Add(run);
            run = 1;
            lastDark = dark;
        }
        if (run > 0) runs.Add(run);
        if (runs.Count == 0) return 1;

        runs.Sort();
        var baseline = runs[0];
        var limit = baseline * 8;
        var filtered = new List<int>(32);
        for (var i = 0; i < runs.Count && filtered.Count < 32; i++) {
            var value = runs[i];
            if (value > limit) break;
            filtered.Add(value);
        }
        if (filtered.Count == 0) return Math.Max(1, baseline);

        filtered.Sort();
        var mid = filtered.Count / 2;
        var median = filtered[mid];
        return Math.Max(1, median);
    }

    private static BitMatrix SampleModules(byte[] gray, int width, int height, int minX, int minY, int maxX, int maxY, byte threshold, int dimension) {
        var matrix = new BitMatrix(dimension, dimension);
        var boxWidth = (maxX - minX) + 1;
        var boxHeight = (maxY - minY) + 1;
        var stepX = boxWidth / (double)dimension;
        var stepY = boxHeight / (double)dimension;

        for (var y = 0; y < dimension; y++) {
            var cy = minY + ((y + 0.5) * stepY);
            var py = ClampInt((int)Math.Round(cy), 0, height - 1);
            for (var x = 0; x < dimension; x++) {
                var cx = minX + ((x + 0.5) * stepX);
                var px = ClampInt((int)Math.Round(cx), 0, width - 1);
                var idx = (py * width) + px;
                matrix[x, y] = gray[idx] < threshold;
            }
        }

        return matrix;
    }

    private static int ClampInt(int value, int min, int max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static int ClampByte(int value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return value;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a QR code from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out QrDecoded decoded) {
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeFallback(pixels.ToArray(), width, height, stride, format, options: null, cancellationToken: default, out decoded, out _);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeFallback(pixels.ToArray(), width, height, stride, format, options, cancellationToken: default, out decoded, out _);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, stride, format, options, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out QrDecoded[] decoded) {
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeAllFallback(pixels.ToArray(), width, height, stride, format, options: null, cancellationToken: default, out decoded);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
        if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
            return TryDecodeAllFallback(pixels.ToArray(), width, height, stride, format, options, cancellationToken: default, out decoded);
        }
        return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(pixels, width, height, stride, format, options, out decoded);
    }
#endif
}
