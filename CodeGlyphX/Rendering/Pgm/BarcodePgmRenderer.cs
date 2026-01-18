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
        var rgba = ExtractRgba(scanlines, height, stride);
        return PgmWriter.WriteRgba32(width, height, rgba, stride);
    }

    /// <summary>
    /// Renders a barcode to a PGM stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, height, stride);
        PgmWriter.WriteRgba32(stream, width, height, rgba, stride);
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

    private static byte[] ExtractRgba(byte[] scanlines, int height, int stride) {
        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, rgba, y * stride, stride);
        }
        return rgba;
    }
}
