using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CodeGlyphX.Wpf.Rendering;

internal static class WpfBitmapRenderer {
    public static WriteableBitmap RenderQr(BitMatrix modules, int moduleSize, int quietZone, uint fgBgra, uint bgBgra) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (moduleSize <= 0) throw new ArgumentOutOfRangeException(nameof(moduleSize));
        if (quietZone < 0) throw new ArgumentOutOfRangeException(nameof(quietZone));

        var size = modules.Width;
        var outModules = size + quietZone * 2;
        var width = outModules * moduleSize;
        var height = width;

        var stride = width * 4;
        var pixels = new byte[height * stride];

        for (var my = -quietZone; my < size + quietZone; my++) {
            for (var sy = 0; sy < moduleSize; sy++) {
                var y = (my + quietZone) * moduleSize + sy;
                var row = y * stride;

                for (var mx = -quietZone; mx < size + quietZone; mx++) {
                    var isDark = (uint)mx < (uint)size && (uint)my < (uint)size && modules[mx, my];
                    var c = isDark ? fgBgra : bgBgra;

                    for (var sx = 0; sx < moduleSize; sx++) {
                        var p = row + ((mx + quietZone) * moduleSize + sx) * 4;
                        pixels[p + 0] = (byte)(c & 0xFF);
                        pixels[p + 1] = (byte)((c >> 8) & 0xFF);
                        pixels[p + 2] = (byte)((c >> 16) & 0xFF);
                        pixels[p + 3] = (byte)((c >> 24) & 0xFF);
                    }
                }
            }
        }

        var bmp = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        bmp.Freeze();
        return bmp;
    }

    public static WriteableBitmap RenderBarcode(Barcode1D barcode, int moduleSize, int quietZone, int heightModules, uint fgBgra, uint bgBgra) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (moduleSize <= 0) throw new ArgumentOutOfRangeException(nameof(moduleSize));
        if (quietZone < 0) throw new ArgumentOutOfRangeException(nameof(quietZone));
        if (heightModules <= 0) throw new ArgumentOutOfRangeException(nameof(heightModules));

        var outModules = barcode.TotalModules + quietZone * 2;
        var width = outModules * moduleSize;
        var height = heightModules * moduleSize;

        var stride = width * 4;
        var pixels = new byte[height * stride];

        // Fill background
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i + 0] = (byte)(bgBgra & 0xFF);
            pixels[i + 1] = (byte)((bgBgra >> 8) & 0xFF);
            pixels[i + 2] = (byte)((bgBgra >> 16) & 0xFF);
            pixels[i + 3] = (byte)((bgBgra >> 24) & 0xFF);
        }

        var xModules = quietZone;
        for (var i = 0; i < barcode.Segments.Count; i++) {
            var seg = barcode.Segments[i];
            if (seg.IsBar) {
                var x0 = xModules * moduleSize;
                var x1 = (xModules + seg.Modules) * moduleSize;
                for (var y = 0; y < height; y++) {
                    var row = y * stride;
                    for (var x = x0; x < x1; x++) {
                        var p = row + x * 4;
                        pixels[p + 0] = (byte)(fgBgra & 0xFF);
                        pixels[p + 1] = (byte)((fgBgra >> 8) & 0xFF);
                        pixels[p + 2] = (byte)((fgBgra >> 16) & 0xFF);
                        pixels[p + 3] = (byte)((fgBgra >> 24) & 0xFF);
                    }
                }
            }
            xModules += seg.Modules;
        }

        var bmp = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        bmp.Freeze();
        return bmp;
    }
}

