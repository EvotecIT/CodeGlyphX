using System;
using System.Collections.Generic;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    // Composed labels often have clear gutters but cross every fixed grid boundary.
    // Isolate those regions before a whole-frame recognizer can spend the allowance
    // repeatedly observing the same symbol. Single-result calls retain their path.
    private static void ScanSeparatedRegions(byte[] rgba, int width, int height, ImageRegion sourceRegion,
        ScanOptions options, ScanDeadline deadline, List<DetectedSymbol> results, HashSet<string>? seen) {
        var grid = ResolveMatrixTileGrid(options, width, height);
        var regions = new List<ImageRegion> { new(0, 0, width, height) };
        for (var i = 0; i < regions.Count; ) {
            if (deadline.ShouldStop) return;
            if (!TrySplitRegion(rgba, width, regions[i], deadline, regions.Count < grid * grid, out var first, out var second)) {
                regions[i] = first;
                i++;
                continue;
            }
            regions[i] = first;
            regions.Insert(i + 1, second);
        }
        if (regions.Count == 1 || deadline.ShouldStop) return;

        // Reserve time for the established whole-image/fixed-grid searches, which
        // remain necessary on photographs, clutter and symbols without clear gutters.
        using var search = deadline.CreateAttempt(2, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        var localOptions = SnapshotOptions(options);
        localOptions.MaxSymbols = 1;
        localOptions.Region = null;
        localOptions.EnableTileScan = false;
        localOptions.Image = null; // The source ROI and image limits were already applied.
        for (var i = 0; i < regions.Count && !ShouldStop(options, search, results); i++) {
            var region = regions[i];
            var tile = new byte[checked(region.Width * region.Height * 4)];
            for (var y = 0; y < region.Height; y++) {
                if (search.ShouldStop) return;
                Buffer.BlockCopy(rgba, ((region.Y + y) * width + region.X) * 4,
                    tile, y * region.Width * 4, region.Width * 4);
            }
            var x = sourceRegion.X + (int)((long)region.X * sourceRegion.Width / width);
            var y0 = sourceRegion.Y + (int)((long)region.Y * sourceRegion.Height / height);
            var right = sourceRegion.X + (int)(((long)(region.X + region.Width) * sourceRegion.Width + width - 1) / width);
            var bottom = sourceRegion.Y + (int)(((long)(region.Y + region.Height) * sourceRegion.Height + height - 1) / height);
            var mappedRegion = new ImageRegion(x, y0, right - x, bottom - y0);
            using var attempt = search.CreateAttempt(regions.Count - i, options.Image?.RecognitionBudgetMilliseconds ?? 0);
            var scan = ScanFrame(ImageFrame.Packed(tile, region.Width, region.Height, PixelFormat.Rgba32), localOptions, attempt);
            foreach (var symbol in scan.Symbols) {
                var geometry = symbol.Geometry is null ? null : MapGeometryToSource(symbol.Geometry, mappedRegion, region.Width, region.Height);
                Add(results, seen, new DetectedSymbol(symbol.Format, symbol.Text,
                    symbol.HasRawBytes ? symbol.RawBytes.ToArray() : null, symbol.Metadata, symbol.PayloadProfile,
                    mappedRegion, geometry, symbol.Confidence, symbol.IsInverted, symbol.IsMirrored,
                    symbol.SymbologyIdentifier, symbol.DirectPartMarkProfile));
                if (ReachedMaximum(options, results)) return;
            }
        }
    }

    private static bool TrySplitRegion(byte[] rgba, int width, ImageRegion region, ScanDeadline deadline, bool allowSplit,
        out ImageRegion first, out ImageRegion second) {
        first = region;
        second = default;
        var columns = new bool[region.Width];
        var rows = new bool[region.Height];
        for (var y = 0; y < region.Height; y++) {
            if (deadline.ShouldStop) return false;
            var at = ((region.Y + y) * width + region.X) * 4;
            for (var x = 0; x < region.Width; x++, at += 4) {
                // Treat transparent pixels as the white background used by recognizers.
                var luminance = (rgba[at] * 77 + rgba[at + 1] * 150 + rgba[at + 2] * 29) >> 8;
                luminance = (luminance * rgba[at + 3] + 255 * (255 - rgba[at + 3]) + 127) / 255;
                if (luminance >= 128) continue;
                columns[x] = true;
                rows[y] = true;
            }
        }
        var left = Array.IndexOf(columns, true);
        var top = Array.IndexOf(rows, true);
        if (left < 0 || top < 0) return false;
        var right = Math.Min(region.Width, Array.LastIndexOf(columns, true) + 17);
        var bottom = Math.Min(region.Height, Array.LastIndexOf(rows, true) + 17);
        left = Math.Max(0, left - 16);
        top = Math.Max(0, top - 16);
        first = new ImageRegion(region.X + left, region.Y + top, right - left, bottom - top);
        if (!allowSplit) return false;
        var minGap = Math.Max(16, Math.Min(region.Width, region.Height) / 20);
        var xSplit = FindGutter(columns, minGap, out var xGap);
        var ySplit = FindGutter(rows, minGap, out var yGap);
        if (xSplit < 0 && ySplit < 0) return false;
        if (xSplit >= 0 && (ySplit < 0 || (long)xGap * region.Height >= (long)yGap * region.Width)) {
            first = new ImageRegion(region.X, region.Y, xSplit, region.Height);
            second = new ImageRegion(region.X + xSplit, region.Y, region.Width - xSplit, region.Height);
        } else {
            first = new ImageRegion(region.X, region.Y, region.Width, ySplit);
            second = new ImageRegion(region.X, region.Y + ySplit, region.Width, region.Height - ySplit);
        }
        return true;
    }

    // Ignore outer margins: both sides of a split must contain foreground.
    private static int FindGutter(bool[] occupied, int minimum, out int gap) {
        gap = 0;
        var split = -1;
        var previous = -1;
        for (var i = 0; i < occupied.Length; i++) {
            if (!occupied[i]) continue;
            var length = i - previous - 1;
            var middle = previous + 1 + length / 2;
            if (previous >= 0 && length >= minimum && length > gap && middle >= 24 && occupied.Length - middle >= 24) {
                gap = length;
                split = middle;
            }
            previous = i;
        }
        return split;
    }
}
