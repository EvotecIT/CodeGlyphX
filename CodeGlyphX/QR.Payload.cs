using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QR {
    /// <summary>
    /// Attempts to parse a raw QR payload into a structured representation.
    /// </summary>
    public static bool TryParsePayload(string payload, out QrParsedPayload parsed) {
        return QrPayloadParser.TryParse(payload, out parsed);
    }

    /// <summary>
    /// Attempts to parse a raw QR payload into a structured representation with optional validation.
    /// </summary>
    public static bool TryParsePayload(string payload, QrPayloadParseOptions? options, out QrParsedPayload parsed, out QrPayloadValidationResult validation) {
        return QrPayloadParser.TryParse(payload, options, out parsed, out validation);
    }

    /// <summary>
    /// Parses a raw QR payload into a structured representation.
    /// </summary>
    public static QrParsedPayload ParsePayload(string payload) {
        return QrPayloadParser.Parse(payload);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG stream.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (stream is null) return false;
        var data = stream.ReadBinary();
        return TryDecodeAllPng(data, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG stream with options and cancellation.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (stream is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var data = stream.ReadBinary();
        return TryDecodeAllPng(data, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out QrDecoded decoded) {
        return TryDecodeImage(image, options: null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return TryDecodeImage(image, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with options and cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (image is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options, cancellationToken);
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
        return QrImageDecoder.TryDecodeImage(image, imageOptions, options, cancellationToken, out decoded);
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
        return QrImageDecoder.TryDecodeImage(image, imageOptions, out decoded, out info, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode all QR codes from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, out QrDecoded[] decoded) {
        return TryDecodeAllImage(image, options: null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with options.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
        return TryDecodeAllImage(image, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with options and cancellation.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (image is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        return QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options, cancellationToken);
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
        return QrImageDecoder.TryDecodeAllImage(image, imageOptions, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out QrDecoded decoded) {
        return TryDecodeImage(stream, options: null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return TryDecodeImage(stream, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with options and cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (stream is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var data = RenderIO.ReadBinary(stream);
        if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) return false;
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options, cancellationToken);
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
        return QrImageDecoder.TryDecodeImage(stream, imageOptions, options, cancellationToken, out decoded);
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
        return QrImageDecoder.TryDecodeImage(stream, imageOptions, out decoded, out info, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode all QR codes from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, out QrDecoded[] decoded) {
        return TryDecodeAllImage(stream, options: null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with options.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
        return TryDecodeAllImage(stream, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with options and cancellation.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (stream is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var data = RenderIO.ReadBinary(stream);
        if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) return false;
        return QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options, cancellationToken);
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
        return QrImageDecoder.TryDecodeAllImage(stream, imageOptions, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Decodes a QR code from a PNG byte array.
    /// </summary>
    public static QrDecoded DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var decoded)) {
            throw new InvalidOperationException("Failed to decode QR from PNG.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a QR code from a PNG file.
    /// </summary>
    public static QrDecoded DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var decoded)) {
            throw new InvalidOperationException("Failed to decode QR from PNG.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a QR code from a PNG stream.
    /// </summary>
    public static QrDecoded DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var decoded)) {
            throw new InvalidOperationException("Failed to decode QR from PNG.");
        }
        return decoded;
    }
}
