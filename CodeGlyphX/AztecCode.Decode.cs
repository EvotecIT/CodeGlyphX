using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CodeGlyphX.Aztec;
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
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class AztecCode {
    private const string FailureCancelled = "Cancelled.";
    private const string FailureInvalid = "Invalid input.";
    private const string FailureDownscale = "Image downscale failed.";
    private const string FailureNoDecoded = "No Aztec decoded.";
    /// <summary>
    /// Attempts to decode an Aztec symbol from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value) {
        return AztecDecoder.TryDecode(modules, out value);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a module matrix, with cancellation.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        return AztecDecoder.TryDecode(modules, cancellationToken, out value);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a module matrix, with diagnostics.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value, out AztecDecodeDiagnostics diagnostics) {
        return AztecDecoder.TryDecode(modules, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a module matrix, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out string value, out AztecDecodeDiagnostics diagnostics) {
        return AztecDecoder.TryDecode(modules, cancellationToken, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, out value);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels, with cancellation.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out value);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value, out AztecDecodeDiagnostics diagnostics) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, out AztecDecodeDiagnostics diagnostics) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out value, out diagnostics);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, out value);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels, with cancellation.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out value);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value, out AztecDecodeDiagnostics diagnostics) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from raw pixels, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, out AztecDecodeDiagnostics diagnostics) {
        return AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out value, out diagnostics);
    }
#endif

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out string text) {
        return TryDecodePng(png, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, CancellationToken cancellationToken, out string text) {
        return TryDecodePng(png, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes with image decode options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, out string text) {
        return TryDecodePng(png, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        return TryDecodePngCore(png, options, cancellationToken, out text, out _);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes in a span.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, out string text) {
        return TryDecodePng(png, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes in a span, with cancellation.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, CancellationToken cancellationToken, out string text) {
        return TryDecodePng(png, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes in a span with image decode options.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, out string text) {
        return TryDecodePng(png, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes in a span with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        return TryDecodePngCore(png.ToArray(), options, cancellationToken, out text, out _);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePng(png, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePng(png, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePngCore(png, options, cancellationToken, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes in a span, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePng(png, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes in a span with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePng(png, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from PNG bytes in a span with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePngCore(png.ToArray(), options, cancellationToken, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out string text) {
        return TryDecodePngFile(path, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG file, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, CancellationToken cancellationToken, out string text) {
        return TryDecodePngFile(path, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG file with image decode options.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, out string text) {
        return TryDecodePngFile(path, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG file with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; return false; }
        if (!DecodeResultHelpers.TryReadBinary(path, options, out var png)) { text = string.Empty; return false; }
        return TryDecodePng(png, options, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG file, with diagnostics.
    /// </summary>
    public static bool TryDecodePngFile(string path, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePngFile(path, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG file with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePngFile(path, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG file with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = FailureCancelled }; return false; }
        if (!DecodeResultHelpers.TryReadBinary(path, options, out var png)) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = FailureInvalid }; return false; }
        return TryDecodePngCore(png, options, cancellationToken, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out string text) {
        return TryDecodePng(stream, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream with image decode options.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, out string text) {
        return TryDecodePng(stream, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePng(stream, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodePng(stream, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out string text) {
        return TryDecodeImage(image, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, CancellationToken cancellationToken, out string text) {
        return TryDecodeImage(image, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, out string text) {
        return TryDecodeImage(image, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodeImage(image, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodeImage(image, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodeImageCore(image, options, cancellationToken, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats in a span.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, out string text) {
        return TryDecodeImage(image, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats in a span, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, CancellationToken cancellationToken, out string text) {
        return TryDecodeImage(image, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats in a span with image decode options.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, out string text) {
        return TryDecodeImage(image, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats in a span with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        return DecodeResultHelpers.TryDecodeImage(
            image,
            options,
            cancellationToken,
            (byte[] rgba, int width, int height, CancellationToken token, out string decoded)
                => AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded),
            out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats in a span, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodeImage(image, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats in a span with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodeImage(image, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from common image formats in a span with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        diagnostics = new AztecDecodeDiagnostics();
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; diagnostics.Failure = FailureCancelled; return false; }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) {
                text = string.Empty;
                diagnostics.Failure ??= "Unsupported image format.";
                return false;
            }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                diagnostics.Failure ??= token.IsCancellationRequested ? FailureCancelled : FailureDownscale;
                return false;
            }
            if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var azDiag)) {
                diagnostics = azDiag;
                return true;
            }
            diagnostics = azDiag;
            diagnostics.Failure ??= FailureNoDecoded;
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out string text) {
        return TryDecodeImage(stream, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, CancellationToken cancellationToken, out string text) {
        return TryDecodeImage(stream, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, out string text) {
        return TryDecodeImage(stream, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        return DecodeResultHelpers.TryDecodeImage(
            stream,
            options,
            cancellationToken,
            (byte[] rgba, int width, int height, CancellationToken token, out string decoded)
                => AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded),
            out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from an image stream, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodeImage(stream, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from an image stream with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, out string text, out AztecDecodeDiagnostics diagnostics) {
        return TryDecodeImage(stream, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from an image stream with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics)
        => DecodeResultHelpers.TryDecodeImageStreamWithDiagnostics(stream, options, cancellationToken, FailureInvalid, FailureCancelled, TryDecodeImageCore, out text, out diagnostics);

    /// <summary>
    /// Decodes an Aztec symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) and returns diagnostics.
    /// </summary>
    public static DecodeResult<string> DecodeImageResult(byte[] image, ImageDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        return DecodeImageResult((ReadOnlySpan<byte>)image, options, cancellationToken);
    }

    /// <summary>
    /// Decodes an Aztec symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) in a span and returns diagnostics.
    /// </summary>
    public static DecodeResult<string> DecodeImageResult(ReadOnlySpan<byte> image, ImageDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        return DecodeResultHelpers.DecodeImageResult(
            image,
            options,
            cancellationToken,
            (byte[] rgba, int width, int height, CancellationToken token, out string text)
                => AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text));
    }

    /// <summary>
    /// Decodes an Aztec symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) and returns diagnostics.
    /// </summary>
    public static DecodeResult<string> DecodeImageResult(Stream stream, ImageDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        return DecodeResultHelpers.DecodeImageResult(
            stream,
            options,
            cancellationToken,
            (byte[] rgba, int width, int height, CancellationToken token, out string text)
                => AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text));
    }

    /// <summary>
    /// Decodes a batch of Aztec images with shared settings and aggregated diagnostics.
    /// </summary>
    public static DecodeBatchResult<string> DecodeImageBatch(IEnumerable<byte[]> images, ImageDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        return DecodeBatchHelpers.Run(images, image => DecodeImageResult(image, options, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Attempts to decode all Aztec symbols from common image formats.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, out string[] texts) {
        return TryDecodeAllImage(image, null, CancellationToken.None, out texts);
    }

    /// <summary>
    /// Attempts to decode all Aztec symbols from common image formats with image decode options.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, ImageDecodeOptions? options, out string[] texts) {
        return TryDecodeAllImage(image, options, CancellationToken.None, out texts);
    }

    /// <summary>
    /// Attempts to decode all Aztec symbols from common image formats with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string[] texts) {
        return TryDecodeAllImageCore(image, options, cancellationToken, out texts);
    }

    /// <summary>
    /// Attempts to decode all Aztec symbols from an image stream.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, out string[] texts) {
        return TryDecodeAllImage(stream, null, CancellationToken.None, out texts);
    }

    /// <summary>
    /// Attempts to decode all Aztec symbols from an image stream with image decode options.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, ImageDecodeOptions? options, out string[] texts) {
        return TryDecodeAllImage(stream, options, CancellationToken.None, out texts);
    }

    /// <summary>
    /// Attempts to decode all Aztec symbols from an image stream with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string[] texts)
        => DecodeResultHelpers.TryDecodeAllImageStream(stream, options, cancellationToken, TryDecodeAllImageCore, out texts);

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, CancellationToken cancellationToken, out string text) {
        return TryDecodePng(stream, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text)
        => DecodeResultHelpers.TryDecodeBinaryStream(stream, options, cancellationToken, TryDecodePng, out text);

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics)
        => DecodeResultHelpers.TryDecodeBinaryStreamWithDiagnostics(stream, options, cancellationToken, FailureInvalid, FailureCancelled, TryDecodePngCore, out text, out diagnostics);

    private static bool TryDecodePngCore(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        var failures = DecodeResultHelpers.DecodeFailureMessages.ForPng(FailureInvalid, FailureCancelled, FailureDownscale, FailureNoDecoded);
        return DecodeResultHelpers.TryDecodePngWithDiagnostics(
            png,
            options,
            cancellationToken,
            failures,
            (byte[] rgba, int width, int height, CancellationToken token, out string decoded, out AztecDecodeDiagnostics diag)
                => AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded, out diag),
            out text,
            out diagnostics);
    }

    private static bool TryDecodeImageCore(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        var failures = DecodeResultHelpers.DecodeFailureMessages.ForImage(FailureCancelled, FailureDownscale, DecodeResultHelpers.FailureUnsupportedImageFormat, FailureNoDecoded);
        return DecodeResultHelpers.TryDecodeImageWithDiagnostics(
            image,
            options,
            cancellationToken,
            failures,
            (byte[] rgba, int width, int height, CancellationToken token, out string decoded, out AztecDecodeDiagnostics diag)
                => AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded, out diag),
            out text,
            out diagnostics);
    }

    private static bool TryDecodeAllImageCore(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string[] texts) {
        return DecodeResultHelpers.TryDecodeAllImage(
            image,
            options,
            cancellationToken,
            (byte[] rgba, int width, int height, int stride, CancellationToken token, out string decoded)
                => AztecDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, token, out decoded),
            out texts);
    }

    /// <summary>
    /// Decodes an Aztec symbol from PNG bytes.
    /// </summary>
    public static string DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var text)) {
            throw new FormatException("PNG does not contain a decodable Aztec symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes an Aztec symbol from a PNG file.
    /// </summary>
    public static string DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var text)) {
            throw new FormatException("PNG file does not contain a decodable Aztec symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes an Aztec symbol from a PNG stream.
    /// </summary>
    public static string DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var text)) {
            throw new FormatException("PNG stream does not contain a decodable Aztec symbol.");
        }
        return text;
    }

}
