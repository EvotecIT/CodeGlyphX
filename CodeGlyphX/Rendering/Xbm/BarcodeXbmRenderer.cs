using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Xbm;

/// <summary>
/// Renders barcodes to XBM.
/// </summary>
public static class BarcodeXbmRenderer {
    /// <summary>
    /// Renders a barcode to an XBM string.
    /// </summary>
    public static string Render(Barcode1D barcode, BarcodePngRenderOptions opts, string? name = null) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        return XbmWriter.WriteRgba32Scanlines(width, height, scanlines, stride, name);
    }

    /// <summary>
    /// Renders a barcode to an XBM stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream, string? name = null) {
        var xbm = Render(barcode, opts, name);
        RenderIO.WriteText(stream, xbm);
    }

    /// <summary>
    /// Renders a barcode to an XBM file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path, string? name = null) {
        var xbm = Render(barcode, opts, name);
        return RenderIO.WriteText(path, xbm);
    }

    /// <summary>
    /// Renders a barcode to an XBM file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName, string? name = null) {
        var xbm = Render(barcode, opts, name);
        return RenderIO.WriteText(directory, fileName, xbm);
    }

}
