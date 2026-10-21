using System;
using System.Threading;
using CodeGlyphX.CodeOne;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    // Code One's central finder has blank rows. Single occupied projection bands therefore
    // split the code, while a full-image bound can include a caption or readable line.
    private static bool TryCodeOneTextBands(MaskView view, ImageRegion region, CancellationToken token,
        out ImageRegion sampledRegion, out DetectedSymbol symbol, out int rotation, out bool mirrored) {
        sampledRegion = region; symbol = null!; rotation = 0; mirrored = false;
        if ((long)region.Width * region.Height > 1048576) return false;
        var lefts = new int[region.Height];
        var rights = new int[region.Height];
        for (var y = 0; y < region.Height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            lefts[y] = region.Width; rights[y] = -1;
            for (var x = 0; x < region.Width; x++) {
                if ((x & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                if (!view.Get(region.X + x, region.Y + y)) continue;
                lefts[y] = Math.Min(lefts[y], x); rights[y] = x;
            }
        }
        var attempts = 0;
        foreach (var layout in CodeOneLayout.All) {
            // Join the finder/data bands across their one-module gaps, keeping the larger
            // clear space around an ordinary printed text line outside the candidate.
            var gap = Math.Max(1, region.Width / (double)layout.Width * 1.5);
            for (var start = 0; start < region.Height;) {
                if (DecodeBudget.ShouldAbort(token)) return false;
                while (start < region.Height && rights[start] < 0) start++;
                if (start == region.Height) break;
                var last = start; var end = start + 1;
                var left = lefts[start]; var right = rights[start];
                while (end < region.Height) {
                    if (DecodeBudget.ShouldAbort(token)) return false;
                    if (rights[end] >= 0) {
                        last = end; left = Math.Min(left, lefts[end]); right = Math.Max(right, rights[end]);
                    } else if (end - last > gap) break;
                    end++;
                }
                var width = right - left + 1; var height = last - start + 1;
                var ratio = width / (double)layout.Width / (height / (double)layout.Height);
                if (width >= layout.Width && height >= layout.Height && ratio >= .8 && ratio <= 1.25) {
                    var area = new ImageRegion(region.X + left, region.Y + start, width, height);
                    if (area.Equals(region)) { start = end; continue; }
                    if (++attempts > 32) return false;
                    if (TrySample(view, area, layout.Width, layout.Height, token, out var grid)
                        && TryOrientations(grid, SymbolFormat.CodeOne, token, out symbol, out rotation, out mirrored)) {
                        sampledRegion = area;
                        return true;
                    }
                }
                start = end;
            }
        }
        return false;
    }
}
