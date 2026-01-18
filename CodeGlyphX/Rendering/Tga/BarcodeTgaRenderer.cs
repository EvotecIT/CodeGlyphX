using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Tga;

/// <summary>
/// Renders 1D barcodes to TGA.
/// </summary>
public static class BarcodeTgaRenderer {
    /// <summary>
    /// Renders the barcode to a TGA byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, height, stride);
        return TgaWriter.WriteRgba32(width, height, rgba, stride);
    }

    /// <summary>
    /// Renders the barcode to a TGA stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, height, stride);
        TgaWriter.WriteRgba32(stream, width, height, rgba, stride);
    }

    /// <summary>
    /// Renders the barcode to a TGA file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var tga = Render(barcode, opts);
        return RenderIO.WriteBinary(path, tga);
    }

    /// <summary>
    /// Renders the barcode to a TGA file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var tga = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, tga);
    }

    private static byte[] ExtractRgba(byte[] scanlines, int height, int stride) {
        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, rgba, y * stride, stride);
        }
        return rgba;
    }
}
