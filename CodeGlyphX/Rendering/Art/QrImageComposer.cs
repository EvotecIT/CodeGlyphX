using CodeGlyphX.Rendering;
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
        var canvasOpacity = options.Canvas?.ArtOpacity ?? 1;
        var fadeWidth = (options.Canvas?.FadeModules ?? 0) * moduleSize;
        var canvasBase = options.Art is not null && options.Strength > 0 ? options.Art.FunctionalBackground : Rgba32.White;
        var tintModel = options.Art is not null && options.Strength > 0 && options.Art.FunctionalTint > 0
            ? new TintModel(options.Art, options.Strength, canvasOpacity, canvasBase, moduleSize, qr.Size, border, fadeWidth) : null;
        var art = options.Art is null ? null : new ArtGeometry(qr.Modules, functions, moduleSize, options.Art, tintModel,
            image, qrOffsetX + border - imageOriginX, qrOffsetY + border - imageOriginY, cancellationToken);
        var finders = options.Art is not null && options.Art.Finders != QrImageFinderStyle.Square && options.Strength > 0
            ? new FinderGeometry(moduleSize, options.Art) : null;
        var halftone = options.Style == QrImageCompositionStyle.Halftone3x3 && options.Strength > 0
            ? DitherSubCells(image, qr.Modules, moduleSize, qrOffsetX + border - imageOriginX,
                qrOffsetY + border - imageOriginY, Math.Min(options.Strength, MaxHalftoneImageWeight), options.Duotone is not null, cancellationToken) : null;
        var photo = options.Art is null && options.Strength > 0 && options.Style != QrImageCompositionStyle.Halftone3x3
            ? new PhotoPainter(qr, functions, options) : null;
        var duotone = options.Style == QrImageCompositionStyle.Halftone3x3 ? options.Duotone : null;
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
                    if (tintModel is not null) {
                        image.Sample(x - imageOriginX, y - imageOriginY, out var qr2, out var qg, out var qb);
                        tintModel.Write(pixels, index, px, py, qr2, qg, qb, 0);
                        continue;
                    }
                    if (hasCanvas && !inQuietZone) {
                        image.Sample(x, y, out var cr, out var cg, out var cb);
                        // Washed art blends toward the quiet-zone colour.
                        var opacity = canvasOpacity;
                        if (opacity < 1) {
                            cr = canvasBase.R + (cr - canvasBase.R) * opacity;
                            cg = canvasBase.G + (cg - canvasBase.G) * opacity;
                            cb = canvasBase.B + (cb - canvasBase.B) * opacity;
                        }
                        pixels[index] = (byte)Math.Round(cr);
                        pixels[index + 1] = (byte)Math.Round(cg);
                        pixels[index + 2] = (byte)Math.Round(cb);
                    } else {
                        WriteGray(pixels, index, 255);
                        if (options.Art is not null && options.Strength > 0) WriteColor(pixels, index, options.Art.FunctionalBackground);
                        else if (duotone is not null) WriteColor(pixels, index, duotone.Paper);
                    }
                    continue;
                }
                if (tintModel is not null) {
                    image.Sample(x - imageOriginX, y - imageOriginY, out var tr, out var tg, out var tb);
                    if (finders is not null && finders.Contains(px, py, qr.Size)) {
                        tintModel.Write(pixels, index, px, py, tr, tg, tb, finders.DarkCoverage(px, py, qr.Size));
                        continue;
                    }
                    // Timing, alignment, format and version modules take the same silhouettes as data modules.
                    var tx = px / moduleSize; var ty = py / moduleSize;
                    art!.Paint(pixels, index, tx, ty, px % moduleSize, py % moduleSize, qr.Modules[tx, ty], tr, tg, tb, options.Strength);
                    continue;
                }
                if (finders is not null && finders.TryPaint(pixels, index, px, py, qr.Size)) continue;
                var mx = px / moduleSize;
                var my = py / moduleSize;
                var dark = qr.Modules[mx, my];
                if (photo is not null) {
                    image.Sample(x - imageOriginX, y - imageOriginY, out var pr, out var pg, out var pb);
                    photo.Paint(pixels, index, px, py, dark, pr, pg, pb);
                    continue;
                }
                if (functions[mx, my] || options.Strength == 0) {
                    WriteGray(pixels, index, dark ? (byte)0 : (byte)255);
                    if (options.Strength > 0 && options.Art is not null)
                        WriteColor(pixels, index, dark ? options.Art.FunctionalForeground : options.Art.FunctionalBackground);
                    else if (duotone is not null) WriteColor(pixels, index, dark ? duotone.Ink : duotone.Paper);
                    continue;
                }
                if (halftone is not null) {
                    var sx = px % moduleSize * 3 / moduleSize;
                    var sy = py % moduleSize * 3 / moduleSize;
                    var isCenter = sx == 1 && sy == 1;
                    var inked = isCenter ? dark : halftone[(my * 3 + sy) * qr.Size * 3 + mx * 3 + sx];
                    if (duotone is not null) WriteColor(pixels, index, inked ? duotone.Ink : duotone.Paper);
                    else WriteGray(pixels, index, inked ? (byte)0 : (byte)255);
                    continue;
                }
                // Remaining pixels belong to module silhouettes: Art is set and Strength is positive.
                image.Sample(x - imageOriginX, y - imageOriginY, out var r, out var g, out var b);
                art!.Paint(pixels, index, mx, my, px % moduleSize, py % moduleSize, dark, r, g, b, options.Strength);
            }
        }
        return new QrImageComposition(pixels, side, qrOffsetX, qrOffsetY, qrFullSize);
    }

    private static void WriteColor(byte[] pixels, int index, CodeGlyphX.Rendering.Rgba32 color) {
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
