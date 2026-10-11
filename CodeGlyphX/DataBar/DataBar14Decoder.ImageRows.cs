using System;

namespace CodeGlyphX.DataBar;

public static partial class DataBar14Decoder {
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
