using System;
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
        GetScanlineLength(modules, opts, out var widthPx, out var heightPx, out _);
        using var ms = new MemoryStream();
        PngWriter.WriteRgba8(ms, widthPx, heightPx, (y, rowBuffer, rowLength) => FillRowSimple(modules, opts, y, rowBuffer, rowLength));
        return ms.ToArray();
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

        FillRowPixels(rowBuffer, 1, widthPx, background);

        var originPx = quiet * moduleSize;
        var qrSizePx = size * moduleSize;
        var yIn = y - originPx;
        if (yIn < 0 || yIn >= qrSizePx) return;

        var my = yIn / moduleSize;
        var px = originPx;
        for (var mx = 0; mx < size; mx++) {
            if (modules[mx, my]) {
                FillRowPixels(rowBuffer, 1 + px * 4, moduleSize, foreground);
            }
            px += moduleSize;
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

    private static void FillRowPixels(byte[] rowBuffer, int offset, int pixelCount, Rgba32 color) {
        var p = offset;
        for (var i = 0; i < pixelCount; i++) {
            rowBuffer[p++] = color.R;
            rowBuffer[p++] = color.G;
            rowBuffer[p++] = color.B;
            rowBuffer[p++] = color.A;
        }
    }
}
