using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Renders 1D barcodes to WebP images.
/// </summary>
public static class BarcodeWebpRenderer {
    /// <summary>
    /// Renders the barcode to a WebP byte array (lossless VP8L).
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        return WebpWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the barcode to a WebP byte array (lossy VP8 when possible).
    /// </summary>
    /// <param name="barcode">Barcode data.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="quality">Quality (0-100). Values &gt;= 100 use lossless VP8L.</param>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts, int quality = 100) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        return WebpWriter.WriteRgba32Lossy(widthPx, heightPx, pixels, stride, quality);
    }

    /// <summary>
    /// Renders the barcode to a WebP stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var webp = Render(barcode, opts);
        RenderIO.WriteBinary(stream, webp);
    }

    /// <summary>
    /// Renders the barcode to a WebP stream (lossy VP8 when possible).
    /// </summary>
    /// <param name="barcode">Barcode data.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="quality">Quality (0-100). Values &gt;= 100 use lossless VP8L.</param>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream, int quality = 100) {
        var webp = Render(barcode, opts, quality);
        RenderIO.WriteBinary(stream, webp);
    }

    /// <summary>
    /// Renders the barcode to a WebP file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var webp = Render(barcode, opts);
        return RenderIO.WriteBinary(path, webp);
    }

    /// <summary>
    /// Renders the barcode to a WebP file (lossy VP8 when possible).
    /// </summary>
    /// <param name="barcode">Barcode data.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="quality">Quality (0-100). Values &gt;= 100 use lossless VP8L.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path, int quality = 100) {
        var webp = Render(barcode, opts, quality);
        return RenderIO.WriteBinary(path, webp);
    }

    /// <summary>
    /// Renders the barcode to a WebP file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var webp = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, webp);
    }

    /// <summary>
    /// Renders the barcode to a WebP file under the specified directory (lossy VP8 when possible).
    /// </summary>
    /// <param name="barcode">Barcode data.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="quality">Quality (0-100). Values &gt;= 100 use lossless VP8L.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName, int quality = 100) {
        var webp = Render(barcode, opts, quality);
        return RenderIO.WriteBinary(directory, fileName, webp);
    }

    /// <summary>
    /// Renders an animated WebP from multiple barcode frames.
    /// </summary>
    public static byte[] RenderAnimation(Barcode1D[] frames, BarcodePngRenderOptions opts, int durationMs, WebpAnimationOptions options = default, int quality = 100) {
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        if (frames.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(frames));
        if (durationMs <= 0) throw new ArgumentOutOfRangeException(nameof(durationMs));

        var webpFrames = new WebpAnimationFrame[frames.Length];
        var canvasWidth = 0;
        var canvasHeight = 0;
        for (var i = 0; i < frames.Length; i++) {
            var pixels = BarcodePngRenderer.RenderPixels(frames[i], opts, out var widthPx, out var heightPx, out var stride);
            if (i == 0) {
                canvasWidth = widthPx;
                canvasHeight = heightPx;
            } else if (widthPx != canvasWidth || heightPx != canvasHeight) {
                throw new ArgumentException("All frames must render to the same pixel size.", nameof(frames));
            }
            webpFrames[i] = new WebpAnimationFrame(pixels, widthPx, heightPx, stride, durationMs);
        }

        return quality >= 100
            ? WebpWriter.WriteAnimationRgba32(canvasWidth, canvasHeight, webpFrames, options)
            : WebpWriter.WriteAnimationRgba32Lossy(canvasWidth, canvasHeight, webpFrames, options, quality);
    }

    /// <summary>
    /// Renders an animated WebP from multiple barcode frames with per-frame durations.
    /// </summary>
    public static byte[] RenderAnimation(Barcode1D[] frames, BarcodePngRenderOptions opts, int[] durationsMs, WebpAnimationOptions options = default, int quality = 100) {
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        if (durationsMs is null) throw new ArgumentNullException(nameof(durationsMs));
        if (frames.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(frames));
        if (frames.Length != durationsMs.Length) throw new ArgumentException("Durations must match frame count.", nameof(durationsMs));

        var webpFrames = new WebpAnimationFrame[frames.Length];
        var canvasWidth = 0;
        var canvasHeight = 0;
        for (var i = 0; i < frames.Length; i++) {
            var duration = durationsMs[i];
            if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(durationsMs));
            var pixels = BarcodePngRenderer.RenderPixels(frames[i], opts, out var widthPx, out var heightPx, out var stride);
            if (i == 0) {
                canvasWidth = widthPx;
                canvasHeight = heightPx;
            } else if (widthPx != canvasWidth || heightPx != canvasHeight) {
                throw new ArgumentException("All frames must render to the same pixel size.", nameof(frames));
            }
            webpFrames[i] = new WebpAnimationFrame(pixels, widthPx, heightPx, stride, duration);
        }

        return quality >= 100
            ? WebpWriter.WriteAnimationRgba32(canvasWidth, canvasHeight, webpFrames, options)
            : WebpWriter.WriteAnimationRgba32Lossy(canvasWidth, canvasHeight, webpFrames, options, quality);
    }

    /// <summary>
    /// Renders an animated WebP to a stream from multiple barcode frames.
    /// </summary>
    public static void RenderAnimationToStream(Barcode1D[] frames, BarcodePngRenderOptions opts, int durationMs, Stream stream, WebpAnimationOptions options = default, int quality = 100) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var webp = RenderAnimation(frames, opts, durationMs, options, quality);
        RenderIO.WriteBinary(stream, webp);
    }

    /// <summary>
    /// Renders an animated WebP to a stream from multiple barcode frames with per-frame durations.
    /// </summary>
    public static void RenderAnimationToStream(Barcode1D[] frames, BarcodePngRenderOptions opts, int[] durationsMs, Stream stream, WebpAnimationOptions options = default, int quality = 100) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var webp = RenderAnimation(frames, opts, durationsMs, options, quality);
        RenderIO.WriteBinary(stream, webp);
    }

    /// <summary>
    /// Renders an animated WebP to a file from multiple barcode frames.
    /// </summary>
    public static string RenderAnimationToFile(Barcode1D[] frames, BarcodePngRenderOptions opts, int durationMs, string path, WebpAnimationOptions options = default, int quality = 100) {
        var webp = RenderAnimation(frames, opts, durationMs, options, quality);
        return RenderIO.WriteBinary(path, webp);
    }

    /// <summary>
    /// Renders an animated WebP to a file from multiple barcode frames with per-frame durations.
    /// </summary>
    public static string RenderAnimationToFile(Barcode1D[] frames, BarcodePngRenderOptions opts, int[] durationsMs, string path, WebpAnimationOptions options = default, int quality = 100) {
        var webp = RenderAnimation(frames, opts, durationsMs, options, quality);
        return RenderIO.WriteBinary(path, webp);
    }

    /// <summary>
    /// Renders an animated WebP to a file under the specified directory.
    /// </summary>
    public static string RenderAnimationToFile(Barcode1D[] frames, BarcodePngRenderOptions opts, int durationMs, string directory, string fileName, WebpAnimationOptions options = default, int quality = 100) {
        var webp = RenderAnimation(frames, opts, durationMs, options, quality);
        return RenderIO.WriteBinary(directory, fileName, webp);
    }

    /// <summary>
    /// Renders an animated WebP to a file under the specified directory with per-frame durations.
    /// </summary>
    public static string RenderAnimationToFile(Barcode1D[] frames, BarcodePngRenderOptions opts, int[] durationsMs, string directory, string fileName, WebpAnimationOptions options = default, int quality = 100) {
        var webp = RenderAnimation(frames, opts, durationsMs, options, quality);
        return RenderIO.WriteBinary(directory, fileName, webp);
    }
}
