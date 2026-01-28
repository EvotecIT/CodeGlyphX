using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Renders generic 2D matrices to WebP images.
/// </summary>
public static class MatrixWebpRenderer {
    /// <summary>
    /// Renders the matrix to a WebP byte array (lossless VP8L).
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return WebpWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the matrix to a WebP byte array (lossy VP8 when possible).
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="quality">Quality (0-100). Values &gt;= 100 use lossless VP8L.</param>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts, int quality = 100) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return WebpWriter.WriteRgba32Lossy(widthPx, heightPx, pixels, stride, quality);
    }

    /// <summary>
    /// Renders the matrix to a WebP stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        var webp = Render(modules, opts);
        RenderIO.WriteBinary(stream, webp);
    }

    /// <summary>
    /// Renders the matrix to a WebP stream (lossy VP8 when possible).
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="quality">Quality (0-100). Values &gt;= 100 use lossless VP8L.</param>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream, int quality = 100) {
        var webp = Render(modules, opts, quality);
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
    /// Renders the matrix to a WebP file (lossy VP8 when possible).
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="quality">Quality (0-100). Values &gt;= 100 use lossless VP8L.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path, int quality = 100) {
        var webp = Render(modules, opts, quality);
        return RenderIO.WriteBinary(path, webp);
    }

    /// <summary>
    /// Renders the matrix to a WebP file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName) {
        var webp = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, webp);
    }

    /// <summary>
    /// Renders the matrix to a WebP file under the specified directory (lossy VP8 when possible).
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="quality">Quality (0-100). Values &gt;= 100 use lossless VP8L.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName, int quality = 100) {
        var webp = Render(modules, opts, quality);
        return RenderIO.WriteBinary(directory, fileName, webp);
    }
}
