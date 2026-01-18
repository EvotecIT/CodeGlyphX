using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ico;

/// <summary>
/// Renders 1D barcodes to ICO (PNG-embedded).
/// </summary>
public static class BarcodeIcoRenderer {
    /// <summary>
    /// Renders the barcode to an ICO byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        return Render(barcode, opts, null);
    }

    /// <summary>
    /// Renders the barcode to an ICO byte array (multi-size).
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts, IcoRenderOptions? icoOptions) {
        var rgba = BarcodePngRenderer.RenderPixels(barcode, opts, out var width, out var height, out var stride);
        if (icoOptions is null) {
            var png = IcoPngBuilder.FromRgbaForIco(rgba, width, height, stride, opts.Background);
            return IcoWriter.WritePng(png);
        }

        var sizes = icoOptions.GetNormalizedSizes();
        var pngs = new byte[sizes.Length][];
        for (var i = 0; i < sizes.Length; i++) {
            var size = sizes[i];
            var scaled = ImageScaler.ResizeToFitNearest(rgba, width, height, stride, size, size, opts.Background, icoOptions.PreserveAspectRatio);
            pngs[i] = IcoPngBuilder.FromRgba(scaled, size, size, size * 4);
        }
        return IcoWriter.WritePngs(pngs);
    }

    /// <summary>
    /// Renders the barcode to an ICO stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        RenderToStream(barcode, opts, stream, null);
    }

    /// <summary>
    /// Renders the barcode to an ICO stream (multi-size).
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream, IcoRenderOptions? icoOptions) {
        var rgba = BarcodePngRenderer.RenderPixels(barcode, opts, out var width, out var height, out var stride);
        if (icoOptions is null) {
            var png = IcoPngBuilder.FromRgbaForIco(rgba, width, height, stride, opts.Background);
            IcoWriter.WritePng(stream, png);
            return;
        }

        var sizes = icoOptions.GetNormalizedSizes();
        var pngs = new byte[sizes.Length][];
        for (var i = 0; i < sizes.Length; i++) {
            var size = sizes[i];
            var scaled = ImageScaler.ResizeToFitNearest(rgba, width, height, stride, size, size, opts.Background, icoOptions.PreserveAspectRatio);
            pngs[i] = IcoPngBuilder.FromRgba(scaled, size, size, size * 4);
        }
        IcoWriter.WritePngs(stream, pngs);
    }

    /// <summary>
    /// Renders the barcode to an ICO file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var ico = Render(barcode, opts);
        return RenderIO.WriteBinary(path, ico);
    }

    /// <summary>
    /// Renders the barcode to an ICO file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var ico = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, ico);
    }
}
