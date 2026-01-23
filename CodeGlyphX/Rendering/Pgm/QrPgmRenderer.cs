using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pgm;

/// <summary>
/// Renders QR modules to PGM (P5).
/// </summary>
public static class QrPgmRenderer {
    /// <summary>
    /// Renders the QR module matrix to a PGM byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return PgmWriter.WriteRgba32Scanlines(width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a PGM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        PgmWriter.WriteRgba32Scanlines(stream, width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a PGM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path) {
        var pgm = Render(modules, opts);
        return RenderIO.WriteBinary(path, pgm);
    }

    /// <summary>
    /// Renders the QR module matrix to a PGM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName) {
        var pgm = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, pgm);
    }

}
