using System;
using System.Threading;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Composes supplied artwork with QR modules without an external service or model.</summary>
public static partial class QrImageComposer {
    /// <summary>
    /// Composes tightly packed RGBA pixels with an encoded QR. Source alpha is flattened onto white
    /// before interpolation. The source buffer and module matrix are not modified or retained.
    /// </summary>
    public static QrImageComposition Render(QrCode qr, byte[] rgba, int width, int height, QrImageCompositionOptions? options = null) =>
        Render(qr, rgba, width, height, options, CancellationToken.None);

    /// <summary>Composes artwork while observing cancellation during image analysis and rasterization.</summary>
    public static QrImageComposition Render(QrCode qr, byte[] rgba, int width, int height, QrImageCompositionOptions? options, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
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
        var qrFullSize = (qr.Size + 2 * options.QuietZone) * moduleSize;
        var padding = (options.Canvas?.PaddingModules ?? 0) * moduleSize;
        var side = qrFullSize + 2 * padding;
        var qrOffsetX = (int)Math.Round(2 * padding * (options.Canvas?.PositionX ?? 0.5));
        var qrOffsetY = (int)Math.Round(2 * padding * (options.Canvas?.PositionY ?? 0.5));
        const string limitMessage = "QR composition exceeds output limits.";
        RenderGuards.EnsureOutputPixels(side, side, limitMessage);
        var pixels = new byte[RenderGuards.EnsureOutputBytes((long)side * side * 4, limitMessage)];
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        var border = options.QuietZone * moduleSize;
        var imageSide = qr.Size * moduleSize;
        var hasCanvas = padding > 0;
        var image = new QrImageSampler(rgba, width, height, hasCanvas ? side : imageSide, options);
        var imageOriginX = hasCanvas ? 0 : qrOffsetX + border;
        var imageOriginY = hasCanvas ? 0 : qrOffsetY + border;
        var art = options.Art is null ? null : new ArtGeometry(qr.Modules, functions, moduleSize, options.Art,
            image, qrOffsetX + border - imageOriginX, qrOffsetY + border - imageOriginY, cancellationToken);
        var finders = options.Art is not null && options.Art.Finders != QrImageFinderStyle.Square && options.Strength > 0
            ? new FinderGeometry(moduleSize, options.Art) : null;
        var centerSize = options.Style == QrImageCompositionStyle.ImageOverlay ? options.CenterSize : 0.35;
        var centerMargin = (int)Math.Floor(moduleSize * (1 - centerSize) / 2);
        for (var y = 0; y < side; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < side; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var index = (y * side + x) * 4;
                pixels[index + 3] = 255;
                var px = x - qrOffsetX - border;
                var py = y - qrOffsetY - border;
                if (px < 0 || py < 0 || px >= imageSide || py >= imageSide) {
                    var inQuietZone = x >= qrOffsetX && y >= qrOffsetY
                        && x < qrOffsetX + qrFullSize && y < qrOffsetY + qrFullSize;
                    if (hasCanvas && !inQuietZone) {
                        image.Sample(x, y, out var cr, out var cg, out var cb);
                        pixels[index] = (byte)Math.Round(cr);
                        pixels[index + 1] = (byte)Math.Round(cg);
                        pixels[index + 2] = (byte)Math.Round(cb);
                    } else {
                        WriteGray(pixels, index, 255);
                        if (options.Art is not null && options.Strength > 0) WriteColor(pixels, index, options.Art.FunctionalBackground);
                    }
                    continue;
                }
                if (finders is not null && finders.TryPaint(pixels, index, px, py, qr.Size)) continue;
                var mx = px / moduleSize;
                var my = py / moduleSize;
                var dark = qr.Modules[mx, my];
                if (functions[mx, my] || options.Strength == 0) {
                    WriteGray(pixels, index, dark ? (byte)0 : (byte)255);
                    if (options.Strength > 0 && options.Art is not null)
                        WriteColor(pixels, index, dark ? options.Art.FunctionalForeground : options.Art.FunctionalBackground);
                    continue;
                }
                image.Sample(x - imageOriginX, y - imageOriginY, out var r, out var g, out var b);
                if (art is not null) {
                    art.Paint(pixels, index, mx, my, px % moduleSize, py % moduleSize, dark, r, g, b, options.Strength);
                    continue;
                }
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
        return new QrImageComposition(pixels, side, qrOffsetX, qrOffsetY, qrFullSize);
    }

    private static void WriteColor(byte[] pixels, int index, CodeGlyphX.Rendering.Png.Rgba32 color) {
        pixels[index] = color.R;
        pixels[index + 1] = color.G;
        pixels[index + 2] = color.B;
    }

    private static byte Adapt(double value, bool dark, double amount) =>
        (byte)Math.Round(dark ? value * amount : 255 - (255 - value) * amount);

    private static void WriteGray(byte[] pixels, int index, byte value) {
        pixels[index] = value;
        pixels[index + 1] = value;
        pixels[index + 2] = value;
    }

}
