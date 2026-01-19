using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class CodeGlyph {
    /// <summary>
    /// Attempts to decode a QR or 1D barcode from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = null!;
        if (png is null) throw new ArgumentNullException(nameof(png));
        if (cancellationToken.IsCancellationRequested) return false;
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        if (cancellationToken.IsCancellationRequested) return false;
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (cancellationToken.IsCancellationRequested) return false;
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from PNG bytes.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (png is null) throw new ArgumentNullException(nameof(png));
        if (cancellationToken.IsCancellationRequested) return false;
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        if (cancellationToken.IsCancellationRequested) return false;
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (cancellationToken.IsCancellationRequested) return false;
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = null!;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) return false;
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        if (cancellationToken.IsCancellationRequested) return false;
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) return false;
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        if (cancellationToken.IsCancellationRequested) return false;
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode a QR or 1D barcode from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = null!;
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) return false;
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, out decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from a PNG file.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) return false;
        var png = RenderIO.ReadBinary(path);
        return TryDecodeAllPng(png, out decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode a QR or 1D barcode from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = null!;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) return false;
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, out decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from a PNG stream.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) return false;
        var png = RenderIO.ReadBinary(stream);
        return TryDecodeAllPng(png, out decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }

    /// <summary>
    /// Decodes a QR or 1D barcode from PNG bytes.
    /// </summary>
    public static CodeGlyphDecoded DecodePng(byte[] png, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        if (!TryDecodePng(png, out var decoded, expectedBarcode, preferBarcode)) {
            throw new FormatException("PNG does not contain a decodable symbol.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a QR or 1D barcode from a PNG file.
    /// </summary>
    public static CodeGlyphDecoded DecodePngFile(string path, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        if (!TryDecodePngFile(path, out var decoded, expectedBarcode, preferBarcode)) {
            throw new FormatException("PNG file does not contain a decodable symbol.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a QR or 1D barcode from a PNG stream.
    /// </summary>
    public static CodeGlyphDecoded DecodePng(Stream stream, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        if (!TryDecodePng(stream, out var decoded, expectedBarcode, preferBarcode)) {
            throw new FormatException("PNG stream does not contain a decodable symbol.");
        }
        return decoded;
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels using a single options object.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0) || stride != width * 4) {
            return TryDecode(pixels, width, height, stride, format, out decoded, expected, prefer, qr, token, barcode);
        }

        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = null!; return false; }
            var buffer = pixels;
            var w = width;
            var h = height;
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, budgetToken)) { decoded = null!; return false; }
            return TryDecode(buffer, w, h, w * 4, format, out decoded, expected, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels using a single options object, with diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, CodeGlyphDecodeOptions? options) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        var expected = options?.ExpectedBarcode;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0) || stride != width * 4) {
            return TryDecode(pixels, width, height, stride, format, out decoded, out diagnostics, expected, prefer, qr, token, barcode);
        }

        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = null!; diagnostics.Failure = "Cancelled."; return false; }
            var buffer = pixels;
            var w = width;
            var h = height;
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, budgetToken)) {
                decoded = null!;
                diagnostics.Failure ??= budgetToken.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            return TryDecode(buffer, w, h, w * 4, format, out decoded, out diagnostics, expected, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode all symbols from raw pixels using a single options object.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var include = options?.IncludeBarcode ?? true;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0) || stride != width * 4) {
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, expected, include, prefer, qr, token, barcode);
        }

        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            var buffer = pixels;
            var w = width;
            var h = height;
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, budgetToken)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            return TryDecodeAll(buffer, w, h, w * 4, format, out decoded, expected, include, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode all symbols from raw pixels using a single options object, with diagnostics.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, out CodeGlyphDecodeDiagnostics diagnostics, CodeGlyphDecodeOptions? options) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        var expected = options?.ExpectedBarcode;
        var include = options?.IncludeBarcode ?? true;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0) || stride != width * 4) {
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, out diagnostics, expected, include, prefer, qr, token, barcode);
        }

        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = Array.Empty<CodeGlyphDecoded>(); diagnostics.Failure = "Cancelled."; return false; }
            var buffer = pixels;
            var w = width;
            var h = height;
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, budgetToken)) {
                decoded = Array.Empty<CodeGlyphDecoded>();
                diagnostics.Failure ??= budgetToken.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            return TryDecodeAll(buffer, w, h, w * 4, format, out decoded, out diagnostics, expected, include, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels using a single options object.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (imageOptions is null || imageOptions.MaxMilliseconds <= 0) {
            return TryDecode(pixels, width, height, stride, format, out decoded, expected, prefer, qr, token, barcode);
        }

        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            return TryDecode(pixels, width, height, stride, format, out decoded, expected, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode all symbols from raw pixels using a single options object.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var include = options?.IncludeBarcode ?? true;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (imageOptions is null || imageOptions.MaxMilliseconds <= 0) {
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, expected, include, prefer, qr, token, barcode);
        }

        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, expected, include, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode all symbols from raw pixels using a single options object, with diagnostics.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, out CodeGlyphDecodeDiagnostics diagnostics, CodeGlyphDecodeOptions? options) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        var expected = options?.ExpectedBarcode;
        var include = options?.IncludeBarcode ?? true;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (imageOptions is null || imageOptions.MaxMilliseconds <= 0) {
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, out diagnostics, expected, include, prefer, qr, token, barcode);
        }

        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = Array.Empty<CodeGlyphDecoded>(); diagnostics.Failure = "Cancelled."; return false; }
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, out diagnostics, expected, include, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels using a single options object, with diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, CodeGlyphDecodeOptions? options) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        var expected = options?.ExpectedBarcode;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (imageOptions is null || imageOptions.MaxMilliseconds <= 0) {
            return TryDecode(pixels, width, height, stride, format, out decoded, out diagnostics, expected, prefer, qr, token, barcode);
        }

        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = null!; diagnostics.Failure = "Cancelled."; return false; }
            return TryDecode(pixels, width, height, stride, format, out decoded, out diagnostics, expected, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }
#endif

    /// <summary>
    /// Attempts to decode a QR or barcode from PNG bytes using a single options object.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (png is null) throw new ArgumentNullException(nameof(png));
        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = null!; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, budgetToken)) { decoded = null!; return false; }
            return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expected, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from PNG bytes using a single options object, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, CodeGlyphDecodeOptions? options) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        var expected = options?.ExpectedBarcode;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (png is null) throw new ArgumentNullException(nameof(png));
        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = null!; diagnostics.Failure = "Cancelled."; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, budgetToken)) {
                decoded = null!;
                diagnostics.Failure ??= budgetToken.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out diagnostics, expected, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from common image formats using a single options object.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (image is null) throw new ArgumentNullException(nameof(image));
        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = null!; return false; }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) { decoded = null!; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, budgetToken)) { decoded = null!; return false; }
            return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expected, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from common image formats using a single options object, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, CodeGlyphDecodeOptions? options) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        var expected = options?.ExpectedBarcode;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (image is null) throw new ArgumentNullException(nameof(image));
        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = null!; diagnostics.Failure = "Cancelled."; return false; }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
                decoded = null!;
                diagnostics.Failure ??= "Unsupported image format.";
                return false;
            }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, budgetToken)) {
                decoded = null!;
                diagnostics.Failure ??= budgetToken.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out diagnostics, expected, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode all symbols from PNG bytes using a single options object.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var include = options?.IncludeBarcode ?? true;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (png is null) throw new ArgumentNullException(nameof(png));
        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, budgetToken)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expected, include, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode all symbols from common image formats using a single options object.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var include = options?.IncludeBarcode ?? true;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (image is null) throw new ArgumentNullException(nameof(image));
        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, budgetToken)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expected, include, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from an image stream using a single options object.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = null!; return false; }
            var data = RenderIO.ReadBinary(stream);
            if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) { decoded = null!; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, budgetToken)) { decoded = null!; return false; }
            return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expected, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode all symbols from an image stream using a single options object.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        var expected = options?.ExpectedBarcode;
        var include = options?.IncludeBarcode ?? true;
        var prefer = options?.PreferBarcode ?? false;
        var qr = options?.Qr;
        var token = options is null ? default : options.CancellationToken;
        var barcode = options?.Barcode;
        var imageOptions = options?.Image;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var budgetToken = ImageDecodeHelper.ApplyBudget(token, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            var data = RenderIO.ReadBinary(stream);
            if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, imageOptions, budgetToken)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expected, include, prefer, qr, budgetToken, barcode);
        } finally {
            budgetCts?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from a PNG file using a single options object.
    /// </summary>
    public static bool TryDecodePngFile(string path, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode all symbols from a PNG file using a single options object.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodeAllPng(png, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from a PNG stream using a single options object.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode all symbols from a PNG stream using a single options object.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodeAllPng(png, out decoded, options);
    }
}
