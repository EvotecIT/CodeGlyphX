using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ppm;

/// <summary>
/// Renders 1D barcodes to PPM (P6).
/// </summary>
public static class BarcodePpmRenderer {
    /// <summary>
    /// Renders the barcode to a PPM byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        return PpmWriter.WriteRgba32Scanlines(width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the barcode to a PPM stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        PpmWriter.WriteRgba32Scanlines(stream, width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the barcode to a PPM file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var ppm = Render(barcode, opts);
        return RenderIO.WriteBinary(path, ppm);
    }

    /// <summary>
    /// Renders the barcode to a PPM file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var ppm = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, ppm);
    }

}
