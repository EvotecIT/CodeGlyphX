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
    /// most six adjacent refinements for each guarded retail-shaped anchor.
    /// The caller restricts recovery to complete retail decodes and requires
    /// a matching neighboring row; digit, parity and checksum validation stays intact.
    /// </summary>
    private static void CollectRetailRecoveryRows(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        CancellationToken cancellationToken, List<BarcodeScanlineCandidate> candidates) {
        var horizontalAnchors = new HashSet<int> { height / 2, height / 3, (int)((long)height * 2 / 3) };
        var verticalAnchors = new HashSet<int> { width / 2, width / 3, (int)((long)width * 2 / 3) };
        // Sampling a scheduled anchor as somebody else's neighbor must not
        // suppress that anchor's own refinement. Cache its guard evidence
        // separately from the set of anchors already expanded.
        var horizontalGuards = new Dictionary<int, bool>();
        var verticalGuards = new Dictionary<int, bool>();
        // Revisit the ordinary positions with constrained parity before widening
        // coverage. Single-result callers skip this pass after an ordinary hit.
        foreach (var y in new[] { height / 2, height / 3, (int)((long)height * 2 / 3) })
            CollectHorizontal(pixels, y);
        foreach (var x in new[] { width / 2, width / 3, (int)((long)width * 2 / 3) })
            CollectVertical(pixels, x);
        foreach (var fraction in RetailRecoveryFractions) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return;
            var y = (int)((long)height * fraction / 16);
            if (horizontalAnchors.Add(y)) {
                if (CollectHorizontal(pixels, y)) {
                    // A mirrored integer anchor can move by one pixel. Three
                    // neighbors cover the same two-pixel gap in either direction.
                    for (var distance = 1; distance <= 3; distance++)
                        for (var adjacent = y - distance; adjacent <= y + distance; adjacent += distance * 2)
                            if ((uint)adjacent < (uint)height)
                                CollectHorizontal(pixels, adjacent);
                }
            }
        }
        foreach (var fraction in RetailRecoveryFractions) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return;
            var x = (int)((long)width * fraction / 16);
            if (verticalAnchors.Add(x)) {
                if (CollectVertical(pixels, x)) {
                    for (var distance = 1; distance <= 3; distance++)
                        for (var adjacent = x - distance; adjacent <= x + distance; adjacent += distance * 2)
                            if ((uint)adjacent < (uint)width)
                                CollectVertical(pixels, adjacent);
                }
            }
        }

        bool CollectHorizontal(PixelSpan input, int y) {
            if (horizontalGuards.TryGetValue(y, out var guards)) return guards;
            guards = TryCollectCandidatesFromHorizontal(input, width, height, stride, format, y, cancellationToken, candidates, retailOnly: true);
            horizontalGuards.Add(y, guards);
            return guards;
        }

        bool CollectVertical(PixelSpan input, int x) {
            if (verticalGuards.TryGetValue(x, out var guards)) return guards;
            guards = TryCollectCandidatesFromVertical(input, width, height, stride, format, x, cancellationToken, candidates, retailOnly: true);
            verticalGuards.Add(x, guards);
            return guards;
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
