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
        var rgba = ExtractRgba(scanlines, height, stride);
        return XbmWriter.WriteRgba32(width, height, rgba, stride, name);
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

    private static byte[] ExtractRgba(byte[] scanlines, int height, int stride) {
        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, rgba, y * stride, stride);
        }
        return rgba;
    }
}
