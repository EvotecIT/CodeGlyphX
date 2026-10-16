using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    private static List<ImageRegion> FindRegions(MaskView view, CancellationToken token) {
        var regions = new List<ImageRegion>();
        var rows = new int[view.Height];
        var columns = new int[view.Width];
        long count = 0;
        for (var y = 0; y < view.Height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return regions;
            for (var x = 0; x < view.Width; x++) {
                if ((x & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return regions;
                if (!view.Get(x, y)) continue;
                rows[y]++; columns[x]++; count++;
            }
        }
        if (count == 0 || count > (long)view.Width * view.Height * .75) return regions;
        var left = 0; while (left < columns.Length && columns[left] == 0) left++;
        var right = columns.Length - 1; while (right >= left && columns[right] == 0) right--;
        var top = 0; while (top < rows.Length && rows[top] == 0) top++;
        var bottom = rows.Length - 1; while (bottom >= top && rows[bottom] == 0) bottom--;
        AddRegion(regions, new ImageRegion(left, top, right - left + 1, bottom - top + 1));
        // Empty projection bands separate captions, adjacent labels and symbols without assuming a renderer's margins.
        AddBands(view, regions, rows, horizontal: true, token);
        AddBands(view, regions, columns, horizontal: false, token);
        return regions;
    }

    private static void AddBands(MaskView view, List<ImageRegion> regions, int[] projection,
        bool horizontal, CancellationToken token) {
        for (var start = 0; start < projection.Length && regions.Count < 32;) {
            while (start < projection.Length && projection[start] == 0) start++;
            if (start == projection.Length) break;
            var end = start + 1;
            while (end < projection.Length && projection[end] > 0) end++;
            if (end - start >= 2) {
                var crossSize = horizontal ? view.Width : view.Height;
                var first = crossSize; var last = -1;
                for (var along = start; along < end; along++) {
                    if (DecodeBudget.ShouldAbort(token)) return;
                    for (var across = 0; across < crossSize; across++) {
                        if ((across & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return;
                        if (!(horizontal ? view.Get(across, along) : view.Get(along, across))) continue;
                        first = Math.Min(first, across); last = Math.Max(last, across);
                    }
                }
                if (last >= first) AddRegion(regions, horizontal
                    ? new ImageRegion(first, start, last - first + 1, end - start)
                    : new ImageRegion(start, first, end - start, last - first + 1));
            }
            start = end;
        }
    }

    private static void AddRegion(List<ImageRegion> regions, ImageRegion region) {
        if (region.Width < 3 || region.Height < 1) return;
        foreach (var existing in regions) if (existing.Equals(region)) return;
        regions.Add(region);
    }

    private static double EstimatePitch(MaskView view, ImageRegion region, bool fractional = false) {
        var lengths = new List<int>();
        for (var line = 1; line <= 7; line++) {
            var y = region.Y + Math.Min(region.Height - 1, region.Height * line / 8);
            var start = region.X;
            var previous = view.Get(start, y);
            for (var x = start + 1; x < region.X + region.Width; x++) {
                var current = view.Get(x, y);
                if (current == previous) continue;
                lengths.Add(x - start); start = x; previous = current;
            }
        }
        if (lengths.Count == 0) return 0;
        lengths.Sort();
        // The lower fifth contains narrow bars and spaces while ignoring occasional one-pixel edge noise.
        var pitch = (double)lengths[Math.Min(lengths.Count - 1, lengths.Count / 5)];
        if (!fractional) return pitch;
        for (var pass = 0; pass < 5; pass++) {
            var numerator = 0d; var denominator = 0d;
            foreach (var length in lengths) {
                var modules = (int)Math.Round(length / pitch);
                if (modules < 1 || modules > 8) continue;
                numerator += length * modules; denominator += modules * modules;
            }
            if (denominator == 0) break;
            pitch = numerator / denominator;
        }
        return pitch;
    }
}
