using System;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.DataBar;

internal static partial class DataBar14ImageDecoder {
    /// <summary>
    /// Fits a photographed separator from transitions between verified data
    /// rows. The band keeps its physical thickness while its center can slope;
    /// the complete encoded separator still has to match the sampled pixels.
    /// </summary>
    private static double FitSeparatorBand(byte[] rgba, int width, int length, bool vertical, bool reverse,
        BitMatrix expected, Row first, Row second, CancellationToken token) {
        var budget = DecodeBudget.Capture();
        if (budget.ShouldAbort(token)) return double.PositiveInfinity;
        var firstCenter = (first.First + first.Last) / 2;
        var secondCenter = (second.First + second.Last) / 2;
        var direction = Math.Sign(secondCenter - firstCenter);
        if (direction == 0) return double.PositiveInfinity;
        var pitch = (first.SampleEnd - first.SampleStart + second.SampleEnd - second.SampleStart) / 100;
        var separatorRows = expected.Height - 2;
        var threshold = (int)Math.Round((first.SampleThreshold + second.SampleThreshold) / 2);
        var xs = new double[42]; var centers = new double[42]; var heights = new double[42];
        var count = 0;
        for (var x = 4; x < 46; x++) {
            if (budget.ShouldAbort(token)) return double.PositiveInfinity;
            // Only columns that change at both data/separator boundaries
            // provide a measured band. The other columns still validate below.
            if (expected[x, 0] == expected[x, 1] || expected[x, expected.Height - 1] == expected[x, expected.Height - 2]) continue;
            if (Read(first, x, firstCenter) != expected[x, 0] || Read(second, x, secondCenter) != expected[x, expected.Height - 1]) continue;
            var top = firstCenter; var bottom = secondCenter;
            for (var at = firstCenter + direction; at != secondCenter; at += direction) {
                if ((at & 31) == 0 && budget.ShouldAbort(token)) return double.PositiveInfinity;
                if (Read(first, x, at) != expected[x, 0]) break;
                top = at;
            }
            for (var at = secondCenter - direction; at != firstCenter; at -= direction) {
                if ((at & 31) == 0 && budget.ShouldAbort(token)) return double.PositiveInfinity;
                if (Read(second, x, at) != expected[x, expected.Height - 1]) break;
                bottom = at;
            }
            var gap = (bottom - top) * direction - 1;
            if (gap < 1 || gap / pitch < separatorRows * 0.45 || gap / pitch > separatorRows * 2.0 + 1) continue;
            xs[count] = x; centers[count] = (top + bottom) / 2.0; heights[count] = gap; count++;
        }
        // Several points across both characters are required; a short stripe
        // beside a finder cannot determine the whole separator's geometry.
        if (count < 6 || xs[count - 1] - xs[0] < 20) return double.PositiveInfinity;
        // Blur beside a vertical data-run edge can connect a column to the
        // neighboring run instead of the separator. Keep a majority band with
        // consistent measured thickness before fitting its centerline.
        var measuredCount = count;
        var sortedHeights = new double[count]; Array.Copy(heights, sortedHeights, count);
        var bandHeight = Median(sortedHeights, count);
        count = 0;
        for (var i = 0; i < measuredCount; i++) {
            if (Math.Abs(heights[i] - bandHeight) > Math.Max(1, pitch * 0.5)) continue;
            xs[count] = xs[i]; centers[count] = centers[i]; heights[count] = heights[i]; count++;
        }
        if (count < 6 || count * 2 <= measuredCount || xs[count - 1] - xs[0] < 20) return double.PositiveInfinity;
        var slopes = new double[count * (count - 1) / 2]; var slopeCount = 0;
        for (var i = 0; i < count; i++) {
            if (budget.ShouldAbort(token)) return double.PositiveInfinity;
            for (var j = i + 1; j < count; j++)
                if (xs[j] - xs[i] >= 8) slopes[slopeCount++] = (centers[j] - centers[i]) / (xs[j] - xs[i]);
        }
        if (slopeCount == 0) return double.PositiveInfinity;
        var slope = Median(slopes, slopeCount);
        var intercepts = new double[count];
        for (var i = 0; i < count; i++) intercepts[i] = centers[i] - slope * xs[i];
        var intercept = Median(intercepts, count);
        var height = Median(heights, count);
        var inliers = 0;
        for (var i = 0; i < count; i++)
            if (Math.Abs(centers[i] - intercept - slope * xs[i]) <= pitch * 0.5) inliers++;
        if (inliers * 4 < count * 3) return double.PositiveInfinity;
        var mismatches = 0;
        for (var row = 1; row <= separatorRows; row++) {
            if (budget.ShouldAbort(token)) return double.PositiveInfinity;
            var errors = 0;
            for (var x = 4; x < 46; x++) {
                var across = (int)Math.Round(intercept + slope * x
                    + direction * ((row - 0.5) / separatorRows - 0.5) * (height + 1));
                // The entire fitted band stays between the two independently
                // verified data rows; it cannot sample a row as its separator.
                if (across <= Math.Min(firstCenter, secondCenter) || across >= Math.Max(firstCenter, secondCenter))
                    return double.PositiveInfinity;
                if (Read(first, x, across) != expected[x, row]) errors++;
            }
            if (errors > 9) return double.PositiveInfinity;
            mismatches += errors;
        }
        return budget.ShouldAbort(token) ? double.PositiveInfinity : mismatches / (42.0 * separatorRows);

        bool Read(Row observation, int module, int across) {
            var along = (int)Math.Floor(observation.SampleStart
                + (module + 0.5) * (observation.SampleEnd - observation.SampleStart) / 50);
            along = Math.Max(0, Math.Min(length - 1, along));
            if (reverse) along = length - 1 - along;
            return (Luminance(rgba, width, vertical, along, across) < threshold) != observation.Inverted;
        }
    }

    private static double Median(double[] values, int count) {
        Array.Sort(values, 0, count);
        return (count & 1) != 0 ? values[count / 2] : (values[count / 2 - 1] + values[count / 2]) / 2;
    }
}
