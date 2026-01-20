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
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0)) {
            return TryDecode(pixels, width, height, stride, format, out decoded, expected, prefer, qr, token, barcode);
        }

        var buffer = pixels;
        var w = width;
        var h = height;
        var outStride = stride;
        if (imageOptions.MaxDimension > 0 && stride == width * 4) {
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, token)) { decoded = null!; return false; }
            outStride = w * 4;
        }
        return TryDecodeWithImageBudget(buffer, w, h, outStride, format, out decoded, expected, prefer, qr, token, barcode, imageOptions);
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
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0)) {
            return TryDecode(pixels, width, height, stride, format, out decoded, out diagnostics, expected, prefer, qr, token, barcode);
        }

        var buffer = pixels;
        var w = width;
        var h = height;
        var outStride = stride;
        if (imageOptions.MaxDimension > 0 && stride == width * 4) {
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, token)) {
                decoded = null!;
                diagnostics.Failure ??= token.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            outStride = w * 4;
        }
        return TryDecodeWithImageBudget(buffer, w, h, outStride, format, out decoded, out diagnostics, expected, prefer, qr, token, barcode, imageOptions);
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
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0)) {
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, expected, include, prefer, qr, token, barcode);
        }

        var buffer = pixels;
        var w = width;
        var h = height;
        var outStride = stride;
        if (imageOptions.MaxDimension > 0 && stride == width * 4) {
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, token)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            outStride = w * 4;
        }
        return TryDecodeAllWithImageBudget(buffer, w, h, outStride, format, out decoded, expected, include, prefer, qr, token, barcode, imageOptions);
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
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0)) {
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, out diagnostics, expected, include, prefer, qr, token, barcode);
        }

        var buffer = pixels;
        var w = width;
        var h = height;
        var outStride = stride;
        if (imageOptions.MaxDimension > 0 && stride == width * 4) {
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, token)) {
                decoded = Array.Empty<CodeGlyphDecoded>();
                diagnostics.Failure ??= token.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            outStride = w * 4;
        }
        return TryDecodeAllWithImageBudget(buffer, w, h, outStride, format, out decoded, out diagnostics, expected, include, prefer, qr, token, barcode, imageOptions);
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
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0)) {
            return TryDecode(pixels, width, height, stride, format, out decoded, expected, prefer, qr, token, barcode);
        }

        if (imageOptions.MaxDimension > 0 && stride == width * 4) {
            var buffer = pixels.ToArray();
            var w = width;
            var h = height;
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, token)) { decoded = null!; return false; }
            return TryDecodeWithImageBudget(buffer, w, h, w * 4, format, out decoded, expected, prefer, qr, token, barcode, imageOptions);
        }

        var rawBuffer = pixels.ToArray();
        return TryDecodeWithImageBudget(rawBuffer, width, height, stride, format, out decoded, expected, prefer, qr, token, barcode, imageOptions);
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
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0)) {
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, expected, include, prefer, qr, token, barcode);
        }

        if (imageOptions.MaxDimension > 0 && stride == width * 4) {
            var buffer = pixels.ToArray();
            var w = width;
            var h = height;
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, token)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
            return TryDecodeAllWithImageBudget(buffer, w, h, w * 4, format, out decoded, expected, include, prefer, qr, token, barcode, imageOptions);
        }

        var rawBuffer = pixels.ToArray();
        return TryDecodeAllWithImageBudget(rawBuffer, width, height, stride, format, out decoded, expected, include, prefer, qr, token, barcode, imageOptions);
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
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0)) {
            return TryDecodeAll(pixels, width, height, stride, format, out decoded, out diagnostics, expected, include, prefer, qr, token, barcode);
        }

        if (imageOptions.MaxDimension > 0 && stride == width * 4) {
            var buffer = pixels.ToArray();
            var w = width;
            var h = height;
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, token)) {
                decoded = Array.Empty<CodeGlyphDecoded>();
                diagnostics.Failure ??= token.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            return TryDecodeAllWithImageBudget(buffer, w, h, w * 4, format, out decoded, out diagnostics, expected, include, prefer, qr, token, barcode, imageOptions);
        }

        var rawBuffer = pixels.ToArray();
        return TryDecodeAllWithImageBudget(rawBuffer, width, height, stride, format, out decoded, out diagnostics, expected, include, prefer, qr, token, barcode, imageOptions);
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
        if (imageOptions is null || (imageOptions.MaxDimension <= 0 && imageOptions.MaxMilliseconds <= 0)) {
            return TryDecode(pixels, width, height, stride, format, out decoded, out diagnostics, expected, prefer, qr, token, barcode);
        }

        if (imageOptions.MaxDimension > 0 && stride == width * 4) {
            var buffer = pixels.ToArray();
            var w = width;
            var h = height;
            if (!ImageDecodeHelper.TryDownscale(ref buffer, ref w, ref h, imageOptions, token)) {
                decoded = null!;
                diagnostics.Failure ??= token.IsCancellationRequested ? "Cancelled." : "Image downscale failed.";
                return false;
            }
            return TryDecodeWithImageBudget(buffer, w, h, w * 4, format, out decoded, out diagnostics, expected, prefer, qr, token, barcode, imageOptions);
        }

        var rawBuffer = pixels.ToArray();
        return TryDecodeWithImageBudget(rawBuffer, width, height, stride, format, out decoded, out diagnostics, expected, prefer, qr, token, barcode, imageOptions);
    }
#endif

    /// <summary>
    /// Attempts to decode a QR or barcode from PNG bytes using a single options object.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from PNG bytes using a single options object, with diagnostics.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, CodeGlyphDecodeOptions? options) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out diagnostics, options);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from common image formats using a single options object.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) { decoded = null!; return false; }
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from common image formats using a single options object, with diagnostics.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out CodeGlyphDecoded decoded, out CodeGlyphDecodeDiagnostics diagnostics, CodeGlyphDecodeOptions? options) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) {
            decoded = null!;
            diagnostics.Failure = "Unsupported image format.";
            return false;
        }
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, out diagnostics, options);
    }

    /// <summary>
    /// Attempts to decode all symbols from PNG bytes using a single options object.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode all symbols from common image formats using a single options object.
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from an image stream using a single options object.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) { decoded = null!; return false; }
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode all symbols from an image stream using a single options object.
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var data = RenderIO.ReadBinary(stream);
        if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) { decoded = Array.Empty<CodeGlyphDecoded>(); return false; }
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options);
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

    private static bool TryWithImageBudget(ImageDecodeOptions? imageOptions, CancellationToken cancellationToken, Func<CancellationToken, bool> action) {
        if (cancellationToken.IsCancellationRequested) return false;
        if (imageOptions is null || imageOptions.MaxMilliseconds <= 0) return action(cancellationToken);

        var budgetToken = ImageDecodeHelper.ApplyBudget(cancellationToken, imageOptions, out var budgetCts);
        try {
            if (budgetToken.IsCancellationRequested) return false;
            return action(budgetToken);
        } finally {
            budgetCts?.Dispose();
        }
    }

    private static bool TryDecodeWithImageBudget(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        out CodeGlyphDecoded decoded,
        BarcodeType? expectedBarcode,
        bool preferBarcode,
        QrPixelDecodeOptions? qrOptions,
        CancellationToken cancellationToken,
        BarcodeDecodeOptions? barcodeOptions,
        ImageDecodeOptions? imageOptions) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;

        if (preferBarcode) {
            BarcodeDecoded barcode = null!;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out barcode))) {
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            var aztec = string.Empty;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => AztecDecoder.TryDecode(pixels, width, height, stride, format, token, out aztec))) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr, qrOptions, cancellationToken)) {
                decoded = new CodeGlyphDecoded(qr);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            var dataMatrix = string.Empty;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, token, out dataMatrix))) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                return true;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            var pdf417 = string.Empty;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => Pdf417Decoder.TryDecode(pixels, width, height, stride, format, token, out pdf417))) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                return true;
            }
            return false;
        }

        var aztecDecoded = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => AztecDecoder.TryDecode(pixels, width, height, stride, format, token, out aztecDecoded))) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, qrOptions, cancellationToken)) {
            decoded = new CodeGlyphDecoded(qrDecoded);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        var dataMatrixDecoded = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, token, out dataMatrixDecoded))) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        var pdf417Decoded = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => Pdf417Decoder.TryDecode(pixels, width, height, stride, format, token, out pdf417Decoded))) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        BarcodeDecoded barcodeDecoded = null!;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out barcodeDecoded))) {
            decoded = new CodeGlyphDecoded(barcodeDecoded);
            return true;
        }
        return false;
    }

    private static bool TryDecodeWithImageBudget(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        out CodeGlyphDecoded decoded,
        out CodeGlyphDecodeDiagnostics diagnostics,
        BarcodeType? expectedBarcode,
        bool preferBarcode,
        QrPixelDecodeOptions? qrOptions,
        CancellationToken cancellationToken,
        BarcodeDecodeOptions? barcodeOptions,
        ImageDecodeOptions? imageOptions) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }

        if (preferBarcode) {
            var barcodeDiag = new BarcodeDecodeDiagnostics();
            BarcodeDecoded barcode = null!;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out barcode, out barcodeDiag))) {
                diagnostics.Barcode = barcodeDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            diagnostics.Barcode = barcodeDiag;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            var aztecDiag = new AztecDecodeDiagnostics();
            var aztec = string.Empty;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => AztecDecoder.TryDecode(pixels, width, height, stride, format, token, out aztec, out aztecDiag))) {
                diagnostics.Aztec = aztecDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Aztec;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                return true;
            }
            diagnostics.Aztec = aztecDiag;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr, out var qrInfo, qrOptions, cancellationToken)) {
                diagnostics.Qr = qrInfo;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Qr;
                decoded = new CodeGlyphDecoded(qr);
                return true;
            }
            diagnostics.Qr = qrInfo;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            var dmDiag = new DataMatrixDecodeDiagnostics();
            var dataMatrix = string.Empty;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, token, out dataMatrix, out dmDiag))) {
                diagnostics.DataMatrix = dmDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                return true;
            }
            diagnostics.DataMatrix = dmDiag;

            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            var pdfDiag = new Pdf417DecodeDiagnostics();
            var pdf417 = string.Empty;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => Pdf417Decoder.TryDecode(pixels, width, height, stride, format, token, out pdf417, out pdfDiag))) {
                diagnostics.Pdf417 = pdfDiag;
                diagnostics.Success = true;
                diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                return true;
            }
            diagnostics.Pdf417 = pdfDiag;

            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }

        var aztecDiag0 = new AztecDecodeDiagnostics();
        var aztecDecoded = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => AztecDecoder.TryDecode(pixels, width, height, stride, format, token, out aztecDecoded, out aztecDiag0))) {
            diagnostics.Aztec = aztecDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Aztec;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
            return true;
        }
        diagnostics.Aztec = aztecDiag0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded, out var qrInfo0, qrOptions, cancellationToken)) {
            diagnostics.Qr = qrInfo0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Qr;
            decoded = new CodeGlyphDecoded(qrDecoded);
            return true;
        }
        diagnostics.Qr = qrInfo0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var dmDiag0 = new DataMatrixDecodeDiagnostics();
        var dataMatrixDecoded = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, token, out dataMatrixDecoded, out dmDiag0))) {
            diagnostics.DataMatrix = dmDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.DataMatrix;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
            return true;
        }
        diagnostics.DataMatrix = dmDiag0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var pdfDiag0 = new Pdf417DecodeDiagnostics();
        var pdf417Decoded = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => Pdf417Decoder.TryDecode(pixels, width, height, stride, format, token, out pdf417Decoded, out pdfDiag0))) {
            diagnostics.Pdf417 = pdfDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Pdf417;
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
            return true;
        }
        diagnostics.Pdf417 = pdfDiag0;

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var barcodeDiag0 = new BarcodeDecodeDiagnostics();
        BarcodeDecoded barcodeDecoded = null!;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out barcodeDecoded, out barcodeDiag0))) {
            diagnostics.Barcode = barcodeDiag0;
            diagnostics.Success = true;
            diagnostics.SuccessKind = CodeGlyphKind.Barcode1D;
            decoded = new CodeGlyphDecoded(barcodeDecoded);
            return true;
        }
        diagnostics.Barcode = barcodeDiag0;
        diagnostics.Failure ??= "No symbol decoded.";
        return false;
    }

    private static bool TryDecodeAllWithImageBudget(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        out CodeGlyphDecoded[] decoded,
        BarcodeType? expectedBarcode,
        bool includeBarcode,
        bool preferBarcode,
        QrPixelDecodeOptions? qrOptions,
        CancellationToken cancellationToken,
        BarcodeDecodeOptions? barcodeOptions,
        ImageDecodeOptions? imageOptions) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (cancellationToken.IsCancellationRequested) return false;

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            BarcodeDecoded barcode = null!;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out barcode))) {
                list.Add(new CodeGlyphDecoded(barcode));
            }
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, qrOptions, cancellationToken)) {
            for (var i = 0; i < qrResults.Length; i++) {
                list.Add(new CodeGlyphDecoded(qrResults[i]));
            }
        }

        if (cancellationToken.IsCancellationRequested) return false;
        var aztec = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => AztecDecoder.TryDecode(pixels, width, height, stride, format, token, out aztec))) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
        }

        if (cancellationToken.IsCancellationRequested) return false;
        var dataMatrix = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, token, out dataMatrix))) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
        }

        if (cancellationToken.IsCancellationRequested) return false;
        var pdf417 = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => Pdf417Decoder.TryDecode(pixels, width, height, stride, format, token, out pdf417))) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
        }

        if (includeBarcode && !preferBarcode) {
            BarcodeDecoded barcode = null!;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out barcode))) {
                list.Add(new CodeGlyphDecoded(barcode));
            }
        }

        if (list.Count == 0) return false;
        decoded = list.ToArray();
        return true;
    }

    private static bool TryDecodeAllWithImageBudget(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        out CodeGlyphDecoded[] decoded,
        out CodeGlyphDecodeDiagnostics diagnostics,
        BarcodeType? expectedBarcode,
        bool includeBarcode,
        bool preferBarcode,
        QrPixelDecodeOptions? qrOptions,
        CancellationToken cancellationToken,
        BarcodeDecodeOptions? barcodeOptions,
        ImageDecodeOptions? imageOptions) {
        diagnostics = new CodeGlyphDecodeDiagnostics();
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            var barcodeDiag = new BarcodeDecodeDiagnostics();
            BarcodeDecoded barcode = null!;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out barcode, out barcodeDiag))) {
                diagnostics.Barcode = barcodeDiag;
                list.Add(new CodeGlyphDecoded(barcode));
            } else {
                diagnostics.Barcode = barcodeDiag;
            }
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, out var qrInfo, qrOptions, cancellationToken)) {
            diagnostics.Qr = qrInfo;
            for (var i = 0; i < qrResults.Length; i++) {
                list.Add(new CodeGlyphDecoded(qrResults[i]));
            }
        } else {
            diagnostics.Qr = qrInfo;
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var aztecDiag = new AztecDecodeDiagnostics();
        var aztec = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => AztecDecoder.TryDecode(pixels, width, height, stride, format, token, out aztec, out aztecDiag))) {
            diagnostics.Aztec = aztecDiag;
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
        } else {
            diagnostics.Aztec = aztecDiag;
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var dmDiag = new DataMatrixDecodeDiagnostics();
        var dataMatrix = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, token, out dataMatrix, out dmDiag))) {
            diagnostics.DataMatrix = dmDiag;
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
        } else {
            diagnostics.DataMatrix = dmDiag;
        }

        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        var pdfDiag = new Pdf417DecodeDiagnostics();
        var pdf417 = string.Empty;
        if (TryWithImageBudget(imageOptions, cancellationToken, token => Pdf417Decoder.TryDecode(pixels, width, height, stride, format, token, out pdf417, out pdfDiag))) {
            diagnostics.Pdf417 = pdfDiag;
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
        } else {
            diagnostics.Pdf417 = pdfDiag;
        }

        if (includeBarcode && !preferBarcode) {
            var barcodeDiag = new BarcodeDecodeDiagnostics();
            BarcodeDecoded barcode = null!;
            if (TryWithImageBudget(imageOptions, cancellationToken, token => BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, barcodeOptions, token, out barcode, out barcodeDiag))) {
                diagnostics.Barcode = barcodeDiag;
                list.Add(new CodeGlyphDecoded(barcode));
            } else {
                diagnostics.Barcode = barcodeDiag;
            }
        }

        if (list.Count == 0) {
            diagnostics.Failure ??= "No symbol decoded.";
            return false;
        }
        diagnostics.Success = true;
        diagnostics.SuccessKind = list.Count == 1 ? list[0].Kind : null;
        decoded = list.ToArray();
        return true;
    }

}
