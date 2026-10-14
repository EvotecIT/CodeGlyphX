using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.DataBar;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    // At most 22 characters, 11 finders and four boundary elements.
    private const int MaximumExpandedRowWidths = 4 + 22 * 8 + 11 * 5;

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
    internal static bool TryDecodeExpandedStacked(byte[] rgba, int width, int height, bool allowSingleRow, CancellationToken token, out string content) {
        content = string.Empty;
        if (width < 1 || height < 1 || (width < 20 && height < 20) || DecodeBudget.ShouldAbort(token)) return false;
        var rented = ArrayPool<byte>.Shared.Rent(Math.Max(width, height));
        int[]? offsets = null;
        try {
            var runs = new List<int>();
            var starts = new List<int>();
            for (var vertical = 0; vertical < 2; vertical++) {
                var length = vertical == 0 ? width : height;
                var across = vertical == 0 ? height : width;
                if (length < 20) continue;
                var step = Math.Max(1, (across + 255) / 256);
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
                            CollectExpandedStackedRows(rented.AsSpan(0, length), threshold, position, step, observations, token,
                                runs, ref offsets, starts);
                        }
                    }
                    if (TryAssembleExpandedRows(observations, allowSingleRow, token, out content)) return true;
                }
            }
            return false;
        } finally {
            if (offsets is not null) ArrayPool<int>.Shared.Return(offsets);
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static void CollectExpandedStackedRows(ReadOnlySpan<byte> luminance, int threshold, int position, int step,
        List<ExpandedRowObservation>[] observations, CancellationToken token, List<int> runs, ref int[]? offsets,
        List<int> starts) {
        // Empty edge runs preserve a light guard clipped at the image boundary.
        runs.Clear();
        runs.Add(0);
        var budget = DecodeBudget.Capture();
        var first = luminance[0] < threshold;
        var current = first;
        var count = 1;
        for (var i = 1; i < luminance.Length; i++) {
            if ((i & 255) == 0 && budget.ShouldAbort(token)) return;
            var bar = luminance[i] < threshold;
            if (bar == current) count++;
            else { runs.Add(count); current = bar; count = 1; }
        }
        runs.Add(count); runs.Add(0);
        if (offsets is null || offsets.Length < runs.Count + 1) {
            var replacement = ArrayPool<int>.Shared.Rent(runs.Count + 1);
            if (offsets is not null) ArrayPool<int>.Shared.Return(offsets);
            offsets = replacement;
        }
        offsets[0] = 0;
        for (var i = 0; i < runs.Count; i++) offsets[i + 1] = offsets[i] + runs[i];
        Span<int> widths = stackalloc int[MaximumExpandedRowWidths];
        foreach (var group in ExpandedStackedPatterns.FinderGroups) {
            if (budget.ShouldAbort(token)) return;
            starts.Clear();
            for (var start = 0; start + group.MinimumWidth <= runs.Count; start++) {
                if ((start & 63) == 0 && budget.ShouldAbort(token)) return;
                if (ExpandedFinderMatches(runs, start + 10, group.Finder)) starts.Add(start);
            }
            // Each pattern still observes starts in their original order. Shared
            // finder shapes are fitted once before the character/guard checks.
            foreach (var patternIndex in group.PatternIndices) {
                if (budget.ShouldAbort(token)) return;
                var pattern = ExpandedStackedPatterns.Patterns[patternIndex];
                for (var startIndex = 0; startIndex < starts.Count; startIndex++) {
                    if ((startIndex & 63) == 0 && budget.ShouldAbort(token)) return;
                    var start = starts[startIndex];
                    if (start + pattern.Widths.Length > runs.Count) break;
                    var firstIsBar = !first == ((start & 1) == 0);
                    var inverted = firstIsBar != pattern.FirstIsBar;
                    widths[0] = pattern.Widths[0];
                    widths[1] = 1;
                    var fit = 0.0;
                    var at = 2;
                    while (at < pattern.Widths.Length - 2) {
                        if (pattern.Widths[at] == 0) {
                            if (!NormalizeExpandedCharacter(runs, start + at, widths.Slice(at, 8), ref fit)) break;
                            at += 8;
                        } else {
                            var finder = pattern.Widths.AsSpan(at, 5);
                            if (!ExpandedFinderMatches(runs, start + at, finder)) break;
                            finder.CopyTo(widths.Slice(at, 5)); at += 5;
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
                    widths[at] = 1; widths[at + 1] = 1;
                    var rowWidths = widths.Slice(0, at + 2);
                    var rowStart = offsets[start + 2] - (int)Math.Round((pattern.Widths[0] + 1) * pitch);
                    var rowEnd = offsets[start + at] + (int)Math.Round(2 * pitch);
                    var list = observations[patternIndex] ??= new List<ExpandedRowObservation>();
                    var match = false;
                    foreach (var existing in list) {
                        if (existing.Inverted != inverted || Math.Abs(existing.Start - rowStart) > pitch * 2 ||
                            Math.Abs(existing.End - rowEnd) > pitch * 2 || position > existing.LastPosition + step * 2 ||
                            !ExpandedWidthsEqual(existing.Widths, rowWidths)) continue;
                        existing.LastPosition = position; match = true; break;
                    }
                    // The bounded observation set avoids unbounded noisy-image work.
                    if (!match && list.Count < 64)
                        list.Add(new ExpandedRowObservation(rowWidths.ToArray(), rowStart, rowEnd, inverted, position));
                }
            }
        }
    }

    private static bool ExpandedWidthsEqual(int[] left, ReadOnlySpan<int> right) {
        if (left.Length != right.Length) return false;
        for (var i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }

    private static bool TryAssembleExpandedRows(List<ExpandedRowObservation>[] observations, bool allowSingleRow, CancellationToken token, out string content) {
        content = string.Empty;
        var decoded = string.Empty;
        var remaining = 4096;
        foreach (var layout in ExpandedStackedPatterns.Layouts) {
            if (DecodeBudget.ShouldAbort(token) || remaining <= 0) return false;
            if (!allowSingleRow && layout.Rows.Length == 1) continue;
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
