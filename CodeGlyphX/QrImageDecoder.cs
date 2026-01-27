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
        return TryDecodeImageFallback(image, options, cancellationToken, out decoded, out info);
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
        return TryDecodeImageFallback(image.ToArray(), options, cancellationToken, out decoded, out _);
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
        return TryDecodeImageFallback(image.ToArray(), options, cancellationToken, out decoded, out info);
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
        if (!TryDecodeImageFallback(image, options, cancellationToken: default, out var single, out _)) {
            decoded = Array.Empty<QrDecoded>();
            return false;
        }
        decoded = new[] { single };
        return true;
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
        if (!TryDecodeImageFallback(image, options, cancellationToken, out var single, out _)) {
            decoded = Array.Empty<QrDecoded>();
            return false;
        }
        decoded = new[] { single };
        return true;
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
        return TryDecodeImageFallback(data, options, cancellationToken, out decoded, out _);
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
        return TryDecodeImageFallback(data, options, cancellationToken, out decoded, out info);
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
                if (TryDecodeFallback(rgba, width, height, width * 4, PixelFormat.Rgba32, options, token, out var decodedFallback, out _)) {
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
        if (!TryDecodeImageFallback(data, options: null, cancellationToken: default, out var single, out _)) {
            decoded = Array.Empty<QrDecoded>();
            return false;
        }
        decoded = new[] { single };
        return true;
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
        if (!TryDecodeImageFallback(data, options, cancellationToken: default, out var single, out _)) {
            decoded = Array.Empty<QrDecoded>();
            return false;
        }
        decoded = new[] { single };
        return true;
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
        if (!TryDecodeImageFallback(data, options, cancellationToken, out var single, out _)) {
            decoded = Array.Empty<QrDecoded>();
            return false;
        }
        decoded = new[] { single };
        return true;
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
                return TryDecodeImageFallback(image, options, token, out decoded, out _);
            }
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
            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                return TryDecodeImageFallback(image, options, token, out decoded, out info);
            }
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
            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                return TryDecodeImageFallback(image.ToArray(), options, token, out decoded, out _);
            }
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
            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                return TryDecodeImageFallback(image.ToArray(), options, token, out decoded, out info);
            }
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
            if (CodeGlyphXFeatures.ForceQrFallbackForTests) {
                if (!TryDecodeImageFallback(image, options, token, out var single, out _)) return false;
                decoded = new[] { single };
                return true;
            }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return global::CodeGlyphX.Qr.QrPixelDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, options, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }
#endif

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
        if (!TryDecodeFallback(pixels, width, height, stride, format, options, cancellationToken, out var single, out _)) {
            return false;
        }
        decoded = new[] { single };
        return true;
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
        var threshold = ComputeMeanThreshold(grayscale);
        if (!TryFindDarkBounds(grayscale, width, height, threshold, out var minX, out var minY, out var maxX, out var maxY)) {
            return false;
        }

        var dimensionEstimate = EstimateDimension(grayscale, width, minX, minY, maxX, maxY, threshold);
        var versionEstimate = dimensionEstimate >= 21 ? ClampInt((dimensionEstimate - 17) / 4, 1, 40) : 1;
        var moduleSize = EstimateModuleSize(grayscale, width, height, minX, minY, maxX, maxY, threshold);
        if (moduleSize <= 0) moduleSize = 1;

        var padHalf = moduleSize / 2;
        var pads = new[] { 0, padHalf, moduleSize, moduleSize * 2 };
        var versions = new[] {
            ClampInt(versionEstimate - 3, 1, 40),
            ClampInt(versionEstimate - 2, 1, 40),
            ClampInt(versionEstimate - 1, 1, 40),
            versionEstimate,
            ClampInt(versionEstimate + 1, 1, 40),
            ClampInt(versionEstimate + 2, 1, 40),
            ClampInt(versionEstimate + 3, 1, 40)
        };

        foreach (var pad in pads) {
            var adjMinX = ClampInt(minX - pad, 0, width - 1);
            var adjMinY = ClampInt(minY - pad, 0, height - 1);
            var adjMaxX = ClampInt(maxX + pad, 0, width - 1);
            var adjMaxY = ClampInt(maxY + pad, 0, height - 1);

            foreach (var version in versions) {
                var dimension = (version * 4) + 17;
                var modules = SampleModules(grayscale, width, height, adjMinX, adjMinY, adjMaxX, adjMaxY, threshold, dimension);
                if (cancellationToken.IsCancellationRequested) return false;

                if (QrDecoder.TryDecode(modules, out decoded, out var moduleInfo)) {
                    info = new QrPixelDecodeInfo(1, threshold, invert: false, candidateCount: 0, candidateTriplesTried: 0, dimension, moduleInfo);
                    return true;
                }

                var inverted = modules.Clone();
                inverted.Invert();
                if (QrDecoder.TryDecode(inverted, out decoded, out moduleInfo)) {
                    info = new QrPixelDecodeInfo(1, threshold, invert: true, candidateCount: 0, candidateTriplesTried: 0, dimension, moduleInfo);
                    return true;
                }
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

    private static byte ComputeMeanThreshold(byte[] gray) {
        long sum = 0;
        for (var i = 0; i < gray.Length; i++) sum += gray[i];
        var mean = (int)(sum / Math.Max(1, gray.Length));
        var adjusted = mean - 8;
        if (adjusted < 0) adjusted = 0;
        if (adjusted > 255) adjusted = 255;
        return (byte)adjusted;
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
