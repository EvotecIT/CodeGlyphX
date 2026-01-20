using System;
using System.IO;
using System.Threading;
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
    public static bool TryDecodePng(byte[] png, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        return TryDecodePng(png, expectedType, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint and image decode options.
    /// </summary>
    public static bool TryDecodePng(byte[] png, BarcodeType? expectedType, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodePng(png, expectedType, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, BarcodeType? expectedType, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodePng(png, expectedType, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint and image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(byte[] png, BarcodeType? expectedType, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { decoded = null!; return false; }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            var original = rgba;
            var originalWidth = width;
            var originalHeight = height;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { decoded = null!; return false; }
            if (BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType, options: null, token, out decoded)) return true;
            if (!ReferenceEquals(rgba, original) && !token.IsCancellationRequested) {
                return BarcodeDecoder.TryDecode(original, originalWidth, originalHeight, originalWidth * 4, PixelFormat.Rgba32, expectedType, options: null, token, out decoded);
            }
            decoded = null!;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
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
    public static bool TryDecodeImage(byte[] image, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint and image decode options.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, BarcodeType? expectedType, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, expectedType, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, BarcodeType? expectedType, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, expectedType, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint and image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, BarcodeType? expectedType, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
            var original = rgba;
            var originalWidth = width;
            var originalHeight = height;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) return false;
            if (BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType, options: null, token, out decoded)) return true;
            if (!ReferenceEquals(rgba, original) && !token.IsCancellationRequested) {
                return BarcodeDecoder.TryDecode(original, originalWidth, originalHeight, originalWidth * 4, PixelFormat.Rgba32, expectedType, options: null, token, out decoded);
            }
            decoded = null!;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
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
    public static bool TryDecodeImage(Stream stream, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        decoded = null!;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint and image decode options.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, BarcodeType? expectedType, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodeImage(stream, expectedType, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, BarcodeType? expectedType, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeImage(stream, expectedType, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint and image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, BarcodeType? expectedType, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
            var original = rgba;
            var originalWidth = width;
            var originalHeight = height;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) return false;
            if (BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType, options: null, token, out decoded)) return true;
            if (!ReferenceEquals(rgba, original) && !token.IsCancellationRequested) {
                return BarcodeDecoder.TryDecode(original, originalWidth, originalHeight, originalWidth * 4, PixelFormat.Rgba32, expectedType, options: null, token, out decoded);
            }
            decoded = null!;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out BarcodeDecoded decoded) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file with image decode options.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodePngFile(path, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePngFile(string path, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, null, options, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out BarcodeDecoded decoded) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, null, cancellationToken, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream with image decode options.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodePng(stream, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream with image decode options, with cancellation.
    /// </summary>
    public static bool TryDecodePng(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, null, options, cancellationToken, out decoded);
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

    private static BarcodePngRenderOptions BuildPngOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodePngRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            Foreground = opts.Foreground,
            Background = opts.Background,
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = opts.LabelColor,
        };
    }

    private static IcoRenderOptions BuildIcoOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new IcoRenderOptions {
            Sizes = opts.IcoSizes ?? new[] { 16, 32, 48, 64, 128, 256 },
            PreserveAspectRatio = opts.IcoPreserveAspectRatio
        };
    }

    private static BarcodeSvgRenderOptions BuildSvgOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodeSvgRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            BarColor = ColorUtils.ToCss(opts.Foreground),
            BackgroundColor = ColorUtils.ToCss(opts.Background),
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = ColorUtils.ToCss(opts.LabelColor),
            LabelFontFamily = opts.LabelFontFamily,
        };
    }

    private static BarcodeHtmlRenderOptions BuildHtmlOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodeHtmlRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            BarColor = ColorUtils.ToCss(opts.Foreground),
            BackgroundColor = ColorUtils.ToCss(opts.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable,
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = ColorUtils.ToCss(opts.LabelColor),
            LabelFontFamily = opts.LabelFontFamily,
        };
    }


}
