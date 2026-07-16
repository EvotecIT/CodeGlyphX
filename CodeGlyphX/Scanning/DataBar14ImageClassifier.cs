using System;
using System.Threading;

namespace CodeGlyphX;

/// <summary>
/// Distinguishes the physically identical GS1 DataBar-14 horizontal payloads by measured bar height.
/// </summary>
internal static class DataBar14ImageClassifier {
    private const double EncodedBlackSpanModules = 95.0;
    private const double OmnidirectionalMinimumHeightModules = 33.0;

    internal static bool TryIsOmnidirectional(
        byte[] rgba,
        int width,
        int height,
        CancellationToken cancellationToken,
        out bool isOmnidirectional) {
        isOmnidirectional = false;
        if (rgba is null || width <= 0 || height <= 0 || rgba.LongLength < (long)width * height * 4) return false;

        var minimum = 255;
        var maximum = 0;
        for (var y = 0; y < height; y++) {
            if ((y & 31) == 0 && cancellationToken.IsCancellationRequested) return false;
            var row = y * width * 4;
            for (var x = 0; x < width; x++) {
                var pixel = row + x * 4;
                var luminance = (rgba[pixel] * 54 + rgba[pixel + 1] * 183 + rgba[pixel + 2] * 19) >> 8;
                if (luminance < minimum) minimum = luminance;
                if (luminance > maximum) maximum = luminance;
            }
        }
        if (maximum <= minimum) return false;

        var threshold = (minimum + maximum) / 2;
        var rowDarkCounts = new int[height];
        var columnDarkCounts = new int[width];
        var maximumRowDarkCount = 0;
        var maximumColumnDarkCount = 0;
        for (var y = 0; y < height; y++) {
            if ((y & 31) == 0 && cancellationToken.IsCancellationRequested) return false;
            var row = y * width * 4;
            for (var x = 0; x < width; x++) {
                var pixel = row + x * 4;
                var luminance = (rgba[pixel] * 54 + rgba[pixel + 1] * 183 + rgba[pixel + 2] * 19) >> 8;
                if (luminance >= threshold) continue;
                rowDarkCounts[y]++;
                columnDarkCounts[x]++;
            }
            if (rowDarkCounts[y] > maximumRowDarkCount) maximumRowDarkCount = rowDarkCounts[y];
        }
        for (var x = 0; x < width; x++) {
            if (columnDarkCounts[x] > maximumColumnDarkCount) maximumColumnDarkCount = columnDarkCounts[x];
        }
        if (maximumRowDarkCount == 0 || maximumColumnDarkCount == 0) return false;

        var rowThreshold = Math.Max(1, (maximumRowDarkCount + 2) / 3);
        var columnThreshold = Math.Max(1, (maximumColumnDarkCount + 2) / 3);
        if (!TryGetActiveExtent(rowDarkCounts, rowThreshold, out var rowExtent)
            || !TryGetActiveExtent(columnDarkCounts, columnThreshold, out var columnExtent)) return false;

        var horizontal = columnExtent >= rowExtent;
        var longSpan = horizontal ? columnExtent : rowExtent;
        var shortSpan = horizontal
            ? GetLargestActiveRun(rowDarkCounts, rowThreshold)
            : GetLargestActiveRun(columnDarkCounts, columnThreshold);
        if (longSpan < 32 || shortSpan <= 0) return false;

        var heightModules = shortSpan * EncodedBlackSpanModules / longSpan;
        isOmnidirectional = heightModules >= OmnidirectionalMinimumHeightModules;
        return true;
    }

    private static bool TryGetActiveExtent(int[] counts, int threshold, out int extent) {
        extent = 0;
        var first = 0;
        while (first < counts.Length && counts[first] < threshold) first++;
        if (first == counts.Length) return false;
        var last = counts.Length - 1;
        while (last > first && counts[last] < threshold) last--;
        extent = last - first + 1;
        return true;
    }

    private static int GetLargestActiveRun(int[] counts, int threshold) {
        var largest = 0;
        var current = 0;
        for (var i = 0; i < counts.Length; i++) {
            if (counts[i] >= threshold) {
                current++;
                if (current > largest) largest = current;
            } else {
                current = 0;
            }
        }
        return largest;
    }
}
