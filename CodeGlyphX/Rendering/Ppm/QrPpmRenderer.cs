using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ppm;

/// <summary>
/// Renders QR modules to PPM (P6).
/// </summary>
public static class QrPpmRenderer {
    /// <summary>
    /// Renders the QR module matrix to a PPM byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return PpmWriter.WriteRgba32Scanlines(width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a PPM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        PpmWriter.WriteRgba32Scanlines(stream, width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a PPM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path) {
        var ppm = Render(modules, opts);
        return RenderIO.WriteBinary(path, ppm);
    }

    /// <summary>
    /// Renders the QR module matrix to a PPM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName) {
        var ppm = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, ppm);
    }

}
