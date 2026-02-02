using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Jpeg;

/// <summary>
/// Renders generic 2D matrices to a JPEG image.
/// </summary>
public static class MatrixJpegRenderer {
    /// <summary>
    /// Renders the matrix to a JPEG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts, int quality = 85) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return JpegWriter.WriteRgbaScanlines(width, height, scanlines, stride, quality);
    }

    /// <summary>
    /// Renders the matrix to a JPEG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts, JpegEncodeOptions options) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return JpegWriter.WriteRgbaScanlines(width, height, scanlines, stride, options);
    }

    /// <summary>
    /// Renders the matrix to a JPEG stream.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream, int quality = 85) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        JpegWriter.WriteRgbaScanlines(stream, width, height, scanlines, stride, quality);
    }

    /// <summary>
    /// Renders the matrix to a JPEG stream.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="options">JPEG encoding options.</param>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream, JpegEncodeOptions options) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        JpegWriter.WriteRgbaScanlines(stream, width, height, scanlines, stride, options);
    }

    /// <summary>
    /// Renders the matrix to a JPEG file.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path, int quality = 85) {
        var jpeg = Render(modules, opts, quality);
        return RenderIO.WriteBinary(path, jpeg);
    }

    /// <summary>
    /// Renders the matrix to a JPEG file.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path, JpegEncodeOptions options) {
        var jpeg = Render(modules, opts, options);
        return RenderIO.WriteBinary(path, jpeg);
    }

    /// <summary>
    /// Renders the matrix to a JPEG file under the specified directory.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName, int quality = 85) {
        var jpeg = Render(modules, opts, quality);
        return RenderIO.WriteBinary(directory, fileName, jpeg);
    }

    /// <summary>
    /// Renders the matrix to a JPEG file under the specified directory.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName, JpegEncodeOptions options) {
        var jpeg = Render(modules, opts, options);
        return RenderIO.WriteBinary(directory, fileName, jpeg);
    }

}
