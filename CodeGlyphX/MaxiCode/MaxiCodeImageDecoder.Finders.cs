using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.MaxiCode;

internal static partial class MaxiCodeImageDecoder {
    private static List<Finder> FindFinders(GrayImage image, CancellationToken token, DecodeBudgetState budget) {
        var result = new List<Finder>();
        var runs = new int[11];
        var ordered = new int[11];
        for (var y = 0; y < image.Height; y++) {
            if (Aborted(token, budget)) return result;
            var count = 0;
            var next = 0;
            var start = 0;
            var color = image.Dark(0, y);
            for (var x = 1; x <= image.Width; x++) {
                if ((x & 1023) == 0 && Aborted(token, budget)) return result;
                if (x < image.Width && image.Dark(x, y) == color) continue;
                runs[next] = x - start;
                next = (next + 1) % 11;
                count++;
                if (count >= 11) {
                    // Cheap symmetry rejects noise before ordering or measuring the two-dimensional finder.
                    var first = runs[next];
                    var last = runs[(next + 10) % 11];
                    if (Math.Abs(first - last) <= Math.Max(1, (first + last) / 5)) {
                        for (var i = 0; i < 11; i++) ordered[i] = runs[(next + i) % 11];
                        if (RingRuns(ordered, out var diameter)) {
                            double center = x - diameter - 0.5;
                            for (var i = 0; i < 5; i++) center += ordered[i];
                            center += ordered[5] / 2.0;
                            if (TryMeasureFinder(image, (int)Math.Floor(center + 0.5), y, diameter, token, budget, out var finder)) {
                                var duplicate = false;
                                foreach (var previous in result) {
                                    if (Math.Abs(previous.X - finder.X) < finder.DiameterX / 4 &&
                                        Math.Abs(previous.Y - finder.Y) < finder.DiameterY / 4) { duplicate = true; break; }
                                }
                                if (!duplicate) {
                                    result.Add(finder);
                                    if (result.Count == MaximumFinders) return result;
                                }
                            }
                        }
                    }
                }
                start = x;
                color = !color;
            }
        }
        return result;
    }

    private static bool RingRuns(int[] runs, out int diameter) {
        diameter = 0;
        for (var i = 0; i < 11; i++) diameter += runs[i];
        if (diameter < 12) return false;
        // Rasterizers differ in the central white disc size. Measure it independently of the ten
        // surrounding bands, then require symmetry and verify circular transitions in two dimensions.
        var darkBand = (runs[0] + runs[2] + runs[4] + runs[6] + runs[8] + runs[10]) / 6.0;
        var lightBand = (runs[1] + runs[3] + runs[7] + runs[9]) / 4.0;
        // Ink spread can widen the dark bands at the expense of the intervening white bands.
        if (darkBand > lightBand * 2.5 || lightBand > darkBand * 2.5 ||
            runs[5] < Math.Min(darkBand, lightBand) || runs[5] > Math.Max(darkBand, lightBand) * 3.6) return false;
        for (var i = 0; i < 11; i++) {
            if (i == 5) continue;
            var expected = (i & 1) == 0 ? darkBand : lightBand;
            if (Math.Abs(runs[i] - expected) > Math.Max(1, expected * 0.32)) return false;
        }
        return true;
    }

    private static bool TryMeasureFinder(GrayImage image, int x, int y, int estimate,
        CancellationToken token, DecodeBudgetState budget, out Finder finder) {
        finder = default;
        if (!MeasureAxis(image, x, y, true, estimate, token, budget, out var centerY, out var diameterY, out _)) return false;
        y = (int)Math.Floor(centerY + 0.5);
        if (!MeasureAxis(image, x, y, false, estimate, token, budget, out var centerX, out var diameterX, out var discX)) return false;
        x = (int)Math.Floor(centerX + 0.5);
        if (!MeasureAxis(image, x, y, true, estimate, token, budget, out centerY, out diameterY, out var discY)) return false;
        if (diameterX > diameterY * 2 || diameterY > diameterX * 2) return false;
        var inverted = image.Dark(x, y);
        // A circular/elliptical finder must agree away from its axes too. Square Aztec rings and
        // repetitive linear bars cannot enter the relatively expensive orientation search.
        var disc = (discX / (double)diameterX + discY / (double)diameterY) / 2;
        var matches = 0;
        for (var angle = 0; angle < 16; angle++) {
            var radians = angle * Math.PI / 8;
            for (var ring = 0; ring < 6; ring++) {
                var radius = ring == 0 ? disc / 2 : disc + (ring - 0.5) * (1 - disc) / 5;
                var px = (int)Math.Floor(centerX + Math.Cos(radians) * diameterX / 2 * radius + 0.5);
                var py = (int)Math.Floor(centerY + Math.Sin(radians) * diameterY / 2 * radius + 0.5);
                if ((uint)px >= (uint)image.Width || (uint)py >= (uint)image.Height) return false;
                if ((image.Dark(px, py) != inverted) == ((ring & 1) != 0)) matches++;
            }
        }
        if (matches < 88) return false;
        finder = new Finder(centerX, centerY, diameterX, diameterY, inverted);
        return true;
    }

    private static bool MeasureAxis(GrayImage image, int x, int y, bool vertical, int estimate,
        CancellationToken token, DecodeBudgetState budget, out double center, out int diameter, out int disc) {
        center = 0;
        diameter = 0;
        disc = 0;
        var position = vertical ? y : x;
        var limit = vertical ? image.Height : image.Width;
        bool Dark(int at) => vertical ? image.Dark(x, at) : image.Dark(at, y);
        var color = Dark(position);
        var low = position;
        var high = position;
        bool Expand(bool lower) {
            while (lower ? low > 0 && Dark(low - 1) == color : high + 1 < limit && Dark(high + 1) == color) {
                if (high - low > estimate * 2L || Aborted(token, budget)) return false;
                if (lower) low--; else high++;
            }
            return true;
        }
        if (!Expand(true) || !Expand(false)) return false;
        center = (low + high) / 2.0;
        var runs = new int[11];
        runs[5] = high - low + 1;
        disc = runs[5];
        for (var i = 1; i <= 5; i++) {
            color = !color;
            var oldLow = low;
            var oldHigh = high;
            if (!Expand(true) || !Expand(false)) return false;
            if (i == 5) {
                // A neighbouring hexagon can touch the outside black ring. Its width is recoverable
                // from the two inner black rings, rather than the extent of the attached data module.
                var band = (runs[2] + runs[4] + runs[6] + runs[8]) / 4.0;
                var expected = (int)Math.Floor(band + 0.5);
                if (oldLow - low > band * 1.5) low = oldLow - expected;
                if (high - oldHigh > band * 1.5) high = oldHigh + expected;
            }
            if (low == 0 || high + 1 == limit || low == oldLow || high == oldHigh) return false;
            runs[5 - i] = oldLow - low;
            runs[5 + i] = high - oldHigh;
        }
        return RingRuns(runs, out diameter);
    }
}
