using System;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    /// <summary>
    /// Proposes a margin color from agreeing ends, or from the longer of two
    /// different end runs. Decoder validation must establish foreground and
    /// quiet-margin evidence before this proposal can exclude a polarity.
    /// </summary>
    private static bool? GetProposedBackgroundPolarity(ReadOnlySpan<byte> luminance, int threshold,
        CancellationToken token) {
        var firstIsDark = luminance[0] < threshold;
        var lastIsDark = luminance[luminance.Length - 1] < threshold;
        if (firstIsDark == lastIsDark) return firstIsDark;
        var leading = 1;
        while (leading < luminance.Length && (luminance[leading] < threshold) == firstIsDark) {
            if ((leading & 127) == 0 && DecodeBudget.ShouldAbort(token)) return null;
            leading++;
        }
        var trailing = 1;
        while (trailing < luminance.Length && (luminance[luminance.Length - trailing - 1] < threshold) == lastIsDark) {
            if ((trailing & 127) == 0 && DecodeBudget.ShouldAbort(token)) return null;
            trailing++;
        }
        if ((long)leading >= (long)trailing * 2) return firstIsDark;
        if ((long)trailing >= (long)leading * 2) return lastIsDark;
        return null;
    }
}
