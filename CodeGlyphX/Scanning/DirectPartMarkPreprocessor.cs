using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX;

internal static class DirectPartMarkPreprocessor {
    internal static IReadOnlyList<byte[]> CreateVariants(byte[] rgba, int width, int height, DirectPartMarkOptions options, CancellationToken cancellationToken) {
        Validate(options);
        var variants = new List<byte[]>(Math.Min(options.MaxAttempts, 8));
        if (cancellationToken.IsCancellationRequested) return variants;
        var gray = ToGray(rgba, width, height, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return variants;
        var radius = options.AdaptiveWindowRadius > 0 ? options.AdaptiveWindowRadius : Math.Max(2, Math.Min(24, Math.Min(width, height) / 24));

        var stretched = ContrastStretch(gray, width, height, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return variants;
        Add(variants, stretched, width, height, options.MaxAttempts, cancellationToken);
        if (cancellationToken.IsCancellationRequested || variants.Count >= options.MaxAttempts) return variants;

        var dark = Adaptive(gray, width, height, radius, options.ThresholdBias, lightMarks: false, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return variants;
        Add(variants, dark, width, height, options.MaxAttempts, cancellationToken);
        if (cancellationToken.IsCancellationRequested || variants.Count >= options.MaxAttempts) return variants;

        var light = Adaptive(gray, width, height, radius, options.ThresholdBias, lightMarks: true, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return variants;
        Add(variants, light, width, height, options.MaxAttempts, cancellationToken);
        if (cancellationToken.IsCancellationRequested || variants.Count >= options.MaxAttempts) return variants;

        if (options.Profile is DirectPartMarkProfile.Auto or DirectPartMarkProfile.DotPeen && options.MorphologyRadius > 0) {
            var closedDark = Close(dark, width, height, options.MorphologyRadius, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return variants;
            Add(variants, closedDark, width, height, options.MaxAttempts, cancellationToken);
            if (cancellationToken.IsCancellationRequested || variants.Count >= options.MaxAttempts) return variants;
            var dilatedDark = Dilate(dark, width, height, options.MorphologyRadius, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return variants;
            Add(variants, dilatedDark, width, height, options.MaxAttempts, cancellationToken);
            if (cancellationToken.IsCancellationRequested || variants.Count >= options.MaxAttempts) return variants;
            var closedLight = Close(light, width, height, options.MorphologyRadius, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return variants;
            Add(variants, closedLight, width, height, options.MaxAttempts, cancellationToken);
        }
        return variants;
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
            if (token.IsCancellationRequested) return gray;
            for (var x = 0; x < width; x++) {
                var i = y * width + x;
                var p = i * 4;
                gray[i] = (byte)((rgba[p] * 77 + rgba[p + 1] * 150 + rgba[p + 2] * 29) >> 8);
            }
        }
        return gray;
    }

    private static byte[] ContrastStretch(byte[] gray, int width, int height, CancellationToken token) {
        var histogram = new int[256];
        for (var y = 0; y < height; y++) {
            if (token.IsCancellationRequested) return (byte[])gray.Clone();
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
            if (token.IsCancellationRequested) return output;
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
            if (token.IsCancellationRequested) return new byte[gray.Length];
            long row = 0;
            for (var x = 0; x < width; x++) { row += gray[y * width + x]; integral[(y + 1) * stride + x + 1] = integral[y * stride + x + 1] + row; }
        }
        var mask = new byte[gray.Length];
        for (var y = 0; y < height; y++) {
            if (token.IsCancellationRequested) return mask;
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
        return token.IsCancellationRequested ? dilated : Erode(dilated, width, height, radius, token);
    }

    private static byte[] Dilate(byte[] mask, int width, int height, int radius, CancellationToken token) {
        var output = new byte[mask.Length];
        for (var y = 0; y < height; y++) {
            if (token.IsCancellationRequested) return output;
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
            if (token.IsCancellationRequested) return output;
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

    private static void Add(List<byte[]> variants, byte[] grayOrMask, int width, int height, int max, CancellationToken token) {
        if (variants.Count >= max || token.IsCancellationRequested) return;
        var rgba = new byte[checked(width * height * 4)];
        var binary = true;
        for (var i = 0; i < grayOrMask.Length; i++) {
            if ((i & 4095) == 0 && token.IsCancellationRequested) return;
            if (grayOrMask[i] > 1) { binary = false; break; }
        }
        for (var i = 0; i < grayOrMask.Length; i++) {
            if ((i & 4095) == 0 && token.IsCancellationRequested) return;
            var value = binary ? (grayOrMask[i] == 0 ? (byte)255 : (byte)0) : grayOrMask[i];
            var p = i * 4; rgba[p] = rgba[p + 1] = rgba[p + 2] = value; rgba[p + 3] = 255;
        }
        variants.Add(rgba);
    }
}
