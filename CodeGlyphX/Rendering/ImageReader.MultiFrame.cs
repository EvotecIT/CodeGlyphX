using System;
using System.IO;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Webp;

namespace CodeGlyphX.Rendering;

public static partial class ImageReader {
    /// <summary>
    /// Attempts to decode GIF animation frames into RGBA32 buffers.
    /// </summary>
    public static bool TryDecodeGifAnimationFrames(
        byte[] data,
        out GifAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryDecodeGifAnimationFrames((ReadOnlySpan<byte>)data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode GIF animation frames into RGBA32 buffers.
    /// </summary>
    public static bool TryDecodeGifAnimationFrames(
        ReadOnlySpan<byte> data,
        out GifAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        frames = Array.Empty<GifAnimationFrame>();
        canvasWidth = 0;
        canvasHeight = 0;
        options = default;
        if (!GifReader.IsGif(data)) return false;
        return GifReader.TryDecodeAnimationFrames(data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Decodes GIF animation frames into RGBA32 buffers.
    /// </summary>
    public static GifAnimationFrame[] DecodeGifAnimationFrames(
        ReadOnlySpan<byte> data,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        if (!GifReader.IsGif(data)) throw new FormatException("Invalid GIF data.");
        return GifReader.DecodeAnimationFrames(data, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Decodes GIF animation frames into RGBA32 buffers.
    /// </summary>
    public static GifAnimationFrame[] DecodeGifAnimationFrames(
        byte[] data,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeGifAnimationFrames((ReadOnlySpan<byte>)data, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode GIF animation frames composited onto the full canvas.
    /// </summary>
    public static bool TryDecodeGifAnimationCanvasFrames(
        byte[] data,
        out GifAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryDecodeGifAnimationCanvasFrames((ReadOnlySpan<byte>)data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode GIF animation frames composited onto the full canvas.
    /// </summary>
    public static bool TryDecodeGifAnimationCanvasFrames(
        ReadOnlySpan<byte> data,
        out GifAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        frames = Array.Empty<GifAnimationFrame>();
        canvasWidth = 0;
        canvasHeight = 0;
        options = default;
        if (!GifReader.IsGif(data)) return false;
        return GifReader.TryDecodeAnimationCanvasFrames(data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Decodes GIF animation frames composited onto the full canvas.
    /// </summary>
    public static GifAnimationFrame[] DecodeGifAnimationCanvasFrames(
        ReadOnlySpan<byte> data,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        if (!GifReader.IsGif(data)) throw new FormatException("Invalid GIF data.");
        return GifReader.DecodeAnimationCanvasFrames(data, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Decodes GIF animation frames composited onto the full canvas.
    /// </summary>
    public static GifAnimationFrame[] DecodeGifAnimationCanvasFrames(
        byte[] data,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeGifAnimationCanvasFrames((ReadOnlySpan<byte>)data, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode GIF animation frames from a stream.
    /// </summary>
    public static bool TryDecodeGifAnimationFrames(
        Stream stream,
        out GifAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return TryDecodeGifAnimationFrames(buffer.AsSpan(), out frames, out canvasWidth, out canvasHeight, out options);
        }
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeGifAnimationFrames(data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode GIF animation canvas frames from a stream.
    /// </summary>
    public static bool TryDecodeGifAnimationCanvasFrames(
        Stream stream,
        out GifAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out GifAnimationOptions options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return TryDecodeGifAnimationCanvasFrames(buffer.AsSpan(), out frames, out canvasWidth, out canvasHeight, out options);
        }
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeGifAnimationCanvasFrames(data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode WebP animation frames into RGBA32 buffers.
    /// </summary>
    public static bool TryDecodeWebpAnimationFrames(
        byte[] data,
        out WebpAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryDecodeWebpAnimationFrames((ReadOnlySpan<byte>)data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode WebP animation frames into RGBA32 buffers.
    /// </summary>
    public static bool TryDecodeWebpAnimationFrames(
        ReadOnlySpan<byte> data,
        out WebpAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        frames = Array.Empty<WebpAnimationFrame>();
        canvasWidth = 0;
        canvasHeight = 0;
        options = default;
        if (!WebpReader.IsWebp(data)) return false;
        return WebpReader.TryDecodeAnimationFrames(data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Decodes WebP animation frames into RGBA32 buffers.
    /// </summary>
    public static WebpAnimationFrame[] DecodeWebpAnimationFrames(
        ReadOnlySpan<byte> data,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        if (!WebpReader.IsWebp(data)) throw new FormatException("Invalid WebP data.");
        return WebpReader.DecodeAnimationFrames(data, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Decodes WebP animation frames into RGBA32 buffers.
    /// </summary>
    public static WebpAnimationFrame[] DecodeWebpAnimationFrames(
        byte[] data,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeWebpAnimationFrames((ReadOnlySpan<byte>)data, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode WebP animation frames composited onto the full canvas.
    /// </summary>
    public static bool TryDecodeWebpAnimationCanvasFrames(
        byte[] data,
        out WebpAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryDecodeWebpAnimationCanvasFrames((ReadOnlySpan<byte>)data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode WebP animation frames composited onto the full canvas.
    /// </summary>
    public static bool TryDecodeWebpAnimationCanvasFrames(
        ReadOnlySpan<byte> data,
        out WebpAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        frames = Array.Empty<WebpAnimationFrame>();
        canvasWidth = 0;
        canvasHeight = 0;
        options = default;
        if (!WebpReader.IsWebp(data)) return false;
        return WebpReader.TryDecodeAnimationCanvasFrames(data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Decodes WebP animation frames composited onto the full canvas.
    /// </summary>
    public static WebpAnimationFrame[] DecodeWebpAnimationCanvasFrames(
        ReadOnlySpan<byte> data,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        if (!WebpReader.IsWebp(data)) throw new FormatException("Invalid WebP data.");
        return WebpReader.DecodeAnimationCanvasFrames(data, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Decodes WebP animation frames composited onto the full canvas.
    /// </summary>
    public static WebpAnimationFrame[] DecodeWebpAnimationCanvasFrames(
        byte[] data,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeWebpAnimationCanvasFrames((ReadOnlySpan<byte>)data, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode WebP animation frames from a stream.
    /// </summary>
    public static bool TryDecodeWebpAnimationFrames(
        Stream stream,
        out WebpAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return TryDecodeWebpAnimationFrames(buffer.AsSpan(), out frames, out canvasWidth, out canvasHeight, out options);
        }
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeWebpAnimationFrames(data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode WebP animation canvas frames from a stream.
    /// </summary>
    public static bool TryDecodeWebpAnimationCanvasFrames(
        Stream stream,
        out WebpAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return TryDecodeWebpAnimationCanvasFrames(buffer.AsSpan(), out frames, out canvasWidth, out canvasHeight, out options);
        }
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeWebpAnimationCanvasFrames(data, out frames, out canvasWidth, out canvasHeight, out options);
    }

    /// <summary>
    /// Attempts to decode all TIFF pages into RGBA32 buffers.
    /// </summary>
    public static bool TryDecodeTiffPagesRgba32(byte[] data, out TiffRgba32Page[] pages) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryDecodeTiffPagesRgba32((ReadOnlySpan<byte>)data, out pages);
    }

    /// <summary>
    /// Attempts to decode all TIFF pages into RGBA32 buffers.
    /// </summary>
    public static bool TryDecodeTiffPagesRgba32(ReadOnlySpan<byte> data, out TiffRgba32Page[] pages) {
        if (!TiffReader.IsTiff(data)) {
            pages = Array.Empty<TiffRgba32Page>();
            return false;
        }
        return TiffReader.TryDecodePagesRgba32(data, out pages);
    }

    /// <summary>
    /// Attempts to decode all TIFF pages into RGBA32 buffers from a stream.
    /// </summary>
    public static bool TryDecodeTiffPagesRgba32(Stream stream, out TiffRgba32Page[] pages) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return TryDecodeTiffPagesRgba32(buffer.AsSpan(), out pages);
        }
        var data = RenderIO.ReadBinary(stream);
        return TryDecodeTiffPagesRgba32(data, out pages);
    }

    /// <summary>
    /// Decodes all TIFF pages into RGBA32 buffers.
    /// </summary>
    public static TiffRgba32Page[] DecodeTiffPagesRgba32(byte[] data) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeTiffPagesRgba32((ReadOnlySpan<byte>)data);
    }

    /// <summary>
    /// Decodes all TIFF pages into RGBA32 buffers.
    /// </summary>
    public static TiffRgba32Page[] DecodeTiffPagesRgba32(Stream stream) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeTiffPagesRgba32(buffer.AsSpan());
        }
        var data = RenderIO.ReadBinary(stream);
        return DecodeTiffPagesRgba32(data);
    }

    /// <summary>
    /// Decodes all TIFF pages into RGBA32 buffers.
    /// </summary>
    public static TiffRgba32Page[] DecodeTiffPagesRgba32(ReadOnlySpan<byte> data) {
        return TiffReader.DecodePagesRgba32(data);
    }
}
