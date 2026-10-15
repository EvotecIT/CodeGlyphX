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

    /// <summary>Locates complete Code 128 start/stop pairs in both physical directions without changing the sampled runs.</summary>
    private static bool TryCollectCode128RunCandidates(List<int> runs, bool firstIsBar, CancellationToken cancellationToken,
        List<BarcodeScanlineCandidate> candidates, int position, bool isVertical, bool inspectFraming = false) {
        var hasFrame = false;
        var budget = DecodeBudget.Capture();
        for (var direction = 0; direction < 2; direction++) {
            var reverse = direction != 0;
            for (var start = 0; start + 25 <= runs.Count; start++) {
                if (budget.ShouldAbort(cancellationToken)) return false;
                var code = MatchCode128(runs, start, 103, 105, out var error, reverse);
                if (code < 0) continue;
                var pitch = SumCode128Runs(runs, start, 6, reverse) / 11.0;
                if (start > 0 && Code128Run(runs, start - 1, reverse) < pitch * 3) continue;
                if (inspectFraming) hasFrame |= HasCode128StopFrame(runs, start, pitch, reverse, cancellationToken, budget);
                var widths = new List<int>(Code128RunPatterns[code]);
                var at = start + 6;
                for (var count = 0; count < 256 && at + 7 <= runs.Count; count++) {
                    if (budget.ShouldAbort(cancellationToken)) return false;
                    if (MatchCode128(runs, at, 106, 106, out var stopError, reverse) == 106 && count >= 2) {
                        var endPitch = SumCode128Runs(runs, at, 7, reverse) / 13.0;
                        if (at + 7 == runs.Count || Code128Run(runs, at + 7, reverse) >= endPitch * 3) {
                            widths.AddRange(Code128RunPatterns[106]);
                            var physicalStart = reverse ? runs.Count - 1 - start : start;
                            AddRunCandidate(widths, firstIsBar == ((physicalStart & 1) == 0), error + stopError, candidates, position, isVertical);
                            hasFrame = true;
                            break;
                        }
                    }
                    code = MatchCode128(runs, at, 0, 102, out var symbolError, reverse);
                    if (code < 0) break;
                    widths.AddRange(Code128RunPatterns[code]); error += symbolError; at += 6;
                }
            }
        }
        return hasFrame;
    }

    /// <summary>Reports complete framing even when a data character at this anchor needs a clearer neighboring row.</summary>
    private static bool HasCode128StopFrame(List<int> runs, int start, double startPitch, bool reverse, CancellationToken token, DecodeBudgetState budget) {
        for (var end = start + 18; end + 7 <= runs.Count && end - start <= 6 * 256; end += 2) {
            if (budget.ShouldAbort(token)) return false;
            if (MatchCode128(runs, end, 106, 106, out _, reverse) != 106) continue;
            var pitch = SumCode128Runs(runs, end, 7, reverse) / 13.0;
            if (pitch < startPitch * 0.5 || pitch > startPitch * 2) continue;
            if (end + 7 == runs.Count || Code128Run(runs, end + 7, reverse) >= pitch * 3) return true;
        }
        return false;
    }

    private static int MatchCode128(List<int> runs, int start, int first, int last, out double error, bool reverse = false) {
        var best = -1; error = double.PositiveInfinity;
        var previousLength = 0;
        var pitch = 0.0;
        for (var code = first; code <= last; code++) {
            var pattern = Code128RunPatterns[code];
            if (start + pattern.Length > runs.Count) continue;
            // All data/start patterns have six runs; the stop has seven.
            // Their observed widths and pitch do not depend on the code value.
            if (pattern.Length != previousLength) {
                var total = SumCode128Runs(runs, start, pattern.Length, reverse);
                pitch = total / (code == 106 ? 13.0 : 11.0);
                previousLength = pattern.Length;
            }
            var fit = 0.0; var valid = true;
            for (var i = 0; i < pattern.Length; i++) {
                var residual = Math.Abs(Code128Run(runs, start + i, reverse) - pattern[i] * pitch);
                if (residual > pitch * 0.6 + 0.5) { valid = false; break; }
                fit += residual * residual / (pitch * pitch);
            }
            if (valid && fit < error && fit < pattern.Length * 0.22) { best = code; error = fit; }
        }
        return best;
    }

    private static int Code128Run(List<int> runs, int at, bool reverse) => runs[reverse ? runs.Count - 1 - at : at];

    private static int SumCode128Runs(List<int> runs, int start, int count, bool reverse) {
        var sum = 0;
        for (var i = 0; i < count; i++) sum += Code128Run(runs, start + i, reverse);
        return sum;
    }

    private static void AddRunCandidate(List<int> widths, bool firstIsBar, double fit,
        List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        var modules = new bool[widths.Sum()]; var offset = 0; var bar = firstIsBar;
        foreach (var width in widths) { for (var i = 0; i < width; i++) modules[offset++] = bar; bar = !bar; }
        AddUniqueLocatedCandidate(candidates, new BarcodeScanlineCandidate(modules, position, isVertical, fitError: fit));
    }
}
