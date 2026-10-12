using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Codabar;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    /// <summary>
    /// Interprets exact pixel runs before module rounding can merge narrow and
    /// wide elements. Complete guards, quiet zones and a consistent two-width
    /// print-gain model are required because Codabar has no mandatory checksum.
    /// </summary>
    private static void TryCollectCodabarRunCandidates(ReadOnlySpan<int> runs, bool firstIsBar,
        CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        for (var direction = 0; direction < 2; direction++) {
            var reverse = direction != 0;
            for (var start = 1; start + 23 < runs.Length; start++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                if (!TryReadCodabarCharacter(runs, start, reverse, out var firstKey, out var first) ||
                    !CodabarTables.StartStopChars.Contains(first)) continue;
                var keys = new List<int>(16) { firstKey };
                for (var at = start + 8; at + 7 < runs.Length && keys.Count < 258; at += 8) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                    if (!TryReadCodabarCharacter(runs, at, reverse, out var key, out var character)) break;
                    keys.Add(key);
                    if (!CodabarTables.StartStopChars.Contains(character)) continue;
                    // A stop ends the symbol; it cannot be silently treated as data.
                    if (keys.Count >= 3 && TryValidateCodabarRuns(runs, start, reverse, keys,
                        cancellationToken, out var fit)) {
                        var widths = new List<int>(keys.Count * 8 - 1);
                        for (var symbol = 0; symbol < keys.Count; symbol++) {
                            if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                            if (symbol != 0) widths.Add(1);
                            for (var element = 0; element < 7; element++)
                                widths.Add((keys[symbol] & (1 << (6 - element))) == 0 ? 1 : 2);
                        }
                        var physicalStart = reverse ? runs.Length - 1 - start : start;
                        AddRunCandidate(widths, firstIsBar == ((physicalStart & 1) == 0), fit,
                            candidates, position, isVertical);
                    }
                    break;
                }
            }
        }
    }

    private static int CodabarRun(ReadOnlySpan<int> runs, int offset, bool reverse) =>
        runs[reverse ? runs.Length - 1 - offset : offset];

    private static bool TryReadCodabarCharacter(ReadOnlySpan<int> runs, int start, bool reverse,
        out int key, out char character) {
        key = 0;
        character = default;
        var minBar = int.MaxValue;
        var maxBar = 0;
        var minSpace = int.MaxValue;
        var maxSpace = 0;
        for (var i = 0; i < 7; i++) {
            var width = CodabarRun(runs, start + i, reverse);
            if ((i & 1) == 0) {
                minBar = Math.Min(minBar, width);
                maxBar = Math.Max(maxBar, width);
            } else {
                minSpace = Math.Min(minSpace, width);
                maxSpace = Math.Max(maxSpace, width);
            }
        }
        if (minBar <= 0 || minSpace <= 0 || minBar == maxBar) return false;
        var barThreshold = (minBar + (double)maxBar) / 2;
        var spaceThreshold = (minSpace + (double)maxSpace) / 2;
        var wideBars = 0;
        for (var i = 0; i < 7; i += 2) {
            if (CodabarRun(runs, start + i, reverse) <= barThreshold) continue;
            key |= 1 << (6 - i);
            wideBars++;
        }
        // Punctuation patterns have three wide bars and only narrow spaces.
        // Their small pixel-width differences do not imply a wide space.
        if (wideBars == 1) {
            if (minSpace == maxSpace) return false;
            for (var i = 1; i < 7; i += 2)
                if (CodabarRun(runs, start + i, reverse) > spaceThreshold) key |= 1 << (6 - i);
        } else if (wideBars != 3) return false;
        return CodabarTables.PatternMap.Value.TryGetValue(key, out character);
    }

    private static bool TryValidateCodabarRuns(ReadOnlySpan<int> runs, int start, bool reverse,
        List<int> keys, CancellationToken cancellationToken, out double fit) {
        fit = 0;
        Span<double> sums = stackalloc double[4];
        Span<int> counts = stackalloc int[4];
        Span<int> minima = stackalloc int[4];
        Span<int> maxima = stackalloc int[4];
        sums.Clear();
        counts.Clear();
        maxima.Clear();
        minima.Fill(int.MaxValue);
        for (var symbol = 0; symbol < keys.Count; symbol++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            for (var element = 0; element < 7; element++) {
                var group = (element & 1) * 2 + ((keys[symbol] >> (6 - element)) & 1);
                var width = CodabarRun(runs, start + symbol * 8 + element, reverse);
                sums[group] += width;
                counts[group]++;
                minima[group] = Math.Min(minima[group], width);
                maxima[group] = Math.Max(maxima[group], width);
            }
        }
        if (!TryFitTwoWidthRunModel(sums, counts, minima, maxima, out var pitch, out var tolerance)) return false;
        var end = start + keys.Count * 8 - 1;
        if (!HasCodabarQuietZone(runs, start - 1, reverse, pitch) ||
            !HasCodabarQuietZone(runs, end, reverse, pitch)) return false;
        for (var symbol = 0; symbol < keys.Count; symbol++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (symbol != 0 && Math.Abs(CodabarRun(runs, start + symbol * 8 - 1, reverse) - sums[2]) > tolerance) return false;
            for (var element = 0; element < 7; element++) {
                var group = (element & 1) * 2 + ((keys[symbol] >> (6 - element)) & 1);
                var residual = CodabarRun(runs, start + symbol * 8 + element, reverse) - sums[group];
                if (Math.Abs(residual) > tolerance) return false;
                fit += residual * residual;
            }
        }
        return true;
    }

    private static bool HasCodabarQuietZone(ReadOnlySpan<int> runs, int offset, bool reverse, double pitch) {
        var width = CodabarRun(runs, offset, reverse);
        // A photograph can crop the quiet zone at its outer image edge. Retain
        // at least one narrow element there; internal clutter still requires
        // the full separation on both sides of the complete guarded symbol.
        return width >= pitch * 3 || (offset == 0 || offset == runs.Length - 1) && width >= pitch;
    }
}
