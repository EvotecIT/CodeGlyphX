using System;

namespace CodeMatrix.Rendering.Png;

/// <summary>
/// Renders 1D barcodes to a PNG image (RGBA8).
/// </summary>
public static class BarcodePngRenderer {
    /// <summary>
    /// Renders the barcode to a PNG byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var scanlines = RenderScanlines(barcode, opts, out var widthPx, out var heightPx, out _);
        return PngWriter.WriteRgba8(widthPx, heightPx, scanlines);
    }

    internal static byte[] RenderScanlines(Barcode1D barcode, BarcodePngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.HeightModules <= 0) throw new ArgumentOutOfRangeException(nameof(opts.HeightModules));

        var outModules = barcode.TotalModules + opts.QuietZone * 2;
        widthPx = outModules * opts.ModuleSize;
        heightPx = opts.HeightModules * opts.ModuleSize;
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

                for (var y = 0; y < heightPx; y++) {
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

        return scanlines;
    }
}
