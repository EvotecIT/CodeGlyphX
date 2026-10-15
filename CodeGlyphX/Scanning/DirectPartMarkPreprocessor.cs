using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

internal static partial class DirectPartMarkPreprocessor {
    /// <summary>Yields independently owned Gray8 retries in recognition order; their pixels remain valid after advancing or disposing the iterator.</summary>
    internal static IEnumerable<byte[]> CreateVariants(byte[] rgba, int width, int height, DirectPartMarkOptions options, CancellationToken cancellationToken) {
        Validate(options);
        return EnumerateVariants(rgba, width, height, options, cancellationToken);
    }

    private static IEnumerable<byte[]> EnumerateVariants(byte[] rgba, int width, int height, DirectPartMarkOptions options, CancellationToken cancellationToken) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) yield break;
        using var workspace = new Workspace(checked(width * height));
        ToGray(rgba, workspace.Gray, width, height, cancellationToken);
        if (DecodeBudget.ShouldAbort(cancellationToken)) yield break;
        var radius = options.AdaptiveWindowRadius > 0 ? options.AdaptiveWindowRadius : Math.Max(2, Math.Min(24, Math.Min(width, height) / 24));
        var count = 0;

        var stretched = ContrastStretch(workspace.Gray, width, height, cancellationToken);
        var variant = ToLuminance(stretched, cancellationToken);
        if (variant is null) yield break;
        yield return variant;
        if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;

        // Both polarities use the same clipped-window means. Keep those means
        // as bytes instead of retaining an image-sized Int64 summed-area table.
        BuildLocalMeans(workspace.Gray, workspace.Means, width, height, radius, cancellationToken);
        if (DecodeBudget.ShouldAbort(cancellationToken)) yield break;
        Adaptive(workspace.Gray, workspace.Means, workspace.Dark, width, height, options.ThresholdBias, lightMarks: false, cancellationToken);
        variant = CopyMaskToLuminance(workspace.Dark, cancellationToken);
        if (variant is null) yield break;
        yield return variant;
        if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;

        Adaptive(workspace.Gray, workspace.Means, workspace.Light, width, height, options.ThresholdBias, lightMarks: true, cancellationToken);
        variant = CopyMaskToLuminance(workspace.Light, cancellationToken);
        if (variant is null) yield break;
        yield return variant;
        if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;

        if (options.Profile is DirectPartMarkProfile.Auto or DirectPartMarkProfile.DotPeen && options.MorphologyRadius > 0) {
            var dilationRadius = options.MorphologyRadius;
            if (!Morphology(workspace.Dark, workspace.Dilated, width, height, dilationRadius, erode: false, cancellationToken)
                || !Morphology(workspace.Dilated, workspace.Eroded, width, height, dilationRadius, erode: true, cancellationToken)) yield break;
            variant = CopyMaskToLuminance(workspace.Eroded, cancellationToken);
            if (variant is null) yield break;
            yield return variant;
            if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;
            // Closing already computed this dilation. Its private mask remains
            // independent of the owned retry image yielded above.
            if (options.MorphologyRadius != dilationRadius
                && !Morphology(workspace.Dark, workspace.Dilated, width, height, options.MorphologyRadius, erode: false, cancellationToken)) yield break;
            variant = CopyMaskToLuminance(workspace.Dilated, cancellationToken);
            if (variant is null) yield break;
            yield return variant;
            if (DecodeBudget.ShouldAbort(cancellationToken) || ++count >= options.MaxAttempts) yield break;
            if (!Morphology(workspace.Light, workspace.Dilated, width, height, options.MorphologyRadius, erode: false, cancellationToken)
                || !Morphology(workspace.Dilated, workspace.Eroded, width, height, options.MorphologyRadius, erode: true, cancellationToken)) yield break;
            variant = CopyMaskToLuminance(workspace.Eroded, cancellationToken);
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

    private static void ToGray(byte[] rgba, Span<byte> gray, int width, int height, CancellationToken token) {
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return;
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
    }

    private static byte[] ContrastStretch(ReadOnlySpan<byte> gray, int width, int height, CancellationToken token) {
        var histogram = new int[256];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return gray.ToArray();
            var offset = y * width;
            for (var x = 0; x < width; x++) histogram[gray[offset + x]]++;
        }
        var tail = Math.Max(1, gray.Length / 100);
        var low = 0; var count = 0;
        while (low < 255 && count + histogram[low] <= tail) count += histogram[low++];
        var high = 255; count = 0;
        while (high > low && count + histogram[high] <= tail) count += histogram[high--];
        if (high <= low) return gray.ToArray();
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

    private static void BuildLocalMeans(ReadOnlySpan<byte> gray, Span<byte> means, int width, int height, int radius, CancellationToken token) {
        var columns = ArrayPool<int>.Shared.Rent(width);
        var budget = DecodeBudget.Capture();
        // Radius is at most 128: columns sum at most 257 bytes, and each
        // clipped window sums at most 257 * 257 * 255 = 16,842,495.
        // Only local sums use Int32; the complete image sum can be larger.
        try {
            Array.Clear(columns, 0, width);
            for (var y = 0; y <= Math.Min(radius, height - 1); y++) {
                if (budget.ShouldAbort(token)) return;
                var offset = y * width;
                for (var x = 0; x < width; x++) {
                    if ((x & 4095) == 0 && budget.ShouldAbort(token)) return;
                    columns[x] += gray[offset + x];
                }
            }
            for (var y = 0; y < height; y++) {
                if (budget.ShouldAbort(token)) return;
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
                    if ((x & 4095) == 0 && budget.ShouldAbort(token)) return;
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
        } finally {
            ArrayPool<int>.Shared.Return(columns);
        }
    }

    private static void Adaptive(ReadOnlySpan<byte> gray, ReadOnlySpan<byte> means, Span<byte> mask, int width, int height, int bias, bool lightMarks, CancellationToken token) {
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return;
            for (var x = 0; x < width; x++) {
                var mean = means[y * width + x];
                var value = gray[y * width + x];
                var dark = lightMarks ? value > mean + bias : value < mean - bias;
                mask[y * width + x] = dark ? (byte)1 : (byte)0;
            }
        }
    }

    private static bool Morphology(ReadOnlySpan<byte> mask, Span<byte> output, int width, int height, int radius, bool erode, CancellationToken token) =>
        BinaryMaskMorphology.FilterSquare(mask, output, width, height, radius, erode, zeroOutside: true, token, DecodeBudget.Capture());

    // Adaptive masks retain their 0/1 representation for later morphology.
    // Only their decode view needs 0/255 intensities; contrast pixels already
    // contain intensities and can be passed directly to the decoder.
    private static byte[]? ToLuminance(byte[] grayOrMask, CancellationToken token) {
        if (DecodeBudget.ShouldAbort(token)) return null;
        for (var i = 0; i < grayOrMask.Length; i++) {
            if ((i & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return null;
            if (grayOrMask[i] > 1) return DecodeBudget.ShouldAbort(token) ? null : grayOrMask;
        }
        return CopyMaskToLuminance(grayOrMask, token);
    }

    private static byte[]? CopyMaskToLuminance(ReadOnlySpan<byte> mask, CancellationToken token) {
        if (DecodeBudget.ShouldAbort(token)) return null;
        var luminance = new byte[mask.Length];
        for (var i = 0; i < mask.Length; i++) {
            if ((i & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return null;
            luminance[i] = mask[i] == 0 ? (byte)255 : (byte)0;
        }
        return DecodeBudget.ShouldAbort(token) ? null : luminance;
    }
}
