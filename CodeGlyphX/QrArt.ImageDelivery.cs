using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>
    /// Checks original, half-size, blurred, screen-sized, JPEG-compressed, perspective-warped and
    /// print-resolution images against the exact payload. Physical camera and print tests remain necessary.
    /// Raster codecs follow imageOptions and do not support cancellation while encoding/decoding.
    /// </summary>
    public static QrImageValidationReport ValidateDelivery(byte[] image, string expectedPayload,
        QrImageDeliveryOptions? options = null, ImageDecodeOptions? imageOptions = null, CancellationToken cancellationToken = default) =>
        ValidateDeliveryCoreAsync(image, expectedPayload, options, imageOptions, cancellationToken, false).GetAwaiter().GetResult();

    /// <summary>Checks delivery simulations with cooperative scheduling between checks for browser hosts.</summary>
    public static Task<QrImageValidationReport> ValidateDeliveryAsync(byte[] image, string expectedPayload,
        QrImageDeliveryOptions? options = null, ImageDecodeOptions? imageOptions = null, CancellationToken cancellationToken = default) =>
        ValidateDeliveryCoreAsync(image, expectedPayload, options, imageOptions, cancellationToken, true);

    private static async Task<QrImageValidationReport> ValidateDeliveryCoreAsync(byte[] image, string expectedPayload,
        QrImageDeliveryOptions? options, ImageDecodeOptions? imageOptions, CancellationToken cancellationToken, bool yieldBetweenChecks) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (expectedPayload is null) throw new ArgumentNullException(nameof(expectedPayload));
        options ??= new QrImageDeliveryOptions();
        options.Validate();
        if (imageOptions is not null && imageOptions.MaxDimension != 0)
            throw new ArgumentException("Delivery validation must preserve original dimensions.", nameof(imageOptions));
        cancellationToken.ThrowIfCancellationRequested();
        var pixels = DecodeValidationPixels(image, imageOptions, cancellationToken, out var width, out var height);
        var checks = new List<QrImageValidationCheck>();
        foreach (var check in EnumerateValidationChecks(pixels, width, height, expectedPayload, options.DecodeBudgetMilliseconds, cancellationToken)) {
            checks.Add(check);
            if (yieldBetweenChecks) await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
        ResizeCheck("ScreenSize", options.ScreenSize);
        if (yieldBetweenChecks) await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        var printSize = Math.Max(1, (int)Math.Round(options.PrintMillimeters / 25.4 * options.PrintDpi));
        ResizeCheck("PrintRaster", printSize);
        if (yieldBetweenChecks) await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var jpeg = JpegWriter.WriteRgba(width, height, pixels, width * 4, options.JpegQuality);
        cancellationToken.ThrowIfCancellationRequested();
        var jpegPixels = ImageReader.DecodeRgba32(jpeg, imageOptions, out var jpegWidth, out var jpegHeight);
        Check("JPEG", jpegPixels, jpegWidth, jpegHeight);
        if (yieldBetweenChecks) await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        var perspective = WarpDeliveryImage(pixels, width, height, options.PerspectiveInset, cancellationToken);
        Check("Perspective", perspective, width, height);
        return new QrImageValidationReport(checks.ToArray());

        void ResizeCheck(string name, int size) {
            var scale = size / (double)Math.Max(width, height);
            var w = Math.Max(1, (int)Math.Round(width * scale));
            var h = Math.Max(1, (int)Math.Round(height * scale));
            var resized = ImageScaler.ResizeToFitBox(pixels, width, height, width * 4, w, h, Rgba32.White, false, cancellationToken);
            Check(name, resized, w, h);
        }
        void Check(string name, byte[] data, int w, int h) {
            cancellationToken.ThrowIfCancellationRequested();
            var found = QrImageDecoder.TryDecode(data, w, h, w * 4, PixelFormat.Rgba32,
                new QrPixelDecodeOptions { Profile = name == "Perspective" ? QrDecodeProfile.Robust : QrDecodeProfile.Balanced, BudgetMilliseconds = options.DecodeBudgetMilliseconds }, cancellationToken, out var decoded);
            cancellationToken.ThrowIfCancellationRequested();
            checks.Add(new QrImageValidationCheck(name, w, h, found ? decoded.Text : null, expectedPayload));
        }
    }

    private static byte[] WarpDeliveryImage(byte[] pixels, int width, int height, double inset, CancellationToken token) {
        token.ThrowIfCancellationRequested();
        if (width < 2 || height < 2 || inset == 0) return pixels;
        var maxX = width - 1.0;
        var maxY = height - 1.0;
        var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            inset * maxX, 0, (1 - inset) * maxX, 0, maxX, maxY, 0, maxY,
            0, 0, maxX, 0, maxX, maxY, 0, maxY);
        var result = new byte[pixels.Length];
        for (var y = 0; y < height; y++) {
            token.ThrowIfCancellationRequested();
            for (var x = 0; x < width; x++) {
                if ((x & 1023) == 0) token.ThrowIfCancellationRequested();
                var p = (y * width + x) * 4;
                transform.Transform(x, y, out var sx, out var sy);
                if (double.IsNaN(sx) || double.IsNaN(sy) || sx < 0 || sy < 0 || sx > maxX || sy > maxY) {
                    result[p] = result[p + 1] = result[p + 2] = result[p + 3] = 255;
                    continue;
                }
                var x0 = (int)sx; var y0 = (int)sy;
                var x1 = Math.Min(width - 1, x0 + 1); var y1 = Math.Min(height - 1, y0 + 1);
                var fx = sx - x0; var fy = sy - y0;
                for (var c = 0; c < 3; c++) result[p + c] = (byte)Math.Round(
                    (pixels[(y0 * width + x0) * 4 + c] * (1 - fx) + pixels[(y0 * width + x1) * 4 + c] * fx) * (1 - fy)
                    + (pixels[(y1 * width + x0) * 4 + c] * (1 - fx) + pixels[(y1 * width + x1) * 4 + c] * fx) * fy);
                result[p + 3] = 255;
            }
        }
        return result;
    }
}
