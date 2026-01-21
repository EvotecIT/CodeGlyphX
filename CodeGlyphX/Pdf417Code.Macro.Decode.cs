using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out Pdf417Decoded decoded) {
        return TryDecodePng(png, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from PNG bytes, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        return TryDecodePng(png, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from PNG bytes with image decode options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, out Pdf417Decoded decoded) {
        return TryDecodePng(png, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from PNG bytes with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? options, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { decoded = null!; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { decoded = null!; return false; }
            return Pdf417Decoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from PNG bytes in a span.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, out Pdf417Decoded decoded) {
        return TryDecodePng(png, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from PNG bytes in a span, with cancellation.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        return TryDecodePng(png, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from PNG bytes in a span with image decode options.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, out Pdf417Decoded decoded) {
        return TryDecodePng(png, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from PNG bytes in a span with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(ReadOnlySpan<byte> png, ImageDecodeOptions? options, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { decoded = null!; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { decoded = null!; return false; }
            return Pdf417Decoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out Pdf417Decoded decoded) {
        return TryDecodePngFile(path, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from a PNG file, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        return TryDecodePngFile(path, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from a PNG file with image decode options.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, out Pdf417Decoded decoded) {
        return TryDecodePngFile(path, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from a PNG file with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out Pdf417Decoded decoded) {
        return TryDecodePng(stream, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from a PNG stream with image decode options.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, out Pdf417Decoded decoded) {
        return TryDecodePng(stream, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from a PNG stream with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out Pdf417Decoded decoded) {
        return TryDecodeImage(image, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA), with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        return TryDecodeImage(image, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, out Pdf417Decoded decoded) {
        return TryDecodeImage(image, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, ImageDecodeOptions? options, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { decoded = null!; return false; }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) { decoded = null!; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { decoded = null!; return false; }
            return Pdf417Decoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) in a span.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, out Pdf417Decoded decoded) {
        return TryDecodeImage(image, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) in a span, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        return TryDecodeImage(image, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) in a span with image decode options.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, out Pdf417Decoded decoded) {
        return TryDecodeImage(image, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a Macro PDF417 symbol from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) in a span with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { decoded = null!; return false; }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) { decoded = null!; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { decoded = null!; return false; }
            return Pdf417Decoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

}
