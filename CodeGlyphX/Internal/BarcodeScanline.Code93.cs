using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Code93;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    private static readonly (char Symbol, int[] Widths)[] Code93RunPatterns = Code93Tables.EncodingTable
        .OrderBy(entry => entry.Value.value)
        .Select(entry => {
            var widths = new List<int>(6);
            var bar = true;
            var length = 0;
            for (var bit = 8; bit >= 0; bit--) {
                var next = (entry.Value.data & (1u << bit)) != 0;
                if (next == bar) length++;
                else { widths.Add(length); bar = next; length = 1; }
            }
            widths.Add(length);
            return (entry.Key, widths.ToArray());
        }).ToArray();

    /// <summary>
    /// Locates complete Code93 symbols inside a physical scanline, independently
    /// of surrounding borders or text. Only this extra localization requires C/K
    /// checksums; complete checksum-free module decoding retains its existing policy.
    /// </summary>
    private static void TryCollectCode93RunCandidates(List<int> runs, bool firstIsBar,
        CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        const int asterisk = 47;
        for (var direction = 0; direction < 2; direction++) {
            var reverse = direction != 0;
            for (var start = 0; start + 25 <= runs.Count; start++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                var startPitch = Code93RunTotal(runs, start, reverse) / 9.0;
                if (start > 0 && Code93Run(runs, start - 1, reverse) < startPitch * 3) continue;
                if (MatchCode93Run(runs, start, reverse, asterisk, asterisk, out var fit) != asterisk) continue;
                var widths = new List<int>(Code93RunPatterns[asterisk].Widths);
                var symbols = new List<char>();
                var at = start + 6;
                // Match the existing bounded linear-symbol search, checking cancellation
                // between symbols. Each pattern search has at most 47 six-run patterns.
                for (var count = 0; count < 256 && at + 7 <= runs.Count; count++) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                    if (MatchCode93Run(runs, at, reverse, asterisk, asterisk, out var stopFit) == asterisk) {
                        var pitch = Code93RunTotal(runs, at, reverse) / 9.0;
                        var termination = Code93Run(runs, at + 6, reverse);
                        if (Math.Abs(termination - pitch) > pitch * 0.6 + 0.5 ||
                            (at + 7 < runs.Count && Code93Run(runs, at + 7, reverse) < pitch * 3) ||
                            !Code93Tables.HasChecksums(new string(symbols.ToArray()).AsSpan())) break;
                        widths.AddRange(Code93RunPatterns[asterisk].Widths);
                        widths.Add(1);
                        var physicalStart = reverse ? runs.Count - 1 - start : start;
                        AddRunCandidate(widths, firstIsBar == ((physicalStart & 1) == 0), fit + stopFit,
                            candidates, position, isVertical);
                        break;
                    }
                    var symbol = MatchCode93Run(runs, at, reverse, 0, asterisk - 1, out var symbolFit);
                    if (symbol < 0) break;
                    widths.AddRange(Code93RunPatterns[symbol].Widths);
                    symbols.Add(Code93RunPatterns[symbol].Symbol);
                    fit += symbolFit;
                    at += 6;
                }
            }
        }
    }

    private static int MatchCode93Run(List<int> runs, int start, bool reverse, int first, int last, out double error) {
        var best = -1;
        error = double.PositiveInfinity;
        var pitch = Code93RunTotal(runs, start, reverse) / 9.0;
        for (var symbol = first; symbol <= last; symbol++) {
            var pattern = Code93RunPatterns[symbol].Widths;
            var fit = 0.0;
            var valid = true;
            for (var i = 0; i < 6; i++) {
                var residual = Math.Abs(Code93Run(runs, start + i, reverse) - pattern[i] * pitch);
                if (residual > pitch * 0.6 + 0.5) { valid = false; break; }
                fit += residual * residual / (pitch * pitch);
            }
            if (valid && fit < error && fit < 6 * 0.22) { best = symbol; error = fit; }
        }
        return best;
    }

    private static double Code93RunTotal(List<int> runs, int start, bool reverse) {
        var total = 0.0;
        for (var i = 0; i < 6; i++) total += Code93Run(runs, start + i, reverse);
        return total;
    }

    private static int Code93Run(List<int> runs, int at, bool reverse) => runs[reverse ? runs.Count - 1 - at : at];
}
