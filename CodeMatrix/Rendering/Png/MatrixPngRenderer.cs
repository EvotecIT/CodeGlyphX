using System;
using System.IO;
using CodeMatrix.Rendering;

namespace CodeMatrix.Rendering.Png;

/// <summary>
/// Renders generic 2D matrices to PNG images (RGBA8).
/// </summary>
public static class MatrixPngRenderer {
    /// <summary>
    /// Renders the matrix to a PNG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        var scanlines = RenderScanlines(modules, opts, out var widthPx, out var heightPx, out _);
        return PngWriter.WriteRgba8(widthPx, heightPx, scanlines);
    }

    /// <summary>
    /// Renders the matrix to a PNG stream.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        var scanlines = RenderScanlines(modules, opts, out var widthPx, out var heightPx, out _);
        PngWriter.WriteRgba8(stream, widthPx, heightPx, scanlines);
    }

    /// <summary>
    /// Renders the matrix to a PNG file.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path) {
        var png = Render(modules, opts);
        return RenderIO.WriteBinary(path, png);
    }

    /// <summary>
    /// Renders the matrix to a PNG file under the specified directory.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName) {
        var png = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, png);
    }

    /// <summary>
    /// Renders the matrix to a raw RGBA pixel buffer (no PNG encoding).
    /// </summary>
    public static byte[] RenderPixels(BitMatrix modules, MatrixPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        var scanlines = RenderScanlines(modules, opts, out widthPx, out heightPx, out stride);
        var pixels = new byte[heightPx * stride];
        for (var y = 0; y < heightPx; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, pixels, y * stride, stride);
        }
        return pixels;
    }

    internal static byte[] RenderScanlines(BitMatrix modules, MatrixPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var outWidthModules = modules.Width + opts.QuietZone * 2;
        var outHeightModules = modules.Height + opts.QuietZone * 2;
        widthPx = outWidthModules * opts.ModuleSize;
        heightPx = outHeightModules * opts.ModuleSize;
        stride = widthPx * 4;

        var scanlines = new byte[heightPx * (stride + 1)];

        // Fill background.
        for (var y = 0; y < heightPx; y++) {
            var rowStart = y * (stride + 1);
            scanlines[rowStart] = 0;
            var p = rowStart + 1;
            for (var x = 0; x < widthPx; x++) {
                scanlines[p++] = opts.Background.R;
                scanlines[p++] = opts.Background.G;
                scanlines[p++] = opts.Background.B;
                scanlines[p++] = opts.Background.A;
            }
        }

        for (var my = 0; my < modules.Height; my++) {
            for (var mx = 0; mx < modules.Width; mx++) {
                if (!modules[mx, my]) continue;

                var x0 = (mx + opts.QuietZone) * opts.ModuleSize;
                var y0 = (my + opts.QuietZone) * opts.ModuleSize;
                for (var sy = 0; sy < opts.ModuleSize; sy++) {
                    var rowStart = (y0 + sy) * (stride + 1) + 1 + x0 * 4;
                    for (var sx = 0; sx < opts.ModuleSize; sx++) {
                        scanlines[rowStart + 0] = opts.Foreground.R;
                        scanlines[rowStart + 1] = opts.Foreground.G;
                        scanlines[rowStart + 2] = opts.Foreground.B;
                        scanlines[rowStart + 3] = opts.Foreground.A;
                        rowStart += 4;
                    }
                }
            }
        }

        return scanlines;
    }
}
