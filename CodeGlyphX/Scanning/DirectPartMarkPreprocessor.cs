using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

internal static class DirectPartMarkPreprocessor {
    /// <summary>Yields packed Gray8 retries in recognition order, without expanding them to color pixels.</summary>
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
        var variant = ToLuminance(stretched, cancellationToken);
        if (variant is null) yield break;
        yield return variant;
        if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;

        // Both polarities use the same clipped-window means. Keep those means
        // as bytes instead of retaining an image-sized Int64 summed-area table.
        var means = BuildLocalMeans(gray, width, height, radius, cancellationToken);
        if (DecodeBudget.ShouldAbort(cancellationToken)) yield break;
        var dark = Adaptive(gray, means, width, height, options.ThresholdBias, lightMarks: false, cancellationToken);
        variant = ToLuminance(dark, cancellationToken);
        if (variant is null) yield break;
        yield return variant;
        if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;

        var light = Adaptive(gray, means, width, height, options.ThresholdBias, lightMarks: true, cancellationToken);
        variant = ToLuminance(light, cancellationToken);
        if (variant is null) yield break;
        yield return variant;
        if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;

        if (options.Profile is DirectPartMarkProfile.Auto or DirectPartMarkProfile.DotPeen && options.MorphologyRadius > 0) {
            var closedDark = Close(dark, width, height, options.MorphologyRadius, cancellationToken);
            variant = ToLuminance(closedDark, cancellationToken);
            if (variant is null) yield break;
            yield return variant;
            if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;
            var dilatedDark = Dilate(dark, width, height, options.MorphologyRadius, cancellationToken);
            variant = ToLuminance(dilatedDark, cancellationToken);
            if (variant is null) yield break;
            yield return variant;
            if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;
            var closedLight = Close(light, width, height, options.MorphologyRadius, cancellationToken);
            variant = ToLuminance(closedLight, cancellationToken);
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

    private static byte[] BuildLocalMeans(byte[] gray, int width, int height, int radius, CancellationToken token) {
        var means = new byte[gray.Length];
        var columns = new int[width];
        var budget = DecodeBudget.Capture();
        // Radius is at most 128: columns sum at most 257 bytes, and each
        // clipped window sums at most 257 * 257 * 255 = 16,842,495.
        // Only local sums use Int32; the complete image sum can be larger.
        for (var y = 0; y <= Math.Min(radius, height - 1); y++) {
            if (budget.ShouldAbort(token)) return means;
            var offset = y * width;
            for (var x = 0; x < width; x++) {
                if ((x & 4095) == 0 && budget.ShouldAbort(token)) return means;
                columns[x] += gray[offset + x];
            }
        }
        for (var y = 0; y < height; y++) {
            if (budget.ShouldAbort(token)) return means;
            var offset = y * width;
            var rowCount = Math.Min(height - 1, y + radius) - Math.Max(0, y - radius) + 1;
            var removeRow = y > radius ? (y - radius - 1) * width : -1;
            var addRow = y > 0 && y + radius < height ? (y + radius) * width : -1;
            var sum = 0;
            for (var x = 0; x <= Math.Min(radius, width - 1); x++) {
                if (removeRow >= 0) columns[x] -= gray[removeRow + x];
                if (addRow >= 0) columns[x] += gray[addRow + x];
                sum += columns[x];
            }
            for (var x = 0; x < width; x++) {
                if ((x & 4095) == 0 && budget.ShouldAbort(token)) return means;
                var columnCount = Math.Min(width - 1, x + radius) - Math.Max(0, x - radius) + 1;
                means[offset + x] = (byte)(sum / (rowCount * columnCount));
                if (x >= radius) sum -= columns[x - radius];
                var entering = x + radius + 1;
                if (entering < width) {
                    // Advance each vertical sum once, as that column enters
                    // this row's horizontal window; avoid a separate full pass.
                    if (removeRow >= 0) columns[entering] -= gray[removeRow + entering];
                    if (addRow >= 0) columns[entering] += gray[addRow + entering];
                    sum += columns[entering];
                }
            }
        }
        return means;
    }

    private static byte[] Adaptive(byte[] gray, byte[] means, int width, int height, int bias, bool lightMarks, CancellationToken token) {
        var mask = new byte[gray.Length];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return mask;
            for (var x = 0; x < width; x++) {
                var mean = means[y * width + x];
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

    // Adaptive masks retain their 0/1 representation for later morphology.
    // Only their decode view needs 0/255 intensities; contrast pixels already
    // contain intensities and can be passed directly to the decoder.
    private static byte[]? ToLuminance(byte[] grayOrMask, CancellationToken token) {
        if (DecodeBudget.ShouldAbort(token)) return null;
        for (var i = 0; i < grayOrMask.Length; i++) {
            if ((i & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return null;
            if (grayOrMask[i] > 1) return DecodeBudget.ShouldAbort(token) ? null : grayOrMask;
        }
        var luminance = new byte[grayOrMask.Length];
        for (var i = 0; i < grayOrMask.Length; i++) {
            if ((i & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return null;
            luminance[i] = grayOrMask[i] == 0 ? (byte)255 : (byte)0;
        }
        return DecodeBudget.ShouldAbort(token) ? null : luminance;
    }
}
