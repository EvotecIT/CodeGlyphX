using System;
using System.Collections.Generic;

namespace CodeGlyphX.DataBar;

internal static partial class DataBar14ImageDecoder {
    /// <summary>
    /// Retains an already valid rounded character; otherwise considers a single
    /// module transfer under the same measured residual limits. The complete
    /// symbol still requires its finders, guards, characters and checksum.
    /// </summary>
    private static bool ValidateOrRepairCharacter(List<int> runs, int start, double pitch,
        Span<int> widths, bool outside, bool reverse) {
        if (DataBar14Decoder.IsValidPhysicalCharacter(widths, outside, reverse)) return true;
        Span<int> best = stackalloc int[8];
        var bestError = double.PositiveInfinity;
        var ambiguous = false;
        for (var from = 0; from < 8; from++) {
            if (widths[from] <= 1 || Math.Abs(widths[from] - 1 - runs[start + from] / pitch) > 0.8) continue;
            for (var to = 0; to < 8; to++) {
                if (to == from || widths[to] >= 8 || Math.Abs(widths[to] + 1 - runs[start + to] / pitch) > 0.8) continue;
                widths[from]--; widths[to]++;
                if (DataBar14Decoder.IsValidPhysicalCharacter(widths, outside, reverse)) {
                    var error = 0.0;
                    for (var i = 0; i < 8; i++) {
                        var residual = widths[i] - runs[start + i] / pitch;
                        error += residual * residual;
                    }
                    if (error < bestError - 1e-9) {
                        bestError = error; widths.CopyTo(best); ambiguous = false;
                    } else if (Math.Abs(error - bestError) <= 1e-9) {
                        ambiguous = true;
                    }
                }
                widths[from]++; widths[to]--;
            }
        }
        if (double.IsPositiveInfinity(bestError) || ambiguous) return false;
        best.CopyTo(widths);
        return true;
    }
}
