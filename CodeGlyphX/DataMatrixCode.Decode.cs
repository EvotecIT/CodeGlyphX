using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CodeGlyphX.DataMatrix;
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
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class DataMatrixCode {
    private const string FailureCancelled = "Cancelled.";
    private const string FailureDownscale = "Image downscale failed.";
    private const string FailureNoDataMatrix = "No Data Matrix decoded.";
    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out string text) {
        return TryDecodePng(png, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, CancellationToken cancellationToken, out string text) {
        return TryDecodePng(png, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes with image decode options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, out string text) {
        return TryDecodePng(png, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes in a span.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, out string text) {
        return TryDecodePng(png, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes in a span, with cancellation.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, CancellationToken cancellationToken, out string text) {
        return TryDecodePng(png, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes in a span with image decode options.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, out string text) {
        return TryDecodePng(png, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes in a span with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodePng(png, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodePng(png, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodePngCore(png, options, cancellationToken, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes in a span, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodePng(png, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes in a span with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodePng(png, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from PNG bytes in a span with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        diagnostics = new DataMatrixDecodeDiagnostics();
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; diagnostics.Failure = FailureCancelled; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                diagnostics.Failure ??= token.IsCancellationRequested ? FailureCancelled : FailureDownscale;
                return false;
            }
            if (DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var dmDiag)) {
                diagnostics = dmDiag;
                return true;
            }
            diagnostics = dmDiag;
            diagnostics.Failure ??= FailureNoDataMatrix;
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out string text) {
        return TryDecodePngFile(path, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file with image decode options.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, out string text) {
        return TryDecodePngFile(path, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; return false; }
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, options, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file, with diagnostics.
    /// </summary>
    public static bool TryDecodePngFile(string path, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodePngFile(path, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodePngFile(path, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG file with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; diagnostics = new DataMatrixDecodeDiagnostics { Failure = FailureCancelled }; return false; }
        var png = RenderIO.ReadBinary(path);
        return TryDecodePngCore(png, options, cancellationToken, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out string text) {
        return TryDecodePng(stream, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream with image decode options.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, out string text) {
        return TryDecodePng(stream, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; return false; }
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, options, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodePng(stream, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodePng(stream, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; diagnostics = new DataMatrixDecodeDiagnostics { Failure = FailureCancelled }; return false; }
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePngCore(png, options, cancellationToken, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a PNG stream, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, CancellationToken cancellationToken, out string text) {
        return TryDecodePng(stream, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out string text) {
        return TryDecodeImage(image, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, CancellationToken cancellationToken, out string text) {
        return TryDecodeImage(image, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, out string text) {
        return TryDecodeImage(image, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodeImage(image, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodeImage(image, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodeImageCore(image, options, cancellationToken, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats in a span, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodeImage(image, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats in a span with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodeImage(image, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats in a span with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        diagnostics = new DataMatrixDecodeDiagnostics();
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
            if (DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var dmDiag)) {
                diagnostics = dmDiag;
                return true;
            }
            diagnostics = dmDiag;
            diagnostics.Failure ??= FailureNoDataMatrix;
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats in a span.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, out string text) {
        return TryDecodeImage(image, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats in a span, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, CancellationToken cancellationToken, out string text) {
        return TryDecodeImage(image, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats in a span with image decode options.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, out string text) {
        return TryDecodeImage(image, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from common image formats in a span with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out string text) {
        return TryDecodeImage(stream, null, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, CancellationToken cancellationToken, out string text) {
        return TryDecodeImage(stream, null, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, out string text) {
        return TryDecodeImage(stream, options, CancellationToken.None, out text);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            var data = RenderIO.ReadBinary(stream);
            if (!ImageReader.TryDecodeRgba32(data, options, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodeImage(stream, null, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream with image decode options, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecodeImage(stream, options, CancellationToken.None, out text, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from an image stream with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; diagnostics = new DataMatrixDecodeDiagnostics { Failure = FailureCancelled }; return false; }
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeImageCore(data, options, cancellationToken, out text, out diagnostics);
    }

    /// <summary>
    /// Decodes a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) and returns diagnostics.
    /// </summary>
    public static DecodeResult<string> DecodeImageResult(byte[] image, ImageDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        return DecodeImageResult((ReadOnlySpan<byte>)image, options, cancellationToken);
    }

    /// <summary>
    /// Decodes a Data Matrix symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) in a span and returns diagnostics.
    /// </summary>
    public static DecodeResult<string> DecodeImageResult(ReadOnlySpan<byte> image, ImageDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        _ = DecodeResultHelpers.TryGetImageInfo(image, out var info, out var formatKnown);
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
            if (DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out var text)) {
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
    /// Decodes a Data Matrix symbol from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) and returns diagnostics.
    /// </summary>
    public static DecodeResult<string> DecodeImageResult(Stream stream, ImageDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeImageResult(buffer.AsSpan(), options, cancellationToken);
        }
        var data = RenderIO.ReadBinary(stream);
        return DecodeImageResult(data, options, cancellationToken);
    }

    /// <summary>
    /// Decodes a batch of Data Matrix images with shared settings and aggregated diagnostics.
    /// </summary>
    public static DecodeBatchResult<string> DecodeImageBatch(IEnumerable<byte[]> images, ImageDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        return DecodeBatchHelpers.Run(images, image => DecodeImageResult(image, options, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Attempts to decode all Data Matrix symbols from common image formats.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, out string[] texts) {
        return TryDecodeAllImage(image, null, CancellationToken.None, out texts);
    }

    /// <summary>
    /// Attempts to decode all Data Matrix symbols from common image formats with image decode options.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, ImageDecodeOptions? options, out string[] texts) {
        return TryDecodeAllImage(image, options, CancellationToken.None, out texts);
    }

    /// <summary>
    /// Attempts to decode all Data Matrix symbols from common image formats with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string[] texts) {
        return TryDecodeAllImageCore(image, options, cancellationToken, out texts);
    }

    /// <summary>
    /// Attempts to decode all Data Matrix symbols from an image stream.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, out string[] texts) {
        return TryDecodeAllImage(stream, null, CancellationToken.None, out texts);
    }

    /// <summary>
    /// Attempts to decode all Data Matrix symbols from an image stream with image decode options.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, ImageDecodeOptions? options, out string[] texts) {
        return TryDecodeAllImage(stream, options, CancellationToken.None, out texts);
    }

    /// <summary>
    /// Attempts to decode all Data Matrix symbols from an image stream with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string[] texts) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { texts = Array.Empty<string>(); return false; }
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeAllImageCore(data, options, cancellationToken, out texts);
    }

    private static bool TryDecodePngCore(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        diagnostics = new DataMatrixDecodeDiagnostics();
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
            if (DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var dmDiag)) {
                diagnostics = dmDiag;
                return true;
            }
            diagnostics = dmDiag;
            diagnostics.Failure ??= FailureNoDataMatrix;
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeImageCore(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out DataMatrixDecodeDiagnostics diagnostics) {
        diagnostics = new DataMatrixDecodeDiagnostics();
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
            if (DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var dmDiag)) {
                diagnostics = dmDiag;
                return true;
            }
            diagnostics = dmDiag;
            diagnostics.Failure ??= FailureNoDataMatrix;
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
        if (DataMatrixDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, token, out var text)) {
            AddUnique(list, seen, text);
        }
        ScanTiles(rgba, width, height, stride, token, (tile, tw, th, tstride) => {
            if (DataMatrixDecoder.TryDecode(tile, tw, th, tstride, PixelFormat.Rgba32, token, out var value)) {
                AddUnique(list, seen, value);
            }
        });
    }

    private static void AddUnique(System.Collections.Generic.List<string> list, System.Collections.Generic.HashSet<string> seen, string text) {
        if (string.IsNullOrEmpty(text)) return;
        if (seen.Add(text)) list.Add(text);
    }

    private static void ScanTiles(byte[] rgba, int width, int height, int stride, CancellationToken token, Action<byte[], int, int, int> onTile) {
        if (width <= 0 || height <= 0 || stride < width * 4) return;
        var grid = Math.Max(width, height) >= 720 ? 3 : 2;
        var pad = Math.Max(8, Math.Min(width, height) / 40);
        var tileW = width / grid;
        var tileH = height / grid;

        for (var ty = 0; ty < grid; ty++) {
            for (var tx = 0; tx < grid; tx++) {
                if (token.IsCancellationRequested) return;
                var x0 = tx * tileW;
                var y0 = ty * tileH;
                var x1 = (tx == grid - 1) ? width : (tx + 1) * tileW;
                var y1 = (ty == grid - 1) ? height : (ty + 1) * tileH;

                x0 = Math.Max(0, x0 - pad);
                y0 = Math.Max(0, y0 - pad);
                x1 = Math.Min(width, x1 + pad);
                y1 = Math.Min(height, y1 + pad);

                var tw = x1 - x0;
                var th = y1 - y0;
                if (tw < 48 || th < 48) continue;

                var tileStride = tw * 4;
                var tile = new byte[tileStride * th];
                for (var y = 0; y < th; y++) {
                    if (token.IsCancellationRequested) return;
                    Buffer.BlockCopy(rgba, (y0 + y) * stride + x0 * 4, tile, y * tileStride, tileStride);
                }

                onTile(tile, tw, th, tileStride);
            }
        }
    }

    /// <summary>
    /// Decodes a Data Matrix symbol from PNG bytes.
    /// </summary>
    public static string DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var text)) {
            throw new FormatException("PNG does not contain a decodable Data Matrix symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes a Data Matrix symbol from a PNG file.
    /// </summary>
    public static string DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var text)) {
            throw new FormatException("PNG file does not contain a decodable Data Matrix symbol.");
        }
        return text;
    }

    /// <summary>
    /// Decodes a Data Matrix symbol from a PNG stream.
    /// </summary>
    public static string DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var text)) {
            throw new FormatException("PNG stream does not contain a decodable Data Matrix symbol.");
        }
        return text;
    }

    private static MatrixPngRenderOptions BuildPngOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixPngRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background
        };
    }

    private static IcoRenderOptions BuildIcoOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new IcoRenderOptions {
            Sizes = opts.IcoSizes ?? new[] { 16, 32, 48, 64, 128, 256 },
            PreserveAspectRatio = opts.IcoPreserveAspectRatio
        };
    }

    private static MatrixSvgRenderOptions BuildSvgOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixSvgRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ColorUtils.ToCss(opts.Foreground),
            LightColor = ColorUtils.ToCss(opts.Background)
        };
    }

    private static MatrixHtmlRenderOptions BuildHtmlOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixHtmlRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ColorUtils.ToCss(opts.Foreground),
            LightColor = ColorUtils.ToCss(opts.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable
        };
    }

}
