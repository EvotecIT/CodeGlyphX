using System;

namespace CodeGlyphX.DataBar;

public static partial class DataBarExpandedDecoder {
    // Twenty-two symbol characters, eleven five-element finders and four guards.
    private const int MaximumPatternElements = 235;

    private static bool TryDecodeModules(bool[] modules, out string content, bool requireLinkage = false) {
        content = string.Empty;
        if (modules.Length < 2) return false;
        // At most two light boundary runs can be discarded by guard restoration.
        Span<int> widths = stackalloc int[MaximumPatternElements + 2];
        if (!DataBarModuleRuns.TryRead(modules, widths, out var count)) return false;
        if (TryDecodeFromWidths(widths.Slice(0, count), out content, requireLinkage)) return true;

        // Generic linear extraction may trim Expanded's light guards along with quiet space.
        // Preserve the original retries: one leading light module, then a trailing one as well.
        var first = modules[0] ? 0 : 1;
        var end = count - (modules[modules.Length - 1] ? 0 : 1);
        var innerCount = end - first;
        if (innerCount <= 0 || innerCount + 1 > MaximumPatternElements) return false;
        widths.Slice(first, innerCount).CopyTo(widths.Slice(1));
        widths[0] = 1;
        if (TryDecodeFromWidths(widths.Slice(0, innerCount + 1), out content, requireLinkage)) return true;
        if (innerCount + 2 > MaximumPatternElements) return false;
        widths[innerCount + 1] = 1;
        return TryDecodeFromWidths(widths.Slice(0, innerCount + 2), out content, requireLinkage);
    }

    private static bool TryDecodeFromWidths(ReadOnlySpan<int> widths, out string content, bool requireLinkage = false) {
        content = string.Empty;
        return TryGetTotalCharacters(widths.Length, out var totalChars)
            && TryDecodeWithTotalChars(widths, totalChars, out content, requireLinkage);
    }

    private static bool TryGetTotalCharacters(int patternWidth, out int totalChars) {
        // Expected width strictly increases with character count, so there is at most one match.
        for (totalChars = 4; totalChars <= 22; totalChars++) {
            var finderCount = (totalChars + 1) / 2;
            if (finderCount * 5 + totalChars * 8 + 4 == patternWidth) return true;
        }
        totalChars = 0;
        return false;
    }

    private static bool TryExtractWidths(bool[] modules, out int[] widths) {
        widths = Array.Empty<int>();
        if (modules.Length < 2) return false;
        // Physical stacked rows retain their widths for assembly. Bound extraction before allocating.
        Span<int> buffer = stackalloc int[MaximumPatternElements];
        if (!DataBarModuleRuns.TryRead(modules, buffer, out var count)) return false;
        widths = buffer.Slice(0, count).ToArray();
        return true;
    }

    private static bool TryDecodeWithTotalChars(ReadOnlySpan<int> elements, int totalChars, out string content, bool requireLinkage = false) {
        content = string.Empty;
        var dataChars = totalChars - 1;
        if (dataChars <= 0) return false;

        if (!HasValidGuards(elements)) return false;
        if (!TryBuildDataWidths(elements, dataChars, out var dataWidths, out var checkWidths)) return false;
        if (!TryDecodeDataValues(dataWidths, out var dataValues)) return false;
        if (!TryDecodeDataCharacter(checkWidths, out var checkChar)) return false;
        if (!DataBarExpandedSpecification.HasValidCheckCharacter(dataWidths, totalChars, checkChar)) return false;
        var bits = BuildDataBits(dataValues);
        if (requireLinkage && !bits[0]) return false;
        return TryDecodePayload(bits, out content);
    }

    private static bool HasValidGuards(ReadOnlySpan<int> elements) {
        return elements.Length >= 10
            && elements[0] == 1
            && elements[1] == 1
            && elements[elements.Length - 1] == 1
            && elements[elements.Length - 2] == 1;
    }

    private static bool TryBuildDataWidths(ReadOnlySpan<int> elements, int dataChars, out int[][] dataWidths, out int[] checkWidths) {
        dataWidths = new int[dataChars][];
        for (var i = 0; i < dataChars; i++) dataWidths[i] = new int[8];
        checkWidths = new int[8];
        for (var i = 0; i < 8; i++) checkWidths[i] = elements[i + 2];

        for (var i = 1; i < dataChars; i += 2) {
            var baseIndex = (((i - 1) / 2) * 21) + 23;
            if (baseIndex + 7 >= elements.Length) return false;
            for (var j = 0; j < 8; j++) {
                dataWidths[i][j] = elements[baseIndex + j];
            }
        }

        for (var i = 0; i < dataChars; i += 2) {
            var baseIndex = ((i / 2) * 21) + 15;
            if (baseIndex + 7 >= elements.Length) return false;
            for (var j = 0; j < 8; j++) {
                dataWidths[i][j] = elements[baseIndex + (7 - j)];
            }
        }
        return true;
    }

    private static bool TryDecodeDataCharacter(int[] widths, out int value) {
        return DataBarExpandedSpecification.TryGetDataCharacterValue(widths, out value);
    }
}
