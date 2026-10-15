using System;

namespace CodeGlyphX.DataBar;

/// <summary>Reads exact module runs into a caller-bounded buffer without allocating for invalid rows.</summary>
internal static class DataBarModuleRuns {
    /// <summary>
    /// Collects every alternating run, including light boundaries. Returns false when the row is empty
    /// or has more runs than the destination can hold; only the reported prefix is initialized.
    /// </summary>
    internal static bool TryRead(ReadOnlySpan<bool> modules, Span<int> widths, out int count) {
        count = 0;
        if (modules.IsEmpty) return false;
        var current = modules[0];
        var length = 1;
        for (var i = 1; i < modules.Length; i++) {
            if (modules[i] == current) {
                length++;
            } else {
                if (count == widths.Length) return false;
                widths[count++] = length;
                current = modules[i];
                length = 1;
            }
        }
        if (count == widths.Length) return false;
        widths[count++] = length;
        return true;
    }
}
