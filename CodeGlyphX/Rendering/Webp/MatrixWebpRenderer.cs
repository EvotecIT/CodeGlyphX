using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Renders generic 2D matrices to WebP images (lossless VP8L).
/// </summary>
public static class MatrixWebpRenderer {
    /// <summary>
    /// Renders the matrix to a WebP byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return WebpWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the matrix to a WebP stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        var webp = Render(modules, opts);
        RenderIO.WriteBinary(stream, webp);
    }

    /// <summary>
    /// Renders the matrix to a WebP file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path) {
        var webp = Render(modules, opts);
        return RenderIO.WriteBinary(path, webp);
    }

    /// <summary>
    /// Renders the matrix to a WebP file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName) {
        var webp = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, webp);
    }
}
