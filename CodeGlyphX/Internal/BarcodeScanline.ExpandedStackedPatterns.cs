using System;
using System.Collections.Generic;
using CodeGlyphX.DataBar;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    private sealed class ExpandedRowPattern {
        internal readonly int[] Widths;
        internal readonly int[] FirstFinder;
        internal readonly bool FirstIsBar;
        internal ExpandedRowPattern(int[] widths, bool firstIsBar) {
            Widths = widths;
            FirstIsBar = firstIsBar;
            FirstFinder = new int[5];
            Array.Copy(widths, 10, FirstFinder, 0, 5);
        }
    }

    private sealed class ExpandedLayout {
        internal readonly int Columns;
        internal readonly int[] Rows;
        internal ExpandedLayout(int columns, int[] rows) { Columns = columns; Rows = rows; }
    }

    private sealed class ExpandedFinderGroup {
        internal readonly int[] Finder;
        internal readonly List<int> PatternIndices = new();
        internal int MinimumWidth = int.MaxValue;
        internal ExpandedFinderGroup(int[] finder) { Finder = finder; }
    }

    private sealed class ExpandedPatternCatalog {
        internal readonly List<ExpandedRowPattern> Patterns = new();
        internal readonly List<ExpandedLayout> Layouts = new();
        internal readonly List<ExpandedFinderGroup> FinderGroups = new();

        internal ExpandedPatternCatalog() {
            var known = new Dictionary<string, int>(StringComparer.Ordinal);
            // Zero widths mark character elements to be fitted from the image.
            // Finder placement, alternating rows and short-row guards come from
            // the encoder's canonical layout rather than another layout implementation.
            for (var characters = 4; characters <= 22; characters++) {
                var blocks = (characters + 1) / 2;
                var sequence = DataBarExpandedSpecification.GetFinderSequence(characters);
                var elements = new int[4 + characters * 8 + blocks * 5];
                for (var block = 0; block < blocks; block++)
                    DataBarExpandedSpecification.CopyFinderPattern(sequence[block], (block & 1) != 0, elements, 10 + block * 21);
                for (var columns = 1; columns <= blocks; columns++) {
                    var rows = new int[(blocks + columns - 1) / columns];
                    for (var row = 0; row < rows.Length; row++) {
                        var count = Math.Min(columns, blocks - row * columns);
                        var forward = DataBarExpandedStacking.GetRowDirection(characters, columns, row, out var special);
                        var widths = DataBarExpandedStacking.BuildRow(elements, characters, row * columns, count, forward, special);
                        var firstBlock = row * columns + (forward ? 0 : count - 1);
                        var firstIsBar = (firstBlock & 1) != 0;
                        var key = (firstIsBar ? "1:" : "0:") + string.Join(",", widths);
                        if (!known.TryGetValue(key, out var index)) {
                            index = Patterns.Count;
                            known.Add(key, index);
                            Patterns.Add(new ExpandedRowPattern(widths, firstIsBar));
                        }
                        rows[row] = index;
                    }
                    Layouts.Add(new ExpandedLayout(columns, rows));
                }
            }
            var finders = new Dictionary<string, ExpandedFinderGroup>(StringComparer.Ordinal);
            for (var index = 0; index < Patterns.Count; index++) {
                var pattern = Patterns[index];
                var key = string.Join(",", pattern.FirstFinder);
                if (!finders.TryGetValue(key, out var group)) {
                    group = new ExpandedFinderGroup(pattern.FirstFinder);
                    finders.Add(key, group);
                    FinderGroups.Add(group);
                }
                group.PatternIndices.Add(index);
                group.MinimumWidth = Math.Min(group.MinimumWidth, pattern.Widths.Length);
            }
        }
    }

    private static class ExpandedStackedPatternCache {
        internal static readonly ExpandedPatternCatalog Value = new();
    }

    private static ExpandedPatternCatalog ExpandedStackedPatterns => ExpandedStackedPatternCache.Value;
}
