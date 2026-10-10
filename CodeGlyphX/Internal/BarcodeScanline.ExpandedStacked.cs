using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.DataBar;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    private sealed class ExpandedRowObservation {
        internal readonly int[] Widths;
        internal readonly int Start;
        internal readonly int End;
        internal readonly bool Inverted;
        internal readonly int FirstPosition;
        internal int LastPosition;
        internal ExpandedRowObservation(int[] widths, int start, int end, bool inverted, int position) {
            Widths = widths; Start = start; End = end; Inverted = inverted;
            FirstPosition = LastPosition = position;
        }
    }

    /// <summary>Locates Expanded Stacked rows in packed RGBA data and validates their assembled checksum.</summary>
    internal static bool TryDecodeExpandedStacked(byte[] rgba, int width, int height, CancellationToken token, out string content) {
        content = string.Empty;
        if (width < 20 || height < 1 || DecodeBudget.ShouldAbort(token)) return false;
        var rented = ArrayPool<byte>.Shared.Rent(Math.Max(width, height));
        try {
            for (var vertical = 0; vertical < 2; vertical++) {
                var length = vertical == 0 ? width : height;
                var across = vertical == 0 ? height : width;
                if (length < 20) continue;
                var step = Math.Max(1, across / 256);
                for (var reverse = 0; reverse < 2; reverse++) {
                    var observations = new List<ExpandedRowObservation>[ExpandedStackedPatterns.Patterns.Count];
                    for (var position = 0; position < across; position += step) {
                        if (DecodeBudget.ShouldAbort(token)) return false;
                        var min = 255;
                        var max = 0;
                        for (var i = 0; i < length; i++) {
                            if ((i & 255) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                            var along = reverse == 0 ? i : length - 1 - i;
                            var at = vertical == 0 ? (position * width + along) * 4 : (along * width + position) * 4;
                            var lum = GetLuminance(rgba[at], rgba[at + 1], rgba[at + 2], rgba[at + 3]);
                            rented[i] = lum;
                            if (lum < min) min = lum;
                            if (lum > max) max = lum;
                        }
                        if (max - min < 8) continue;
                        // Separate cutoffs let gray edges vary without changing the character decoder.
                        for (var thresholdIndex = 0; thresholdIndex < 3; thresholdIndex++) {
                            var threshold = min + (max - min) * (thresholdIndex + 1) / 4;
                            CollectExpandedStackedRows(rented.AsSpan(0, length), threshold, position, step, observations, token);
                        }
                    }
                    if (TryAssembleExpandedRows(observations, token, out content)) return true;
                }
            }
            return false;
        } finally { ArrayPool<byte>.Shared.Return(rented); }
    }

    private static void CollectExpandedStackedRows(ReadOnlySpan<byte> luminance, int threshold, int position, int step,
        List<ExpandedRowObservation>[] observations, CancellationToken token) {
        // Empty edge runs preserve a light guard clipped at the image boundary.
        var runs = new List<int> { 0 };
        var first = luminance[0] < threshold;
        var current = first;
        var count = 1;
        for (var i = 1; i < luminance.Length; i++) {
            if ((i & 255) == 0 && DecodeBudget.ShouldAbort(token)) return;
            var bar = luminance[i] < threshold;
            if (bar == current) count++;
            else { runs.Add(count); current = bar; count = 1; }
        }
        runs.Add(count); runs.Add(0);
        var offsets = new int[runs.Count + 1];
        for (var i = 0; i < runs.Count; i++) offsets[i + 1] = offsets[i] + runs[i];
        for (var patternIndex = 0; patternIndex < observations.Length; patternIndex++) {
            if (DecodeBudget.ShouldAbort(token)) return;
            var pattern = ExpandedStackedPatterns.Patterns[patternIndex];
            for (var start = 0; start + pattern.Widths.Length <= runs.Count; start++) {
                if ((start & 63) == 0 && DecodeBudget.ShouldAbort(token)) return;
                if (!ExpandedFinderMatches(runs, start + 10, pattern.FirstFinder)) continue;
                var firstIsBar = !first == ((start & 1) == 0);
                var inverted = firstIsBar != pattern.FirstIsBar;
                var widths = new List<int>(pattern.Widths.Length) { pattern.Widths[0], 1 };
                var fit = 0.0;
                var at = 2;
                while (at < pattern.Widths.Length - 2) {
                    if (pattern.Widths[at] == 0) {
                        if (!NormalizeExpandedCharacter(runs, start + at, widths, ref fit)) break;
                        at += 8;
                    } else {
                        var finder = new int[5];
                        Array.Copy(pattern.Widths, at, finder, 0, 5);
                        if (!ExpandedFinderMatches(runs, start + at, finder)) break;
                        widths.AddRange(finder); at += 5;
                    }
                }
                if (at != pattern.Widths.Length - 2) continue;
                var pitch = 0.0;
                for (var i = 0; i < 5; i++) pitch += runs[start + 10 + i];
                pitch /= 15;
                var firstDark = pattern.FirstIsBar ? 0 : 1;
                // Polarity changes colors, not which physical guard must have
                // a measurable transition against the surrounding quiet space.
                if (Math.Abs(runs[start + firstDark] / pitch - pattern.Widths[firstDark]) > 0.85) continue;
                var lastDark = pattern.FirstIsBar == ((pattern.Widths.Length & 1) != 0) ? at + 1 : at;
                if (Math.Abs(runs[start + lastDark] / pitch - 1) > 0.85) continue;
                widths.Add(1); widths.Add(1);
                var rowStart = offsets[start + 2] - (int)Math.Round((pattern.Widths[0] + 1) * pitch);
                var rowEnd = offsets[start + at] + (int)Math.Round(2 * pitch);
                var list = observations[patternIndex] ??= new List<ExpandedRowObservation>();
                var match = false;
                foreach (var existing in list) {
                    if (existing.Inverted != inverted || Math.Abs(existing.Start - rowStart) > pitch * 2 ||
                        Math.Abs(existing.End - rowEnd) > pitch * 2 || position > existing.LastPosition + step * 2 ||
                        !ExpandedWidthsEqual(existing.Widths, widths)) continue;
                    existing.LastPosition = position; match = true; break;
                }
                // The bounded observation set avoids unbounded noisy-image work.
                if (!match && list.Count < 64)
                    list.Add(new ExpandedRowObservation(widths.ToArray(), rowStart, rowEnd, inverted, position));
            }
        }
    }

    private static bool ExpandedWidthsEqual(int[] left, List<int> right) {
        if (left.Length != right.Count) return false;
        for (var i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }

    private static bool TryAssembleExpandedRows(List<ExpandedRowObservation>[] observations, CancellationToken token, out string content) {
        content = string.Empty;
        var decoded = string.Empty;
        var remaining = 4096;
        foreach (var layout in ExpandedStackedPatterns.Layouts) {
            if (DecodeBudget.ShouldAbort(token) || remaining <= 0) return false;
            var complete = true;
            foreach (var row in layout.Rows) if (observations[row] is null) { complete = false; break; }
            if (!complete) continue;
            for (var reverse = 0; reverse < 2; reverse++) {
                var selected = new ExpandedRowObservation[layout.Rows.Length];
                if (Search(0)) { content = decoded; return true; }

                bool Search(int index) {
                    if (DecodeBudget.ShouldAbort(token) || --remaining < 0) return false;
                    if (index == selected.Length) {
                        var rows = new List<int[]>(selected.Length);
                        for (var i = 0; i < selected.Length; i++) rows.Add(selected[reverse == 0 ? i : selected.Length - 1 - i].Widths);
                        return DataBarExpandedDecoder.TryDecodeExpandedStackedRows(rows, layout.Columns, out decoded);
                    }
                    var pattern = layout.Rows[reverse == 0 ? index : selected.Length - 1 - index];
                    foreach (var row in observations[pattern]) {
                        if (index > 0) {
                            var prior = selected[index - 1];
                            var span = Math.Max(prior.End - prior.Start, row.End - row.Start);
                            if (row.FirstPosition <= prior.LastPosition || row.Inverted != prior.Inverted ||
                                Math.Abs(row.Start - prior.Start) > span / 4 ||
                                row.FirstPosition - prior.LastPosition > span) continue;
                        }
                        selected[index] = row;
                        if (Search(index + 1)) return true;
                    }
                    return false;
                }
            }
        }
        return false;
    }
}
