using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    /// <summary>
    /// Finds neighboring Pharmacodes separated by background wider than ordinary
    /// inter-bar spacing. Mid-gap crops retain each side's quiet margin even when
    /// a regular overlapping tile reaches into the next symbol's first bar.
    /// This supplements only explicitly requested Pharmacode tile searches.
    /// </summary>
    private static List<ImageRegion> FindPharmacodeRegions(byte[] pixels, int width, int height, int stride,
        PixelFormat format, CancellationToken token) {
        var regions = new List<ImageRegion>();
        var seen = new HashSet<ImageRegion>();
        foreach (var vertical in new[] { false, true }) {
            var length = vertical ? height : width;
            var across = vertical ? width : height;
            var luminance = new byte[length];
            foreach (var position in new[] { across / 2, across / 3, across * 2 / 3 }) {
                var minimum = 255; var maximum = 0;
                for (var at = 0; at < length; at++) {
                    if ((at & 127) == 0 && DecodeBudget.ShouldAbort(token)) return regions;
                    var offset = vertical ? at * stride + position * 4 : position * stride + at * 4;
                    var red = format == PixelFormat.Rgba32 ? pixels[offset] : pixels[offset + 2];
                    var blue = format == PixelFormat.Rgba32 ? pixels[offset + 2] : pixels[offset];
                    var value = BarcodeScanline.GetLuminance(red, pixels[offset + 1], blue, pixels[offset + 3]);
                    luminance[at] = value;
                    minimum = Math.Min(minimum, value); maximum = Math.Max(maximum, value);
                }
                if (maximum - minimum < 8) continue;
                var threshold = (minimum + maximum) / 2;
                var darkBackground = luminance[0] < threshold;
                if (darkBackground != (luminance[length - 1] < threshold)) continue;
                var runs = new List<(int Start, int End, bool Foreground)>();
                var start = 0;
                while (start < length) {
                    if (DecodeBudget.ShouldAbort(token)) return regions;
                    var foreground = (luminance[start] < threshold) != darkBackground;
                    var end = start + 1;
                    while (end < length && ((luminance[end] < threshold) != darkBackground) == foreground) {
                        if ((end & 127) == 0 && DecodeBudget.ShouldAbort(token)) return regions;
                        end++;
                    }
                    runs.Add((start, end, foreground)); start = end;
                }
                var minimumBar = int.MaxValue;
                foreach (var run in runs) {
                    if (!run.Foreground) continue;
                    minimumBar = Math.Min(minimumBar, run.End - run.Start);
                }
                if (minimumBar == int.MaxValue) continue;
                var boundaries = new List<int> { 0 };
                for (var i = 1; i + 1 < runs.Count; i++) {
                    var run = runs[i];
                    var gap = run.End - run.Start;
                    if (!run.Foreground && (long)gap >= (long)minimumBar * 4)
                        boundaries.Add(run.Start + gap / 2);
                }
                if (boundaries.Count == 1) continue;
                boundaries.Add(length);
                var runIndex = 0;
                var rowRegions = new List<ImageRegion>();
                var completePartition = true;
                for (var i = 0; i + 1 < boundaries.Count; i++) {
                    if (DecodeBudget.ShouldAbort(token)) return regions;
                    var left = boundaries[i]; var right = boundaries[i + 1];
                    var first = right; var last = left; var bars = 0;
                    for (; runIndex < runs.Count && runs[runIndex].End <= right; runIndex++) {
                        var run = runs[runIndex];
                        if (!run.Foreground || run.Start < left || run.End > right) continue;
                        first = Math.Min(first, run.Start); last = Math.Max(last, run.End); bars++;
                    }
                    if (bars < 2 || bars > 16 || (long)first - left < (long)minimumBar * 2
                        || (long)right - last < (long)minimumBar * 2) { completePartition = false; break; }
                    var foregroundModules = new bool[last - first];
                    for (var at = first; at < last; at++)
                        foregroundModules[at - first] = (luminance[at] < threshold) != darkBackground;
                    if (!TryDecodePharmacode(foregroundModules, out _)) { completePartition = false; break; }
                    var region = vertical ? new ImageRegion(0, left, width, right - left)
                        : new ImageRegion(left, 0, right - left, height);
                    rowRegions.Add(region);
                }
                // Partial matches inside a guarded barcode are not evidence of
                // neighboring Pharmacodes. Every partition must validate.
                if (completePartition && rowRegions.Count >= 2)
                    foreach (var region in rowRegions) if (seen.Add(region)) regions.Add(region);
            }
        }
        return regions;
    }
}
