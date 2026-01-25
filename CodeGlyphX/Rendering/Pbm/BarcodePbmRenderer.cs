using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pbm;

/// <summary>
/// Renders 1D barcodes to PBM (P4).
/// </summary>
public static class BarcodePbmRenderer {
    /// <summary>
    /// Renders the barcode to a PBM byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        return PbmWriter.WriteRgba32Scanlines(width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the barcode to a PBM stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        PbmWriter.WriteRgba32Scanlines(stream, width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the barcode to a PBM file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var pbm = Render(barcode, opts);
        return RenderIO.WriteBinary(path, pbm);
    }

    /// <summary>
    /// Renders the barcode to a PBM file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var pbm = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, pbm);
    }

}
