using System;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    /// <summary>
    /// Fits narrow/wide bars and spaces with one common pitch and print gain.
    /// Groups are narrow bar, wide bar, narrow space and wide space. Callers
    /// retain their own framing, quiet-zone and per-element residual checks.
    /// </summary>
    private static bool TryFitTwoWidthRunModel(Span<double> sums, ReadOnlySpan<int> counts,
        ReadOnlySpan<int> minima, ReadOnlySpan<int> maxima, out double pitch, out double tolerance) {
        pitch = tolerance = 0;
        for (var group = 0; group < 4; group++) {
            if (counts[group] == 0) return false;
            sums[group] /= counts[group];
        }
        if (maxima[0] >= minima[1] || maxima[2] >= minima[3]) return false;
        pitch = (sums[0] + sums[2]) / 2;
        var widePitch = (sums[1] + sums[3]) / 2;
        var gain = (sums[2] - sums[0]) / 2;
        if (widePitch < pitch * 1.5 || widePitch > pitch * 4 || Math.Abs(gain) > pitch * 0.8) return false;
        // Narrow and wide elements must agree on the same printing bias.
        if (Math.Abs((sums[3] - sums[1]) - 2 * gain) > pitch * 0.75 + 0.5) return false;
        tolerance = pitch * 0.65 + 0.5;
        return true;
    }
}
