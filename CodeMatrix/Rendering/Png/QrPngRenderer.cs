using System;

namespace CodeMatrix.Rendering.Png;

public static class QrPngRenderer {
    public static byte[] Render(BitMatrix modules, QrPngRenderOptions opts) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var size = modules.Width;
        var outModules = size + opts.QuietZone * 2;
        var widthPx = outModules * opts.ModuleSize;
        var heightPx = widthPx;
        var stride = widthPx * 4;

        var scanlines = new byte[heightPx * (stride + 1)];

        for (var my = -opts.QuietZone; my < size + opts.QuietZone; my++) {
            for (var sy = 0; sy < opts.ModuleSize; sy++) {
                var y = (my + opts.QuietZone) * opts.ModuleSize + sy;
                var rowStart = y * (stride + 1);
                scanlines[rowStart] = 0; // filter
                var p = rowStart + 1;

                for (var mx = -opts.QuietZone; mx < size + opts.QuietZone; mx++) {
                    var isDark = (uint)mx < (uint)size && (uint)my < (uint)size && modules[mx, my];
                    var c = isDark ? opts.Foreground : opts.Background;
                    for (var sx = 0; sx < opts.ModuleSize; sx++) {
                        scanlines[p++] = c.R;
                        scanlines[p++] = c.G;
                        scanlines[p++] = c.B;
                        scanlines[p++] = c.A;
                    }
                }
            }
        }

        return PngWriter.WriteRgba8(widthPx, heightPx, scanlines);
    }
}

