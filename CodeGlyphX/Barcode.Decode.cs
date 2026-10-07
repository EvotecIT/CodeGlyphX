using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class Barcode {
    /// <summary>
    /// Attempts to decode a barcode from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out BarcodeDecoded decoded) {
        return TryDecodePng(png, null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodePng(png, null, cancellationToken, out decoded);
    }


    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint.
    /// </summary>
    public static bool TryDecodePng(byte[] png, SymbolFormat? expectedType, out BarcodeDecoded decoded) {
        return TryDecodePng(png, expectedType, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint and image decode options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, SymbolFormat? expectedType, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodePng(png, expectedType, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint, image decode options, and barcode decode options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, SymbolFormat? expectedType, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, out BarcodeDecoded decoded) {
        return TryDecodePng(png, expectedType, options, decodeOptions, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, SymbolFormat? expectedType, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodePng(png, expectedType, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint and image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, SymbolFormat? expectedType, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodePng(png, expectedType, options, decodeOptions: null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint, image decode options, barcode decode options, and cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, SymbolFormat? expectedType, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        decoded = null!;
        var token = cancellationToken;
        if (token.IsCancellationRequested) return false;
        if (!ImageDecodeHelper.TryDecodePngRgba32(png, options, out var rgba, out var width, out var height)) return false;
        using var budget = ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, options, out token);
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType.HasValue ? SymbolCapabilities.GetBarcodeType(expectedType.Value) : null, decodeOptions, token, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, null, cancellationToken, out decoded);
    }


    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, SymbolFormat? expectedType, out BarcodeDecoded decoded) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType.HasValue ? SymbolCapabilities.GetBarcodeType(expectedType.Value) : null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint and image decode options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, SymbolFormat? expectedType, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, expectedType, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint, image decode options, and barcode decode options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, SymbolFormat? expectedType, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, expectedType, options, decodeOptions, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, SymbolFormat? expectedType, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, expectedType, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint and image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, SymbolFormat? expectedType, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, expectedType, options, decodeOptions: null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint, image decode options, barcode decode options, and cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, SymbolFormat? expectedType, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = cancellationToken;
        if (token.IsCancellationRequested) return false;
        if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) return false;
        using var budget = ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, options, out token);
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType.HasValue ? SymbolCapabilities.GetBarcodeType(expectedType.Value) : null, decodeOptions, token, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, null, options: null, decodeOptions: null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, null, options: null, decodeOptions: null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, SymbolFormat? expectedType, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, expectedType, options: null, decodeOptions: null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint and image decode options.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, SymbolFormat? expectedType, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, expectedType, options, decodeOptions, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint, image decode options, barcode decode options, and cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, SymbolFormat? expectedType, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        var token = cancellationToken;
        if (token.IsCancellationRequested) return false;
        if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) return false;
        using var budget = ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, options, out token);
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType.HasValue ? SymbolCapabilities.GetBarcodeType(expectedType.Value) : null, decodeOptions, token, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out BarcodeDecoded decoded) {
        return TryDecodeImage(stream, null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeImage(stream, null, cancellationToken, out decoded);
    }


    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, SymbolFormat? expectedType, out BarcodeDecoded decoded) {
        decoded = null!;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (!ImageReader.TryDecodeRgba32(stream, out var rgba, out var width, out var height)) return false;
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType.HasValue ? SymbolCapabilities.GetBarcodeType(expectedType.Value) : null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint and image decode options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, SymbolFormat? expectedType, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodeImage(stream, expectedType, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint, image decode options, and barcode decode options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, SymbolFormat? expectedType, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, out BarcodeDecoded decoded) {
        return TryDecodeImage(stream, expectedType, options, decodeOptions, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, SymbolFormat? expectedType, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeImage(stream, expectedType, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint and image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, SymbolFormat? expectedType, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeImage(stream, expectedType, options, decodeOptions: null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint, image decode options, barcode decode options, and cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, SymbolFormat? expectedType, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var token = cancellationToken;
        if (token.IsCancellationRequested) return false;
        if (!ImageReader.TryDecodeRgba32(stream, options, out var rgba, out var width, out var height)) return false;
        using var budget = ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, options, out token);
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType.HasValue ? SymbolCapabilities.GetBarcodeType(expectedType.Value) : null, decodeOptions, token, out decoded);
    }

    /// <summary>
    /// Decodes a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) and returns diagnostics.
    /// </summary>
    public static DecodeResult<BarcodeDecoded> DecodeImageResult(byte[] image, SymbolFormat? expectedType = null, ImageDecodeOptions? options = null, BarcodeDecodeOptions? decodeOptions = null, CancellationToken cancellationToken = default) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        return DecodeImageResult((ReadOnlySpan<byte>)image, expectedType, options, decodeOptions, cancellationToken);
    }

    /// <summary>
    /// Decodes a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) in a span and returns diagnostics.
    /// </summary>
    public static DecodeResult<BarcodeDecoded> DecodeImageResult(ReadOnlySpan<byte> image, SymbolFormat? expectedType = null, ImageDecodeOptions? options = null, BarcodeDecodeOptions? decodeOptions = null, CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        if (!DecodeResultHelpers.TryCheckImageLimits(image, options, out var info, out var formatKnown, out var limitMessage)) {
            return new DecodeResult<BarcodeDecoded>(DecodeFailureReason.InvalidInput, info, stopwatch.Elapsed, limitMessage);
        }
        var token = cancellationToken;
        try {
            if (token.IsCancellationRequested) {
                return new DecodeResult<BarcodeDecoded>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
            }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) {
                var imageFailure = DecodeResultHelpers.FailureForImageRead(image, formatKnown, token);
                return new DecodeResult<BarcodeDecoded>(imageFailure, info, stopwatch.Elapsed);
            }
            using var budget = ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, options, out token);

            info = DecodeResultHelpers.EnsureDimensions(info, formatKnown, width, height);

            if (BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType.HasValue ? SymbolCapabilities.GetBarcodeType(expectedType.Value) : null, decodeOptions, token, out var decoded)) {
                return new DecodeResult<BarcodeDecoded>(decoded, info, stopwatch.Elapsed);
            }
            var failure = DecodeResultHelpers.FailureForDecode(token);
            return new DecodeResult<BarcodeDecoded>(failure, info, stopwatch.Elapsed);
        } catch (Exception ex) {
            return new DecodeResult<BarcodeDecoded>(DecodeFailureReason.Error, info, stopwatch.Elapsed, ex.Message);
        }
    }

    /// <summary>
    /// Decodes a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) and returns diagnostics.
    /// </summary>
    public static DecodeResult<BarcodeDecoded> DecodeImageResult(Stream stream, SymbolFormat? expectedType = null, ImageDecodeOptions? options = null, BarcodeDecodeOptions? decodeOptions = null, CancellationToken cancellationToken = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var maxBytes = Math.Max(0, options?.MaxBytes ?? ImageReader.MaxImageBytes);
        if (!RenderIO.TryReadBinary(stream, maxBytes, out var data)) {
            return new DecodeResult<BarcodeDecoded>(DecodeFailureReason.InvalidInput, default, TimeSpan.Zero, "image payload exceeds size limits");
        }
        return DecodeImageResult(data, expectedType, options, decodeOptions, cancellationToken);
    }

    /// <summary>
    /// Decodes a batch of barcode images with shared settings and aggregated diagnostics.
    /// </summary>
    public static DecodeBatchResult<BarcodeDecoded> DecodeImageBatch(IEnumerable<byte[]> images, SymbolFormat? expectedType = null, ImageDecodeOptions? options = null, BarcodeDecodeOptions? decodeOptions = null, CancellationToken cancellationToken = default) {
        return DecodeBatchHelpers.Run(images, image => DecodeImageResult(image, expectedType, options, decodeOptions, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out BarcodeDecoded decoded) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (!TryReadBinary(path, options: null, out var png)) { decoded = null!; return false; }
        return TryDecodePng(png, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        if (!TryReadBinary(path, options: null, out var png)) { decoded = null!; return false; }
        return TryDecodePng(png, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file with image decode options.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodePngFile(path, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file with image decode options and barcode decode options.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, out BarcodeDecoded decoded) {
        return TryDecodePngFile(path, options, decodeOptions, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodePngFile(path, options, decodeOptions: null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file with image decode options, barcode decode options, and cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        if (!TryReadBinary(path, options, out var png)) { decoded = null!; return false; }
        return TryDecodePng(png, null, options, decodeOptions, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out BarcodeDecoded decoded) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (!TryReadBinary(stream, options: null, out var png)) { decoded = null!; return false; }
        return TryDecodePng(png, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        if (!TryReadBinary(stream, options: null, out var png)) { decoded = null!; return false; }
        return TryDecodePng(png, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream with image decode options.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodePng(stream, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream with image decode options and barcode decode options.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, out BarcodeDecoded decoded) {
        return TryDecodePng(stream, options, decodeOptions, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodePng(stream, options, decodeOptions: null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream with image decode options, barcode decode options, and cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, BarcodeDecodeOptions? decodeOptions, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        if (!TryReadBinary(stream, options, out var png)) { decoded = null!; return false; }
        return TryDecodePng(png, null, options, decodeOptions, cancellationToken, out decoded);
    }

    /// <summary>
    /// Decodes a barcode from PNG bytes.
    /// </summary>
    public static BarcodeDecoded DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var decoded)) {
            throw new FormatException("PNG does not contain a decodable barcode.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a barcode from a PNG file.
    /// </summary>
    public static BarcodeDecoded DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var decoded)) {
            throw new FormatException("PNG file does not contain a decodable barcode.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a barcode from a PNG stream.
    /// </summary>
    public static BarcodeDecoded DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var decoded)) {
            throw new FormatException("PNG stream does not contain a decodable barcode.");
        }
        return decoded;
    }

    private static int ResolveMaxBytes(ImageDecodeOptions? options) {
        return Math.Max(0, options?.MaxBytes ?? ImageReader.MaxImageBytes);
    }

    private static bool TryReadBinary(string path, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(path, ResolveMaxBytes(options), out data);
    }

    private static bool TryReadBinary(Stream stream, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(stream, ResolveMaxBytes(options), out data);
    }

}
