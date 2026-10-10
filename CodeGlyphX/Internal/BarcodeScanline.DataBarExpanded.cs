using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.DataBar;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    // Fit each 17-module character and 15-module finder independently. A photo
    // can have a varying pitch even when each character remains readable.
    private static void TryCollectExpandedRunCandidates(List<int> runs, bool firstIsBar, CancellationToken token,
        List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        for (var direction = 0; direction < 2; direction++) {
            var source = direction == 0 ? runs : new List<int>(runs);
            var first = firstIsBar;
            if (direction != 0) { source.Reverse(); first = firstIsBar == ((runs.Count & 1) != 0); }
            for (var start = 0; start + 46 <= source.Count; start++) {
                if (DecodeBudget.ShouldAbort(token)) return;
                // First finder is always A, following the two guards and check character.
                var firstFinder = new int[5];
                DataBarExpandedSpecification.CopyFinderPattern(0, false, firstFinder, 0);
                if (!ExpandedFinderMatches(source, start + 10, firstFinder)) continue;
                for (var chars = 4; chars <= 22; chars++) {
                    if (DecodeBudget.ShouldAbort(token)) return;
                    var count = 4 + chars * 8 + ((chars + 1) / 2) * 5;
                    if (start + count > source.Count) break;
                    var widths = new List<int>(count) { 1, 1 };
                    var at = start + 2;
                    var fit = 0.0;
                    var valid = true;
                    var sequence = DataBarExpandedSpecification.GetFinderSequence(chars);
                    for (var block = 0; block < (chars + 1) / 2; block++) {
                        if (!NormalizeExpandedCharacter(source, at, widths, ref fit)) { valid = false; break; }
                        at += 8;
                        var finder = new int[5];
                        DataBarExpandedSpecification.CopyFinderPattern(sequence[block], (block & 1) != 0, finder, 0);
                        if (!ExpandedFinderMatches(source, at, finder)) { valid = false; break; }
                        widths.AddRange(finder); at += 5;
                        if (block * 2 + 1 >= chars) continue;
                        if (!NormalizeExpandedCharacter(source, at, widths, ref fit)) { valid = false; break; }
                        at += 8;
                    }
                    if (!valid) continue;
                    var pitch = (source[start + 10] + source[start + 11] + source[start + 12] + source[start + 13] + source[start + 14]) / 15.0;
                    // Light boundary guards can merge with quiet space; dark guards cannot.
                    if (Math.Abs(source[start + 1] / pitch - 1) > 0.85) continue;
                    var endPitch = (source[at - 8] + source[at - 7] + source[at - 6] + source[at - 5] + source[at - 4] + source[at - 3] + source[at - 2] + source[at - 1]) / 17.0;
                    var darkGuard = ((count & 1) == 0) ? at + 1 : at;
                    if (Math.Abs(source[darkGuard] / endPitch - 1) > 0.85) continue;
                    widths.Add(1); widths.Add(1);
                    AddRunCandidate(widths, first == ((start & 1) == 0), fit, candidates, position, isVertical);
                }
            }
        }
    }

    private static bool ExpandedFinderMatches(List<int> runs, int start, int[] pattern) {
        if (start + 5 > runs.Count) return false;
        var total = 0;
        for (var i = 0; i < 5; i++) total += runs[start + i];
        var pitch = total / 15.0;
        var error = 0.0;
        for (var i = 0; i < 5; i++) {
            var residual = runs[start + i] / pitch - pattern[i];
            if (Math.Abs(residual) > 0.85) return false;
            error += residual * residual;
        }
        return error < 1.2;
    }

    private static bool NormalizeExpandedCharacter(List<int> runs, int start, List<int> widths, ref double fit) {
        if (start + 8 > runs.Count) return false;
        var total = 0;
        for (var i = 0; i < 8; i++) total += runs[start + i];
        var pitch = total / 17.0;
        var normalized = new int[8];
        var sum = 0;
        for (var i = 0; i < 8; i++) { normalized[i] = Math.Max(1, (int)Math.Round(runs[start + i] / pitch)); sum += normalized[i]; }
        // Correct pixel-rounding error using the element nearest the missing boundary.
        while (sum != 17) {
            var adjust = sum < 17 ? 1 : -1;
            var best = -1;
            var bestError = double.PositiveInfinity;
            for (var i = 0; i < 8; i++) {
                if (normalized[i] + adjust < 1 || normalized[i] + adjust > 8) continue;
                var before = normalized[i] - runs[start + i] / pitch;
                var error = (before + adjust) * (before + adjust) - before * before;
                if (error < bestError) { bestError = error; best = i; }
            }
            if (best < 0) return false;
            normalized[best] += adjust; sum += adjust;
        }
        for (var i = 0; i < 8; i++) {
            var residual = normalized[i] - runs[start + i] / pitch;
            if (Math.Abs(residual) > 0.85) return false;
            fit += residual * residual;
        }
        widths.AddRange(normalized);
        return true;
    }
}
