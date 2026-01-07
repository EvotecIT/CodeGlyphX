using System;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Rendering.Jpeg;

/// <summary>
/// Renders QR modules to a JPEG image (baseline, 4:4:4).
/// </summary>
public static class QrJpegRenderer {
    /// <summary>
    /// Renders the QR module matrix to a JPEG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts, int quality = 85) {
        var scanlines = QrPngRenderer.RenderScanlines(modules, opts, out var width, out var height, out var stride);
        var rgba = ExtractRgba(scanlines, width, height, stride);
        return JpegWriter.WriteRgba(width, height, rgba, stride, quality);
    }

    private static byte[] ExtractRgba(byte[] scanlines, int width, int height, int stride) {
        var rgba = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, rgba, y * stride, stride);
        }
        return rgba;
    }
}
