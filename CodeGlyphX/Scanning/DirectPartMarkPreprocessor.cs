using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

internal static class DirectPartMarkPreprocessor {
    internal static IEnumerable<byte[]> CreateVariants(byte[] rgba, int width, int height, DirectPartMarkOptions options, CancellationToken cancellationToken) {
        Validate(options);
        return EnumerateVariants(rgba, width, height, options, cancellationToken);
    }

    private static IEnumerable<byte[]> EnumerateVariants(byte[] rgba, int width, int height, DirectPartMarkOptions options, CancellationToken cancellationToken) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) yield break;
        var gray = ToGray(rgba, width, height, cancellationToken);
        if (DecodeBudget.ShouldAbort(cancellationToken)) yield break;
        var radius = options.AdaptiveWindowRadius > 0 ? options.AdaptiveWindowRadius : Math.Max(2, Math.Min(24, Math.Min(width, height) / 24));
        var count = 0;

        var stretched = ContrastStretch(gray, width, height, cancellationToken);
        var variant = ToRgba(stretched, width, height, cancellationToken);
        if (variant is null) yield break;
        yield return variant;
        if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;

        var dark = Adaptive(gray, width, height, radius, options.ThresholdBias, lightMarks: false, cancellationToken);
        variant = ToRgba(dark, width, height, cancellationToken);
        if (variant is null) yield break;
        yield return variant;
        if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;

        var light = Adaptive(gray, width, height, radius, options.ThresholdBias, lightMarks: true, cancellationToken);
        variant = ToRgba(light, width, height, cancellationToken);
        if (variant is null) yield break;
        yield return variant;
        if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;

        if (options.Profile is DirectPartMarkProfile.Auto or DirectPartMarkProfile.DotPeen && options.MorphologyRadius > 0) {
            var closedDark = Close(dark, width, height, options.MorphologyRadius, cancellationToken);
            variant = ToRgba(closedDark, width, height, cancellationToken);
            if (variant is null) yield break;
            yield return variant;
            if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;
            var dilatedDark = Dilate(dark, width, height, options.MorphologyRadius, cancellationToken);
            variant = ToRgba(dilatedDark, width, height, cancellationToken);
            if (variant is null) yield break;
            yield return variant;
            if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;
            var closedLight = Close(light, width, height, options.MorphologyRadius, cancellationToken);
            variant = ToRgba(closedLight, width, height, cancellationToken);
            if (variant is not null) yield return variant;
        }
    }

    internal static void Validate(DirectPartMarkOptions options) {
        if (options.Profile is < DirectPartMarkProfile.Auto or > DirectPartMarkProfile.DotPeen) throw new ArgumentOutOfRangeException(nameof(options), options.Profile, "Unknown direct-part-mark profile.");
        if (options.AdaptiveWindowRadius is < 0 or > 128) throw new ArgumentOutOfRangeException(nameof(options), options.AdaptiveWindowRadius, "Adaptive window radius must be between 0 and 128.");
        if (options.ThresholdBias is < 0 or > 64) throw new ArgumentOutOfRangeException(nameof(options), options.ThresholdBias, "Threshold bias must be between 0 and 64.");
        if (options.MorphologyRadius is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(options), options.MorphologyRadius, "Morphology radius must be between 0 and 3.");
        if (options.MaxAttempts is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(options), options.MaxAttempts, "DPM attempts must be between 1 and 8.");
    }

    private static byte[] ToGray(byte[] rgba, int width, int height, CancellationToken token) {
        var gray = new byte[checked(width * height)];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return gray;
            for (var x = 0; x < width; x++) {
                var i = y * width + x;
                var p = i * 4;
                var luminance = (rgba[p] * 77 + rgba[p + 1] * 150 + rgba[p + 2] * 29) >> 8;
                var alpha = rgba[p + 3];
                // Match ordinary pixel decoding: transparent RGB contributes
                // no visible mark against the scanner's white background.
                gray[i] = (byte)((luminance * alpha + 255 * (255 - alpha) + 127) / 255);
            }
        }
        return gray;
    }

    private static byte[] ContrastStretch(byte[] gray, int width, int height, CancellationToken token) {
        var histogram = new int[256];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return (byte[])gray.Clone();
            var offset = y * width;
            for (var x = 0; x < width; x++) histogram[gray[offset + x]]++;
        }
        var tail = Math.Max(1, gray.Length / 100);
        var low = 0; var count = 0;
        while (low < 255 && count + histogram[low] <= tail) count += histogram[low++];
        var high = 255; count = 0;
        while (high > low && count + histogram[high] <= tail) count += histogram[high--];
        if (high <= low) return (byte[])gray.Clone();
        var output = new byte[gray.Length];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return output;
            var offset = y * width;
            for (var x = 0; x < width; x++) {
                var i = offset + x;
                output[i] = (byte)Math.Max(0, Math.Min(255, (gray[i] - low) * 255 / (high - low)));
            }
        }
        return output;
    }

    private static byte[] Adaptive(byte[] gray, int width, int height, int radius, int bias, bool lightMarks, CancellationToken token) {
        var stride = width + 1;
        var integral = new long[checked((width + 1) * (height + 1))];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return new byte[gray.Length];
            long row = 0;
            for (var x = 0; x < width; x++) { row += gray[y * width + x]; integral[(y + 1) * stride + x + 1] = integral[y * stride + x + 1] + row; }
        }
        var mask = new byte[gray.Length];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return mask;
            var top = Math.Max(0, y - radius); var bottom = Math.Min(height - 1, y + radius);
            for (var x = 0; x < width; x++) {
                var left = Math.Max(0, x - radius); var right = Math.Min(width - 1, x + radius);
                var sum = integral[(bottom + 1) * stride + right + 1] - integral[top * stride + right + 1] - integral[(bottom + 1) * stride + left] + integral[top * stride + left];
                var mean = (int)(sum / ((right - left + 1) * (bottom - top + 1)));
                var value = gray[y * width + x];
                var dark = lightMarks ? value > mean + bias : value < mean - bias;
                mask[y * width + x] = dark ? (byte)1 : (byte)0;
            }
        }
        return mask;
    }

    private static byte[] Close(byte[] mask, int width, int height, int radius, CancellationToken token) {
        var dilated = Dilate(mask, width, height, radius, token);
        return DecodeBudget.ShouldAbort(token) ? dilated : Erode(dilated, width, height, radius, token);
    }

    private static byte[] Dilate(byte[] mask, int width, int height, int radius, CancellationToken token) {
        var output = new byte[mask.Length];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return output;
            for (var x = 0; x < width; x++) {
                byte value = 0;
                for (var dy = -radius; dy <= radius && value == 0; dy++) for (var dx = -radius; dx <= radius; dx++) {
                    var sx = x + dx; var sy = y + dy;
                    if ((uint)sx < (uint)width && (uint)sy < (uint)height && mask[sy * width + sx] != 0) { value = 1; break; }
                }
                output[y * width + x] = value;
            }
        }
        return output;
    }

    private static byte[] Erode(byte[] mask, int width, int height, int radius, CancellationToken token) {
        var output = new byte[mask.Length];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return output;
            for (var x = 0; x < width; x++) {
                byte value = 1;
                for (var dy = -radius; dy <= radius && value != 0; dy++) for (var dx = -radius; dx <= radius; dx++) {
                    var sx = x + dx; var sy = y + dy;
                    if ((uint)sx >= (uint)width || (uint)sy >= (uint)height || mask[sy * width + sx] == 0) { value = 0; break; }
                }
                output[y * width + x] = value;
            }
        }
        return output;
    }

    private static byte[]? ToRgba(byte[] grayOrMask, int width, int height, CancellationToken token) {
        if (DecodeBudget.ShouldAbort(token)) return null;
        var rgba = new byte[checked(width * height * 4)];
        var binary = true;
        for (var i = 0; i < grayOrMask.Length; i++) {
            if ((i & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return null;
            if (grayOrMask[i] > 1) { binary = false; break; }
        }
        for (var i = 0; i < grayOrMask.Length; i++) {
            if ((i & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return null;
            var value = binary ? (grayOrMask[i] == 0 ? (byte)255 : (byte)0) : grayOrMask[i];
            var p = i * 4; rgba[p] = rgba[p + 1] = rgba[p + 2] = value; rgba[p + 3] = 255;
        }
        return rgba;
    }
}
