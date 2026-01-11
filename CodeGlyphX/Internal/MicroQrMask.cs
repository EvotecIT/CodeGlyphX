using System;

namespace CodeGlyphX.Internal;

internal static class MicroQrMask {
    internal static bool ShouldInvert(int mask, int x, int y) {
        return mask switch {
            0 => (y & 1) == 0,
            1 => (((y / 2) + (x / 3)) & 1) == 0,
            2 => ((((x * y) & 1) + (x * y) % 3) & 1) == 0,
            3 => ((((x + y) & 1) + ((x * y) % 3)) & 1) == 0,
            _ => throw new ArgumentOutOfRangeException(nameof(mask)),
        };
    }

    internal static int EvaluateSymbol(BitMatrix modules) {
        var width = modules.Width;
        var sum1 = 0;
        var sum2 = 0;

        for (var x = 1; x < width; x++) {
            if (modules[x, width - 1]) sum1++;
        }

        for (var y = 1; y < width; y++) {
            if (modules[width - 1, y]) sum2++;
        }

        return sum1 <= sum2 ? (sum1 * 16 + sum2) : (sum2 * 16 + sum1);
    }
}
