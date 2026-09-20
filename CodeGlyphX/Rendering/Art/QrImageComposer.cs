using System;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Composes supplied artwork with QR modules without an external service or model.</summary>
public static class QrImageComposer {
    /// <summary>
    /// Composes tightly packed RGBA pixels with an encoded QR. Source alpha is flattened onto white
    /// before interpolation. The source buffer and module matrix are not modified or retained.
    /// </summary>
    public static QrImageComposition Render(QrCode qr, byte[] rgba, int width, int height, QrImageCompositionOptions? options = null) {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if ((long)width * height > int.MaxValue / 4 || (long)width * height * 4 != rgba.Length)
            throw new ArgumentException("Expected a tightly packed RGBA image.", nameof(rgba));
        if (qr.Size != qr.Version * 4 + 17 || qr.Modules.Height != qr.Size)
            throw new ArgumentException("QR dimensions do not match its version.", nameof(qr));
        options ??= new QrImageCompositionOptions();
        options.Validate();
        var moduleSize = options.ModuleSize;
        var side = (qr.Size + 2 * options.QuietZone) * moduleSize;
        const string limitMessage = "QR composition exceeds output limits.";
        RenderGuards.EnsureOutputPixels(side, side, limitMessage);
        var pixels = new byte[RenderGuards.EnsureOutputBytes((long)side * side * 4, limitMessage)];
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        var border = options.QuietZone * moduleSize;
        var imageSide = qr.Size * moduleSize;
        var scale = options.Fit == QrImageFit.Cover
            ? Math.Max(imageSide / (double)width, imageSide / (double)height)
            : Math.Min(imageSide / (double)width, imageSide / (double)height);
        var offsetX = (imageSide - width * scale) / 2;
        var offsetY = (imageSide - height * scale) / 2;
        var centerSize = options.Style == QrImageCompositionStyle.ImageOverlay ? options.CenterSize : 0.35;
        var centerMargin = (int)Math.Floor(moduleSize * (1 - centerSize) / 2);
        for (var y = 0; y < side; y++) {
            for (var x = 0; x < side; x++) {
                var index = (y * side + x) * 4;
                pixels[index + 3] = 255;
                var px = x - border;
                var py = y - border;
                if (px < 0 || py < 0 || px >= imageSide || py >= imageSide) {
                    WriteGray(pixels, index, 255);
                    continue;
                }
                var mx = px / moduleSize;
                var my = py / moduleSize;
                var dark = qr.Modules[mx, my];
                if (functions[mx, my] || options.Strength == 0) {
                    WriteGray(pixels, index, dark ? (byte)0 : (byte)255);
                    continue;
                }
                var sx = (px + 0.5 - offsetX) / scale - 0.5;
                var sy = (py + 0.5 - offsetY) / scale - 0.5;
                Sample(rgba, width, height, sx, sy, out var r, out var g, out var b);
                var insideCenter = px % moduleSize >= centerMargin && px % moduleSize < moduleSize - centerMargin
                    && py % moduleSize >= centerMargin && py % moduleSize < moduleSize - centerMargin;
                var overlayEdge = options.Style == QrImageCompositionStyle.ImageOverlay && !insideCenter;
                // Strong centers also give block-thresholding readers local contrast within flat colored regions.
                // Overlay edges allow more of the image through than full-module color adaptation.
                var darkLimit = insideCenter ? 20.0 : overlayEdge ? 128.0 : 60.0;
                var lightLimit = insideCenter ? 235.0 : overlayEdge ? 127.0 : 195.0;
                var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
                var amount = dark ? (luminance > darkLimit ? darkLimit / luminance : 1)
                    : (luminance < lightLimit ? (255 - lightLimit) / (255 - luminance) : 1);
                amount *= options.Strength;
                pixels[index] = Adapt(r, dark, amount);
                pixels[index + 1] = Adapt(g, dark, amount);
                pixels[index + 2] = Adapt(b, dark, amount);
            }
        }
        return new QrImageComposition(pixels, side);
    }

    private static byte Adapt(double value, bool dark, double amount) =>
        (byte)Math.Round(dark ? value * amount : 255 - (255 - value) * amount);

    private static void WriteGray(byte[] pixels, int index, byte value) {
        pixels[index] = value;
        pixels[index + 1] = value;
        pixels[index + 2] = value;
    }

    private static void Sample(byte[] rgba, int width, int height, double x, double y, out double r, out double g, out double b) {
        if (x < -0.5 || y < -0.5 || x >= width - 0.5 || y >= height - 0.5) {
            r = g = b = 255;
            return;
        }
        x = Math.Max(0, Math.Min(width - 1, x));
        y = Math.Max(0, Math.Min(height - 1, y));
        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = Math.Min(width - 1, x0 + 1);
        var y1 = Math.Min(height - 1, y0 + 1);
        var fx = x - x0;
        var fy = y - y0;
        var p00 = (y0 * width + x0) * 4;
        var p10 = (y0 * width + x1) * 4;
        var p01 = (y1 * width + x0) * 4;
        var p11 = (y1 * width + x1) * 4;
        r = Channel(0);
        g = Channel(1);
        b = Channel(2);
        double Channel(int channel) =>
            (Flatten(p00, channel) * (1 - fx) + Flatten(p10, channel) * fx) * (1 - fy)
            + (Flatten(p01, channel) * (1 - fx) + Flatten(p11, channel) * fx) * fy;
        double Flatten(int index, int channel) => 255 + (rgba[index + channel] - 255) * (rgba[index + 3] / 255.0);
    }
}
