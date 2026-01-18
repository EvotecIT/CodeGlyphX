using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pbm;

/// <summary>
/// Renders generic 2D matrices to PBM (P4).
/// </summary>
public static class MatrixPbmRenderer {
    /// <summary>
    /// Renders the matrix to a PBM byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, height, stride);
        return PbmWriter.WriteRgba32(width, height, rgba, stride);
    }

    /// <summary>
    /// Renders the matrix to a PBM stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, height, stride);
        PbmWriter.WriteRgba32(stream, width, height, rgba, stride);
    }

    /// <summary>
    /// Renders the matrix to a PBM file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path) {
        var pbm = Render(modules, opts);
        return RenderIO.WriteBinary(path, pbm);
    }

    /// <summary>
    /// Renders the matrix to a PBM file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName) {
        var pbm = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, pbm);
    }

    private static byte[] ExtractRgba(byte[] scanlines, int height, int stride) {
        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, rgba, y * stride, stride);
        }
        return rgba;
    }
}
