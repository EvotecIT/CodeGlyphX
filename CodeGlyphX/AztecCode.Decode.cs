using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        if (png is null) throw new ArgumentNullException(nameof(png));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
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
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
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
        diagnostics = new AztecDecodeDiagnostics();
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; diagnostics.Failure = FailureCancelled; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
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
        if (!TryReadBinary(path, options, out var png)) { text = string.Empty; return false; }
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
        if (!TryReadBinary(path, options, out var png)) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = FailureInvalid }; return false; }
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
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            if (!TryReadBinary(stream, options, out var data)) { text = string.Empty; return false; }
            if (!ImageReader.TryDecodeRgba32(data, options, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
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
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = FailureCancelled }; return false; }
        var maxBytes = options?.MaxBytes > 0 ? options.MaxBytes : ImageReader.MaxImageBytes;
        if (!RenderIO.TryReadBinary(stream, maxBytes, out var data)) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = FailureInvalid }; return false; }
        return TryDecodeImageCore(data, options, cancellationToken, out text, out diagnostics);
    }

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
        var stopwatch = Stopwatch.StartNew();
        if (!DecodeResultHelpers.TryCheckImageLimits(image, options, out var info, out var formatKnown, out var limitMessage)) {
            return new DecodeResult<string>(DecodeFailureReason.InvalidInput, info, stopwatch.Elapsed, limitMessage);
        }
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) {
                return new DecodeResult<string>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
            }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) {
                var imageFailure = DecodeResultHelpers.FailureForImageRead(image, formatKnown, token);
                return new DecodeResult<string>(imageFailure, info, stopwatch.Elapsed);
            }

            info = DecodeResultHelpers.EnsureDimensions(info, formatKnown, width, height);

            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                return new DecodeResult<string>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
            }
            if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out var text)) {
                return new DecodeResult<string>(text, info, stopwatch.Elapsed);
            }
            var failure = DecodeResultHelpers.FailureForDecode(token);
            return new DecodeResult<string>(failure, info, stopwatch.Elapsed);
        } catch (Exception ex) {
            return new DecodeResult<string>(DecodeFailureReason.Error, info, stopwatch.Elapsed, ex.Message);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Decodes an Aztec symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) and returns diagnostics.
    /// </summary>
    public static DecodeResult<string> DecodeImageResult(Stream stream, ImageDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeImageResult(buffer.AsSpan(), options, cancellationToken);
        }
        var maxBytes = options?.MaxBytes > 0 ? options.MaxBytes : ImageReader.MaxImageBytes;
        if (!RenderIO.TryReadBinary(stream, maxBytes, out var data)) {
            return new DecodeResult<string>(DecodeFailureReason.InvalidInput, default, TimeSpan.Zero, "image payload exceeds size limits");
        }
        return DecodeImageResult(data, options, cancellationToken);
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
    public static bool TryDecodeAllImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string[] texts) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { texts = Array.Empty<string>(); return false; }
        if (!TryReadBinary(stream, options, out var data)) { texts = Array.Empty<string>(); return false; }
        return TryDecodeAllImageCore(data, options, cancellationToken, out texts);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, CancellationToken cancellationToken, out string text) {
        return TryDecodePng(stream, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; return false; }
        if (!TryReadBinary(stream, options, out var png)) { text = string.Empty; return false; }
        return TryDecodePng(png, options, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = FailureCancelled }; return false; }
        if (!TryReadBinary(stream, options, out var png)) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = FailureInvalid }; return false; }
        return TryDecodePngCore(png, options, cancellationToken, out text, out diagnostics);
    }

    private static bool TryDecodePngCore(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        diagnostics = new AztecDecodeDiagnostics();
        if (png is null) throw new ArgumentNullException(nameof(png));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; diagnostics.Failure = FailureCancelled; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                diagnostics.Failure ??= token.IsCancellationRequested ? FailureCancelled : FailureDownscale;
                return false;
            }
            if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var aztecDiag)) {
                diagnostics = aztecDiag;
                return true;
            }
            diagnostics = aztecDiag;
            diagnostics.Failure ??= FailureNoDecoded;
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeImageCore(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        diagnostics = new AztecDecodeDiagnostics();
        if (image is null) throw new ArgumentNullException(nameof(image));
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
            if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var aztecDiag)) {
                diagnostics = aztecDiag;
                return true;
            }
            diagnostics = aztecDiag;
            diagnostics.Failure ??= FailureNoDecoded;
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeAllImageCore(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string[] texts) {
        texts = Array.Empty<string>();
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) return false;
            var original = rgba;
            var originalWidth = width;
            var originalHeight = height;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) return false;

            var list = new System.Collections.Generic.List<string>(4);
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            CollectAllFromRgba(rgba, width, height, width * 4, token, list, seen);
            if (!ReferenceEquals(rgba, original) && !token.IsCancellationRequested) {
                CollectAllFromRgba(original, originalWidth, originalHeight, originalWidth * 4, token, list, seen);
            }

            if (list.Count == 0) return false;
            texts = list.ToArray();
            return true;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static void CollectAllFromRgba(byte[] rgba, int width, int height, int stride, CancellationToken token, System.Collections.Generic.List<string> list, System.Collections.Generic.HashSet<string> seen) {
        if (token.IsCancellationRequested) return;
        if (AztecDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, token, out var text)) {
            AddUnique(list, seen, text);
        }
        ScanTiles(rgba, width, height, stride, token, (tile, tw, th, tstride) => {
            if (AztecDecoder.TryDecode(tile, tw, th, tstride, PixelFormat.Rgba32, token, out var value)) {
                AddUnique(list, seen, value);
            }
        });
    }

    private static void AddUnique(System.Collections.Generic.List<string> list, System.Collections.Generic.HashSet<string> seen, string text) {
        if (string.IsNullOrEmpty(text)) return;
        if (seen.Add(text)) list.Add(text);
    }

    private static void ScanTiles(byte[] rgba, int width, int height, int stride, CancellationToken token, Action<byte[], int, int, int> onTile) {
        if (!CanScanTiles(width, height, stride)) return;
        var grid = Math.Max(width, height) >= 720 ? 3 : 2;
        var pad = Math.Max(8, Math.Min(width, height) / 40);
        var tileW = width / grid;
        var tileH = height / grid;

        var ctx = new TileScanContext(rgba, width, height, stride, token, onTile, grid, pad, tileW, tileH);

        for (var ty = 0; ty < grid; ty++) {
            for (var tx = 0; tx < grid; tx++) {
                if (!ProcessTile(ctx, tx, ty)) return;
            }
        }
    }

    private static bool CanScanTiles(int width, int height, int stride) {
        return width > 0 && height > 0 && stride >= width * 4;
    }

    private static bool ProcessTile(TileScanContext ctx, int tx, int ty) {
        if (ctx.Token.IsCancellationRequested) return false;
        if (!TryGetTileBounds(ctx, tx, ty, out var x0, out var y0, out var tw, out var th)) return true;
        if (!TryExtractTile(ctx, x0, y0, tw, th, out var tile, out var tileStride)) return false;
        ctx.OnTile(tile, tw, th, tileStride);
        return true;
    }

    private static bool TryGetTileBounds(TileScanContext ctx, int tx, int ty, out int x0, out int y0, out int tw, out int th) {
        x0 = tx * ctx.TileW;
        y0 = ty * ctx.TileH;
        var x1 = (tx == ctx.Grid - 1) ? ctx.Width : (tx + 1) * ctx.TileW;
        var y1 = (ty == ctx.Grid - 1) ? ctx.Height : (ty + 1) * ctx.TileH;

        x0 = Math.Max(0, x0 - ctx.Pad);
        y0 = Math.Max(0, y0 - ctx.Pad);
        x1 = Math.Min(ctx.Width, x1 + ctx.Pad);
        y1 = Math.Min(ctx.Height, y1 + ctx.Pad);

        tw = x1 - x0;
        th = y1 - y0;
        return tw >= 48 && th >= 48;
    }

    private static bool TryExtractTile(TileScanContext ctx, int x0, int y0, int tw, int th, out byte[] tile, out int tileStride) {
        tileStride = tw * 4;
        tile = new byte[tileStride * th];
        for (var y = 0; y < th; y++) {
            if (ctx.Token.IsCancellationRequested) return false;
            Buffer.BlockCopy(ctx.Rgba, (y0 + y) * ctx.Stride + x0 * 4, tile, y * tileStride, tileStride);
        }
        return true;
    }

    private readonly struct TileScanContext {
        public byte[] Rgba { get; }
        public int Width { get; }
        public int Height { get; }
        public int Stride { get; }
        public CancellationToken Token { get; }
        public Action<byte[], int, int, int> OnTile { get; }
        public int Grid { get; }
        public int Pad { get; }
        public int TileW { get; }
        public int TileH { get; }

        public TileScanContext(
            byte[] rgba,
            int width,
            int height,
            int stride,
            CancellationToken token,
            Action<byte[], int, int, int> onTile,
            int grid,
            int pad,
            int tileW,
            int tileH) {
            Rgba = rgba;
            Width = width;
            Height = height;
            Stride = stride;
            Token = token;
            OnTile = onTile;
            Grid = grid;
            Pad = pad;
            TileW = tileW;
            TileH = tileH;
        }
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

    private static int ResolveMaxBytes(ImageDecodeOptions? options) {
        if (options is not null && options.MaxBytes > 0) return options.MaxBytes;
        return ImageReader.MaxImageBytes;
    }

    private static bool TryReadBinary(string path, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(path, ResolveMaxBytes(options), out data);
    }

    private static bool TryReadBinary(Stream stream, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(stream, ResolveMaxBytes(options), out data);
    }

}
