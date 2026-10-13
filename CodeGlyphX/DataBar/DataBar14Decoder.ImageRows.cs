using System;

namespace CodeGlyphX.DataBar;

public static partial class DataBar14Decoder {
    /// <summary>Validates a measured character in its physical scan direction through the module decoder's groups.</summary>
    internal static bool IsValidPhysicalCharacter(ReadOnlySpan<int> widths, bool outside, bool reverse) {
        if (widths.Length != 8) return false;
        Span<int> odd = stackalloc int[4];
        Span<int> even = stackalloc int[4];
        for (var i = 0; i < 4; i++) {
            odd[i] = widths[reverse ? 7 - i * 2 : i * 2];
            even[i] = widths[reverse ? 6 - i * 2 : i * 2 + 1];
        }
        return outside
            ? TryDecodeDataCharacterGroups(odd, even, 0, 4, swapEvenOdd: false, out _)
            : TryDecodeDataCharacterGroups(odd, even, 5, 8, swapEvenOdd: true, out _);
    }

    /// <summary>Validates two normalized physical rows through the module decoder's checksum and character rules.</summary>
    internal static bool TryDecodeStackedRows(int[] top, int[] bottom, out string content) {
        content = string.Empty;
        if (top.Length != 25 || bottom.Length != 25) return false;
        var widths = new int[46];
        Array.Copy(top, 0, widths, 0, 23);
        Array.Copy(bottom, 2, widths, 23, 23);
        return TryDecodeFromWidths(widths, out content);
    }
}
