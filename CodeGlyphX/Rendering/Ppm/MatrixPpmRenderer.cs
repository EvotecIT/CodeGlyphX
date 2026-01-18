using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ppm;

/// <summary>
/// Renders generic 2D matrices to PPM (P6).
/// </summary>
public static class MatrixPpmRenderer {
    /// <summary>
    /// Renders the matrix to a PPM byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, height, stride);
        return PpmWriter.WriteRgba32(width, height, rgba, stride);
    }

    /// <summary>
    /// Renders the matrix to a PPM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, height, stride);
        PpmWriter.WriteRgba32(stream, width, height, rgba, stride);
    }

    /// <summary>
    /// Renders the matrix to a PPM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path) {
        var ppm = Render(modules, opts);
        return RenderIO.WriteBinary(path, ppm);
    }

    /// <summary>
    /// Renders the matrix to a PPM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName) {
        var ppm = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, ppm);
    }

    private static byte[] ExtractRgba(byte[] scanlines, int height, int stride) {
        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, rgba, y * stride, stride);
        }
        return rgba;
    }
}
