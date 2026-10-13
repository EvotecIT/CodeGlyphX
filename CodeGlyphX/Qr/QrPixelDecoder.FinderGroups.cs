#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    /// <summary>
    /// Collects spatially independent finder groups before mixing corners from
    /// already recognized symbols. The permissive and predicate-driven searches
    /// retain their existing traversal and acceptance behavior.
    /// </summary>
    private static void CollectIndependentFinderGroups(QrGrayImage image, bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates, PooledList<QrDecoded> results,
        HashSet<byte[]>? seen, DecodeBudget budget, bool stylized) {
        const int maxFinders = 24;
        const int maxGroups = 48;
        Span<QrFinderPatternDetector.FinderPattern> finders = stackalloc QrFinderPatternDetector.FinderPattern[maxFinders];
        var count = 0;
        foreach (var candidate in candidates) {
            if (count == maxFinders && candidate.Count <= finders[count - 1].Count) continue;
            var at = Math.Min(count, maxFinders - 1);
            while (at > 0 && candidate.Count > finders[at - 1].Count) {
                finders[at] = finders[at - 1];
                at--;
            }
            finders[at] = candidate;
            if (count < maxFinders) count++;
        }

        Span<FinderGroup> groups = stackalloc FinderGroup[maxGroups];
        var groupCount = 0;
        for (var i = 0; i < count - 2; i++) {
            for (var j = i + 1; j < count - 1; j++) {
                for (var k = j + 1; k < count; k++) {
                    if (budget.IsExpired) return;
                    var a = finders[i];
                    var b = finders[j];
                    var c = finders[k];
                    var minPitch = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                    var maxPitch = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                    if (minPitch <= 0 || maxPitch > minPitch * 1.75 ||
                        !TryOrderAsTlTrBl(a, b, c, 1.8, out var tl, out var tr, out var bl)) continue;
                    var ax = tr.X - tl.X;
                    var ay = tr.Y - tl.Y;
                    var bx = bl.X - tl.X;
                    var by = bl.Y - tl.Y;
                    var sideA = ax * ax + ay * ay;
                    var sideB = bx * bx + by * by;
                    // Ranking only: distorted groups remain eligible. Independent
                    // square symbols normally rank ahead of cross-symbol triangles.
                    var score = Math.Abs(sideA - sideB) / Math.Max(sideA, sideB) +
                        Math.Abs(ax * bx + ay * by) / Math.Sqrt(sideA * sideB) +
                        (maxPitch - minPitch) / maxPitch;
                    if (groupCount == maxGroups && score >= groups[groupCount - 1].Score) continue;
                    var at = Math.Min(groupCount, maxGroups - 1);
                    while (at > 0 && score < groups[at - 1].Score) {
                        groups[at] = groups[at - 1];
                        at--;
                    }
                    groups[at] = new FinderGroup(i, j, k, score);
                    if (groupCount < maxGroups) groupCount++;
                }
            }
        }

        Span<bool> used = stackalloc bool[maxFinders];
        used.Clear();
        var tried = 0;
        var limit = budget.Enabled ? 24 : 48;
        for (var at = 0; at < groupCount && tried < limit; at++) {
            if (budget.IsExpired) return;
            var group = groups[at];
            if (used[group.A] || used[group.B] || used[group.C]) continue;
            TryOrderAsTlTrBl(finders[group.A], finders[group.B], finders[group.C], 1.8, out var tl, out var tr, out var bl);
            tried++;
            if (!TrySampleAndDecode(1, image.Threshold, image, invert, tl, tr, bl,
                candidates.Count, tried, accept: null, aggressive: false, stylized, budget, out var decoded, out _)) continue;
            AddResult(results, seen, decoded, accept: null);
            used[group.A] = used[group.B] = used[group.C] = true;
        }
    }

    private readonly struct FinderGroup {
        public readonly int A;
        public readonly int B;
        public readonly int C;
        public readonly double Score;

        public FinderGroup(int a, int b, int c, double score) {
            A = a;
            B = b;
            C = c;
            Score = score;
        }
    }
}
#endif
