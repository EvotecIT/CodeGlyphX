using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    /// <summary>
    /// Adds two ordinary local-contrast hypotheses and at most eight during retail recovery.
    /// Sliding extrema keep work linear in the scanline length; the ordinary
    /// decoder still validates digit parity and the complete symbol checksum.
    /// </summary>
    private static bool TryCollectAdaptiveRetailCandidates(ReadOnlySpan<byte> luminance,
        CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates, int position, bool isVertical, bool retailRecovery = false) {
        if (luminance.Length < 51 || DecodeBudget.ShouldAbort(cancellationToken)) return false;
        var hasRetailGuards = false;
        var binary = ArrayPool<byte>.Shared.Rent(luminance.Length);
        var minima = ArrayPool<int>.Shared.Rent(luminance.Length);
        var maxima = ArrayPool<int>.Shared.Rent(luminance.Length);
        try {
            // The two windows cover narrow symbols and larger retail photographs
            // without searching an unbounded collection of threshold scales.
            var small = Math.Max(8, Math.Min(32, luminance.Length / 32));
            var large = Math.Max(16, Math.Min(64, luminance.Length / 16));
            for (var pass = 0; pass < (retailRecovery ? 8 : 2); pass++) {
                // Blur can widen dark or light elements asymmetrically. Recovery
                // adds two complementary cutoffs in the larger window; guard,
                // digit, checksum and neighboring-row validation remain required.
                // A smaller midpoint window preserves fine bars under blur. A
                // middle window and complementary eighth-step cutoffs cover
                // glare without broadening any guard or digit-fit tolerance.
                // Each hypothesis must locate complete retail guard geometry;
                // wider windows can lose that geometry on fine inverted bars.
                var radius = pass == 0 ? small : pass == 4 || pass == 5 ? Math.Max(4, small / 4) : pass >= 6 ? (small + large) / 2 : large;
                var bias = pass == 2 ? 2 : pass == 3 ? -2 : pass == 6 ? 1 : pass == 7 ? -1 : 0;
                if (!TryBinarizeRetailRow(luminance, radius, binary, minima, maxima, cancellationToken, bias, invertLuminance: pass == 5)) return false;
                hasRetailGuards |= TryCollectRetailRunCandidates(binary.AsSpan(0, luminance.Length), 128,
                    cancellationToken, candidates, position, isVertical, localThreshold: true, includeParityRecovery: retailRecovery).HasGuards;
            }
            return hasRetailGuards;
        } finally {
            ArrayPool<byte>.Shared.Return(binary);
            ArrayPool<int>.Shared.Return(minima);
            ArrayPool<int>.Shared.Return(maxima);
        }

    }

    private static bool TryBinarizeRetailRow(ReadOnlySpan<byte> luminance, int radius, byte[] binary,
        int[] minima, int[] maxima, CancellationToken cancellationToken, int bias = 0, bool invertLuminance = false) {
        var minHead = 0;
        var minTail = 0;
        var maxHead = 0;
        var maxTail = 0;
        var right = -1;
        for (var x = 0; x < luminance.Length; x++) {
            if ((x & 127) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var last = Math.Min(luminance.Length - 1, x + radius);
            while (right < last) {
                right++;
                while (minTail > minHead && luminance[minima[minTail - 1]] >= luminance[right]) minTail--;
                while (maxTail > maxHead && luminance[maxima[maxTail - 1]] <= luminance[right]) maxTail--;
                minima[minTail++] = right;
                maxima[maxTail++] = right;
            }
            var first = Math.Max(0, x - radius);
            while (minima[minHead] < first) minHead++;
            while (maxima[maxHead] < first) maxHead++;
            var min = luminance[minima[minHead]];
            var max = luminance[maxima[maxHead]];
            var threshold = min + (max - min) * (4 + bias) / 8;
            // Normalize inverted fine bars before classifying low-contrast
            // padding as white. Merely inverting the binary row would also
            // invert that padding and can merge it into the start guard.
            var bar = invertLuminance
                ? luminance[x] > max - (max - min) * (4 + bias) / 8
                : luminance[x] < threshold;
            binary[x] = (byte)(max - min >= 8 && bar ? 0 : 255);
        }
        return true;
    }
}
