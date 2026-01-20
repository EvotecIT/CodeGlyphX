using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Aztec;
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
            if (token.IsCancellationRequested) { text = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                diagnostics.Failure ??= token.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var azDiag)) {
                diagnostics = azDiag;
                return true;
            }
            diagnostics = azDiag;
            diagnostics.Failure ??= "No Aztec decoded.";
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
        var png = RenderIO.ReadBinary(path);
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
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = "Cancelled." }; return false; }
        var png = RenderIO.ReadBinary(path);
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
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
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
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
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
            if (token.IsCancellationRequested) { text = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
                text = string.Empty;
                diagnostics.Failure ??= "Unsupported image format.";
                return false;
            }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                diagnostics.Failure ??= token.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var azDiag)) {
                diagnostics = azDiag;
                return true;
            }
            diagnostics = azDiag;
            diagnostics.Failure ??= "No Aztec decoded.";
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
            var data = RenderIO.ReadBinary(stream);
            if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
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
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = "Cancelled." }; return false; }
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeImageCore(data, options, cancellationToken, out text, out diagnostics);
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
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, options, cancellationToken, out text);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a PNG stream with image decode options, cancellation, and diagnostics.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; diagnostics = new AztecDecodeDiagnostics { Failure = "Cancelled." }; return false; }
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePngCore(png, options, cancellationToken, out text, out diagnostics);
    }

    private static bool TryDecodePngCore(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out string text, out AztecDecodeDiagnostics diagnostics) {
        diagnostics = new AztecDecodeDiagnostics();
        if (png is null) throw new ArgumentNullException(nameof(png));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                diagnostics.Failure ??= token.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var aztecDiag)) {
                diagnostics = aztecDiag;
                return true;
            }
            diagnostics = aztecDiag;
            diagnostics.Failure ??= "No Aztec decoded.";
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
            if (token.IsCancellationRequested) { text = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
                text = string.Empty;
                diagnostics.Failure ??= "Unsupported image format.";
                return false;
            }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                diagnostics.Failure ??= token.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out text, out var aztecDiag)) {
                diagnostics = aztecDiag;
                return true;
            }
            diagnostics = aztecDiag;
            diagnostics.Failure ??= "No Aztec decoded.";
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
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


}
