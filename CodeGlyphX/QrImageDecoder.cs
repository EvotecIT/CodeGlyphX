using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Decodes QR codes from raw pixel buffers.
/// </summary>
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
