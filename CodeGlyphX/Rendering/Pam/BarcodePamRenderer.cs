using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Pam;

/// <summary>
/// Renders barcodes to PAM (P7).
/// </summary>
public static class BarcodePamRenderer {
    /// <summary>
    /// Renders a barcode to a PAM byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        return PamWriter.WriteRgba32Scanlines(width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders a barcode to a PAM stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var scanlines = BarcodePngRenderer.RenderScanlines(barcode, opts, out var width, out var height, out var stride);
        PamWriter.WriteRgba32Scanlines(stream, width, height, scanlines, stride);
    }

    /// <summary>
    /// Renders a barcode to a PAM file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var pam = Render(barcode, opts);
        return RenderIO.WriteBinary(path, pam);
    }

    /// <summary>
    /// Renders a barcode to a PAM file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var pam = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, pam);
    }

}
