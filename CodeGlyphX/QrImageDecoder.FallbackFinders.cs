using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX;

public static partial class QrImageDecoder {
    // The legacy global dark rectangle includes sticker text and borders. Locate
    // an axis-aligned symbol from its actual 1:1:3:1:1 finder strokes instead.
    // This is image localization, with no encoded-symbol metadata or clean-image substitution.
    private static bool TryDecodeFinderRegion(byte[] gray, int width, int height, byte threshold,
        QrPixelDecodeOptions? options, CancellationToken token, out QrDecoded decoded, out QrPixelDecodeInfo info) {
        decoded = null!; info = default;
        var finders = new List<FallbackFinder>();
        var runs = new int[5];
        for (var y = 0; y < height; y += Math.Max(1, height / 256)) {
            ThrowIfFallbackAborted(token);
            Array.Clear(runs, 0, runs.Length);
            var count = 0;
            var lastDark = false;
            for (var x = 0; x <= width; x++) {
                if ((x & 1023) == 0) ThrowIfFallbackAborted(token);
                var dark = x < width && gray[y * width + x] < threshold;
                if (dark == lastDark && x < width) { runs[count]++; continue; }
                if (count == 4) {
                    if (lastDark && FinderRatio(runs, out var module)) {
                        var center = x - runs[4] - runs[3] - runs[2] / 2.0;
                        if (FinderVertical(gray, width, height, (int)center, y, threshold, module, out var cy, out var verticalModule))
                            AddFinder(finders, center, cy, verticalModule);
                    }
                    for (var i = 0; i < 4; i++) runs[i] = runs[i + 1];
                    runs[4] = 1;
                } else { count++; runs[count] = 1; }
                lastDark = dark;
            }
        }
        var attempts = 0;
        foreach (var topLeft in finders) foreach (var topRight in finders) {
            if (topLeft.Hits < 2 || topRight.Hits < 2) continue;
            var unit = (topLeft.Module + topRight.Module) / 2;
            var dx = topRight.X - topLeft.X;
            if (dx < unit * 12 || Math.Abs(topLeft.Y - topRight.Y) > unit) continue;
            foreach (var bottomLeft in finders) {
                ThrowIfFallbackAborted(token);
                var dy = bottomLeft.Y - topLeft.Y;
                if (bottomLeft.Hits < 2 || dy < unit * 12 || Math.Abs(bottomLeft.X - topLeft.X) > unit
                    || Math.Abs(dx - dy) > unit * 2 || Math.Abs(bottomLeft.Module - unit) > unit * .5) continue;
                if (++attempts > 128) return false;
                var dimension = 17 + 4 * (int)Math.Round(((dx + dy) / (2 * unit) + 7 - 17) / 4);
                if (dimension < 21 || dimension > 177) continue;
                var stepX = dx / (dimension - 7);
                var stepY = dy / (dimension - 7);
                var left = topLeft.X - 3.5 * stepX;
                var top = topLeft.Y - 3.5 * stepY;
                if (left < -1 || top < -1 || left + dimension * stepX > width + 1 || top + dimension * stepY > height + 1) continue;
                var modules = new BitMatrix(dimension, dimension);
                for (var my = 0; my < dimension; my++) {
                    ThrowIfFallbackAborted(token);
                    for (var mx = 0; mx < dimension; mx++)
                        modules[mx, my] = SampleModuleMajority(gray, width, height,
                            left + (mx + .5) * stepX, top + (my + .5) * stepY, threshold, stepX * .1, stepY * .1);
                }
                if (!TryDecodeModules(modules, options, token, out decoded, out var moduleInfo)) continue;
                info = new QrPixelDecodeInfo(1, threshold, false, finders.Count, attempts, dimension, moduleInfo);
                return true;
            }
        }
        return false;
    }

    private static bool FinderRatio(int[] runs, out double module) {
        module = (runs[0] + runs[1] + runs[2] + runs[3] + runs[4]) / 7.0;
        if (module < 1) return false;
        for (var i = 0; i < 5; i++)
            if (runs[i] == 0 || Math.Abs(runs[i] - module * (i == 2 ? 3 : 1)) > module * .7) return false;
        return true;
    }

    private static bool FinderVertical(byte[] gray, int width, int height, int x, int y, byte threshold,
        double module, out double centerY, out double verticalModule) {
        centerY = 0;
        verticalModule = 0;
        if ((uint)x >= (uint)width) return false;
        var limit = Math.Max(2, (int)Math.Ceiling(module * 5));
        var upper = y;
        while (upper >= 0 && y - upper <= limit && gray[upper * width + x] < threshold) upper--;
        var lower = y;
        while (lower < height && lower - y <= limit && gray[lower * width + x] < threshold) lower++;
        var counts = new int[5];
        counts[2] = lower - upper - 1;
        for (var i = 1; i >= 0; i--) {
            var start = upper;
            while (upper >= 0 && start - upper <= limit && (gray[upper * width + x] < threshold) == (i == 0)) upper--;
            counts[i] = start - upper;
        }
        for (var i = 3; i <= 4; i++) {
            var start = lower;
            while (lower < height && lower - start <= limit && (gray[lower * width + x] < threshold) == (i == 4)) lower++;
            counts[i] = lower - start;
        }
        if (!FinderRatio(counts, out verticalModule) || Math.Abs(module - verticalModule) > module * .5) return false;
        centerY = upper + counts[0] + counts[1] + counts[2] / 2.0 + 1;
        return true;
    }

    private static void AddFinder(List<FallbackFinder> finders, double x, double y, double module) {
        for (var i = 0; i < finders.Count; i++) {
            var candidate = finders[i];
            if (Math.Abs(candidate.X - x) > module * 1.5 || Math.Abs(candidate.Y - y) > module * 1.5) continue;
            var hits = candidate.Hits + 1;
            finders[i] = new FallbackFinder((candidate.X * candidate.Hits + x) / hits,
                (candidate.Y * candidate.Hits + y) / hits, (candidate.Module * candidate.Hits + module) / hits, hits);
            return;
        }
        if (finders.Count < 48) finders.Add(new FallbackFinder(x, y, module, 1));
    }

    private readonly struct FallbackFinder {
        internal readonly double X, Y, Module;
        internal readonly int Hits;
        internal FallbackFinder(double x, double y, double module, int hits) { X = x; Y = y; Module = module; Hits = hits; }
    }
}
