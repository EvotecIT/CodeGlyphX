using System;
using System.Buffers;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Renders generic 2D matrices to PNG images (RGBA8).
/// </summary>
public static partial class MatrixPngRenderer {
    /// <summary>
    /// Renders the matrix to a PNG byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        if (TryRenderGray1(modules, opts, out var gray)) return gray;
        if (TryRenderIndexed1(modules, opts, out var indexed)) return indexed;
        var length = GetScanlineLength(modules, opts, out var widthPx, out var heightPx, out var stride);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderScanlines(modules, opts, out widthPx, out heightPx, out stride, scanlines);
            return PngWriter.WriteRgba8(widthPx, heightPx, scanlines, length);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    /// <summary>
    /// Renders the matrix to a PNG stream.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        if (TryRenderGray1ToStream(modules, opts, stream)) return;
        if (TryRenderIndexed1ToStream(modules, opts, stream)) return;
        var length = GetScanlineLength(modules, opts, out var widthPx, out var heightPx, out var stride);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderScanlines(modules, opts, out widthPx, out heightPx, out stride, scanlines);
            PngWriter.WriteRgba8(stream, widthPx, heightPx, scanlines, length);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
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

    internal static int GetScanlineLength(BitMatrix modules, MatrixPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var outWidthModules = modules.Width + opts.QuietZone * 2;
        var outHeightModules = modules.Height + opts.QuietZone * 2;
        widthPx = outWidthModules * opts.ModuleSize;
        heightPx = outHeightModules * opts.ModuleSize;
        stride = widthPx * 4;
        return heightPx * (stride + 1);
    }

    internal static byte[] RenderScanlines(BitMatrix modules, MatrixPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        return RenderScanlines(modules, opts, out widthPx, out heightPx, out stride, scanlines: null);
    }

    internal static byte[] RenderScanlines(BitMatrix modules, MatrixPngRenderOptions opts, out int widthPx, out int heightPx, out int stride, byte[]? scanlines) {
        var length = GetScanlineLength(modules, opts, out widthPx, out heightPx, out stride);
        var buffer = scanlines ?? new byte[length];
        if (buffer.Length < length) throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlines));

        // Fill background.
        for (var y = 0; y < heightPx; y++) {
            var rowStart = y * (stride + 1);
            buffer[rowStart] = 0;
            var p = rowStart + 1;
            for (var x = 0; x < widthPx; x++) {
                buffer[p++] = opts.Background.R;
                buffer[p++] = opts.Background.G;
                buffer[p++] = opts.Background.B;
                buffer[p++] = opts.Background.A;
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
                        buffer[rowStart + 0] = opts.Foreground.R;
                        buffer[rowStart + 1] = opts.Foreground.G;
                        buffer[rowStart + 2] = opts.Foreground.B;
                        buffer[rowStart + 3] = opts.Foreground.A;
                        rowStart += 4;
                    }
                }
            }
        }

        return buffer;
    }
}
