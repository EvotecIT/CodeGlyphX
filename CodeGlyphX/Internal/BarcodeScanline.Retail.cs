using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Ean;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    private static readonly int[][] RetailDigitRuns = BuildRetailDigitRuns();

    // Photographs contain text, borders, add-ons and varying bar pitch. Locate guarded
    // retail symbols inside a scanline and estimate pitch independently for each digit.
    // The existing decoder remains responsible for parity and checksum validation.
    private static void TryCollectRetailRunCandidates(ReadOnlySpan<byte> luminance, int threshold,
        CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        if (luminance.Length < 51) return;
        var runs = new List<int>();
        var firstIsBar = luminance[0] < threshold;
        var current = firstIsBar;
        var length = 1;
        for (var i = 1; i < luminance.Length; i++) {
            if ((i & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return;
            var bar = luminance[i] < threshold;
            if (bar == current) length++;
            else { runs.Add(length); current = bar; length = 1; }
        }
        runs.Add(length);
        TryCollectCode128RunCandidates(runs, firstIsBar, cancellationToken, candidates, position, isVertical);
        TryCollectCode39RunCandidates(runs, firstIsBar, cancellationToken, candidates, position, isVertical);
        for (var start = 0; start + 33 <= runs.Count; start++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return;
            // Also locate inverted symbols; the normal transform path resolves polarity.
            var pitch = (runs[start] + runs[start + 1] + runs[start + 2]) / 3.0;
            if (!RetailGuardMatches(runs, start, 3, pitch) || (start > 0 && runs[start - 1] < pitch * 3)) continue;
            Collect(6, 6, false);
            Collect(4, 4, false);
            Collect(6, 0, true);

            void Collect(int leftDigits, int rightDigits, bool upce) {
                var count = 3 + leftDigits * 4 + (upce ? 6 : 5 + rightDigits * 4 + 3);
                var end = start + count;
                if (end > runs.Count) return;
                var lastGuard = upce ? 6 : 3;
                var endPitch = 0.0;
                for (var i = end - lastGuard; i < end; i++) endPitch += runs[i];
                endPitch /= lastGuard;
                if (!RetailGuardMatches(runs, end - lastGuard, lastGuard, endPitch) ||
                    (end < runs.Count && runs[end] < endPitch * 3)) return;
                var widths = new List<int>(count);
                widths.AddRange(new[] { 1, 1, 1 });
                var at = start + 3;
                var fit = 0.0;
                for (var digit = 0; digit < leftDigits; digit++, at += 4) {
                    if (!TryNormalizeRetailDigit(runs, at, out var pattern, out var error)) return;
                    widths.AddRange(pattern); fit += error;
                }
                if (!upce) {
                    var centerPitch = 0.0;
                    for (var i = at; i < at + 5; i++) centerPitch += runs[i];
                    centerPitch /= 5;
                    if (!RetailGuardMatches(runs, at, 5, centerPitch)) return;
                    widths.AddRange(new[] { 1, 1, 1, 1, 1 }); at += 5;
                    for (var digit = 0; digit < rightDigits; digit++, at += 4) {
                        if (!TryNormalizeRetailDigit(runs, at, out var pattern, out var error)) return;
                        widths.AddRange(pattern); fit += error;
                    }
                }
                for (var i = 0; i < lastGuard; i++) widths.Add(1);
                var moduleCount = upce ? 51 : leftDigits == 6 ? 95 : 67;
                var modules = new bool[moduleCount];
                var offset = 0;
                var bar = firstIsBar == ((start & 1) == 0);
                foreach (var width in widths) {
                    for (var i = 0; i < width; i++) modules[offset++] = bar;
                    bar = !bar;
                }
                AddUniqueLocatedCandidate(candidates, new BarcodeScanlineCandidate(modules, position, isVertical, fitError: fit));
            }
        }
    }

    private static bool RetailGuardMatches(List<int> runs, int start, int count, double pitch) {
        for (var i = 0; i < count; i++) if (Math.Abs(runs[start + i] - pitch) > pitch * 0.5 + 0.5) return false;
        return true;
    }

    private static bool TryNormalizeRetailDigit(List<int> runs, int start, out int[] pattern, out double fit) {
        pattern = Array.Empty<int>(); fit = double.PositiveInfinity;
        var total = runs[start] + runs[start + 1] + runs[start + 2] + runs[start + 3];
        var pitch = total / 7.0;
        foreach (var candidate in RetailDigitRuns) {
            var error = 0.0;
            var valid = true;
            for (var i = 0; i < 4; i++) {
                var residual = Math.Abs(runs[start + i] - candidate[i] * pitch);
                if (residual > pitch * 0.7 + 0.5) { valid = false; break; }
                error += residual * residual;
            }
            if (valid && error < fit) { pattern = candidate; fit = error; }
        }
        return pattern.Length != 0 && fit / (pitch * pitch) < 1.0;
    }

    private static int[][] BuildRetailDigitRuns() {
        var patterns = new List<int[]>();
        foreach (var digit in EanTables.EncodingTable.Values) {
            Add(digit.LeftOdd); Add(digit.LeftEven);
        }
        return patterns.ToArray();
        void Add(bool[] modules) {
            var runs = new List<int>(); var current = modules[0]; var length = 1;
            for (var i = 1; i < modules.Length; i++) {
                if (modules[i] == current) length++;
                else { runs.Add(length); current = modules[i]; length = 1; }
            }
            runs.Add(length); patterns.Add(runs.ToArray());
        }
    }
}
