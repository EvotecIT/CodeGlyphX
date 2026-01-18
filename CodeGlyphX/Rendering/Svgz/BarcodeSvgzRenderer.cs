using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Svg;

namespace CodeGlyphX.Rendering.Svgz;

/// <summary>
/// Renders barcodes to SVGZ.
/// </summary>
public static class BarcodeSvgzRenderer {
    /// <summary>
    /// Renders a barcode to SVGZ bytes.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodeSvgRenderOptions opts) {
        var svg = SvgBarcodeRenderer.Render(barcode, opts);
        return SvgzWriter.WriteSvg(svg);
    }

    /// <summary>
    /// Renders a barcode to an SVGZ stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodeSvgRenderOptions opts, Stream stream) {
        var svg = SvgBarcodeRenderer.Render(barcode, opts);
        SvgzWriter.WriteSvg(stream, svg);
    }

    /// <summary>
    /// Renders a barcode to an SVGZ file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodeSvgRenderOptions opts, string path) {
        var svgz = Render(barcode, opts);
        return RenderIO.WriteBinary(path, svgz);
    }

    /// <summary>
    /// Renders a barcode to an SVGZ file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodeSvgRenderOptions opts, string directory, string fileName) {
        var svgz = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, svgz);
    }
}
