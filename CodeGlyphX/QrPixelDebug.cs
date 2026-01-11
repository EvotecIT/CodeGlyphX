using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Debug visualizations for pixel-based QR decoding.
/// </summary>
public static class QrPixelDebug {
    /// <summary>
    /// Renders a debug visualization to a PNG byte array.
    /// </summary>
    public static byte[] RenderPng(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDebugMode mode, QrPixelDebugOptions? options = null) {
#if NET8_0_OR_GREATER
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return RenderPng((ReadOnlySpan<byte>)pixels, width, height, stride, format, mode, options);
#else
        _ = pixels;
        _ = width;
        _ = height;
        _ = stride;
        _ = format;
        _ = mode;
        _ = options;
        throw new PlatformNotSupportedException("QR pixel debug rendering requires NET8_0_OR_GREATER.");
#endif
    }

    /// <summary>
    /// Renders a debug visualization to a PNG stream.
    /// </summary>
    public static void RenderToStream(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDebugMode mode, Stream stream, QrPixelDebugOptions? options = null) {
#if NET8_0_OR_GREATER
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        RenderToStream((ReadOnlySpan<byte>)pixels, width, height, stride, format, mode, stream, options);
#else
        _ = pixels;
        _ = width;
        _ = height;
        _ = stride;
        _ = format;
        _ = mode;
        _ = stream;
        _ = options;
        throw new PlatformNotSupportedException("QR pixel debug rendering requires NET8_0_OR_GREATER.");
#endif
    }

    /// <summary>
    /// Renders a debug visualization to a PNG file.
    /// </summary>
    public static string RenderToFile(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDebugMode mode, string path, QrPixelDebugOptions? options = null) {
        var png = RenderPng(pixels, width, height, stride, format, mode, options);
        return RenderIO.WriteBinary(path, png);
    }

    /// <summary>
    /// Renders a debug visualization to a PNG file under the specified directory.
    /// </summary>
    public static string RenderToFile(byte[] pixels, int width, int height, int stride, PixelFormat format, QrPixelDebugMode mode, string directory, string fileName, QrPixelDebugOptions? options = null) {
        var png = RenderPng(pixels, width, height, stride, format, mode, options);
        return RenderIO.WriteBinary(directory, fileName, png);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Renders a debug visualization to a PNG byte array.
    /// </summary>
    public static byte[] RenderPng(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, QrPixelDebugMode mode, QrPixelDebugOptions? options = null) {
        var scanlines = RenderScanlines(pixels, width, height, stride, format, mode, options, out var widthPx, out var heightPx, out _);
        return PngWriter.WriteRgba8(widthPx, heightPx, scanlines);
    }

    /// <summary>
    /// Renders a debug visualization to a PNG stream.
    /// </summary>
    public static void RenderToStream(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, QrPixelDebugMode mode, Stream stream, QrPixelDebugOptions? options = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var scanlines = RenderScanlines(pixels, width, height, stride, format, mode, options, out var widthPx, out var heightPx, out _);
        PngWriter.WriteRgba8(stream, widthPx, heightPx, scanlines);
    }

    /// <summary>
    /// Renders a debug visualization to raw RGBA pixels (no PNG encoding).
    /// </summary>
    public static byte[] RenderPixels(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, QrPixelDebugMode mode, out int widthPx, out int heightPx, out int stridePx, QrPixelDebugOptions? options = null) {
        var scanlines = RenderScanlines(pixels, width, height, stride, format, mode, options, out widthPx, out heightPx, out stridePx);
        var rgba = new byte[heightPx * stridePx];
        for (var y = 0; y < heightPx; y++) {
            Buffer.BlockCopy(scanlines, y * (stridePx + 1) + 1, rgba, y * stridePx, stridePx);
        }
        return rgba;
    }

    private static byte[] RenderScanlines(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, QrPixelDebugMode mode, QrPixelDebugOptions? options, out int widthPx, out int heightPx, out int stridePx) {
        var opts = options ?? new QrPixelDebugOptions();
        if (opts.Scale is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(opts.Scale));
        if (opts.OutputScale <= 0) throw new ArgumentOutOfRangeException(nameof(opts.OutputScale));
        if (opts.BoxBlurRadius < 0) throw new ArgumentOutOfRangeException(nameof(opts.BoxBlurRadius));
        if (opts.ContrastStretchMinRange < 0) throw new ArgumentOutOfRangeException(nameof(opts.ContrastStretchMinRange));
        if (opts.NormalizeWindowSize < 0) throw new ArgumentOutOfRangeException(nameof(opts.NormalizeWindowSize));

        if (!Qr.QrGrayImage.TryCreate(pixels, width, height, stride, format, opts.Scale, minContrast: 0, out var image)) {
            throw new ArgumentException("Unable to build a grayscale image from the input buffer.", nameof(pixels));
        }

        if (opts.ContrastStretch) image = image.WithContrastStretch(opts.ContrastStretchMinRange);
        if (opts.BoxBlurRadius > 0) image = image.WithBoxBlur(opts.BoxBlurRadius);
        if (opts.NormalizeBackground) {
            var window = opts.NormalizeWindowSize <= 0 ? 33 : opts.NormalizeWindowSize;
            image = image.WithLocalNormalize(window);
        }
        if (opts.AdaptiveThreshold) image = image.WithAdaptiveThreshold(opts.AdaptiveWindowSize, opts.AdaptiveOffset);

        var outScale = opts.OutputScale;
        widthPx = image.Width * outScale;
        heightPx = image.Height * outScale;
        stridePx = widthPx * 4;

        var scanlines = new byte[heightPx * (stridePx + 1)];
        var srcWidth = image.Width;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        var invert = opts.Invert;

        for (var y = 0; y < heightPx; y++) {
            var row = y * (stridePx + 1);
            scanlines[row] = 0;
            var sy = y / outScale;
            var srcRow = sy * srcWidth;

            for (var x = 0; x < widthPx; x++) {
                var sx = x / outScale;
                var idx = srcRow + sx;
                var lum = gray[idx];
                var t = thresholdMap is null ? threshold : thresholdMap[idx];

                byte r, g, b;
                switch (mode) {
                    case QrPixelDebugMode.Grayscale:
                        var l = invert ? (byte)(255 - lum) : lum;
                        r = l;
                        g = l;
                        b = l;
                        break;
                    case QrPixelDebugMode.Threshold:
                        r = t;
                        g = t;
                        b = t;
                        break;
                    case QrPixelDebugMode.Binarized:
                        var black = lum <= t;
                        if (invert) black = !black;
                        var v = black ? (byte)0 : (byte)255;
                        r = v;
                        g = v;
                        b = v;
                        break;
                    case QrPixelDebugMode.Heatmap:
                        var diff = lum - t;
                        if (invert) diff = -diff;
                        var abs = diff < 0 ? -diff : diff;
                        if (abs > 255) abs = 255;
                        var intensity = 255 - abs;
                        if (diff >= 0) {
                            r = 255;
                            g = (byte)intensity;
                            b = (byte)intensity;
                        } else {
                            b = 255;
                            g = (byte)intensity;
                            r = (byte)intensity;
                        }
                        break;
                    default:
                        r = lum;
                        g = lum;
                        b = lum;
                        break;
                }

                var p = row + 1 + x * 4;
                scanlines[p + 0] = r;
                scanlines[p + 1] = g;
                scanlines[p + 2] = b;
                scanlines[p + 3] = 255;
            }
        }

        return scanlines;
    }
#endif
}
