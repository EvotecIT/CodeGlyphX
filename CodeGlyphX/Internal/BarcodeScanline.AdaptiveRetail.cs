using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    /// <summary>
    /// Adds two ordinary local-contrast hypotheses and at most ten during retail recovery.
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
        ushort[]? extrema = null;
        try {
            if (retailRecovery) extrema = ArrayPool<ushort>.Shared.Rent(luminance.Length);
            // The two windows cover narrow symbols and larger retail photographs
            // without searching an unbounded collection of threshold scales.
            var small = Math.Max(8, Math.Min(32, luminance.Length / 32));
            var large = Math.Max(16, Math.Min(64, luminance.Length / 16));
            var previousRadius = -1;
            for (var pass = 0; pass < (retailRecovery ? 10 : 2); pass++) {
                // Fine cutoffs are useful only after the existing masks establish
                // complete retail guard geometry. Unstructured rows stop at eight.
                if (pass >= 8 && !hasRetailGuards) break;
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
                // Adjacent cutoffs use exactly the same extrema. Keep every
                // binary hypothesis and its order, but build each window once.
                var binarized = pass >= 8
                    ? TryBinarizeFineRetailRowFromExtrema(luminance, extrema!, binary, cancellationToken, invertLuminance: pass == 9)
                    : extrema is not null && radius == previousRadius
                        ? TryBinarizeRetailRowFromExtrema(luminance, extrema, binary, cancellationToken, bias, invertLuminance: pass == 5)
                        : TryBinarizeRetailRow(luminance, radius, binary, minima, maxima, cancellationToken, bias, invertLuminance: pass == 5, extrema: extrema);
                if (!binarized) return false;
                previousRadius = radius;
                hasRetailGuards |= TryCollectRetailRunCandidates(binary.AsSpan(0, luminance.Length), 128,
                    cancellationToken, candidates, position, isVertical, localThreshold: true, includeParityRecovery: retailRecovery,
                    fullLengthRetailOnly: pass >= 8).HasGuards;
            }
            return hasRetailGuards;
        } finally {
            ArrayPool<byte>.Shared.Return(binary);
            ArrayPool<int>.Shared.Return(minima);
            ArrayPool<int>.Shared.Return(maxima);
            if (extrema is not null) ArrayPool<ushort>.Shared.Return(extrema);
        }

    }

    private static bool TryBinarizeRetailRow(ReadOnlySpan<byte> luminance, int radius, byte[] binary,
        int[] minima, int[] maxima, CancellationToken cancellationToken, int bias = 0, bool invertLuminance = false, ushort[]? extrema = null) {
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
            if (extrema is not null) extrema[x] = (ushort)(min | max << 8);
            binary[x] = GetRetailBinaryPixel(luminance[x], min, max, bias, invertLuminance);
        }
        return true;
    }

    /// <summary>Reuses a completed window without changing its cutoff arithmetic or abort checks.</summary>
    private static bool TryBinarizeRetailRowFromExtrema(ReadOnlySpan<byte> luminance, ushort[] extrema,
        byte[] binary, CancellationToken cancellationToken, int bias, bool invertLuminance) {
        for (var x = 0; x < luminance.Length; x++) {
            if ((x & 127) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var packed = extrema[x];
            binary[x] = GetRetailBinaryPixel(luminance[x], packed & 255, packed >> 8, bias, invertLuminance);
        }
        return true;
    }

    /// <summary>Reuses the completed middle window for two complementary 9/16 cutoffs.</summary>
    private static bool TryBinarizeFineRetailRowFromExtrema(ReadOnlySpan<byte> luminance, ushort[] extrema,
        byte[] binary, CancellationToken cancellationToken, bool invertLuminance) {
        for (var x = 0; x < luminance.Length; x++) {
            if ((x & 127) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var min = extrema[x] & 255;
            var max = extrema[x] >> 8;
            var cutoff = (max - min) * 9 / 16;
            var bar = invertLuminance ? luminance[x] > max - cutoff : luminance[x] < min + cutoff;
            // Keep low-contrast padding white in both polarities, as in the
            // existing masks; inverting the binary row would merge its guards.
            binary[x] = (byte)(max - min >= 8 && bar ? 0 : 255);
        }
        return true;
    }

    private static byte GetRetailBinaryPixel(byte luminance, int min, int max, int bias, bool invertLuminance) {
        var threshold = min + (max - min) * (4 + bias) / 8;
        // Normalize inverted fine bars before classifying low-contrast
        // padding as white. Merely inverting the binary row would also
        // invert that padding and can merge it into the start guard.
        var bar = invertLuminance
            ? luminance > max - (max - min) * (4 + bias) / 8
            : luminance < threshold;
        return (byte)(max - min >= 8 && bar ? 0 : 255);
    }
}
