using System;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Rendering.Jpeg;

/// <summary>
/// Renders generic 2D matrices to a JPEG image (baseline, 4:4:4).
/// </summary>
public static class MatrixJpegRenderer {
    /// <summary>
    /// Renders the matrix to a JPEG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts, int quality = 85) {
        var scanlines = MatrixPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, height, stride);
        return JpegWriter.WriteRgba(width, height, rgba, stride, quality);
    }

    private static byte[] ExtractRgba(byte[] scanlines, int height, int stride) {
        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, rgba, y * stride, stride);
        }
        return rgba;
    }
}
