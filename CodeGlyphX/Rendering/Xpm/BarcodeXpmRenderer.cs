using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Xpm;

/// <summary>
/// Renders barcodes to XPM.
/// </summary>
public static class BarcodeXpmRenderer {
    /// <summary>
    /// Renders a barcode to an XPM string.
    /// </summary>
    public static string Render(Barcode1D barcode, BarcodePngRenderOptions opts, string? name = null) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        return XpmWriter.WriteRgba32Scanlines(width, height, scanlines, stride, name, opts.Foreground, opts.Background);
    }

    /// <summary>
    /// Renders a barcode to an XPM stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream, string? name = null) {
        var xpm = Render(barcode, opts, name);
        RenderIO.WriteText(stream, xpm);
    }

    /// <summary>
    /// Renders a barcode to an XPM file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path, string? name = null) {
        var xpm = Render(barcode, opts, name);
        return RenderIO.WriteText(path, xpm);
    }

    /// <summary>
    /// Renders a barcode to an XPM file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName, string? name = null) {
        var xpm = Render(barcode, opts, name);
        return RenderIO.WriteText(directory, fileName, xpm);
    }

}
