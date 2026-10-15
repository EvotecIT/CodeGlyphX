using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    /// <summary>
    /// Locates complete ITF symbols before rounding bars and spaces to one pitch.
    /// Two wide elements per digit and a common print-gain model constrain the
    /// fit; the decoder still owns digit interpretation and ITF-14 checksums.
    /// </summary>
    private static void TryCollectItfRunCandidates(ReadOnlySpan<int> runs, bool firstIsBar,
        CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        for (var direction = 0; direction < 2; direction++) {
            var reverse = direction != 0;
            for (var start = 1; start + 17 < runs.Length; start++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                var bar = (ItfRun(runs, start, reverse) + (double)ItfRun(runs, start + 2, reverse)) / 2;
                var space = (ItfRun(runs, start + 1, reverse) + (double)ItfRun(runs, start + 3, reverse)) / 2;
                var pitch = (bar + space) / 2;
                var tolerance = pitch * 0.65 + 0.5;
                if (Math.Abs(bar - space) > pitch * 1.6 ||
                    Math.Abs(ItfRun(runs, start, reverse) - bar) > tolerance ||
                    Math.Abs(ItfRun(runs, start + 1, reverse) - space) > tolerance ||
                    !HasItfQuietZone(runs, start - 1, reverse, pitch)) continue;
                var widths = new List<int>(64) { 1, 1, 1, 1 };
                for (var pairs = 0; pairs <= 128; pairs++) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                    var at = start + widths.Count;
                    if (at + 3 >= runs.Length) break;
                    if (pairs > 0 && ItfRun(runs, at, reverse) > bar &&
                        HasItfQuietZone(runs, at + 3, reverse, pitch)) {
                        widths.Add(2); widths.Add(1); widths.Add(1);
                        if (TryValidateItfRuns(runs, start, reverse, widths, cancellationToken, out var fit)) {
                            var physicalStart = reverse ? runs.Length - 1 - start : start;
                            AddRunCandidate(widths, firstIsBar == ((physicalStart & 1) == 0), fit, candidates, position, isVertical);
                            break;
                        }
                        widths.RemoveRange(widths.Count - 3, 3);
                    }
                    if (pairs == 128 || at + 10 >= runs.Length ||
                        !TryClassifyItfDigit(runs, at, reverse, out var bars) ||
                        !TryClassifyItfDigit(runs, at + 1, reverse, out var spaces)) break;
                    for (var element = 0; element < 5; element++) {
                        widths.Add((bars & (1 << element)) == 0 ? 1 : 2);
                        widths.Add((spaces & (1 << element)) == 0 ? 1 : 2);
                    }
                }
            }
        }
    }

    private static bool TryClassifyItfDigit(ReadOnlySpan<int> runs, int start, bool reverse, out int wideMask) {
        wideMask = 0;
        Span<int> ordered = stackalloc int[5];
        for (var i = 0; i < 5; i++) {
            var width = ItfRun(runs, start + i * 2, reverse);
            var at = i;
            while (at > 0 && ordered[at - 1] > width) { ordered[at] = ordered[at - 1]; at--; }
            ordered[at] = width;
        }
        if (ordered[2] >= ordered[3]) return false;
        for (var i = 0; i < 5; i++)
            if (ItfRun(runs, start + i * 2, reverse) >= ordered[3]) wideMask |= 1 << i;
        return true;
    }

    private static bool TryValidateItfRuns(ReadOnlySpan<int> runs, int start, bool reverse, List<int> widths,
        CancellationToken cancellationToken, out double fit) {
        fit = 0;
        Span<double> sums = stackalloc double[4];
        Span<int> counts = stackalloc int[4];
        Span<int> minima = stackalloc int[4];
        Span<int> maxima = stackalloc int[4];
        sums.Clear(); counts.Clear(); maxima.Clear(); minima.Fill(int.MaxValue);
        for (var element = 0; element < widths.Count; element++) {
            if ((element & 31) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var group = (element & 1) * 2 + widths[element] - 1;
            var width = ItfRun(runs, start + element, reverse);
            sums[group] += width; counts[group]++;
            minima[group] = Math.Min(minima[group], width); maxima[group] = Math.Max(maxima[group], width);
        }
        if (!TryFitTwoWidthRunModel(sums, counts, minima, maxima, out var pitch, out var tolerance) ||
            !HasItfQuietZone(runs, start - 1, reverse, pitch) ||
            !HasItfQuietZone(runs, start + widths.Count, reverse, pitch)) return false;
        for (var element = 0; element < widths.Count; element++) {
            if ((element & 31) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var group = (element & 1) * 2 + widths[element] - 1;
            var residual = ItfRun(runs, start + element, reverse) - sums[group];
            if (Math.Abs(residual) > tolerance) return false;
            fit += residual * residual;
        }
        return true;
    }

    private static bool HasItfQuietZone(ReadOnlySpan<int> runs, int offset, bool reverse, double pitch) =>
        ItfRun(runs, offset, reverse) >= pitch * 10 ||
        (offset == 0 || offset == runs.Length - 1) && ItfRun(runs, offset, reverse) >= pitch;

    private static int ItfRun(ReadOnlySpan<int> runs, int offset, bool reverse) =>
        runs[reverse ? runs.Length - 1 - offset : offset];
}
