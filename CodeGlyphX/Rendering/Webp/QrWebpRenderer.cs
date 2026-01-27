using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Renders QR modules to WebP images (lossless VP8L).
/// </summary>
public static class QrWebpRenderer {
    /// <summary>
    /// Renders the QR module matrix to a WebP byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        var pixels = QrPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return WebpWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a WebP stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        var webp = Render(modules, opts);
        RenderIO.WriteBinary(stream, webp);
    }

    /// <summary>
    /// Renders the QR module matrix to a WebP file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path) {
        var webp = Render(modules, opts);
        return RenderIO.WriteBinary(path, webp);
    }

    /// <summary>
    /// Renders the QR module matrix to a WebP file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName) {
        var webp = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, webp);
    }
}
