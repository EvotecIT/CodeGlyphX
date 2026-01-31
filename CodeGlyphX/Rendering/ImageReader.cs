using System;
using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Psd;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Auto-detects common image formats and decodes to RGBA buffers.
/// </summary>
public static partial class ImageReader {
    private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected).
    /// </summary>
    public static byte[] DecodeRgba32(byte[] data, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeRgba32((ReadOnlySpan<byte>)data, out width, out height);
    }

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected) with decode options.
    /// </summary>
    public static byte[] DecodeRgba32(byte[] data, ImageDecodeOptions? options, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeRgba32((ReadOnlySpan<byte>)data, options, out width, out height);
    }

    /// <summary>
    /// Decodes a multipage image to an RGBA buffer (auto-detected).
    /// </summary>
    public static byte[] DecodeRgba32(byte[] data, int pageIndex, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeRgba32((ReadOnlySpan<byte>)data, pageIndex, out width, out height);
    }

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected), returning the first composited animation frame when available.
    /// </summary>
    public static byte[] DecodeRgba32Composite(byte[] data, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeRgba32Composite((ReadOnlySpan<byte>)data, out width, out height);
    }

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected), returning the first composited animation frame when available, with decode options.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new ImageDecodeOptions()
    ///     .WithJpegOptions(highQualityChroma: true, allowTruncated: true);
    /// var rgba = ImageReader.DecodeRgba32Composite(bytes, options, out var width, out var height);
    /// </code>
    /// </example>
    public static byte[] DecodeRgba32Composite(byte[] data, ImageDecodeOptions? options, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeRgba32Composite((ReadOnlySpan<byte>)data, options, out width, out height);
    }

    /// <summary>
    /// Decodes animation frames (non-composited) (auto-detected).
    /// </summary>
    public static ImageAnimationFrame[] DecodeAnimationFrames(byte[] data, out int width, out int height, out ImageAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeAnimationFrames((ReadOnlySpan<byte>)data, out width, out height, out options);
    }

    /// <summary>
    /// Decodes animation frames composited onto a full canvas (auto-detected).
    /// </summary>
    public static ImageAnimationFrame[] DecodeAnimationCanvasFrames(byte[] data, out int width, out int height, out ImageAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeAnimationCanvasFrames((ReadOnlySpan<byte>)data, out width, out height, out options);
    }

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected).
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        return DecodeRgba32Core(data, null, out width, out height);
    }

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected) with decode options.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, ImageDecodeOptions? options, out int width, out int height) {
        return DecodeRgba32Core(data, options?.JpegOptions, out width, out height);
    }

    private static byte[] DecodeRgba32Core(ReadOnlySpan<byte> data, JpegDecodeOptions? jpegOptions, out int width, out int height) {
        if (data.Length < 2) throw new FormatException("Unknown image format.");

        if (IsPng(data)) return PngReader.DecodeRgba32(data, out width, out height);
        if (WebpReader.IsWebp(data)) return WebpReader.DecodeRgba32(data, out width, out height);
        if (TiffReader.IsTiff(data)) return TiffReader.DecodeRgba32(data, out width, out height);
        if (IcoReader.IsIco(data)) return IcoReader.DecodeRgba32(data, out width, out height);
        if (JpegReader.IsJpeg(data)) return JpegReader.DecodeRgba32(data, out width, out height, jpegOptions ?? default);
        if (GifReader.IsGif(data)) return GifReader.DecodeRgba32(data, out width, out height);
        if (PdfReader.IsPdf(data)) return PdfReader.DecodeRgba32(data, out width, out height);
        if (PsdReader.IsPsd(data)) return PsdReader.DecodeRgba32(data, out width, out height);
        if (IsBmp(data)) return BmpReader.DecodeRgba32(data, out width, out height);
        if (IsPbm(data)) return PbmReader.DecodeRgba32(data, out width, out height);
        if (IsPgm(data)) return PgmReader.DecodeRgba32(data, out width, out height);
        if (IsPam(data)) return PamReader.DecodeRgba32(data, out width, out height);
        if (IsPpm(data)) return PpmReader.DecodeRgba32(data, out width, out height);
        if (TgaReader.LooksLikeTga(data)) return TgaReader.DecodeRgba32(data, out width, out height);
        if (IsXpm(data)) return XpmReader.DecodeRgba32(data, out width, out height);
        if (IsXbm(data)) return XbmReader.DecodeRgba32(data, out width, out height);

        ThrowIfUnsupportedFormat(data);
        throw new FormatException("Unknown image format.");
    }

    /// <summary>
    /// Decodes animation frames (non-composited) (auto-detected).
    /// </summary>
    public static ImageAnimationFrame[] DecodeAnimationFrames(ReadOnlySpan<byte> data, out int width, out int height, out ImageAnimationOptions options) {
        if (data.Length < 2) throw new FormatException("Unknown image format.");

        if (GifReader.IsGif(data)) {
            if (!GifReader.TryDecodeAnimationFrames(data, out var frames, out width, out height, out var gifOptions)) {
                throw new FormatException("Unsupported or invalid animated GIF.");
            }
            options = new ImageAnimationOptions(gifOptions.LoopCount, gifOptions.BackgroundRgba);
            return MapGifFrames(frames);
        }

        if (WebpReader.IsWebp(data)) {
            if (WebpReader.TryDecodeAnimationFrames(data, out var frames, out width, out height, out var webpOptions)) {
                options = new ImageAnimationOptions(webpOptions.LoopCount, ConvertBgraToRgba(webpOptions.BackgroundBgra));
                return MapWebpFrames(frames);
            }

            var rgba = WebpReader.DecodeRgba32(data, out width, out height);
            options = new ImageAnimationOptions(loopCount: 1, backgroundRgba: 0);
            return new[] { new ImageAnimationFrame(rgba, width, height, width * 4, durationMs: 0) };
        }

        throw new FormatException("Unsupported animated image format.");
    }

    /// <summary>
    /// Decodes a multipage image to an RGBA buffer (auto-detected).
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, int pageIndex, out int width, out int height) {
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (pageIndex == 0) return DecodeRgba32(data, out width, out height);
        if (data.Length < 2) throw new FormatException("Unknown image format.");

        if (TiffReader.IsTiff(data)) return TiffReader.DecodeRgba32(data, pageIndex, out width, out height);

        throw new FormatException("Page index decoding is only supported for TIFF images.");
    }

    /// <summary>
    /// Decodes animation frames composited onto a full canvas (auto-detected).
    /// </summary>
    public static ImageAnimationFrame[] DecodeAnimationCanvasFrames(ReadOnlySpan<byte> data, out int width, out int height, out ImageAnimationOptions options) {
        if (data.Length < 2) throw new FormatException("Unknown image format.");

        if (GifReader.IsGif(data)) {
            if (!GifReader.TryDecodeAnimationCanvasFrames(data, out var frames, out width, out height, out var gifOptions)) {
                throw new FormatException("Unsupported or invalid animated GIF.");
            }
            options = new ImageAnimationOptions(gifOptions.LoopCount, gifOptions.BackgroundRgba);
            return MapGifFrames(frames);
        }

        if (WebpReader.IsWebp(data)) {
            if (WebpReader.TryDecodeAnimationCanvasFrames(data, out var frames, out width, out height, out var webpOptions)) {
                options = new ImageAnimationOptions(webpOptions.LoopCount, ConvertBgraToRgba(webpOptions.BackgroundBgra));
                return MapWebpFrames(frames);
            }

            var rgba = WebpReader.DecodeRgba32(data, out width, out height);
            options = new ImageAnimationOptions(loopCount: 1, backgroundRgba: 0);
            return new[] { new ImageAnimationFrame(rgba, width, height, width * 4, durationMs: 0) };
        }

        throw new FormatException("Unsupported animated image format.");
    }

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected), returning the first composited animation frame when available.
    /// </summary>
    public static byte[] DecodeRgba32Composite(ReadOnlySpan<byte> data, out int width, out int height) {
        return DecodeRgba32CompositeCore(data, null, out width, out height);
    }

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected), returning the first composited animation frame when available, with decode options.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new ImageDecodeOptions()
    ///     .WithJpegOptions(highQualityChroma: true);
    /// var rgba = ImageReader.DecodeRgba32Composite(data, options, out var width, out var height);
    /// </code>
    /// </example>
    public static byte[] DecodeRgba32Composite(ReadOnlySpan<byte> data, ImageDecodeOptions? options, out int width, out int height) {
        return DecodeRgba32CompositeCore(data, options?.JpegOptions, out width, out height);
    }

    private static byte[] DecodeRgba32CompositeCore(ReadOnlySpan<byte> data, JpegDecodeOptions? jpegOptions, out int width, out int height) {
        if (data.Length < 2) throw new FormatException("Unknown image format.");

        if (IsPng(data)) return PngReader.DecodeRgba32(data, out width, out height);
        if (WebpReader.IsWebp(data)) {
            if (WebpReader.TryDecodeAnimationCanvasFrames(data, out var frames, out width, out height, out _)) {
                if (frames.Length > 0) return frames[0].Rgba;
            }
            return WebpReader.DecodeRgba32(data, out width, out height);
        }
        if (TiffReader.IsTiff(data)) return TiffReader.DecodeRgba32(data, out width, out height);
        if (IcoReader.IsIco(data)) return IcoReader.DecodeRgba32(data, out width, out height);
        if (JpegReader.IsJpeg(data)) return JpegReader.DecodeRgba32(data, out width, out height, jpegOptions ?? default);
        if (GifReader.IsGif(data)) {
            if (GifReader.TryDecodeAnimationCanvasFrames(data, out var frames, out width, out height, out _)) {
                if (frames.Length > 0) return frames[0].Rgba;
            }
            return GifReader.DecodeRgba32(data, out width, out height);
        }
        if (IsBmp(data)) return BmpReader.DecodeRgba32(data, out width, out height);
        if (IsPbm(data)) return PbmReader.DecodeRgba32(data, out width, out height);
        if (IsPgm(data)) return PgmReader.DecodeRgba32(data, out width, out height);
        if (IsPam(data)) return PamReader.DecodeRgba32(data, out width, out height);
        if (IsPpm(data)) return PpmReader.DecodeRgba32(data, out width, out height);
        if (TgaReader.LooksLikeTga(data)) return TgaReader.DecodeRgba32(data, out width, out height);
        if (IsXpm(data)) return XpmReader.DecodeRgba32(data, out width, out height);
        if (IsXbm(data)) return XbmReader.DecodeRgba32(data, out width, out height);

        ThrowIfUnsupportedFormat(data);
        throw new FormatException("Unknown image format.");
    }

    /// <summary>
    /// Decodes an image stream to an RGBA buffer (auto-detected).
    /// </summary>
    public static byte[] DecodeRgba32(Stream stream, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeRgba32(buffer.AsSpan(), out width, out height);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.TryGetBuffer(out var segment)) {
            return DecodeRgba32(segment.AsSpan(), out width, out height);
        }
        return DecodeRgba32(ms.ToArray(), out width, out height);
    }

    /// <summary>
    /// Decodes an image stream to an RGBA buffer (auto-detected) with decode options.
    /// </summary>
    public static byte[] DecodeRgba32(Stream stream, ImageDecodeOptions? options, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeRgba32(buffer.AsSpan(), options, out width, out height);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.TryGetBuffer(out var segment)) {
            return DecodeRgba32(segment.AsSpan(), options, out width, out height);
        }
        return DecodeRgba32(ms.ToArray(), options, out width, out height);
    }

    /// <summary>
    /// Decodes a multipage image stream to an RGBA buffer (auto-detected).
    /// </summary>
    public static byte[] DecodeRgba32(Stream stream, int pageIndex, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeRgba32(buffer.AsSpan(), pageIndex, out width, out height);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.TryGetBuffer(out var segment)) {
            return DecodeRgba32(segment.AsSpan(), pageIndex, out width, out height);
        }
        return DecodeRgba32(ms.ToArray(), pageIndex, out width, out height);
    }

    /// <summary>
    /// Decodes an image stream to an RGBA buffer (auto-detected), returning the first composited animation frame when available.
    /// </summary>
    public static byte[] DecodeRgba32Composite(Stream stream, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeRgba32Composite(buffer.AsSpan(), out width, out height);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.TryGetBuffer(out var segment)) {
            return DecodeRgba32Composite(segment.AsSpan(), out width, out height);
        }
        return DecodeRgba32Composite(ms.ToArray(), out width, out height);
    }

    /// <summary>
    /// Decodes an image stream to an RGBA buffer (auto-detected), returning the first composited animation frame when available, with decode options.
    /// </summary>
    /// <example>
    /// <code>
    /// using var stream = File.OpenRead("image.jpg");
    /// var options = new ImageDecodeOptions().WithJpegOptions(highQualityChroma: true);
    /// var rgba = ImageReader.DecodeRgba32Composite(stream, options, out var width, out var height);
    /// </code>
    /// </example>
    public static byte[] DecodeRgba32Composite(Stream stream, ImageDecodeOptions? options, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeRgba32Composite(buffer.AsSpan(), options, out width, out height);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.TryGetBuffer(out var segment)) {
            return DecodeRgba32Composite(segment.AsSpan(), options, out width, out height);
        }
        return DecodeRgba32Composite(ms.ToArray(), options, out width, out height);
    }

    /// <summary>
    /// Decodes animation frames composited onto a full canvas from a stream (auto-detected).
    /// </summary>
    public static ImageAnimationFrame[] DecodeAnimationCanvasFrames(Stream stream, out int width, out int height, out ImageAnimationOptions options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeAnimationCanvasFrames(buffer.AsSpan(), out width, out height, out options);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.TryGetBuffer(out var segment)) {
            return DecodeAnimationCanvasFrames(segment.AsSpan(), out width, out height, out options);
        }
        return DecodeAnimationCanvasFrames(ms.ToArray(), out width, out height, out options);
    }

    /// <summary>
    /// Decodes animation frames (non-composited) from a stream (auto-detected).
    /// </summary>
    public static ImageAnimationFrame[] DecodeAnimationFrames(Stream stream, out int width, out int height, out ImageAnimationOptions options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeAnimationFrames(buffer.AsSpan(), out width, out height, out options);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.TryGetBuffer(out var segment)) {
            return DecodeAnimationFrames(segment.AsSpan(), out width, out height, out options);
        }
        return DecodeAnimationFrames(ms.ToArray(), out width, out height, out options);
    }

    /// <summary>
    /// Attempts to decode an image to an RGBA buffer (auto-detected).
    /// </summary>
    public static bool TryDecodeRgba32(byte[] data, out byte[] rgba, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryDecodeRgba32((ReadOnlySpan<byte>)data, out rgba, out width, out height);
    }

    /// <summary>
    /// Attempts to decode an image to an RGBA buffer (auto-detected) with decode options.
    /// </summary>
    public static bool TryDecodeRgba32(byte[] data, ImageDecodeOptions? options, out byte[] rgba, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryDecodeRgba32((ReadOnlySpan<byte>)data, options, out rgba, out width, out height);
    }

    /// <summary>
    /// Attempts to decode an image stream to an RGBA buffer (auto-detected).
    /// </summary>
    public static bool TryDecodeRgba32(Stream stream, out byte[] rgba, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        try {
            rgba = DecodeRgba32(stream, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode an image stream to an RGBA buffer (auto-detected) with decode options.
    /// </summary>
    public static bool TryDecodeRgba32(Stream stream, ImageDecodeOptions? options, out byte[] rgba, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        try {
            rgba = DecodeRgba32(stream, options, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode an image to an RGBA buffer (auto-detected).
    /// </summary>
    public static bool TryDecodeRgba32(ReadOnlySpan<byte> data, out byte[] rgba, out int width, out int height) {
        try {
            rgba = DecodeRgba32(data, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode an image to an RGBA buffer (auto-detected) with decode options.
    /// </summary>
    public static bool TryDecodeRgba32(ReadOnlySpan<byte> data, ImageDecodeOptions? options, out byte[] rgba, out int width, out int height) {
        try {
            rgba = DecodeRgba32(data, options, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode a multipage image to an RGBA buffer (auto-detected).
    /// </summary>
    public static bool TryDecodeRgba32(ReadOnlySpan<byte> data, int pageIndex, out byte[] rgba, out int width, out int height) {
        try {
            rgba = DecodeRgba32(data, pageIndex, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        } catch (ArgumentOutOfRangeException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode an image to an RGBA buffer (auto-detected), returning the first composited animation frame when available.
    /// </summary>
    public static bool TryDecodeRgba32Composite(byte[] data, out byte[] rgba, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryDecodeRgba32Composite((ReadOnlySpan<byte>)data, out rgba, out width, out height);
    }

    /// <summary>
    /// Attempts to decode an image to an RGBA buffer (auto-detected), returning the first composited animation frame when available, with decode options.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new ImageDecodeOptions().WithJpegOptions(allowTruncated: true);
    /// if (ImageReader.TryDecodeRgba32Composite(bytes, options, out var rgba, out var width, out var height)) {
    ///     Console.WriteLine($\"{width}x{height}\");
    /// }
    /// </code>
    /// </example>
    public static bool TryDecodeRgba32Composite(byte[] data, ImageDecodeOptions? options, out byte[] rgba, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryDecodeRgba32Composite((ReadOnlySpan<byte>)data, options, out rgba, out width, out height);
    }

    /// <summary>
    /// Attempts to decode an image stream to an RGBA buffer (auto-detected), returning the first composited animation frame when available.
    /// </summary>
    public static bool TryDecodeRgba32Composite(Stream stream, out byte[] rgba, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        try {
            rgba = DecodeRgba32Composite(stream, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode an image stream to an RGBA buffer (auto-detected), returning the first composited animation frame when available, with decode options.
    /// </summary>
    /// <example>
    /// <code>
    /// using var stream = File.OpenRead("image.jpg");
    /// var options = new ImageDecodeOptions().WithJpegOptions(highQualityChroma: true);
    /// if (ImageReader.TryDecodeRgba32Composite(stream, options, out var rgba, out var width, out var height)) {
    ///     Console.WriteLine($\"{width}x{height}\");
    /// }
    /// </code>
    /// </example>
    public static bool TryDecodeRgba32Composite(Stream stream, ImageDecodeOptions? options, out byte[] rgba, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        try {
            rgba = DecodeRgba32Composite(stream, options, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode an image to an RGBA buffer (auto-detected), returning the first composited animation frame when available.
    /// </summary>
    public static bool TryDecodeRgba32Composite(ReadOnlySpan<byte> data, out byte[] rgba, out int width, out int height) {
        try {
            rgba = DecodeRgba32Composite(data, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode an image to an RGBA buffer (auto-detected), returning the first composited animation frame when available, with decode options.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new ImageDecodeOptions().WithJpegOptions(highQualityChroma: true);
    /// if (ImageReader.TryDecodeRgba32Composite(data, options, out var rgba, out var width, out var height)) {
    ///     Console.WriteLine($\"{width}x{height}\");
    /// }
    /// </code>
    /// </example>
    public static bool TryDecodeRgba32Composite(ReadOnlySpan<byte> data, ImageDecodeOptions? options, out byte[] rgba, out int width, out int height) {
        try {
            rgba = DecodeRgba32Composite(data, options, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode animation frames composited onto a full canvas (auto-detected).
    /// </summary>
    public static bool TryDecodeAnimationCanvasFrames(ReadOnlySpan<byte> data, out ImageAnimationFrame[] frames, out int width, out int height, out ImageAnimationOptions options) {
        try {
            frames = DecodeAnimationCanvasFrames(data, out width, out height, out options);
            return true;
        } catch (FormatException) {
            frames = Array.Empty<ImageAnimationFrame>();
            width = 0;
            height = 0;
            options = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode animation frames (non-composited) (auto-detected).
    /// </summary>
    public static bool TryDecodeAnimationFrames(ReadOnlySpan<byte> data, out ImageAnimationFrame[] frames, out int width, out int height, out ImageAnimationOptions options) {
        try {
            frames = DecodeAnimationFrames(data, out width, out height, out options);
            return true;
        } catch (FormatException) {
            frames = Array.Empty<ImageAnimationFrame>();
            width = 0;
            height = 0;
            options = default;
            return false;
        }
    }

    /// <summary>
    /// Detects the image format from a byte buffer.
    /// </summary>
    public static ImageFormat DetectFormat(ReadOnlySpan<byte> data) {
        if (TryDetectFormat(data, out var format)) return format;
        throw new FormatException("Unknown image format.");
    }

    /// <summary>
    /// Attempts to detect the image format from a byte buffer.
    /// </summary>
    public static bool TryDetectFormat(ReadOnlySpan<byte> data, out ImageFormat format) {
        format = ImageFormat.Unknown;
        if (data.Length < 2) return false;

        if (IsPng(data)) { format = ImageFormat.Png; return true; }
        if (WebpReader.IsWebp(data)) { format = ImageFormat.Webp; return true; }
        if (TiffReader.IsTiff(data)) { format = ImageFormat.Tiff; return true; }
        if (IcoReader.IsIco(data)) { format = ImageFormat.Ico; return true; }
        if (JpegReader.IsJpeg(data)) { format = ImageFormat.Jpeg; return true; }
        if (GifReader.IsGif(data)) { format = ImageFormat.Gif; return true; }
        if (PdfReader.IsPdf(data)) { format = ImageFormat.Pdf; return true; }
        if (PsdReader.IsPsd(data)) { format = ImageFormat.Psd; return true; }
        if (IsBmp(data)) { format = ImageFormat.Bmp; return true; }
        if (IsPbm(data)) { format = ImageFormat.Pbm; return true; }
        if (IsPgm(data)) { format = ImageFormat.Pgm; return true; }
        if (IsPam(data)) { format = ImageFormat.Pam; return true; }
        if (IsPpm(data)) { format = ImageFormat.Ppm; return true; }
        if (TgaReader.LooksLikeTga(data)) { format = ImageFormat.Tga; return true; }
        if (IsXpm(data)) { format = ImageFormat.Xpm; return true; }
        if (IsXbm(data)) { format = ImageFormat.Xbm; return true; }
        if (IsPdf(data)) { format = ImageFormat.Pdf; return true; }
        if (IsPostScript(data)) { format = ImageFormat.Ps; return true; }
        if (IsPsd(data)) { format = ImageFormat.Psd; return true; }
        if (IsJpeg2000(data)) { format = ImageFormat.Jpeg2000; return true; }
        if (IsAvif(data)) { format = ImageFormat.Avif; return true; }
        if (IsHeif(data)) { format = ImageFormat.Heic; return true; }

        return false;
    }

    private static bool IsPng(ReadOnlySpan<byte> data) {
        if (data.Length < PngSignature.Length) return false;
        for (var i = 0; i < PngSignature.Length; i++) {
            if (data[i] != PngSignature[i]) return false;
        }
        return true;
    }

    private static bool IsBmp(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M';
    }

    private static bool IsPpm(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'P' && (data[1] == (byte)'3' || data[1] == (byte)'5' || data[1] == (byte)'6');
    }

    private static bool IsPbm(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'P' && (data[1] == (byte)'1' || data[1] == (byte)'4');
    }

    private static bool IsPgm(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'P' && (data[1] == (byte)'2' || data[1] == (byte)'5');
    }

    private static bool IsPam(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'P' && data[1] == (byte)'7';
    }

    private static bool IsXpm(ReadOnlySpan<byte> data) {
        return StartsWithAscii(data, "/* XPM */");
    }

    private static bool IsXbm(ReadOnlySpan<byte> data) {
        return StartsWithAscii(data, "#define") && ContainsAscii(data, "_width");
    }

    private static void ThrowIfUnsupportedFormat(ReadOnlySpan<byte> data) {
        if (IsPdf(data) || IsPostScript(data)) {
            throw new FormatException("PDF/PS decode is not supported (rasterize first).");
        }
        if (IsPsd(data)) {
            throw new FormatException("PSD decode is not supported.");
        }
        if (IsJpeg2000(data)) {
            throw new FormatException("JPEG2000 decode is not supported.");
        }
        if (IsAvif(data) || IsHeif(data)) {
            throw new FormatException("AVIF/HEIC decode is not supported.");
        }
    }

    private static bool IsPdf(ReadOnlySpan<byte> data) {
        return data.Length >= 5
            && data[0] == (byte)'%'
            && data[1] == (byte)'P'
            && data[2] == (byte)'D'
            && data[3] == (byte)'F'
            && data[4] == (byte)'-';
    }

    private static bool IsPostScript(ReadOnlySpan<byte> data) {
        return data.Length >= 4
            && data[0] == (byte)'%'
            && data[1] == (byte)'!'
            && data[2] == (byte)'P'
            && data[3] == (byte)'S';
    }

    private static bool IsPsd(ReadOnlySpan<byte> data) {
        return data.Length >= 4
            && data[0] == (byte)'8'
            && data[1] == (byte)'B'
            && data[2] == (byte)'P'
            && data[3] == (byte)'S';
    }

    private static bool IsJpeg2000(ReadOnlySpan<byte> data) {
        if (data.Length >= 12
            && data[0] == 0x00
            && data[1] == 0x00
            && data[2] == 0x00
            && data[3] == 0x0C
            && data[4] == 0x6A
            && data[5] == 0x50
            && data[6] == 0x20
            && data[7] == 0x20
            && data[8] == 0x0D
            && data[9] == 0x0A
            && data[10] == 0x87
            && data[11] == 0x0A) {
            return true;
        }
        return data.Length >= 2 && data[0] == 0xFF && data[1] == 0x4F;
    }

    private static bool IsAvif(ReadOnlySpan<byte> data) {
        return HasIsobmffBrand(data, "avif", "avis");
    }

    private static bool IsHeif(ReadOnlySpan<byte> data) {
        return HasIsobmffBrand(data, "heic", "heix", "hevc", "hevx", "mif1", "msf1");
    }

    private static bool HasIsobmffBrand(ReadOnlySpan<byte> data, params string[] brands) {
        if (data.Length < 16) return false;
        if (!IsFourCc(data, 4, "ftyp")) return false;

        var boxSize = ReadUInt32BE(data, 0);
        var limit = boxSize >= 16 && boxSize <= data.Length ? (int)boxSize : Math.Min(data.Length, 128);
        if (limit < 16) return false;

        if (IsAnyBrand(data.Slice(8, 4), brands)) return true;
        for (var offset = 16; offset + 4 <= limit; offset += 4) {
            if (IsAnyBrand(data.Slice(offset, 4), brands)) return true;
        }
        return false;
    }

    private static bool IsFourCc(ReadOnlySpan<byte> data, int offset, string fourCc) {
        if (offset < 0 || offset + 4 > data.Length) return false;
        return data[offset] == (byte)fourCc[0]
            && data[offset + 1] == (byte)fourCc[1]
            && data[offset + 2] == (byte)fourCc[2]
            && data[offset + 3] == (byte)fourCc[3];
    }

    private static bool IsAnyBrand(ReadOnlySpan<byte> data, string[] brands) {
        for (var i = 0; i < brands.Length; i++) {
            var brand = brands[i];
            if (brand.Length != 4) continue;
            if (data[0] == (byte)brand[0]
                && data[1] == (byte)brand[1]
                && data[2] == (byte)brand[2]
                && data[3] == (byte)brand[3]) {
                return true;
            }
        }
        return false;
    }

    private static uint ReadUInt32BE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 4 > data.Length) return 0;
        return (uint)((data[offset] << 24)
            | (data[offset + 1] << 16)
            | (data[offset + 2] << 8)
            | data[offset + 3]);
    }

    private static ImageAnimationFrame[] MapGifFrames(GifAnimationFrame[] frames) {
        if (frames.Length == 0) return Array.Empty<ImageAnimationFrame>();
        var mapped = new ImageAnimationFrame[frames.Length];
        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            mapped[i] = new ImageAnimationFrame(frame.Rgba, frame.Width, frame.Height, frame.Stride, frame.DurationMs, frame.X, frame.Y);
        }
        return mapped;
    }

    private static ImageAnimationFrame[] MapWebpFrames(WebpAnimationFrame[] frames) {
        if (frames.Length == 0) return Array.Empty<ImageAnimationFrame>();
        var mapped = new ImageAnimationFrame[frames.Length];
        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            mapped[i] = new ImageAnimationFrame(frame.Rgba, frame.Width, frame.Height, frame.Stride, frame.DurationMs, frame.X, frame.Y);
        }
        return mapped;
    }

    private static uint ConvertBgraToRgba(uint bgra) {
        var b = (byte)(bgra & 0xFF);
        var g = (byte)((bgra >> 8) & 0xFF);
        var r = (byte)((bgra >> 16) & 0xFF);
        var a = (byte)((bgra >> 24) & 0xFF);
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }

    private static bool StartsWithAscii(ReadOnlySpan<byte> data, string prefix) {
        var pos = 0;
        while (pos < data.Length && data[pos] <= 32) pos++;
        if (pos + prefix.Length > data.Length) return false;
        for (var i = 0; i < prefix.Length; i++) {
            if (data[pos + i] != (byte)prefix[i]) return false;
        }
        return true;
    }

    private static bool ContainsAscii(ReadOnlySpan<byte> data, string token) {
        if (token.Length == 0) return false;
        for (var i = 0; i <= data.Length - token.Length; i++) {
            var match = true;
            for (var j = 0; j < token.Length; j++) {
                var c = data[i + j];
                var t = (byte)token[j];
                if (c == t) continue;
                if (c >= (byte)'A' && c <= (byte)'Z' && c + 32 == t) continue;
                if (c >= (byte)'a' && c <= (byte)'z' && c - 32 == t) continue;
                match = false;
                break;
            }
            if (match) return true;
        }
        return false;
    }
}
