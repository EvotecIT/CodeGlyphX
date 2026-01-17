using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Bmp;

/// <summary>
/// Renders 1D barcodes to BMP images (BGRA32).
/// </summary>
public static class BarcodeBmpRenderer {
    /// <summary>
    /// Renders the barcode to a BMP byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        return BmpWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the barcode to a BMP stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        BmpWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the barcode to a BMP file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var bmp = Render(barcode, opts);
        return RenderIO.WriteBinary(path, bmp);
    }

    /// <summary>
    /// Renders the barcode to a BMP file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var bmp = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, bmp);
    }
}
