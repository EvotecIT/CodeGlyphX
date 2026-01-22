using System;
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
        var scanlines = RenderScanlines(barcode, opts, out var widthPx, out var heightPx, out _);
        return PngWriter.WriteRgba8(widthPx, heightPx, scanlines);
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
        var scanlines = RenderScanlines(barcode, opts, out var widthPx, out var heightPx, out _);
        PngWriter.WriteRgba8(stream, widthPx, heightPx, scanlines);
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
        var scanlines = RenderScanlines(barcode, opts, out widthPx, out heightPx, out stride);
        var pixels = new byte[heightPx * stride];
        for (var y = 0; y < heightPx; y++) {
            Buffer.BlockCopy(scanlines, y * (stride + 1) + 1, pixels, y * stride, stride);
        }
        return pixels;
    }

    internal static byte[] RenderScanlines(Barcode1D barcode, BarcodePngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.HeightModules <= 0) throw new ArgumentOutOfRangeException(nameof(opts.HeightModules));

        var outModules = barcode.TotalModules + opts.QuietZone * 2;
        widthPx = outModules * opts.ModuleSize;
        var barHeightPx = opts.HeightModules * opts.ModuleSize;

        var labelText = BarcodeLabelText.Normalize(opts.LabelText);
        var hasLabel = !string.IsNullOrEmpty(labelText);
        var labelFontPx = Math.Max(1, opts.LabelFontSize);
        var labelMarginPx = Math.Max(0, opts.LabelMargin);
        var labelScale = Math.Max(1, (int)Math.Round(labelFontPx / (double)BarcodeLabelFont.GlyphHeight));
        var labelHeightPx = hasLabel ? labelMarginPx + BarcodeLabelFont.GlyphHeight * labelScale : 0;

        heightPx = barHeightPx + labelHeightPx;
        stride = widthPx * 4;

        var scanlines = new byte[heightPx * (stride + 1)];

        // Fill background
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

        var xModules = opts.QuietZone;
        for (var i = 0; i < barcode.Segments.Count; i++) {
            var seg = barcode.Segments[i];
            if (seg.IsBar) {
                var x0 = xModules * opts.ModuleSize;
                var x1 = (xModules + seg.Modules) * opts.ModuleSize;

                for (var y = 0; y < barHeightPx; y++) {
                    var p = y * (stride + 1) + 1 + x0 * 4;
                    for (var x = x0; x < x1; x++) {
                        scanlines[p++] = opts.Foreground.R;
                        scanlines[p++] = opts.Foreground.G;
                        scanlines[p++] = opts.Foreground.B;
                        scanlines[p++] = opts.Foreground.A;
                    }
                }
            }
            xModules += seg.Modules;
        }

        if (hasLabel) {
            var spacing = labelScale;
            var textWidth = BarcodeLabelFont.MeasureTextWidth(labelText, labelScale, spacing);
            var xStart = (widthPx - textWidth) / 2;
            if (xStart < 0) xStart = 0;
            var yStart = barHeightPx + labelMarginPx;
            DrawLabel(scanlines, widthPx, heightPx, stride, xStart, yStart, labelText, labelScale, spacing, opts.LabelColor);
        }

        return scanlines;
    }

    private static void DrawLabel(byte[] scanlines, int widthPx, int heightPx, int stride, int xStart, int yStart, string text, int scale, int spacing, Rgba32 color) {
        var x = xStart;
        for (var i = 0; i < text.Length; i++) {
            var glyph = BarcodeLabelFont.GetGlyph(text[i]);
            for (var row = 0; row < BarcodeLabelFont.GlyphHeight; row++) {
                var bits = glyph[row];
                for (var col = 0; col < BarcodeLabelFont.GlyphWidth; col++) {
                    if ((bits & (1 << (BarcodeLabelFont.GlyphWidth - 1 - col))) == 0) continue;
                    var px = x + col * scale;
                    var py = yStart + row * scale;
                    for (var sy = 0; sy < scale; sy++) {
                        var y = py + sy;
                        if ((uint)y >= (uint)heightPx) continue;
                        var rowStart = y * (stride + 1) + 1;
                        for (var sx = 0; sx < scale; sx++) {
                            var px2 = px + sx;
                            if ((uint)px2 >= (uint)widthPx) continue;
                            var p = rowStart + px2 * 4;
                            scanlines[p + 0] = color.R;
                            scanlines[p + 1] = color.G;
                            scanlines[p + 2] = color.B;
                            scanlines[p + 3] = color.A;
                        }
                    }
                }
            }
            x += BarcodeLabelFont.GlyphWidth * scale + spacing;
        }
    }
}
