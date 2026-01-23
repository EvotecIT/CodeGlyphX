using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pgm;

/// <summary>
/// Renders barcodes to PGM (P5).
/// </summary>
public static class BarcodePgmRenderer {
    /// <summary>
    /// Renders a barcode to a PGM byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        return PgmWriter.WriteRgba32Scanlines(width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders a barcode to a PGM stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        PgmWriter.WriteRgba32Scanlines(stream, width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders a barcode to a PGM file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var pgm = Render(barcode, opts);
        return RenderIO.WriteBinary(path, pgm);
    }

    /// <summary>
    /// Renders a barcode to a PGM file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var pgm = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, pgm);
    }

}
