using System.Collections.Generic;

namespace CodeGlyphX.DataBar;

public static partial class DataBarExpandedDecoder {
    /// <summary>Validates sampled physical rows with the same assembly and checksum as module decoding.</summary>
    internal static bool TryDecodeExpandedStackedRows(List<int[]> rows, int columns, out string content, bool requireLinkage = false) {
        content = string.Empty;
        if (rows.Count == 0 || rows.Count > 11 || columns < 1 || columns > 11) return false;
        var blocks = 0;
        foreach (var row in rows) {
            if (row.Length < 17 || row.Length > 235) return false;
            var elements = row.Length - 4;
            if (elements % 21 != 0 && elements % 21 != 13) return false;
            blocks += (elements + 20) / 21;
        }
        if (blocks > 11) return false;
        if (!TryBuildStackedElements(rows, columns, out var assembled)) return false;
        if (!TryTrimElements(assembled, out var trimmed)) return false;
        return TryDecodeFromWidths(trimmed, out content, requireLinkage);
    }
}
