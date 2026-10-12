using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Code128;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    private static readonly int[][] Code128RunPatterns = Enumerable.Range(0, 107).Select(code => {
        var packed = Code128Tables.GetPattern(code); var count = code == 106 ? 7 : 6; var widths = new int[count];
        for (var i = count - 1; i >= 0; i--) { widths[i] = (int)(packed & 15); packed >>= 4; }
        return widths;
    }).ToArray();

    private static void TryCollectCode128RunCandidates(List<int> runs, bool firstIsBar, CancellationToken cancellationToken,
        List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        for (var start = 0; start + 25 <= runs.Count; start++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return;
            var code = MatchCode128(runs, start, 103, 105, out var error);
            if (code < 0) continue;
            var pitch = runs.Skip(start).Take(6).Sum() / 11.0;
            if (start > 0 && runs[start - 1] < pitch * 3) continue;
            var widths = new List<int>(Code128RunPatterns[code]);
            var at = start + 6;
            for (var count = 0; count < 256 && at + 7 <= runs.Count; count++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                if (MatchCode128(runs, at, 106, 106, out var stopError) == 106 && count >= 2) {
                    var endPitch = runs.Skip(at).Take(7).Sum() / 13.0;
                    if (at + 7 == runs.Count || runs[at + 7] >= endPitch * 3) {
                        widths.AddRange(Code128RunPatterns[106]);
                        AddRunCandidate(widths, firstIsBar == ((start & 1) == 0), error + stopError, candidates, position, isVertical);
                        break;
                    }
                }
                code = MatchCode128(runs, at, 0, 102, out var symbolError);
                if (code < 0) break;
                widths.AddRange(Code128RunPatterns[code]); error += symbolError; at += 6;
            }
        }
    }

    private static int MatchCode128(List<int> runs, int start, int first, int last, out double error) {
        var best = -1; error = double.PositiveInfinity;
        for (var code = first; code <= last; code++) {
            var pattern = Code128RunPatterns[code];
            if (start + pattern.Length > runs.Count) continue;
            var total = 0;
            for (var i = 0; i < pattern.Length; i++) total += runs[start + i];
            var pitch = total / (code == 106 ? 13.0 : 11.0);
            var fit = 0.0; var valid = true;
            for (var i = 0; i < pattern.Length; i++) {
                var residual = Math.Abs(runs[start + i] - pattern[i] * pitch);
                if (residual > pitch * 0.6 + 0.5) { valid = false; break; }
                fit += residual * residual / (pitch * pitch);
            }
            if (valid && fit < error && fit < pattern.Length * 0.22) { best = code; error = fit; }
        }
        return best;
    }

    private static void AddRunCandidate(List<int> widths, bool firstIsBar, double fit,
        List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        var modules = new bool[widths.Sum()]; var offset = 0; var bar = firstIsBar;
        foreach (var width in widths) { for (var i = 0; i < width; i++) modules[offset++] = bar; bar = !bar; }
        AddUniqueLocatedCandidate(candidates, new BarcodeScanlineCandidate(modules, position, isVertical, fitError: fit));
    }
}
