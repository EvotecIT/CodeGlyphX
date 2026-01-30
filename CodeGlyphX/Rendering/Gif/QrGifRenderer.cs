using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Gif;

/// <summary>
/// Renders QR modules to a GIF image (indexed color).
/// </summary>
public static class QrGifRenderer {
    /// <summary>
    /// Renders the QR module matrix to a GIF byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return GifWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a GIF stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        GifWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a GIF file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path) {
        var gif = Render(modules, opts);
        return RenderIO.WriteBinary(path, gif);
    }

    /// <summary>
    /// Renders the QR module matrix to a GIF file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName) {
        var gif = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, gif);
    }

    /// <summary>
    /// Renders an animated GIF from multiple QR module frames.
    /// </summary>
    public static byte[] RenderAnimation(BitMatrix[] frames, QrPngRenderOptions opts, int durationMs, GifAnimationOptions options = default) {
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        if (frames.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(frames));
        if (durationMs <= 0) throw new ArgumentOutOfRangeException(nameof(durationMs));

        var gifFrames = new GifAnimationFrame[frames.Length];
        var canvasWidth = 0;
        var canvasHeight = 0;
        for (var i = 0; i < frames.Length; i++) {
            var pixels = QrPngRenderer.RenderPixels(frames[i], opts, out var widthPx, out var heightPx, out var stride);
            if (i == 0) {
                canvasWidth = widthPx;
                canvasHeight = heightPx;
            } else if (widthPx != canvasWidth || heightPx != canvasHeight) {
                throw new ArgumentException("All frames must render to the same pixel size.", nameof(frames));
            }
            gifFrames[i] = new GifAnimationFrame(pixels, widthPx, heightPx, stride, durationMs);
        }

        return GifWriter.WriteAnimation(canvasWidth, canvasHeight, gifFrames, options);
    }

    /// <summary>
    /// Renders an animated GIF from multiple QR module frames with per-frame durations.
    /// </summary>
    public static byte[] RenderAnimation(BitMatrix[] frames, QrPngRenderOptions opts, int[] durationsMs, GifAnimationOptions options = default) {
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        if (durationsMs is null) throw new ArgumentNullException(nameof(durationsMs));
        if (frames.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(frames));
        if (frames.Length != durationsMs.Length) throw new ArgumentException("Durations must match frame count.", nameof(durationsMs));

        var gifFrames = new GifAnimationFrame[frames.Length];
        var canvasWidth = 0;
        var canvasHeight = 0;
        for (var i = 0; i < frames.Length; i++) {
            var duration = durationsMs[i];
            if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(durationsMs));
            var pixels = QrPngRenderer.RenderPixels(frames[i], opts, out var widthPx, out var heightPx, out var stride);
            if (i == 0) {
                canvasWidth = widthPx;
                canvasHeight = heightPx;
            } else if (widthPx != canvasWidth || heightPx != canvasHeight) {
                throw new ArgumentException("All frames must render to the same pixel size.", nameof(frames));
            }
            gifFrames[i] = new GifAnimationFrame(pixels, widthPx, heightPx, stride, duration);
        }

        return GifWriter.WriteAnimation(canvasWidth, canvasHeight, gifFrames, options);
    }

    /// <summary>
    /// Renders an animated GIF to a stream from multiple QR module frames.
    /// </summary>
    public static void RenderAnimationToStream(BitMatrix[] frames, QrPngRenderOptions opts, int durationMs, Stream stream, GifAnimationOptions options = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var gif = RenderAnimation(frames, opts, durationMs, options);
        stream.Write(gif, 0, gif.Length);
    }

    /// <summary>
    /// Renders an animated GIF to a stream from multiple QR module frames with per-frame durations.
    /// </summary>
    public static void RenderAnimationToStream(BitMatrix[] frames, QrPngRenderOptions opts, int[] durationsMs, Stream stream, GifAnimationOptions options = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var gif = RenderAnimation(frames, opts, durationsMs, options);
        stream.Write(gif, 0, gif.Length);
    }

    /// <summary>
    /// Renders an animated GIF to a file from multiple QR module frames.
    /// </summary>
    public static string RenderAnimationToFile(BitMatrix[] frames, QrPngRenderOptions opts, int durationMs, string path, GifAnimationOptions options = default) {
        var gif = RenderAnimation(frames, opts, durationMs, options);
        return RenderIO.WriteBinary(path, gif);
    }

    /// <summary>
    /// Renders an animated GIF to a file from multiple QR module frames with per-frame durations.
    /// </summary>
    public static string RenderAnimationToFile(BitMatrix[] frames, QrPngRenderOptions opts, int[] durationsMs, string path, GifAnimationOptions options = default) {
        var gif = RenderAnimation(frames, opts, durationsMs, options);
        return RenderIO.WriteBinary(path, gif);
    }

    /// <summary>
    /// Renders an animated GIF to a file under the specified directory.
    /// </summary>
    public static string RenderAnimationToFile(BitMatrix[] frames, QrPngRenderOptions opts, int durationMs, string directory, string fileName, GifAnimationOptions options = default) {
        var gif = RenderAnimation(frames, opts, durationMs, options);
        return RenderIO.WriteBinary(directory, fileName, gif);
    }

    /// <summary>
    /// Renders an animated GIF to a file under the specified directory with per-frame durations.
    /// </summary>
    public static string RenderAnimationToFile(BitMatrix[] frames, QrPngRenderOptions opts, int[] durationsMs, string directory, string fileName, GifAnimationOptions options = default) {
        var gif = RenderAnimation(frames, opts, durationsMs, options);
        return RenderIO.WriteBinary(directory, fileName, gif);
    }
}
