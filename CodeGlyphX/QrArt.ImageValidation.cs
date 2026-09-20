using System;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>
    /// Decodes an exported image, a box-filtered half-size copy, and a 3x3 box-blurred copy.
    /// Compares recovered text with the expected payload using ordinal equality. Each recognition
    /// attempt has its own cooperative budget; raster decoding is governed by imageOptions.
    /// This uses CodeGlyphX's decoder, not independent camera certification. Legacy runtimes use
    /// the limited managed fallback. MaxDimension must remain zero so the original is tested at its
    /// exported dimensions. Test actual printed/resized/compressed delivery assets as well.
    /// </summary>
    public static QrImageValidationReport ValidateImage(byte[] image, string expectedPayload,
        int budgetMilliseconds = 1000, ImageDecodeOptions? imageOptions = null, CancellationToken cancellationToken = default) {
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (expectedPayload is null) throw new ArgumentNullException(nameof(expectedPayload));
        if (budgetMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(budgetMilliseconds));
        if (imageOptions is not null && imageOptions.MaxDimension != 0)
            throw new ArgumentException("Validation must preserve exported dimensions; MaxDimension must be zero.", nameof(imageOptions));
        cancellationToken.ThrowIfCancellationRequested();
        var pixels = ImageReader.DecodeRgba32(image, imageOptions, out var width, out var height);
        // Validate what a reader sees on white, including alpha in externally supplied exports.
        for (var i = 0; i < pixels.Length; i += 4) {
            if ((i & 65535) == 0) cancellationToken.ThrowIfCancellationRequested();
            var alpha = pixels[i + 3];
            for (var c = 0; c < 3; c++) pixels[i + c] = (byte)((pixels[i + c] * alpha + 255 * (255 - alpha) + 127) / 255);
            pixels[i + 3] = 255;
        }
        var options = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced, BudgetMilliseconds = budgetMilliseconds };
        var original = Check("Original", pixels, width, height);
        var halfWidth = Math.Max(1, width / 2);
        var halfHeight = Math.Max(1, height / 2);
        cancellationToken.ThrowIfCancellationRequested();
        var half = ImageScaler.ResizeToFitBox(pixels, width, height, width * 4, halfWidth, halfHeight, Rgba32.White, false, cancellationToken);
        var halfCheck = Check("HalfSize", half, halfWidth, halfHeight);
        var blurred = Blur(pixels, width, height, cancellationToken);
        var blurCheck = Check("BoxBlur", blurred, width, height);
        return new QrImageValidationReport(new[] { original, halfCheck, blurCheck });

        QrImageValidationCheck Check(string name, byte[] data, int w, int h) {
            cancellationToken.ThrowIfCancellationRequested();
            var found = QrImageDecoder.TryDecode(data, w, h, w * 4, PixelFormat.Rgba32, options, cancellationToken, out var decoded);
            cancellationToken.ThrowIfCancellationRequested();
            return new QrImageValidationCheck(name, w, h, found ? decoded.Text : null, expectedPayload);
        }
    }

    private static byte[] Blur(byte[] pixels, int width, int height, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new byte[pixels.Length];
        for (var y = 0; y < height; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < width; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var target = (y * width + x) * 4;
                for (var c = 0; c < 3; c++) {
                    var sum = 0;
                    var count = 0;
                    for (var yy = Math.Max(0, y - 1); yy <= Math.Min(height - 1, y + 1); yy++) {
                        for (var xx = Math.Max(0, x - 1); xx <= Math.Min(width - 1, x + 1); xx++) {
                            sum += pixels[(yy * width + xx) * 4 + c];
                            count++;
                        }
                    }
                    result[target + c] = (byte)((sum + count / 2) / count);
                }
                result[target + 3] = 255;
            }
        }
        return result;
    }
}
