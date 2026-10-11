using System.Collections.Generic;
using System.Threading;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    private static readonly int[] RetailRecoveryFractions = { 4, 12, 6, 10, 2, 14, 7, 9, 5, 11, 3, 13, 1, 15 };

    /// <summary>
    /// Searches fourteen anchors per axis when retail recovery is requested, with at
    /// most two adjacent refinements for each guarded retail-shaped anchor.
    /// The caller restricts recovery to complete retail decodes and requires
    /// a matching neighboring row; digit, parity and checksum validation stays intact.
    /// </summary>
    private static void CollectRetailRecoveryRows(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates) {
        var horizontal = new HashSet<int> { height / 2, height / 3, (int)((long)height * 2 / 3) };
        var vertical = new HashSet<int> { width / 2, width / 3, (int)((long)width * 2 / 3) };
        // Revisit the ordinary positions with constrained parity before widening
        // coverage. Single-result callers skip this pass after an ordinary hit.
        foreach (var y in new[] { height / 2, height / 3, (int)((long)height * 2 / 3) })
            TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, y, cancellationToken, candidates, retailOnly: true);
        foreach (var x in new[] { width / 2, width / 3, (int)((long)width * 2 / 3) })
            TryCollectCandidatesFromVertical(pixels, width, height, stride, format, x, cancellationToken, candidates, retailOnly: true);
        foreach (var fraction in RetailRecoveryFractions) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return;
            var y = (int)((long)height * fraction / 16);
            if (horizontal.Add(y)) {
                if (TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, y, cancellationToken, candidates, retailOnly: true)) {
                    for (var adjacent = y - 1; adjacent <= y + 1; adjacent += 2)
                        if ((uint)adjacent < (uint)height && horizontal.Add(adjacent))
                            TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, adjacent, cancellationToken, candidates, retailOnly: true);
                }
            }
        }
        foreach (var fraction in RetailRecoveryFractions) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return;
            var x = (int)((long)width * fraction / 16);
            if (vertical.Add(x)) {
                if (TryCollectCandidatesFromVertical(pixels, width, height, stride, format, x, cancellationToken, candidates, retailOnly: true)) {
                    for (var adjacent = x - 1; adjacent <= x + 1; adjacent += 2)
                        if ((uint)adjacent < (uint)width && vertical.Add(adjacent))
                            TryCollectCandidatesFromVertical(pixels, width, height, stride, format, adjacent, cancellationToken, candidates, retailOnly: true);
                }
            }
        }
    }

    /// <summary>Collects one neighboring physical row for independent recovery confirmation.</summary>
    internal static bool TryGetRetailRowCandidates(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        int position, bool isVertical, CancellationToken cancellationToken, out BarcodeScanlineCandidate[] candidates) {
        var list = new List<BarcodeScanlineCandidate>(8);
        if (isVertical)
            TryCollectCandidatesFromVertical(pixels, width, height, stride, format, position, cancellationToken, list, retailOnly: true);
        else
            TryCollectCandidatesFromHorizontal(pixels, width, height, stride, format, position, cancellationToken, list, retailOnly: true);
        candidates = list.ToArray();
        return !DecodeBudget.ShouldAbort(cancellationToken) && candidates.Length > 0;
    }
}
