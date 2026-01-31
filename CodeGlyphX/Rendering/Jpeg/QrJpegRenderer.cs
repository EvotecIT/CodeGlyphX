using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Jpeg;

/// <summary>
/// Renders QR modules to a JPEG image.
/// </summary>
public static class QrJpegRenderer {
    /// <summary>
    /// Renders the QR module matrix to a JPEG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts, int quality = 85) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return JpegWriter.WriteRgbaScanlines(width, height, scanlines, stride, quality);
    }

    /// <summary>
    /// Renders the QR module matrix to a JPEG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts, JpegEncodeOptions options) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return JpegWriter.WriteRgbaScanlines(width, height, scanlines, stride, options);
    }

    /// <summary>
    /// Renders the QR module matrix to a JPEG stream.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, int quality = 85) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        JpegWriter.WriteRgbaScanlines(stream, width, height, scanlines, stride, quality);
    }

    /// <summary>
    /// Renders the QR module matrix to a JPEG stream.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="options">JPEG encoding options.</param>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, JpegEncodeOptions options) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        JpegWriter.WriteRgbaScanlines(stream, width, height, scanlines, stride, options);
    }

    /// <summary>
    /// Renders the QR module matrix to a JPEG file.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path, int quality = 85) {
        var jpeg = Render(modules, opts, quality);
        return RenderIO.WriteBinary(path, jpeg);
    }

    /// <summary>
    /// Renders the QR module matrix to a JPEG file.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path, JpegEncodeOptions options) {
        var jpeg = Render(modules, opts, options);
        return RenderIO.WriteBinary(path, jpeg);
    }

    /// <summary>
    /// Renders the QR module matrix to a JPEG file under the specified directory.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName, int quality = 85) {
        var jpeg = Render(modules, opts, quality);
        return RenderIO.WriteBinary(directory, fileName, jpeg);
    }

    /// <summary>
    /// Renders the QR module matrix to a JPEG file under the specified directory.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName, JpegEncodeOptions options) {
        var jpeg = Render(modules, opts, options);
        return RenderIO.WriteBinary(directory, fileName, jpeg);
    }

}
