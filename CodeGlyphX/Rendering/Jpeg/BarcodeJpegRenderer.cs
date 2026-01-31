using System;
using System.Buffers;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Jpeg;

/// <summary>
/// Renders 1D barcodes to a JPEG image.
/// </summary>
public static class BarcodeJpegRenderer {
    /// <summary>
    /// Renders the barcode to a JPEG byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts, int quality = 85) {
        var length = BarcodePngRenderer.GetScanlineLength(barcode, opts, out var width, out var height, out var stride);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            BarcodePngRenderer.RenderScanlines(barcode, opts, out width, out height, out stride, scanlines);
            return JpegWriter.WriteRgbaScanlines(width, height, scanlines, stride, quality);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    /// <summary>
    /// Renders the barcode to a JPEG byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts, JpegEncodeOptions options) {
        var length = BarcodePngRenderer.GetScanlineLength(barcode, opts, out var width, out var height, out var stride);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            BarcodePngRenderer.RenderScanlines(barcode, opts, out width, out height, out stride, scanlines);
            return JpegWriter.WriteRgbaScanlines(width, height, scanlines, stride, options);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    /// <summary>
    /// Renders the barcode to a JPEG stream.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream, int quality = 85) {
        var length = BarcodePngRenderer.GetScanlineLength(barcode, opts, out var width, out var height, out var stride);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            BarcodePngRenderer.RenderScanlines(barcode, opts, out width, out height, out stride, scanlines);
            JpegWriter.WriteRgbaScanlines(stream, width, height, scanlines, stride, quality);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    /// <summary>
    /// Renders the barcode to a JPEG stream.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="options">JPEG encoding options.</param>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream, JpegEncodeOptions options) {
        var length = BarcodePngRenderer.GetScanlineLength(barcode, opts, out var width, out var height, out var stride);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            BarcodePngRenderer.RenderScanlines(barcode, opts, out width, out height, out stride, scanlines);
            JpegWriter.WriteRgbaScanlines(stream, width, height, scanlines, stride, options);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    /// <summary>
    /// Renders the barcode to a JPEG file.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path, int quality = 85) {
        var jpeg = Render(barcode, opts, quality);
        return RenderIO.WriteBinary(path, jpeg);
    }

    /// <summary>
    /// Renders the barcode to a JPEG file.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path, JpegEncodeOptions options) {
        var jpeg = Render(barcode, opts, options);
        return RenderIO.WriteBinary(path, jpeg);
    }

    /// <summary>
    /// Renders the barcode to a JPEG file under the specified directory.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="quality">JPEG quality (1-100).</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName, int quality = 85) {
        var jpeg = Render(barcode, opts, quality);
        return RenderIO.WriteBinary(directory, fileName, jpeg);
    }

    /// <summary>
    /// Renders the barcode to a JPEG file under the specified directory.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName, JpegEncodeOptions options) {
        var jpeg = Render(barcode, opts, options);
        return RenderIO.WriteBinary(directory, fileName, jpeg);
    }

}
