using System;
using System.Buffers;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private const double UnitScaleEpsilon = 1e-9;

    private static bool CanRenderSimpleRgba(QrPngRenderOptions opts) {
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        return opts.ForegroundGradient is null
            && opts.Eyes is null
            && opts.Logo is null
            && opts.ModuleShape == QrPngModuleShape.Square
            && IsUnitScale(opts.ModuleScale)
            && opts.ModuleCornerRadiusPx == 0;
    }

    private static bool IsUnitScale(double scale) {
        return Math.Abs(scale - 1.0) <= UnitScaleEpsilon;
    }

    private static byte[] RenderSimpleRgba(BitMatrix modules, QrPngRenderOptions opts) {
        var length = GetScanlineLength(modules, opts, out var widthPx, out var heightPx, out var stride);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderSimpleScanlines(modules, opts, widthPx, heightPx, stride, scanlines);
            return PngWriter.WriteRgba8(widthPx, heightPx, scanlines, length);
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static void RenderSimplePixels(BitMatrix modules, QrPngRenderOptions opts, byte[] pixels, int widthPx, int heightPx, int stride) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));

        PngRenderHelpers.FillBackgroundPixels(pixels, widthPx, heightPx, stride, opts.Background);

        var size = modules.Width;
        var moduleSize = opts.ModuleSize;
        var quiet = opts.QuietZone;
        var foreground = CompositeForeground(opts.Foreground, opts.Background);
        var rowLength = stride;

        var backgroundRow = ArrayPool<byte>.Shared.Rent(rowLength);
        var rowBuffer = ArrayPool<byte>.Shared.Rent(rowLength);
        try {
            PngRenderHelpers.FillRowPixels(backgroundRow, 0, widthPx, opts.Background);

            for (var my = 0; my < size; my++) {
                Buffer.BlockCopy(backgroundRow, 0, rowBuffer, 0, rowLength);
                var rowHasDark = false;
                var px = quiet * moduleSize;

                var mx = 0;
                while (mx < size) {
                    if (!modules[mx, my]) {
                        mx++;
                        px += moduleSize;
                        continue;
                    }

                    rowHasDark = true;
                    var runStart = mx;
                    mx++;
                    while (mx < size && modules[mx, my]) mx++;
                    var runSize = mx - runStart;
                    PngRenderHelpers.FillRowPixels(rowBuffer, px * 4, runSize * moduleSize, foreground);
                    px += runSize * moduleSize;
                }

                if (!rowHasDark) continue;

                var y0 = (my + quiet) * moduleSize;
                for (var sy = 0; sy < moduleSize; sy++) {
                    Buffer.BlockCopy(rowBuffer, 0, pixels, (y0 + sy) * rowLength, rowLength);
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(rowBuffer);
            ArrayPool<byte>.Shared.Return(backgroundRow);
        }
    }

    private static void RenderSimpleScanlines(BitMatrix modules, QrPngRenderOptions opts, int widthPx, int heightPx, int stride, byte[] scanlines) {
        if (scanlines is null) throw new ArgumentNullException(nameof(scanlines));

        var rowLength = stride + 1;
        PngRenderHelpers.FillBackground(scanlines, widthPx, heightPx, stride, opts.Background);

        var size = modules.Width;
        var moduleSize = opts.ModuleSize;
        var quiet = opts.QuietZone;
        var foreground = CompositeForeground(opts.Foreground, opts.Background);

        var backgroundRow = ArrayPool<byte>.Shared.Rent(rowLength);
        var rowBuffer = ArrayPool<byte>.Shared.Rent(rowLength);
        try {
            Buffer.BlockCopy(scanlines, 0, backgroundRow, 0, rowLength);

            for (var my = 0; my < size; my++) {
                Buffer.BlockCopy(backgroundRow, 0, rowBuffer, 0, rowLength);
                var rowHasDark = false;
                var px = quiet * moduleSize;

                var mx = 0;
                while (mx < size) {
                    if (!modules[mx, my]) {
                        mx++;
                        px += moduleSize;
                        continue;
                    }

                    rowHasDark = true;
                    var runStart = mx;
                    mx++;
                    while (mx < size && modules[mx, my]) mx++;
                    var runSize = mx - runStart;
                    PngRenderHelpers.FillRowPixels(rowBuffer, 1 + px * 4, runSize * moduleSize, foreground);
                    px += runSize * moduleSize;
                }

                if (!rowHasDark) continue;

                var y0 = (my + quiet) * moduleSize;
                var rowStart = y0 * rowLength;
                for (var sy = 0; sy < moduleSize; sy++) {
                    Buffer.BlockCopy(rowBuffer, 0, scanlines, rowStart + sy * rowLength, rowLength);
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(rowBuffer);
            ArrayPool<byte>.Shared.Return(backgroundRow);
        }
    }

    private static void FillRowSimple(BitMatrix modules, QrPngRenderOptions opts, int y, byte[] rowBuffer, int rowLength) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (rowBuffer is null) throw new ArgumentNullException(nameof(rowBuffer));
        if (rowLength < 1 || rowLength > rowBuffer.Length) throw new ArgumentOutOfRangeException(nameof(rowLength));

        var size = modules.Width;
        var moduleSize = opts.ModuleSize;
        var quiet = opts.QuietZone;
        var widthPx = (size + quiet * 2) * moduleSize;
        if (rowLength != widthPx * 4 + 1) throw new ArgumentException("Invalid row buffer length.", nameof(rowLength));

        rowBuffer[0] = 0;
        var background = opts.Background;
        var foreground = CompositeForeground(opts.Foreground, background);

        PngRenderHelpers.FillRowPixels(rowBuffer, 1, widthPx, background);

        var originPx = quiet * moduleSize;
        var qrSizePx = size * moduleSize;
        var yIn = y - originPx;
        if (yIn < 0 || yIn >= qrSizePx) return;

        var my = yIn / moduleSize;
        var px = originPx;
        var mx = 0;
        while (mx < size) {
            if (!modules[mx, my]) {
                mx++;
                px += moduleSize;
                continue;
            }

            var runStart = mx;
            mx++;
            while (mx < size && modules[mx, my]) mx++;
            var runSize = mx - runStart;
            PngRenderHelpers.FillRowPixels(rowBuffer, 1 + px * 4, runSize * moduleSize, foreground);
            px += runSize * moduleSize;
        }
    }

    private static Rgba32 CompositeForeground(Rgba32 foreground, Rgba32 background) {
        if (foreground.A == 255 && background.A == 255) return foreground;
        var sa = foreground.A;
        var inv = 255 - sa;
        var r = (byte)((foreground.R * sa + background.R * inv + 127) / 255);
        var g = (byte)((foreground.G * sa + background.G * inv + 127) / 255);
        var b = (byte)((foreground.B * sa + background.B * inv + 127) / 255);
        var a = (byte)((sa + background.A * inv + 127) / 255);
        return new Rgba32(r, g, b, a);
    }

}
