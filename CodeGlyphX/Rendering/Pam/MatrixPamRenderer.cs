using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pam;

/// <summary>
/// Renders matrix modules to PAM (P7).
/// </summary>
public static class MatrixPamRenderer {
    /// <summary>
    /// Renders the module matrix to a PAM byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        return PamWriter.WriteRgba32Scanlines(width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the module matrix to a PAM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        PamWriter.WriteRgba32Scanlines(stream, width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders the module matrix to a PAM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path) {
        var pam = Render(modules, opts);
        return RenderIO.WriteBinary(path, pam);
    }

    /// <summary>
    /// Renders the module matrix to a PAM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName) {
        var pam = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, pam);
    }

}
