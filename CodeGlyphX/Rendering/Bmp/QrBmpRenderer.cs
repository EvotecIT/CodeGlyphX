using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Bmp;

/// <summary>
/// Renders QR modules to a BMP image (BGRA32).
/// </summary>
public static class QrBmpRenderer {
    /// <summary>
    /// Renders the QR module matrix to a BMP byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return BmpWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a BMP stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        BmpWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a BMP file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path) {
        var bmp = Render(modules, opts);
        return RenderIO.WriteBinary(path, bmp);
    }

    /// <summary>
    /// Renders the QR module matrix to a BMP file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName) {
        var bmp = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, bmp);
    }
}
