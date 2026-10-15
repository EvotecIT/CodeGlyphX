using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.DataBar;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DataBarModuleRunTests {
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(47)]
    [InlineData(235)]
    public void BoundedRunReader_ReportsOnlyInitializedWidthsAndPreservesUnusedStorage(int capacity) {
        for (var length = 0; length <= 300; length++) {
            foreach (var period in new[] { 1, 2, 7, 301 }) {
                var modules = Enumerable.Range(0, length).Select(i => (i / period & 1) == 0).ToArray();
                var expected = ReferenceRuns(modules);
                var storage = Enumerable.Repeat(-12345, capacity + 2).ToArray();

                var success = DataBarModuleRuns.TryRead(modules, storage.AsSpan(1, capacity), out var count);

                Assert.Equal(length > 0 && expected.Count <= capacity, success);
                Assert.Equal(Math.Min(capacity, expected.Count), count);
                Assert.Equal(expected.Take(count), storage.Skip(1).Take(count));
                Assert.Equal(-12345, storage[0]);
                Assert.All(storage.Skip(count + 1), value => Assert.Equal(-12345, value));
            }
        }
    }

    [Theory]
    [InlineData("1234567890123", "0100110011110010100010011101011010101100100101001010100101000001110001110100000")]
    [InlineData("0000000000000", "0101010101010000001000000111010111010100100101010101010100000010000001110100000")]
    [InlineData("1999999999999", "0100111100110110101101111101010101101011000101010000101110001101011110010100000")]
    public void Limited_IndependentModulesRestoreEitherTrimmedLightGuard(string value, string fixture) {
        // Independent Zint vectors already cover the complete symbol. Exercise the trimmed facade inputs.
        var modules = fixture.Select(bit => bit == '1').ToArray();
        foreach (var leading in new[] { 0, 1 }) {
            foreach (var trailing in new[] { 0, 5 }) {
                var trimmed = modules.Skip(leading).Take(modules.Length - leading - trailing).ToArray();
                Assert.True(DataBarLimitedDecoder.TryDecode(trimmed, out var decoded));
                Assert.Equal(value, decoded);
            }
        }
    }

    [Fact]
    public void Expanded_AllSupportedCharacterCountsPreserveTrimmedAndPaddedGuardRetries() {
        var lengths = new[] { 1, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38 };
        var runCounts = new HashSet<int>();
        foreach (var length in lengths) {
            var input = "(91)" + new string('A', length);
            var expected = Gs1.ElementString(input);
            var barcode = DataBarExpandedEncoder.EncodeExpanded(input);
            runCounts.Add(barcode.Segments.Count);
            Assert.True(DataBarExpandedDecoder.TryDecodeExpanded(barcode, out var decoded));
            Assert.Equal(expected, decoded);
            var modules = Expand(barcode);
            foreach (var trimLeading in new[] { false, true }) {
                foreach (var trimTrailing in new[] { false, true }) {
                    var first = trimLeading ? 1 : 0;
                    var end = modules.Length - (trimTrailing && !modules[modules.Length - 1] ? 1 : 0);
                    var trimmed = modules.Skip(first).Take(end - first).ToArray();
                    Assert.True(DataBarExpandedDecoder.TryDecodeExpanded(trimmed, out decoded));
                    Assert.Equal(expected, decoded);
                }
            }
            var padded = new bool[modules.Length + 10];
            Array.Copy(modules, 0, padded, 3, modules.Length);
            Assert.True(DataBarExpandedDecoder.TryDecodeExpanded(padded, out decoded));
            Assert.Equal(expected, decoded);
        }
        Assert.Equal(19, runCounts.Count);
        Assert.Contains(235, runCounts);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    public void ExpandedStacked_MaximumCharacterCountPreservesPhysicalRowAssembly(int columns) {
        var input = "(91)" + new string('A', 38);
        var matrix = DataBarExpandedEncoder.EncodeExpandedStacked(input, columns);

        Assert.True(DataBarExpandedDecoder.TryDecodeExpandedStacked(matrix, out var decoded));
        Assert.Equal(Gs1.ElementString(input), decoded);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(46)]
    [InlineData(47)]
    [InlineData(235)]
    [InlineData(237)]
    [InlineData(238)]
    [InlineData(8192)]
    public void DenseNonsymbolRows_AreRejectedAtRunCapacityBoundaries(int length) {
        foreach (var firstIsBar in new[] { false, true }) {
            var modules = Enumerable.Range(0, length).Select(i => (i % 2 == 0) == firstIsBar).ToArray();
            Assert.False(DataBarLimitedDecoder.TryDecode(modules, out var limited));
            Assert.Equal(string.Empty, limited);
            Assert.False(DataBarExpandedDecoder.TryDecodeExpanded(modules, out var expanded));
            Assert.Equal(string.Empty, expanded);
        }
    }

    private static bool[] Expand(Barcode1D barcode) {
        var modules = new bool[barcode.TotalModules];
        var position = 0;
        foreach (var segment in barcode.Segments) {
            for (var i = 0; i < segment.Modules; i++) modules[position++] = segment.IsBar;
        }
        return modules;
    }

    private static List<int> ReferenceRuns(bool[] modules) {
        var runs = new List<int>();
        for (var first = 0; first < modules.Length;) {
            var end = first + 1;
            while (end < modules.Length && modules[end] == modules[first]) end++;
            runs.Add(end - first);
            first = end;
        }
        return runs;
    }
}
