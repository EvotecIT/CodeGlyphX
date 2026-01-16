using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Bmp;

/// <summary>
/// Renders generic 2D matrices to BMP images (BGRA32).
/// </summary>
public static class MatrixBmpRenderer {
    /// <summary>
    /// Renders the matrix to a BMP byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return BmpWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the matrix to a BMP stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        BmpWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the matrix to a BMP file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path) {
        var bmp = Render(modules, opts);
        return RenderIO.WriteBinary(path, bmp);
    }

    /// <summary>
    /// Renders the matrix to a BMP file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName) {
        var bmp = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, bmp);
    }
}
