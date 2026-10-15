using System.Collections.Generic;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    // Default image symbologies need a luminance transition. Explicit weak
    // formats retain their decoder rules, including single-bar interpretations.
    private static bool IsUniformDefaultFrame(byte[] rgba, IList<SymbolFormat> requested, ScanDeadline deadline) {
        foreach (var format in requested) if (!SymbolCapabilities.Get(format).IsDefaultScanFormat) return false;
        if (rgba[3] != 255) return false;
        var luminance = (rgba[0] * 77 + rgba[1] * 150 + rgba[2] * 29) >> 8;
        for (var at = 4; at < rgba.Length; at += 4) {
            if ((at & 4095) == 0 && deadline.ShouldStop) return false;
            if (rgba[at + 3] != 255) return false;
            if ((rgba[at] * 77 + rgba[at + 1] * 150 + rgba[at + 2] * 29) >> 8 != luminance) return false;
        }
        return true;
    }
}
