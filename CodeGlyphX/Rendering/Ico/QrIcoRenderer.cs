using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ico;

/// <summary>
/// Renders QR modules to ICO (PNG-embedded).
/// </summary>
public static class QrIcoRenderer {
    /// <summary>
    /// Renders the QR module matrix to an ICO byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        return Render(modules, opts, null);
    }

    /// <summary>
    /// Renders the QR module matrix to an ICO byte array (multi-size).
    /// </summary>
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts, IcoRenderOptions? icoOptions) {
        var rgba = QrPngRenderer.RenderPixels(modules, opts, out var width, out var height, out var stride);
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
    /// Renders the QR module matrix to an ICO stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        RenderToStream(modules, opts, stream, null);
    }

    /// <summary>
    /// Renders the QR module matrix to an ICO stream (multi-size).
    /// </summary>
    public static void RenderToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream, IcoRenderOptions? icoOptions) {
        var rgba = QrPngRenderer.RenderPixels(modules, opts, out var width, out var height, out var stride);
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
    /// Renders the QR module matrix to an ICO file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string path) {
        var ico = Render(modules, opts);
        return RenderIO.WriteBinary(path, ico);
    }

    /// <summary>
    /// Renders the QR module matrix to an ICO file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, QrPngRenderOptions opts, string directory, string fileName) {
        var ico = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, ico);
    }
}
