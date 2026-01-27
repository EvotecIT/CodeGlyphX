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
            if (opts.PngCompressionLevel > 0) {
                PngRenderHelpers.ApplyAdaptiveFilterHeuristic(scanlines, heightPx, stride);
            }
            return PngWriter.WriteRgba8(widthPx, heightPx, scanlines, length, opts.PngCompressionLevel);
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
            if (opts.PngCompressionLevel > 0) {
                PngRenderHelpers.ApplyAdaptiveFilterHeuristic(scanlines, heightPx, stride);
            }
            PngWriter.WriteRgba8(stream, widthPx, heightPx, scanlines, length, opts.PngCompressionLevel);
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
        GetScanlineLength(modules, opts, out widthPx, out heightPx, out stride);
        var pixels = new byte[heightPx * stride];
        PngRenderHelpers.FillBackgroundPixels(pixels, widthPx, heightPx, stride, opts.Background);

        var rowLength = stride;
        var backgroundRow = ArrayPool<byte>.Shared.Rent(rowLength);
        var rowBuffer = ArrayPool<byte>.Shared.Rent(rowLength);
        try {
            PngRenderHelpers.FillRowPixels(backgroundRow, 0, widthPx, opts.Background);

            for (var my = 0; my < modules.Height; my++) {
                Buffer.BlockCopy(backgroundRow, 0, rowBuffer, 0, rowLength);
                var rowHasDark = false;

                for (var mx = 0; mx < modules.Width; mx++) {
                    if (!modules[mx, my]) continue;
                    rowHasDark = true;
                    var x0 = (mx + opts.QuietZone) * opts.ModuleSize;
                    PngRenderHelpers.FillRowPixels(rowBuffer, x0 * 4, opts.ModuleSize, opts.Foreground);
                }

                if (!rowHasDark) continue;

                var y0 = (my + opts.QuietZone) * opts.ModuleSize;
                for (var sy = 0; sy < opts.ModuleSize; sy++) {
                    Buffer.BlockCopy(rowBuffer, 0, pixels, (y0 + sy) * rowLength, rowLength);
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(rowBuffer);
            ArrayPool<byte>.Shared.Return(backgroundRow);
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

        PngRenderHelpers.FillBackground(buffer, widthPx, heightPx, stride, opts.Background);

        var rowLength = stride + 1;
        var backgroundRow = ArrayPool<byte>.Shared.Rent(rowLength);
        var rowBuffer = ArrayPool<byte>.Shared.Rent(rowLength);
        try {
            backgroundRow[0] = 0;
            PngRenderHelpers.FillRowPixels(backgroundRow, 1, widthPx, opts.Background);
            for (var my = 0; my < modules.Height; my++) {
                Buffer.BlockCopy(backgroundRow, 0, rowBuffer, 0, rowLength);
                var rowHasDark = false;

                for (var mx = 0; mx < modules.Width; mx++) {
                    if (!modules[mx, my]) continue;
                    rowHasDark = true;
                    var x0 = (mx + opts.QuietZone) * opts.ModuleSize;
                    PngRenderHelpers.FillRowPixels(rowBuffer, 1 + x0 * 4, opts.ModuleSize, opts.Foreground);
                }

                if (!rowHasDark) continue;

                var y0 = (my + opts.QuietZone) * opts.ModuleSize;
                for (var sy = 0; sy < opts.ModuleSize; sy++) {
                    Buffer.BlockCopy(rowBuffer, 0, buffer, (y0 + sy) * rowLength, rowLength);
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(rowBuffer);
            ArrayPool<byte>.Shared.Return(backgroundRow);
        }

        return buffer;
    }
}
