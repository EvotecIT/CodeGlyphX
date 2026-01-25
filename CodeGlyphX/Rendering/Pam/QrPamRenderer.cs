using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pam;

/// <summary>
/// Renders QR modules to PAM (P7).
/// </summary>
public static class QrPamRenderer {
    /// <summary>
    /// Renders the QR module matrix to a PAM byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return PamWriter.WriteRgba32Scanlines(width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a PAM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        PamWriter.WriteRgba32Scanlines(stream, width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the QR module matrix to a PAM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path) {
        var pam = Render(modules, opts);
        return RenderIO.WriteBinary(path, pam);
    }

    /// <summary>
    /// Renders the QR module matrix to a PAM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName) {
        var pam = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, pam);
    }

}
