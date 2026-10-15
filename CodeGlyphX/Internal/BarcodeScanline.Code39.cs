using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Code39;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    /// <summary>
    /// Locates complete Code 39 guard pairs in either scanline direction. Only a
    /// quiet run touching the image edge may be clipped; interior quiet zones and
    /// every character keep the existing width checks and decoder validation.
    /// </summary>
    private static void TryCollectCode39RunCandidates(List<int> runs, bool firstIsBar, CancellationToken cancellationToken,
        List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        for (var direction = 0; direction < 2; direction++) {
            var reverse = direction != 0;
            for (var start = 0; start + 29 <= runs.Count; start++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                if (!NormalizeCode39(runs, start, reverse, out var pattern, out var pitch) || !IsCode39Asterisk(pattern)) continue;
                // Zero or one preceding run reaches the physical image boundary.
                if (start > 1 && Code39Run(runs, start - 1, reverse) < pitch * 3) continue;
                var widths = new List<int>(pattern);
                var at = start + 9;
                for (var count = 0; count < 256 && at + 10 <= runs.Count; count++) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                    var gap = Code39Run(runs, at, reverse);
                    if (gap > pitch * 3 || gap < pitch * 0.4) break;
                    widths.Add(1); at++;
                    if (!NormalizeCode39(runs, at, reverse, out pattern, out pitch)) break;
                    widths.AddRange(pattern); at += 9;
                    if (IsCode39Asterisk(pattern)) {
                        if (count >= 1 && (at >= runs.Count - 1 || Code39Run(runs, at, reverse) >= pitch * 3)) {
                            var physicalStart = reverse ? runs.Count - 1 - start : start;
                            AddRunCandidate(widths, firstIsBar == ((physicalStart & 1) == 0), 0, candidates, position, isVertical);
                        }
                        break;
                    }
                }
            }
        }
    }

    internal static bool NormalizeCode39(List<int> runs, int start, bool reverse, out int[] pattern, out double pitch) {
        pattern = Array.Empty<int>(); pitch = 0;
        if (start + 9 > runs.Count) return false;
        // Nine widths have a fixed small bound. Keep rejected hypotheses on the
        // stack because both directions examine each possible guard position.
        Span<int> ordered = stackalloc int[9];
        for (var i = 0; i < ordered.Length; i++) {
            var width = Code39Run(runs, start + i, reverse);
            var at = i;
            while (at > 0 && ordered[at - 1] > width) {
                ordered[at] = ordered[at - 1]; at--;
            }
            ordered[at] = width;
        }
        var narrowTotal = 0;
        for (var i = 0; i < 6; i++) narrowTotal += ordered[i];
        pitch = narrowTotal / 6.0;
        var wide = (ordered[6] + ordered[7] + ordered[8]) / 3.0;
        if (wide < pitch * 1.5 || wide > pitch * 3.8 || ordered[6] <= ordered[5]) return false;
        for (var i = 0; i < 9; i++) {
            var width = Code39Run(runs, start + i, reverse);
            if (Math.Abs(width - (width >= ordered[6] ? wide : pitch)) > pitch * 0.65 + 0.5) return false;
        }
        // The split between the sixth and seventh sorted widths guarantees
        // exactly three wide elements, including when widths repeat.
        pattern = new int[9];
        for (var i = 0; i < 9; i++) pattern[i] = Code39Run(runs, start + i, reverse) >= ordered[6] ? 2 : 1;
        return true;
    }

    private static bool IsCode39Asterisk(int[] pattern) {
        var bits = Code39Tables.EncodingTable['*'].data;
        var offset = 0; var bar = true;
        foreach (var width in pattern) {
            for (var i = 0; i < width; i++) if (bits[offset++] != bar) return false;
            bar = !bar;
        }
        return offset == bits.Length;
    }

    private static int Code39Run(List<int> runs, int at, bool reverse) => runs[reverse ? runs.Count - 1 - at : at];
}
