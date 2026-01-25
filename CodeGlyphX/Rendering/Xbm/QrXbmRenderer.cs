using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Xbm;

/// <summary>
/// Renders QR modules to XBM.
/// </summary>
public static class QrXbmRenderer {
    /// <summary>
    /// Renders the QR module matrix to an XBM string.
    /// </summary>
    public static string Render(BitMatrix modules, QrPngRenderOptions opts, string? name = null) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return XbmWriter.WriteRgba32Scanlines(width, height, scanlines, stride, name);
    }

    /// <summary>
    /// Renders the QR module matrix to an XBM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, string? name = null) {
        var xbm = Render(modules, opts, name);
        RenderIO.WriteText(stream, xbm);
    }

    /// <summary>
    /// Renders the QR module matrix to an XBM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path, string? name = null) {
        var xbm = Render(modules, opts, name);
        return RenderIO.WriteText(path, xbm);
    }

    /// <summary>
    /// Renders the QR module matrix to an XBM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName, string? name = null) {
        var xbm = Render(modules, opts, name);
        return RenderIO.WriteText(directory, fileName, xbm);
    }

}
