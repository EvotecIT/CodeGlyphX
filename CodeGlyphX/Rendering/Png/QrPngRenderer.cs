using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Renders QR modules to a PNG image (RGBA8).
/// </summary>
public static partial class QrPngRenderer {
    /// <summary>
    /// Renders the QR module matrix to a PNG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        if (TryRenderGray1(modules, opts, out var gray)) return gray;
        if (TryRenderIndexed1(modules, opts, out var indexed)) return indexed;
        var scanlines = RenderScanlines(modules, opts, out var widthPx, out var heightPx, out _);
        return PngWriter.WriteRgba8(widthPx, heightPx, scanlines);
    }

    /// <summary>
    /// Renders the QR module matrix to a PNG stream.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        if (TryRenderGray1ToStream(modules, opts, stream)) return;
        if (TryRenderIndexed1ToStream(modules, opts, stream)) return;
        var scanlines = RenderScanlines(modules, opts, out var widthPx, out var heightPx, out _);
        PngWriter.WriteRgba8(stream, widthPx, heightPx, scanlines);
    }

    /// <summary>
    /// Renders the QR module matrix to a PNG file.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path) {
        var png = Render(modules, opts);
        return RenderIO.WriteBinary(path, png);
    }

    /// <summary>
    /// Renders the QR module matrix to a PNG file under the specified directory.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName) {
        var png = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, png);
    }

    /// <summary>
    /// Renders the QR module matrix to a raw RGBA pixel buffer (no PNG encoding).
    /// </summary>
    public static byte[] RenderPixels(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        var scanlines = RenderScanlines(modules, opts, out widthPx, out heightPx, out stride);
        var pixels = new byte[heightPx * stride];
        for (var y = 0; y < heightPx; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, pixels, y * stride, stride);
        }
        return pixels;
    }
}
