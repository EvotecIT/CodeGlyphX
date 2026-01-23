using System;
using System.Buffers;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Renders 1D barcodes to a PNG image (RGBA8).
/// </summary>
public static partial class BarcodePngRenderer {
    /// <summary>
    /// Renders the barcode to a PNG byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        if (TryRenderGray1(barcode, opts, out var gray)) return gray;
        if (TryRenderIndexed1(barcode, opts, out var indexed)) return indexed;
        var length = GetScanlineLength(barcode, opts, out var widthPx, out var heightPx, out var stride);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderScanlines(barcode, opts, out widthPx, out heightPx, out stride, scanlines);
            return PngWriter.WriteRgba8(widthPx, heightPx, scanlines, length);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    /// <summary>
    /// Renders the barcode to a PNG stream.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        if (TryRenderGray1ToStream(barcode, opts, stream)) return;
        if (TryRenderIndexed1ToStream(barcode, opts, stream)) return;
        var length = GetScanlineLength(barcode, opts, out var widthPx, out var heightPx, out var stride);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderScanlines(barcode, opts, out widthPx, out heightPx, out stride, scanlines);
            PngWriter.WriteRgba8(stream, widthPx, heightPx, scanlines, length);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    /// <summary>
    /// Renders the barcode to a PNG file.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var png = Render(barcode, opts);
        return RenderIO.WriteBinary(path, png);
    }

    /// <summary>
    /// Renders the barcode to a PNG file under the specified directory.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var png = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, png);
    }

    /// <summary>
    /// Renders the barcode to a raw RGBA pixel buffer (no PNG encoding).
    /// </summary>
    public static byte[] RenderPixels(Barcode1D barcode, BarcodePngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        ComputeLayout(barcode, opts, out widthPx, out heightPx, out stride, out var barHeightPx, out var labelText, out var labelScale, out var labelMarginPx);
        var pixels = new byte[heightPx * stride];
        PngRenderHelpers.FillBackgroundPixels(pixels, widthPx, heightPx, stride, opts.Background);

        if (barHeightPx > 0) {
            var backgroundRow = ArrayPool<byte>.Shared.Rent(stride);
            var rowBuffer = ArrayPool<byte>.Shared.Rent(stride);
            try {
                PngRenderHelpers.FillRowPixels(backgroundRow, 0, widthPx, opts.Background);
                Buffer.BlockCopy(backgroundRow, 0, rowBuffer, 0, stride);

                var xModules = opts.QuietZone;
                for (var i = 0; i < barcode.Segments.Count; i++) {
                    var seg = barcode.Segments[i];
                    if (seg.IsBar) {
                        var x0 = xModules * opts.ModuleSize;
                        var x1 = (xModules + seg.Modules) * opts.ModuleSize;
                        var pixelCount = x1 - x0;
                        PngRenderHelpers.FillRowPixels(rowBuffer, x0 * 4, pixelCount, opts.Foreground);
                    }
                    xModules += seg.Modules;
                }

                var offset = 0;
                for (var y = 0; y < barHeightPx; y++) {
                    Buffer.BlockCopy(rowBuffer, 0, pixels, offset, stride);
                    offset += stride;
                }
            } finally {
                ArrayPool<byte>.Shared.Return(rowBuffer);
                ArrayPool<byte>.Shared.Return(backgroundRow);
            }
        }

        if (!string.IsNullOrEmpty(labelText)) {
            var spacing = labelScale;
            var textWidth = BarcodeLabelFont.MeasureTextWidth(labelText, labelScale, spacing);
            var xStart = (widthPx - textWidth) / 2;
            if (xStart < 0) xStart = 0;
            var yStart = barHeightPx + labelMarginPx;
            DrawLabelCore(pixels, widthPx, heightPx, rowOffset: 0, rowStride: stride, xStart, yStart, labelText, labelScale, spacing, opts.LabelColor);
        }

        return pixels;
    }

    internal static int GetScanlineLength(Barcode1D barcode, BarcodePngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        ComputeLayout(barcode, opts, out widthPx, out heightPx, out stride, out _, out _, out _, out _);
        return heightPx * (stride + 1);
    }

    internal static byte[] RenderScanlines(Barcode1D barcode, BarcodePngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        return RenderScanlines(barcode, opts, out widthPx, out heightPx, out stride, scanlines: null);
    }

    internal static byte[] RenderScanlines(Barcode1D barcode, BarcodePngRenderOptions opts, out int widthPx, out int heightPx, out int stride, byte[]? scanlines) {
        ComputeLayout(barcode, opts, out widthPx, out heightPx, out stride, out var barHeightPx, out var labelText, out var labelScale, out var labelMarginPx);
        var length = heightPx * (stride + 1);
        var buffer = scanlines ?? new byte[length];
        if (buffer.Length < length) throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlines));

        PngRenderHelpers.FillBackground(buffer, widthPx, heightPx, stride, opts.Background);

        if (barHeightPx > 0) {
            var rowLength = stride + 1;
            var backgroundRow = ArrayPool<byte>.Shared.Rent(rowLength);
            var rowBuffer = ArrayPool<byte>.Shared.Rent(rowLength);
            try {
                backgroundRow[0] = 0;
                PngRenderHelpers.FillRowPixels(backgroundRow, 1, widthPx, opts.Background);
                Buffer.BlockCopy(backgroundRow, 0, rowBuffer, 0, rowLength);

                var xModules = opts.QuietZone;
                for (var i = 0; i < barcode.Segments.Count; i++) {
                    var seg = barcode.Segments[i];
                    if (seg.IsBar) {
                        var x0 = xModules * opts.ModuleSize;
                        var x1 = (xModules + seg.Modules) * opts.ModuleSize;
                        var pixelCount = x1 - x0;
                        PngRenderHelpers.FillRowPixels(rowBuffer, 1 + x0 * 4, pixelCount, opts.Foreground);
                    }
                    xModules += seg.Modules;
                }

                var offset = 0;
                for (var y = 0; y < barHeightPx; y++) {
                    Buffer.BlockCopy(rowBuffer, 0, buffer, offset, rowLength);
                    offset += rowLength;
                }
            } finally {
                ArrayPool<byte>.Shared.Return(rowBuffer);
                ArrayPool<byte>.Shared.Return(backgroundRow);
            }
        }

        if (!string.IsNullOrEmpty(labelText)) {
            var spacing = labelScale;
            var textWidth = BarcodeLabelFont.MeasureTextWidth(labelText, labelScale, spacing);
            var xStart = (widthPx - textWidth) / 2;
            if (xStart < 0) xStart = 0;
            var yStart = barHeightPx + labelMarginPx;
            DrawLabelCore(buffer, widthPx, heightPx, rowOffset: 1, rowStride: stride + 1, xStart, yStart, labelText, labelScale, spacing, opts.LabelColor);
        }

        return buffer;
    }

    private static void ComputeLayout(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        out int widthPx,
        out int heightPx,
        out int stride,
        out int barHeightPx,
        out string labelText,
        out int labelScale,
        out int labelMarginPx) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.HeightModules <= 0) throw new ArgumentOutOfRangeException(nameof(opts.HeightModules));

        var outModules = barcode.TotalModules + opts.QuietZone * 2;
        widthPx = outModules * opts.ModuleSize;
        barHeightPx = opts.HeightModules * opts.ModuleSize;

        labelText = BarcodeLabelText.Normalize(opts.LabelText);
        var hasLabel = !string.IsNullOrEmpty(labelText);
        var labelFontPx = Math.Max(1, opts.LabelFontSize);
        labelMarginPx = Math.Max(0, opts.LabelMargin);
        labelScale = Math.Max(1, (int)Math.Round(labelFontPx / (double)BarcodeLabelFont.GlyphHeight));
        var labelHeightPx = hasLabel ? labelMarginPx + BarcodeLabelFont.GlyphHeight * labelScale : 0;

        heightPx = barHeightPx + labelHeightPx;
        stride = widthPx * 4;
    }

    private static void DrawLabelCore(byte[] scanlines, int widthPx, int heightPx, int rowOffset, int rowStride, int xStart, int yStart, string text, int scale, int spacing, Rgba32 color) {
        var x = xStart;
        var glyphWidth = BarcodeLabelFont.GlyphWidth;
        var glyphHeight = BarcodeLabelFont.GlyphHeight;
        for (var i = 0; i < text.Length; i++) {
            var glyph = BarcodeLabelFont.GetGlyph(text[i]);
            for (var row = 0; row < glyphHeight; row++) {
                var bits = glyph[row];
                if (bits == 0) continue;
                var py = yStart + row * scale;
                var col = 0;
                while (col < glyphWidth) {
                    var mask = 1 << (glyphWidth - 1 - col);
                    if ((bits & mask) == 0) {
                        col++;
                        continue;
                    }

                    var runStart = col;
                    col++;
                    while (col < glyphWidth && (bits & (1 << (glyphWidth - 1 - col))) != 0) col++;
                    var runLen = col - runStart;
                    var px = x + runStart * scale;
                    var pixelCount = runLen * scale;
                    if ((uint)px >= (uint)widthPx) continue;
                    var maxPixels = widthPx - px;
                    if (pixelCount > maxPixels) pixelCount = maxPixels;

                    for (var sy = 0; sy < scale; sy++) {
                        var y = py + sy;
                        if ((uint)y >= (uint)heightPx) continue;
                        var rowStart = y * rowStride + rowOffset + px * 4;
                        PngRenderHelpers.FillRowPixels(scanlines, rowStart, pixelCount, color);
                    }
                }
            }
            x += glyphWidth * scale + spacing;
        }
    }
}
