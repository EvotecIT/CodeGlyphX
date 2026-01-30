using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Renders QR modules to a TIFF image.
/// </summary>
public static class QrTiffRenderer {
    /// <summary>
    /// Renders the QR module matrix to a TIFF byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return TiffWriter.WriteRgba32(widthPx, heightPx, pixels, stride, compression);
    }

    /// <summary>
    /// Renders the QR module matrix to a TIFF stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        TiffWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride, compression);
    }

    /// <summary>
    /// Renders the QR module matrix to a TIFF file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        var tiff = Render(modules, opts, compression);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the QR module matrix to a TIFF file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        var tiff = Render(modules, opts, compression);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }
}
