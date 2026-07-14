using System;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Implements the GS1 DataBar Expanded symbol-character and checksum arithmetic.
/// </summary>
internal static class DataBarExpandedSpecification {
    private static readonly int[] GroupOffsets = { 0, 348, 1388, 2948, 3988 };
    private static readonly int[] EvenTotals = { 4, 20, 52, 104, 204 };
    private static readonly int[] OddModules = { 12, 10, 8, 6, 4 };
    private static readonly int[] EvenModules = { 5, 7, 9, 11, 13 };
    private static readonly int[] OddMaximumWidths = { 7, 5, 4, 3, 1 };
    private static readonly int[] EvenMaximumWidths = { 2, 4, 5, 6, 8 };
    private static readonly int[] ChecksumWeights = BuildChecksumWeights();

    private static readonly int[][] FinderPatterns = {
        new[] { 1, 8, 4, 1, 1 },
        new[] { 3, 6, 4, 1, 1 },
        new[] { 3, 4, 6, 1, 1 },
        new[] { 3, 2, 8, 1, 1 },
        new[] { 2, 6, 5, 1, 1 },
        new[] { 2, 2, 9, 1, 1 }
    };

    private static readonly int[][] FinderSequences = {
        new[] { 0, 0 },
        new[] { 0, 1, 1 },
        new[] { 0, 2, 1, 3 },
        new[] { 0, 4, 1, 3, 2 },
        new[] { 0, 4, 1, 3, 3, 5 },
        new[] { 0, 4, 1, 3, 4, 5, 5 },
        new[] { 0, 0, 1, 1, 2, 2, 3, 3 },
        new[] { 0, 0, 1, 1, 2, 2, 3, 4, 4 },
        new[] { 0, 0, 1, 1, 2, 2, 3, 4, 5, 5 },
        new[] { 0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5 }
    };

    internal static int[] GetDataCharacterWidths(int value) {
        if ((uint)value > 4095u) {
            throw new ArgumentOutOfRangeException(nameof(value), "A DataBar Expanded data character must be between 0 and 4095.");
        }

        var group = GetGroup(value);
        var groupValue = value - GroupOffsets[group];
        var oddValue = groupValue / EvenTotals[group];
        var evenValue = groupValue % EvenTotals[group];
        var oddWidths = DataBarCommon.GetWidths(oddValue, OddModules[group], 4, OddMaximumWidths[group], 0);
        var evenWidths = DataBarCommon.GetWidths(evenValue, EvenModules[group], 4, EvenMaximumWidths[group], 1);
        var widths = new int[8];

        for (var i = 0; i < 4; i++) {
            widths[i * 2] = oddWidths[i];
            widths[(i * 2) + 1] = evenWidths[i];
        }

        return widths;
    }

    internal static bool TryGetDataCharacterValue(ReadOnlySpan<int> widths, out int value) {
        value = 0;
        if (widths.Length < 8) return false;

        Span<int> odd = stackalloc int[4];
        Span<int> even = stackalloc int[4];
        for (var i = 0; i < 4; i++) {
            odd[i] = widths[i * 2];
            even[i] = widths[(i * 2) + 1];
        }

        for (var group = 0; group < GroupOffsets.Length; group++) {
            var oddValue = DataBarCommon.GetValue(odd, OddModules[group], 4, OddMaximumWidths[group], 0);
            var evenValue = DataBarCommon.GetValue(even, EvenModules[group], 4, EvenMaximumWidths[group], 1);
            if (oddValue < 0 || evenValue < 0) continue;

            var candidate = (oddValue * EvenTotals[group]) + evenValue + GroupOffsets[group];
            if (candidate < GroupOffsets[group] || candidate > GetGroupMaximum(group)) continue;
            value = candidate;
            return true;
        }

        return false;
    }

    internal static int ComputeCheckCharacter(int[][] dataWidths, int totalCharacters) {
        if (dataWidths is null) throw new ArgumentNullException(nameof(dataWidths));
        if (totalCharacters < 4 || totalCharacters > 22) throw new ArgumentOutOfRangeException(nameof(totalCharacters));
        if (dataWidths.Length != totalCharacters - 1) throw new ArgumentException("Data character count does not match the symbol size.", nameof(dataWidths));

        var finderSequence = GetFinderSequence(totalCharacters);
        var checksum = 0;
        for (var dataIndex = 0; dataIndex < dataWidths.Length; dataIndex++) {
            var row = GetChecksumRow(finderSequence, dataIndex);
            var widths = dataWidths[dataIndex];
            for (var element = 0; element < 8; element++) {
                checksum += widths[element] * ChecksumWeights[(row * 8) + element];
            }
        }

        return (211 * (totalCharacters - 4)) + (checksum % 211);
    }

    internal static bool HasValidCheckCharacter(int[][] dataWidths, int totalCharacters, int checkCharacter) {
        return ComputeCheckCharacter(dataWidths, totalCharacters) == checkCharacter;
    }

    internal static int[] GetFinderSequence(int totalCharacters) {
        if (totalCharacters < 4 || totalCharacters > 22) throw new ArgumentOutOfRangeException(nameof(totalCharacters));

        var sequenceIndex = totalCharacters switch {
            4 => 0,
            5 or 6 => 1,
            7 or 8 => 2,
            9 or 10 => 3,
            11 or 12 => 4,
            13 or 14 => 5,
            15 or 16 => 6,
            17 or 18 => 7,
            19 or 20 => 8,
            _ => 9
        };
        return FinderSequences[sequenceIndex];
    }

    internal static void CopyFinderPattern(int finderValue, bool reverse, int[] destination, int destinationIndex) {
        if ((uint)finderValue >= (uint)FinderPatterns.Length) throw new ArgumentOutOfRangeException(nameof(finderValue));
        if (destination is null) throw new ArgumentNullException(nameof(destination));
        if (destinationIndex < 0 || destinationIndex + 5 > destination.Length) throw new ArgumentOutOfRangeException(nameof(destinationIndex));

        var pattern = FinderPatterns[finderValue];
        for (var i = 0; i < 5; i++) {
            destination[destinationIndex + i] = reverse ? pattern[4 - i] : pattern[i];
        }
    }

    private static int GetGroup(int value) {
        if (value <= 347) return 0;
        if (value <= 1387) return 1;
        if (value <= 2947) return 2;
        if (value <= 3987) return 3;
        return 4;
    }

    private static int GetGroupMaximum(int group) {
        return group == GroupOffsets.Length - 1 ? 4095 : GroupOffsets[group + 1] - 1;
    }

    private static int GetChecksumRow(int[] finderSequence, int dataIndex) {
        if (dataIndex == 0) return 0;

        var finderIndex = (dataIndex + 1) / 2;
        var finderValue = finderSequence[finderIndex];
        var beforeFinder = (dataIndex & 1) == 1;
        if ((finderIndex & 1) == 1) {
            return (finderValue * 4) + (beforeFinder ? 1 : 2);
        }

        return (finderValue * 4) + (beforeFinder ? -1 : 0);
    }

    private static int[] BuildChecksumWeights() {
        var weights = new int[23 * 8];
        var value = 1;
        for (var i = 0; i < weights.Length; i++) {
            weights[i] = value;
            value = (value * 3) % 211;
        }
        return weights;
    }
}
