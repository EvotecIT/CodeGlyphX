using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Xpm;

/// <summary>
/// Renders QR modules to XPM.
/// </summary>
public static class QrXpmRenderer {
    /// <summary>
    /// Renders the QR module matrix to an XPM string.
    /// </summary>
    public static string Render(BitMatrix modules, QrPngRenderOptions opts, string? name = null) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return XpmWriter.WriteRgba32Scanlines(width, height, scanlines, stride, name, opts.Foreground, opts.Background);
    }

    /// <summary>
    /// Renders the QR module matrix to an XPM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, string? name = null) {
        var xpm = Render(modules, opts, name);
        RenderIO.WriteText(stream, xpm);
    }

    /// <summary>
    /// Renders the QR module matrix to an XPM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path, string? name = null) {
        var xpm = Render(modules, opts, name);
        return RenderIO.WriteText(path, xpm);
    }

    /// <summary>
    /// Renders the QR module matrix to an XPM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName, string? name = null) {
        var xpm = Render(modules, opts, name);
        return RenderIO.WriteText(directory, fileName, xpm);
    }

}
