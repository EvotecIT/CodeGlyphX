using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Xbm;

/// <summary>
/// Renders matrix modules to XBM.
/// </summary>
public static class MatrixXbmRenderer {
    /// <summary>
    /// Renders the module matrix to an XBM string.
    /// </summary>
    public static string Render(BitMatrix modules, MatrixPngRenderOptions opts, string? name = null) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return XbmWriter.WriteRgba32Scanlines(width, height, scanlines, stride, name);
    }

    /// <summary>
    /// Renders the module matrix to an XBM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream, string? name = null) {
        var xbm = Render(modules, opts, name);
        RenderIO.WriteText(stream, xbm);
    }

    /// <summary>
    /// Renders the module matrix to an XBM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path, string? name = null) {
        var xbm = Render(modules, opts, name);
        return RenderIO.WriteText(path, xbm);
    }

    /// <summary>
    /// Renders the module matrix to an XBM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName, string? name = null) {
        var xbm = Render(modules, opts, name);
        return RenderIO.WriteText(directory, fileName, xbm);
    }

}
