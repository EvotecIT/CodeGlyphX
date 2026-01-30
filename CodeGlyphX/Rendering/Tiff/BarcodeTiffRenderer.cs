using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Renders barcodes to a TIFF image.
/// </summary>
public static class BarcodeTiffRenderer {
    /// <summary>
    /// Renders the barcode to a TIFF byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        return TiffWriter.WriteRgba32(widthPx, heightPx, pixels, stride, compression);
    }

    /// <summary>
    /// Renders the barcode to a TIFF stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        TiffWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride, compression);
    }

    /// <summary>
    /// Renders the barcode to a TIFF file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        var tiff = Render(barcode, opts, compression);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the barcode to a TIFF file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        var tiff = Render(barcode, opts, compression);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }
}
