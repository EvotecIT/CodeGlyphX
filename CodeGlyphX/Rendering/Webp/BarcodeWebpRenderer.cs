using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Renders 1D barcodes to WebP images (lossless VP8L).
/// </summary>
public static class BarcodeWebpRenderer {
    /// <summary>
    /// Renders the barcode to a WebP byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        return WebpWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the barcode to a WebP stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var webp = Render(barcode, opts);
        RenderIO.WriteBinary(stream, webp);
    }

    /// <summary>
    /// Renders the barcode to a WebP file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var webp = Render(barcode, opts);
        return RenderIO.WriteBinary(path, webp);
    }

    /// <summary>
    /// Renders the barcode to a WebP file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var webp = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, webp);
    }
}
