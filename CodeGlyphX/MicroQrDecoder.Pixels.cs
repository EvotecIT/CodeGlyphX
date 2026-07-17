using System;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class MicroQrDecoder {
#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to recognize and decode a Micro QR symbol from RGBA32 or BGRA32 pixels.
    /// </summary>
    public static bool TryDecode(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        out MicroQrDecoded decoded) {
        return MicroQrPixelDecoder.TryDecode(pixels, width, height, stride, format, CancellationToken.None, out decoded, out _);
    }

    /// <summary>
    /// Attempts to recognize and decode a Micro QR symbol from RGBA32 or BGRA32 pixels, with cancellation.
    /// </summary>
    public static bool TryDecode(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        CancellationToken cancellationToken,
        out MicroQrDecoded decoded) {
        return MicroQrPixelDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out decoded, out _);
    }

    /// <summary>
    /// Attempts to recognize and decode a Micro QR symbol from RGBA32 or BGRA32 pixels and returns recognition metadata.
    /// </summary>
    public static bool TryDecode(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info) {
        return MicroQrPixelDecoder.TryDecode(pixels, width, height, stride, format, CancellationToken.None, out decoded, out info);
    }

    /// <summary>
    /// Attempts to recognize and decode a Micro QR symbol from RGBA32 or BGRA32 pixels, with cancellation and recognition metadata.
    /// </summary>
    public static bool TryDecode(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        CancellationToken cancellationToken,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info) {
        return MicroQrPixelDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out decoded, out info);
    }
#endif

    /// <summary>
    /// Attempts to recognize and decode a Micro QR symbol from RGBA32 or BGRA32 pixels.
    /// </summary>
    public static bool TryDecode(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        out MicroQrDecoded decoded) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return MicroQrPixelDecoder.TryDecode(pixels, width, height, stride, format, CancellationToken.None, out decoded, out _);
    }

    /// <summary>
    /// Attempts to recognize and decode a Micro QR symbol from RGBA32 or BGRA32 pixels, with cancellation.
    /// </summary>
    public static bool TryDecode(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        CancellationToken cancellationToken,
        out MicroQrDecoded decoded) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return MicroQrPixelDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out decoded, out _);
    }

    /// <summary>
    /// Attempts to recognize and decode a Micro QR symbol from RGBA32 or BGRA32 pixels and returns recognition metadata.
    /// </summary>
    public static bool TryDecode(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return MicroQrPixelDecoder.TryDecode(pixels, width, height, stride, format, CancellationToken.None, out decoded, out info);
    }

    /// <summary>
    /// Attempts to recognize and decode a Micro QR symbol from RGBA32 or BGRA32 pixels, with cancellation and recognition metadata.
    /// </summary>
    public static bool TryDecode(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        CancellationToken cancellationToken,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return MicroQrPixelDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out decoded, out info);
    }

    /// <summary>
    /// Attempts to decode an encoded raster image supported by the managed image reader and recognize a Micro QR symbol.
    /// </summary>
    public static bool TryDecodeImage(
        byte[] encodedImage,
        out MicroQrDecoded decoded,
        ImageDecodeOptions? options = null,
        CancellationToken cancellationToken = default) {
        return TryDecodeImage(encodedImage, out decoded, out _, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to decode an encoded raster image supported by the managed image reader, recognize a Micro QR symbol, and return recognition metadata.
    /// </summary>
    public static bool TryDecodeImage(
        byte[] encodedImage,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info,
        ImageDecodeOptions? options = null,
        CancellationToken cancellationToken = default) {
        if (encodedImage is null) throw new ArgumentNullException(nameof(encodedImage));
        decoded = null!;
        info = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        if (!ImageReader.TryDecodeRgba32(encodedImage, ResolveSourceImageDecodeOptions(options), out var rgba, out var width, out var height)) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        var sourceWidth = width;
        var sourceHeight = height;
        if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, cancellationToken)) return false;
        using (ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, options, out var recognitionToken)) {
            if (!MicroQrPixelDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, recognitionToken, out decoded, out var decodedInfo)) return false;
            info = MapInfoToSource(decodedInfo, sourceWidth, sourceHeight, width, height);
            return true;
        }
    }

    private static ImageDecodeOptions? ResolveSourceImageDecodeOptions(ImageDecodeOptions? options) {
        if (options is null || options.MaxDimension <= 0) return options;
        return new ImageDecodeOptions {
            MaxDimension = 0,
            MaxPixels = options.MaxPixels,
            MaxBytes = options.MaxBytes,
            RecognitionBudgetMilliseconds = options.RecognitionBudgetMilliseconds,
            MaxAnimationFrames = options.MaxAnimationFrames,
            MaxAnimationDurationMs = options.MaxAnimationDurationMs,
            MaxAnimationFramePixels = options.MaxAnimationFramePixels,
            JpegOptions = options.JpegOptions
        };
    }

    private static MicroQrPixelDecodeInfo MapInfoToSource(
        MicroQrPixelDecodeInfo info,
        int sourceWidth,
        int sourceHeight,
        int decodedWidth,
        int decodedHeight) {
        if (sourceWidth == decodedWidth && sourceHeight == decodedHeight) return info;
        var geometry = info.Geometry;
        return new MicroQrPixelDecodeInfo(
            new SymbolGeometry(
                MapPointToSource(geometry.TopLeft, sourceWidth, sourceHeight, decodedWidth, decodedHeight),
                MapPointToSource(geometry.TopRight, sourceWidth, sourceHeight, decodedWidth, decodedHeight),
                MapPointToSource(geometry.BottomRight, sourceWidth, sourceHeight, decodedWidth, decodedHeight),
                MapPointToSource(geometry.BottomLeft, sourceWidth, sourceHeight, decodedWidth, decodedHeight)),
            info.IsInverted,
            info.IsMirrored,
            info.Threshold);
    }

    private static SymbolPoint MapPointToSource(
        SymbolPoint point,
        int sourceWidth,
        int sourceHeight,
        int decodedWidth,
        int decodedHeight) {
        return new SymbolPoint(
            point.X * sourceWidth / decodedWidth,
            point.Y * sourceHeight / decodedHeight);
    }
}
