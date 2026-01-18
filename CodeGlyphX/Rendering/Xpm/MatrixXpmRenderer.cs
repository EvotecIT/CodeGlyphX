using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Xpm;

/// <summary>
/// Renders matrix modules to XPM.
/// </summary>
public static class MatrixXpmRenderer {
    /// <summary>
    /// Renders the module matrix to an XPM string.
    /// </summary>
    public static string Render(BitMatrix modules, MatrixPngRenderOptions opts, string? name = null) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, height, stride);
        return XpmWriter.WriteRgba32(width, height, rgba, stride, name, opts.Foreground, opts.Background);
    }

    /// <summary>
    /// Renders the module matrix to an XPM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream, string? name = null) {
        var xpm = Render(modules, opts, name);
        RenderIO.WriteText(stream, xpm);
    }

    /// <summary>
    /// Renders the module matrix to an XPM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path, string? name = null) {
        var xpm = Render(modules, opts, name);
        return RenderIO.WriteText(path, xpm);
    }

    /// <summary>
    /// Renders the module matrix to an XPM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName, string? name = null) {
        var xpm = Render(modules, opts, name);
        return RenderIO.WriteText(directory, fileName, xpm);
    }

    private static byte[] ExtractRgba(byte[] scanlines, int height, int stride) {
        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, rgba, y * stride, stride);
        }
        return rgba;
    }
}
