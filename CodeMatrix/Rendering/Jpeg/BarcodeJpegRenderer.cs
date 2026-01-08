using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Jpeg;

/// <summary>
/// Renders 1D barcodes to a JPEG image (baseline, 4:4:4).
/// </summary>
public static class BarcodeJpegRenderer {
    /// <summary>
    /// Renders the barcode to a JPEG byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts, int quality = 85) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, width, height, stride);
        return JpegWriter.WriteRgba(width, height, rgba, stride, quality);
    }

    /// <summary>
    /// Renders the barcode to a JPEG stream.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream, int quality = 85) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, width, height, stride);
        JpegWriter.WriteRgba(stream, width, height, rgba, stride, quality);
    }

    /// <summary>
    /// Renders the barcode to a JPEG file.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path, int quality = 85) {
        var jpeg = Render(barcode, opts, quality);
        return RenderIO.WriteBinary(path, jpeg);
    }

    /// <summary>
    /// Renders the barcode to a JPEG file under the specified directory.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName, int quality = 85) {
        var jpeg = Render(barcode, opts, quality);
        return RenderIO.WriteBinary(directory, fileName, jpeg);
    }

    private static byte[] ExtractRgba(byte[] scanlines, int width, int height, int stride) {
        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, rgba, y * stride, stride);
        }
        return rgba;
    }
}
