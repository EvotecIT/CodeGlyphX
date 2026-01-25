using System;

namespace CodeGlyphX.Rendering;

internal static class LumaTables {
    private static readonly int[] LumaR = BuildTable(299);
    private static readonly int[] LumaG = BuildTable(587);
    private static readonly int[] LumaB = BuildTable(114);

    internal static int Luma(byte r, byte g, byte b) {
        return (LumaR[r] + LumaG[g] + LumaB[b] + 500) / 1000;
    }

    private static int[] BuildTable(int factor) {
        var table = new int[256];
        for (var i = 0; i < 256; i++) {
            table[i] = i * factor;
        }
        return table;
    }
}
