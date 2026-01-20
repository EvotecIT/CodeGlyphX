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
    /// Attempts to decode a QR code from a PNG byte array.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out QrDecoded decoded) {
        return TryDecodePng(png, options: null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG byte array with options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return TryDecodePng(png, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG byte array with options and cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (png is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG byte array with image decode options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return TryDecodePng(png, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG byte array with image decode options and cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        return TryDecodePngCore(png, imageOptions, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG byte array with image decode options, diagnostics, and profile options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodePng(png, imageOptions, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG byte array with image decode options, diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        return TryDecodePngCore(png, imageOptions, options, cancellationToken, out decoded, out info);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG byte array.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, out QrDecoded[] decoded) {
        return TryDecodeAllPng(png, options: null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG byte array with options.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
        return TryDecodeAllPng(png, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG byte array with diagnostics and options.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodeAllPng(png, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG byte array with diagnostics, options, and cancellation.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        decoded = Array.Empty<QrDecoded>();
        info = default;
        if (png is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG byte array with options and cancellation.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (png is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG byte array with image decode options.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
        return TryDecodeAllPng(png, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG byte array with image decode options and cancellation.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        return TryDecodeAllPngCore(png, imageOptions, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG byte array with image decode options, diagnostics, and profile options.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, ImageDecodeOptions? imageOptions, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodeAllPng(png, imageOptions, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG byte array with image decode options, diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, ImageDecodeOptions? imageOptions, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        return TryDecodeAllPngCore(png, imageOptions, options, cancellationToken, out decoded, out info);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out QrDecoded decoded) {
        decoded = null!;
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodePng(data, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG file with options and cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodePng(data, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG file with image decode options, diagnostics, and profile options.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodePngFile(path, imageOptions, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG file with image decode options, diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        decoded = null!;
        info = default;
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodePng(data, imageOptions, out decoded, out info, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG file with image decode options.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return TryDecodePngFile(path, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG file with image decode options and cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodePng(data, imageOptions, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG file.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodeAllPng(data, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG file with options and cancellation.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodeAllPng(data, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG file with diagnostics and options.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodeAllPngFile(path, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG file with diagnostics, options, and cancellation.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        decoded = Array.Empty<QrDecoded>();
        info = default;
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodeAllPng(data, out decoded, out info, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG file with image decode options.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded[] decoded) {
        return TryDecodeAllPngFile(path, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG file with image decode options and cancellation.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodeAllPng(data, imageOptions, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG file with image decode options, diagnostics, and profile options.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, ImageDecodeOptions? imageOptions, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodeAllPngFile(path, imageOptions, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG file with image decode options, diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, ImageDecodeOptions? imageOptions, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        decoded = Array.Empty<QrDecoded>();
        info = default;
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.TryReadBinary(out var data)) return false;
        return TryDecodeAllPng(data, imageOptions, out decoded, out info, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out QrDecoded decoded) {
        decoded = null!;
        if (stream is null) return false;
        var data = stream.ReadBinary();
        return TryDecodePng(data, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG stream with options and cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (stream is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var data = stream.ReadBinary();
        return TryDecodePng(data, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG stream with image decode options, diagnostics, and profile options.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodePng(stream, imageOptions, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG stream with image decode options, diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? imageOptions, out QrDecoded decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        decoded = null!;
        info = default;
        if (stream is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var data = stream.ReadBinary();
        return TryDecodePng(data, imageOptions, out decoded, out info, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG stream with image decode options.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, out QrDecoded decoded) {
        return TryDecodePng(stream, imageOptions, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a QR code from a PNG stream with image decode options and cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (stream is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var data = stream.ReadBinary();
        return TryDecodePng(data, imageOptions, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG stream with diagnostics and options.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodeAllPng(stream, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG stream with diagnostics, options, and cancellation.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        decoded = Array.Empty<QrDecoded>();
        info = default;
        if (stream is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var data = stream.ReadBinary();
        return TryDecodeAllPng(data, out decoded, out info, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG stream with image decode options, diagnostics, and profile options.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, ImageDecodeOptions? imageOptions, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options = null) {
        return TryDecodeAllPng(stream, imageOptions, out decoded, out info, options, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to decode all QR codes from a PNG stream with image decode options, diagnostics, profile options, and cancellation.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, ImageDecodeOptions? imageOptions, out QrDecoded[] decoded, out QrPixelDecodeInfo info, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        decoded = Array.Empty<QrDecoded>();
        info = default;
        if (stream is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var data = stream.ReadBinary();
        return TryDecodeAllPng(data, imageOptions, out decoded, out info, options, cancellationToken);
    }

    private static bool TryDecodePngCore(byte[] png, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded) {
        decoded = null!;
        if (png is null) return false;
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options, token);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodePngCore(byte[] png, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded decoded, out QrPixelDecodeInfo info) {
        decoded = null!;
        info = default;
        if (png is null) return false;
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options, token);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeAllPngCore(byte[] png, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded) {
        decoded = Array.Empty<QrDecoded>();
        if (png is null) return false;
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options, token);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool TryDecodeAllPngCore(byte[] png, ImageDecodeOptions? imageOptions, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] decoded, out QrPixelDecodeInfo info) {
        decoded = Array.Empty<QrDecoded>();
        info = default;
        if (png is null) return false;
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, token)) return false;
            return QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out info, options, token);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }
}
