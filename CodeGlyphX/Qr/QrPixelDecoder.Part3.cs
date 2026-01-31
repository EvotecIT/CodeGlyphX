#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static int GetNormalizeWindow(QrGrayImage image) {
        var minDim = Math.Min(image.Width, image.Height);
        var window = minDim / 8;
        if (window < 15) window = 15;
        if (window > 51) window = 51;
        if ((window & 1) == 0) window++;
        return window;
    }

    private static void CollectAllAtScale(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat fmt,
        int scale,
        QrProfileSettings settings,
        PooledList<QrDecoded> list,
        HashSet<byte[]> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        QrGrayImagePool? pool) {
        if (budget.IsExpired) return;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsNearDeadline(120) : null;
        if (!QrGrayImage.TryCreate(pixels, width, height, stride, fmt, scale, settings.MinContrast, shouldStop, pool, out var image)) {
            return;
        }

        CollectAllFromImage(image, settings, list, seen, accept, budget, pool);
        if (budget.IsExpired) return;

        if (settings.AllowContrastStretch) {
            var range = image.Max - image.Min;
            if (range < 48) {
                var stretched = image.WithContrastStretch(48, pool);
                if (!ReferenceEquals(stretched.Gray, image.Gray)) {
                    CollectAllFromImage(stretched, settings, list, seen, accept, budget, pool);
                }
            }
        }
    }

    private static void AddPercentileThresholds(QrGrayImage image, ref Span<byte> list, ref int count) {
        var total = image.Width * image.Height;
        if (total == 0) return;

        Span<int> histogram = stackalloc int[256];
        var gray = image.Gray;
        for (var i = 0; i < total; i++) {
            histogram[gray[i]]++;
        }

        var target25 = total * 25 / 100;
        var target50 = total / 2;
        var target75 = total * 75 / 100;
        var q25 = (byte)0;
        var q50 = (byte)0;
        var q75 = (byte)0;
        var got25 = target25 <= 0;
        var got50 = target50 <= 0;
        var got75 = target75 <= 0;
        var sum = 0;
        for (var i = 0; i < histogram.Length; i++) {
            sum += histogram[i];
            if (!got25 && sum >= target25) { q25 = (byte)i; got25 = true; }
            if (!got50 && sum >= target50) { q50 = (byte)i; got50 = true; }
            if (!got75 && sum >= target75) { q75 = (byte)i; got75 = true; }
            if (got25 && got50 && got75) break;
        }

        AddThresholdCandidate(ref list, ref count, q25);
        AddThresholdCandidate(ref list, ref count, q50);
        AddThresholdCandidate(ref list, ref count, q75);
    }

    private static void CollectFromImage(
        QrGrayImage image,
        bool invert,
        PooledList<QrDecoded> results,
        HashSet<byte[]> seen,
        Func<QrDecoded, bool>? accept,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        DecodeBudget budget,
        bool aggressive,
        bool stylized) {
        if (budget.IsExpired) return;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        var tightBudget = budget.Enabled && budget.MaxMilliseconds <= 800;
        var rowStepOverride = 0;
        var maxCandidates = 0;
        if (tightBudget) {
            var minDim = image.Width < image.Height ? image.Width : image.Height;
            rowStepOverride = minDim >= 720 ? 3 : 2;
            maxCandidates = aggressive ? 36 : 24;
        }
        QrFinderPatternDetector.FindCandidates(image, invert, candidates, aggressive, shouldStop, rowStepOverride, maxCandidates, allowFullScan: !tightBudget, requireDiagonalCheck: !tightBudget && !aggressive);
        var candidatesSorted = false;
        if (budget.Enabled && candidates.Count > 64) {
            candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
            candidates.RemoveRange(64, candidates.Count - 64);
            candidatesSorted = true;
        }
        if (budget.IsExpired) return;
        if (candidates.Count >= 3) {
            CollectFromFinderCandidates(image, invert, candidates, results, seen, accept, budget, aggressive, stylized, candidatesSorted);
        }

        if (!budget.IsExpired && (!budget.Enabled || !budget.IsNearDeadline(200))) {
            CollectFromComponents(image, invert, results, seen, accept, budget);
        }

    }

    private static void CollectFromFinderCandidates(
        QrGrayImage image,
        bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        PooledList<QrDecoded> results,
        HashSet<byte[]> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        bool aggressive,
        bool stylized,
        bool candidatesSorted) {
        var totalCandidates = candidates.Count;
        var topLimit = stylized && !budget.IsNearDeadline(300) ? 20 : stylized ? 14 : 10;
        if (candidatesSorted || totalCandidates <= topLimit) {
            if (!candidatesSorted) {
                candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
            }
            CollectFromFinderCandidatesCore(image, invert, CollectionsMarshal.AsSpan(candidates), totalCandidates, results, seen, accept, budget, aggressive, stylized);
            return;
        }

        Span<QrFinderPatternDetector.FinderPattern> top = stackalloc QrFinderPatternDetector.FinderPattern[20];
        var topCount = 0;
        foreach (var candidate in candidates) {
            var count = candidate.Count;
            if (topCount == topLimit && count <= top[topCount - 1].Count) continue;
            var insertPos = topCount < topLimit ? topCount : topLimit - 1;
            while (insertPos > 0 && count > top[insertPos - 1].Count) {
                if (insertPos < topLimit) {
                    top[insertPos] = top[insertPos - 1];
                }
                insertPos--;
            }
            top[insertPos] = candidate;
            if (topCount < topLimit) topCount++;
        }
        CollectFromFinderCandidatesCore(image, invert, top.Slice(0, topCount), totalCandidates, results, seen, accept, budget, aggressive, stylized);
    }

    private static void CollectFromFinderCandidatesCore(
        QrGrayImage image,
        bool invert,
        ReadOnlySpan<QrFinderPatternDetector.FinderPattern> candidateSpan,
        int totalCandidates,
        PooledList<QrDecoded> results,
        HashSet<byte[]> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        bool aggressive,
        bool stylized) {
        var n = Math.Min(candidateSpan.Length, stylized && !budget.IsNearDeadline(300) ? 20 : stylized ? 14 : 10);
        var triedTriples = 0;
        var maxTriples = budget.Enabled ? 24 : 48;
        if (stylized && !budget.IsNearDeadline(300)) {
            maxTriples = Math.Max(maxTriples, budget.Enabled ? 200 : 280);
        }

        var useNeighborGate = false;
        var minLink2 = 0.0;
        var maxLink2 = 0.0;
        var linkScale2 = 0.0;
        Span<double> nnDist2 = stackalloc double[0];
        if (stylized && n >= 9 && !budget.IsNearDeadline(260)) {
            nnDist2 = stackalloc double[n];
            useNeighborGate = TryBuildNeighborGate(candidateSpan.Slice(0, n), stylized, nnDist2, out minLink2, out maxLink2, out linkScale2);
        }

        if (stylized && candidateSpan.Length >= 9 && !budget.IsNearDeadline(260)) {
            CollectFromCandidateClusters(
                scale: 1,
                threshold: image.Threshold,
                image,
                invert,
                candidateSpan,
                totalCandidates,
                results,
                seen,
                accept,
                budget,
                aggressive,
                stylized);
            if (budget.IsExpired || budget.IsNearDeadline(180)) return;
        }

        Span<ScoredTriple> scoredTriples = stackalloc ScoredTriple[8];
        var scoredCount = 0;
        var useTimingScore = stylized && !budget.IsNearDeadline(350) && n >= 6;
        if (useTimingScore) {
            var scoreScanLimit = Math.Min(maxTriples * 2, 120);
            var scanned = 0;
            for (var i = 0; i < n - 2 && scanned < scoreScanLimit; i++) {
                for (var j = i + 1; j < n - 1 && scanned < scoreScanLimit; j++) {
                    for (var k = j + 1; k < n && scanned < scoreScanLimit; k++) {
                        if (budget.IsExpired) return;
                        scanned++;

                        var a = candidateSpan[i];
                        var b = candidateSpan[j];
                        var c = candidateSpan[k];
                        if (useNeighborGate && !PassesNeighborGate(candidateSpan, nnDist2, i, j, k, minLink2, maxLink2, linkScale2)) {
                            continue;
                        }

                        var msMin = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                        var msMax = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                        if (msMin <= 0) continue;
                        var moduleRatioLimit = stylized && !budget.IsNearDeadline(250) ? 2.6 : 1.75;
                        if (msMax > msMin * moduleRatioLimit) continue;

                        var ratioLimit = stylized && !budget.IsNearDeadline(250) ? 2.8 : 1.8;
                        if (!TryOrderAsTlTrBl(a, b, c, ratioLimit, out var tl, out var tr, out var bl)) continue;

                        if (!TryEstimateDimensionForTriple(tl, tr, bl, msMin, out var dimension)) continue;
                        if (dimension < 25) continue;

                        var score = ComputeTimingScore(image, invert, tl, tr, bl, dimension, msMin);
                        if (score <= 0) continue;
                        InsertScoredTriple(ref scoredTriples, ref scoredCount, i, j, k, score);
                    }
                }
            }
        }

        var stop = false;
        if (scoredCount > 0) {
            for (var s = 0; s < scoredCount; s++) {
                if (budget.IsExpired) return;
                if (triedTriples >= maxTriples) { stop = true; break; }

                var scored = scoredTriples[s];
                var a = candidateSpan[scored.I];
                var b = candidateSpan[scored.J];
                var c = candidateSpan[scored.K];

                var msMin = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                var msMax = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                if (msMin <= 0) continue;
                var moduleRatioLimit = stylized && !budget.IsNearDeadline(250) ? 2.6 : 1.75;
                if (msMax > msMin * moduleRatioLimit) continue;

                var ratioLimit = stylized && !budget.IsNearDeadline(250) ? 2.8 : 1.8;
                if (!TryOrderAsTlTrBl(a, b, c, ratioLimit, out var tl, out var tr, out var bl)) continue;

                triedTriples++;
                if (TrySampleAndDecode(
                        scale: 1,
                        threshold: image.Threshold,
                        image,
                        invert,
                        tl,
                        tr,
                        bl,
                        totalCandidates,
                        triedTriples,
                        accept,
                        aggressive,
                        stylized,
                        budget,
                        out var decoded,
                        out _)) {
                    AddResult(results, seen, decoded, accept);
                }
            }
        }

        for (var i = 0; i < n - 2 && !stop; i++) {
            for (var j = i + 1; j < n - 1 && !stop; j++) {
                for (var k = j + 1; k < n; k++) {
                    if (budget.IsExpired) return;
                    if (useTimingScore && IsScoredTriple(scoredTriples, scoredCount, i, j, k)) continue;
                    if (triedTriples++ >= maxTriples) { stop = true; break; }

                    var a = candidateSpan[i];
                    var b = candidateSpan[j];
                    var c = candidateSpan[k];
                    if (useNeighborGate && !PassesNeighborGate(candidateSpan, nnDist2, i, j, k, minLink2, maxLink2, linkScale2)) {
                        continue;
                    }

                    var msMin = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                    var msMax = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                    if (msMin <= 0) continue;
                    var moduleRatioLimit = stylized && !budget.IsNearDeadline(250) ? 2.6 : 1.75;
                    if (msMax > msMin * moduleRatioLimit) continue;

                    var ratioLimit = stylized && !budget.IsNearDeadline(250) ? 2.8 : 1.8;
                    if (!TryOrderAsTlTrBl(a, b, c, ratioLimit, out var tl, out var tr, out var bl)) continue;
                    if (TrySampleAndDecode(
                            scale: 1,
                            threshold: image.Threshold,
                            image,
                            invert,
                            tl,
                            tr,
                            bl,
                            totalCandidates,
                            triedTriples,
                            accept,
                            aggressive,
                            stylized,
                            budget,
                            out var decoded,
                            out _)) {
                        AddResult(results, seen, decoded, accept);
                    }
                }
            }
        }
    }

    private readonly struct ScoredTriple {
        public readonly int I;
        public readonly int J;
        public readonly int K;
        public readonly int Score;

        public ScoredTriple(int i, int j, int k, int score) {
            I = i;
            J = j;
            K = k;
            Score = score;
        }
    }

    private static bool TryBuildNeighborGate(
        ReadOnlySpan<QrFinderPatternDetector.FinderPattern> candidates,
        bool stylized,
        Span<double> nnDist2,
        out double minLink2,
        out double maxLink2,
        out double linkScale2) {
        minLink2 = 0;
        maxLink2 = 0;
        linkScale2 = 0;

        Span<double> sizes = stackalloc double[32];
        var sizeCount = 0;
        for (var i = 0; i < candidates.Length && sizeCount < sizes.Length; i++) {
            var ms = candidates[i].ModuleSize;
            if (ms > 0) sizes[sizeCount++] = ms;
        }
        if (sizeCount == 0) return false;

        var ordered = sizes.Slice(0, sizeCount).ToArray();
        Array.Sort(ordered);
        var medianSize = ordered[ordered.Length / 2];
        if (medianSize <= 0) return false;

        var useTighterGate = stylized && candidates.Length >= 10;
        var stylizedMin = useTighterGate ? 20.0 : 26.0;
        var stylizedMax = useTighterGate ? 32.0 : 42.0;
        var minLink = Math.Max(24.0, medianSize * (stylized ? stylizedMin : 22.0));
        var maxLink = Math.Max(minLink, medianSize * (stylized ? stylizedMax : 34.0));
        minLink2 = minLink * minLink;
        maxLink2 = maxLink * maxLink;
        var linkScale = useTighterGate ? 1.45 : (stylized ? 1.6 : 1.45);
        linkScale2 = linkScale * linkScale;

        for (var i = 0; i < nnDist2.Length; i++) {
            nnDist2[i] = double.PositiveInfinity;
        }
        for (var i = 0; i < candidates.Length - 1; i++) {
            var a = candidates[i];
            for (var j = i + 1; j < candidates.Length; j++) {
                var b = candidates[j];
                var dX = b.X - a.X;
                var dY = b.Y - a.Y;
                var d2 = dX * dX + dY * dY;
                if (d2 < nnDist2[i]) nnDist2[i] = d2;
                if (d2 < nnDist2[j]) nnDist2[j] = d2;
            }
        }

        return true;
    }

    private static bool PassesNeighborGate(
        ReadOnlySpan<QrFinderPatternDetector.FinderPattern> candidates,
        ReadOnlySpan<double> nnDist2,
        int i,
        int j,
        int k,
        double minLink2,
        double maxLink2,
        double linkScale2) {
        var a = candidates[i];
        var b = candidates[j];
        var c = candidates[k];

        var dAB = Dist2(a, b);
        var dAC = Dist2(a, c);
        var dBC = Dist2(b, c);

        var limitA = NeighborLimit(nnDist2, i, minLink2, maxLink2, linkScale2);
        if (dAB > limitA && dAC > limitA) return false;
        var limitB = NeighborLimit(nnDist2, j, minLink2, maxLink2, linkScale2);
        if (dAB > limitB && dBC > limitB) return false;
        var limitC = NeighborLimit(nnDist2, k, minLink2, maxLink2, linkScale2);
        if (dAC > limitC && dBC > limitC) return false;

        return true;
    }

    private static double NeighborLimit(
        ReadOnlySpan<double> nnDist2,
        int idx,
        double minLink2,
        double maxLink2,
        double linkScale2) {
        var base2 = nnDist2[idx];
        if (double.IsInfinity(base2) || base2 <= 0) base2 = maxLink2;
        var limit2 = base2 * linkScale2;
        if (limit2 < minLink2) limit2 = minLink2;
        if (limit2 > maxLink2) limit2 = maxLink2;
        return limit2;
    }

    private static void InsertScoredTriple(ref Span<ScoredTriple> list, ref int count, int i, int j, int k, int score) {
        var insertPos = count;
        if (insertPos >= list.Length && score <= list[list.Length - 1].Score) return;

        if (insertPos >= list.Length) insertPos = list.Length - 1;
        while (insertPos > 0 && score > list[insertPos - 1].Score) {
            if (insertPos < list.Length) {
                list[insertPos] = list[insertPos - 1];
            }
            insertPos--;
        }
        list[insertPos] = new ScoredTriple(i, j, k, score);
        if (count < list.Length) count++;
    }

    private static bool IsScoredTriple(ReadOnlySpan<ScoredTriple> list, int count, int i, int j, int k) {
        for (var idx = 0; idx < count; idx++) {
            var t = list[idx];
            if (t.I == i && t.J == j && t.K == k) return true;
        }
        return false;
    }

    private static bool TryEstimateDimensionForTriple(
        QrFinderPatternDetector.FinderPattern tl,
        QrFinderPatternDetector.FinderPattern tr,
        QrFinderPatternDetector.FinderPattern bl,
        double moduleSize,
        out int dimension) {
        dimension = 0;
        if (moduleSize <= 0) return false;
        var distX = Distance(tl.X, tl.Y, tr.X, tr.Y);
        var distY = Distance(tl.X, tl.Y, bl.X, bl.Y);
        if (distX <= 0 || distY <= 0) return false;
        var dimH = QrMath.RoundToInt(distX / moduleSize) + 7;
        var dimV = QrMath.RoundToInt(distY / moduleSize) + 7;
        dimension = NearestValidDimension((dimH + dimV) / 2);
        return dimension is >= 21 and <= 177;
    }

    private static int ComputeTimingScore(
        QrGrayImage image,
        bool invert,
        QrFinderPatternDetector.FinderPattern tl,
        QrFinderPatternDetector.FinderPattern tr,
        QrFinderPatternDetector.FinderPattern bl,
        int dimension,
        double moduleSize) {
        if (dimension is < 21 or > 177) return -1;
        var modulesBetweenCenters = dimension - 7;
        if (modulesBetweenCenters <= 0) return -1;

        var vxX = (tr.X - tl.X) / modulesBetweenCenters;
        var vxY = (tr.Y - tl.Y) / modulesBetweenCenters;
        var vyX = (bl.X - tl.X) / modulesBetweenCenters;
        var vyY = (bl.Y - tl.Y) / modulesBetweenCenters;

        const double finderCenterToCorner = 3.5;
        var cornerTlX = tl.X - (vxX + vyX) * finderCenterToCorner;
        var cornerTlY = tl.Y - (vxY + vyY) * finderCenterToCorner;

        var cornerTrX = cornerTlX + vxX * dimension;
        var cornerTrY = cornerTlY + vxY * dimension;

        var cornerBlX = cornerTlX + vyX * dimension;
        var cornerBlY = cornerTlY + vyY * dimension;

        var cornerBrX = cornerTlX + (vxX + vyX) * dimension;
        var cornerBrY = cornerTlY + (vxY + vyY) * dimension;

        var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0,
            dimension, 0,
            dimension, dimension,
            0, dimension,
            cornerTlX, cornerTlY,
            cornerTrX, cornerTrY,
            cornerBrX, cornerBrY,
            cornerBlX, cornerBlY);

        var start = 8;
        var end = dimension - 9;
        if (end <= start) return -1;

        var delta = QrPixelSampling.GetSampleDeltaCenterForModule(moduleSize);

        if (!TrySampleTimingModule(image, invert, transform, start + 0.5, 6.5, delta, out var lastRow)) {
            return -1;
        }

        var rowAlt = 0;
        for (var x = start + 1; x <= end; x++) {
            if (!TrySampleTimingModule(image, invert, transform, x + 0.5, 6.5, delta, out var v)) {
                return -1;
            }
            if (v != lastRow) rowAlt++;
            lastRow = v;
        }

        if (!TrySampleTimingModule(image, invert, transform, 6.5, start + 0.5, delta, out var lastCol)) {
            return -1;
        }

        var colAlt = 0;
        for (var y = start + 1; y <= end; y++) {
            if (!TrySampleTimingModule(image, invert, transform, 6.5, y + 0.5, delta, out var v)) {
                return -1;
            }
            if (v != lastCol) colAlt++;
            lastCol = v;
        }

        return rowAlt + colAlt;
    }

    private static bool TrySampleTimingModule(QrGrayImage image, bool invert, in QrPerspectiveTransform transform, double x, double y, double delta, out bool value) {
        transform.Transform(x, y, out var sx, out var sy);
        if (!double.IsFinite(sx + sy)) {
            value = false;
            return false;
        }

        value = QrPixelSampling.SampleModuleCenter3x3WithDelta(image, sx, sy, invert, delta);
        return true;
    }

    private static void CollectFromComponents(
        QrGrayImage image,
        bool invert,
        PooledList<QrDecoded> results,
        HashSet<byte[]> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget) {
        if (budget.IsExpired) return;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        using var comps = FindComponents(image, invert, shouldStop, minAreaDivisor: 400, minComponentSize: 21);
        if (comps.Count == 0) return;

        const int maxTry = 12;
        Span<Component> top = stackalloc Component[maxTry];
        var topCount = 0;
        for (var i = 0; i < comps.Count; i++) {
            var comp = comps[i];
            var area = comp.Area;
            if (topCount == maxTry && area <= top[topCount - 1].Area) continue;
            var insertPos = topCount < maxTry ? topCount : maxTry - 1;
            while (insertPos > 0 && area > top[insertPos - 1].Area) {
                if (insertPos < maxTry) {
                    top[insertPos] = top[insertPos - 1];
                }
                insertPos--;
            }
            top[insertPos] = comp;
            if (topCount < maxTry) topCount++;
        }

        for (var i = 0; i < topCount; i++) {
            if (budget.IsExpired || budget.IsNearDeadline(120)) return;
            var c = top[i];
            var pad = Math.Max(2, (int)Math.Round(Math.Min(c.Width, c.Height) * 0.05));
            var bminX = c.MinX - pad;
            var bminY = c.MinY - pad;
            var bmaxX = c.MaxX + pad;
            var bmaxY = c.MaxY + pad;

            if (bminX < 0) bminX = 0;
            if (bminY < 0) bminY = 0;
            if (bmaxX >= image.Width) bmaxX = image.Width - 1;
            if (bmaxY >= image.Height) bmaxY = image.Height - 1;

            if (TryDecodeByBoundingBox(scale: 1, threshold: image.Threshold, image, invert, accept, aggressive: false, stylized: false, budget, out var decoded, out _, candidateCount: 0, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY)) {
                AddResult(results, seen, decoded, accept);
            }
        }
    }

    private static void AddResult(PooledList<QrDecoded> results, HashSet<byte[]> seen, QrDecoded decoded, Func<QrDecoded, bool>? accept) {
        if (accept is not null && !accept(decoded)) return;
        if (!seen.Add(decoded.Bytes)) return;
        results.Add(decoded);
    }

    private sealed class PooledList<T> : IDisposable {
        private T[] _buffer;
        public int Count { get; private set; }

        public PooledList(int capacity) {
            if (capacity < 1) capacity = 1;
            _buffer = ArrayPool<T>.Shared.Rent(capacity);
        }

        public void Add(T item) {
            if (Count == _buffer.Length) Grow();
            _buffer[Count++] = item;
        }

        public T this[int index] {
            get => _buffer[index];
            set => _buffer[index] = value;
        }

        public void Sort(Comparison<T> comparison) {
            Array.Sort(_buffer, 0, Count, Comparer<T>.Create(comparison));
        }

        public T[] ToArray() {
            if (Count == 0) return Array.Empty<T>();
            var result = new T[Count];
            Array.Copy(_buffer, 0, result, 0, Count);
            return result;
        }

        public void Dispose() {
            ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
            _buffer = Array.Empty<T>();
            Count = 0;
        }

        private void Grow() {
            var next = ArrayPool<T>.Shared.Rent(_buffer.Length * 2);
            Array.Copy(_buffer, 0, next, 0, _buffer.Length);
            ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
            _buffer = next;
        }
    }

    private static bool TryDecodeFromGray(int scale, byte threshold, QrGrayImage image, bool invert, List<QrFinderPatternDetector.FinderPattern> candidates, Func<QrDecoded, bool>? accept, bool aggressive, bool stylized, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        // Finder-based sampling (robust to extra background/noise). Try multiple triples when the region contains UI/text.
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        var tightBudget = budget.Enabled && budget.MaxMilliseconds <= 800;
        var rowStepOverride = 0;
        if (tightBudget) {
            var minDim = image.Width < image.Height ? image.Width : image.Height;
            rowStepOverride = minDim >= 720 ? 3 : 2;
        } else if (aggressive) {
            rowStepOverride = 1;
        }
        var maxCandidates = 0;
        if (tightBudget) {
            maxCandidates = aggressive ? 36 : 24;
        }
        QrFinderPatternDetector.FindCandidates(image, invert, candidates, aggressive, shouldStop, rowStepOverride, maxCandidates, allowFullScan: !tightBudget, requireDiagonalCheck: !tightBudget && !aggressive);
        var candidatesSorted = false;
        if (budget.Enabled && candidates.Count > 64) {
            candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
            candidates.RemoveRange(64, candidates.Count - 64);
            candidatesSorted = true;
        }
        var tightCandidateLimit = aggressive ? 36 : 30;
        if (tightBudget && candidates.Count > tightCandidateLimit) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidates.Count, candidateTriplesTried: 0, dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload));
            return false;
        }

        if (candidates.Count >= 3) {
            var tightBoundsLimit = aggressive ? 32 : 16;
            if (tightBudget && candidates.Count > tightBoundsLimit) {
                if (TryDecodeByCandidateBounds(scale, threshold, image, invert, candidates, accept, aggressive, stylized, budget, out result, out var diagBounds)) {
                    diagnostics = diagBounds;
                    return true;
                }
                diagnostics = Better(diagnostics, diagBounds);
                return false;
            }
            if (stylized && !tightBudget && candidates.Count >= 9 && !budget.IsNearDeadline(200)) {
                if (TryDecodeByCandidateClusters(scale, threshold, image, invert, candidates, accept, aggressive, stylized, budget, out result, out var diagClusters)) {
                    diagnostics = diagClusters;
                    return true;
                }
                diagnostics = Better(diagnostics, diagClusters);
            }
            if (TryDecodeFromFinderCandidates(scale, threshold, image, invert, candidates, candidatesSorted, accept, aggressive, stylized, budget, out result, out var diagF)) {
                diagnostics = diagF;
                return true;
            }
            candidatesSorted = true;
            diagnostics = Better(diagnostics, diagF);

            if (TryDecodeByCandidateBounds(scale, threshold, image, invert, candidates, accept, aggressive, stylized, budget, out result, out var diagC)) {
                diagnostics = diagC;
                return true;
            }
            diagnostics = Better(diagnostics, diagC);

            if (aggressive && !budget.IsNearDeadline(200)) {
                var diagRelaxed = default(QrPixelDecodeDiagnostics);
                if (TryDecodeFromFinderCandidates(
                        scale,
                        threshold,
                        image,
                        invert,
                        candidates,
                        candidatesSorted,
                        accept,
                        aggressive,
                        stylized,
                        budget,
                        out result,
                        out diagRelaxed,
                        moduleRatioLimit: 2.4,
                        sideRatioLimit: 2.4,
                        maxTriplesOverride: 320,
                        maxCandidatesOverride: 24)) {
                    diagnostics = diagRelaxed;
                    return true;
                }
                candidatesSorted = true;
                diagnostics = Better(diagnostics, diagRelaxed);
            }

            if (stylized && !budget.IsNearDeadline(260)) {
                var diagStylizedRelaxed = default(QrPixelDecodeDiagnostics);
                if (TryDecodeFromFinderCandidates(
                        scale,
                        threshold,
                        image,
                        invert,
                        candidates,
                        candidatesSorted,
                        accept,
                        aggressive,
                        stylized,
                        budget,
                        out result,
                        out diagStylizedRelaxed,
                        moduleRatioLimit: 3.2,
                        sideRatioLimit: 3.2,
                        maxTriplesOverride: 420,
                        maxCandidatesOverride: 32)) {
                    diagnostics = diagStylizedRelaxed;
                    return true;
                }
                candidatesSorted = true;
                diagnostics = Better(diagnostics, diagStylizedRelaxed);
            }

            if (stylized && !budget.IsNearDeadline(300)) {
                if (!candidatesSorted) {
                    candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
                    candidatesSorted = true;
                }
                var diagStylizedLoose = default(QrPixelDecodeDiagnostics);
                if (TryDecodeFromFinderCandidates(
                        scale,
                        threshold,
                        image,
                        invert,
                        candidates,
                        candidatesSorted,
                        accept,
                        aggressive,
                        stylized,
                        budget,
                        out result,
                        out diagStylizedLoose,
                        moduleRatioLimit: 0.0,
                        sideRatioLimit: 5.0,
                        maxTriplesOverride: 200,
                        maxCandidatesOverride: 20)) {
                    diagnostics = diagStylizedLoose;
                    return true;
                }
                diagnostics = Better(diagnostics, diagStylizedLoose);
            }
        }

        if (aggressive && !budget.IsNearDeadline(200)) {
            var diagComponents = default(QrPixelDecodeDiagnostics);
            if (TryDecodeFromComponentFinders(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagComponents)) {
                diagnostics = diagComponents;
                return true;
            }
            diagnostics = Better(diagnostics, diagComponents);
        }

        if (aggressive && !budget.IsNearDeadline(250)) {
            var diagTemplate = default(QrPixelDecodeDiagnostics);
            if (TryDecodeFromTemplateFinders(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagTemplate)) {
                diagnostics = diagTemplate;
                return true;
            }
            diagnostics = Better(diagnostics, diagTemplate);

            if (!budget.IsNearDeadline(250)) {
                var closed = image.WithBinaryClose(1);
                if (!ReferenceEquals(closed.Gray, image.Gray)) {
                    if (TryDecodeFromTemplateFinders(scale, closed.Threshold, closed, invert, accept, aggressive, stylized, budget, out result, out diagTemplate)) {
                        diagnostics = diagTemplate;
                        return true;
                    }
                    diagnostics = Better(diagnostics, diagTemplate);
                }
            }

            if (!budget.IsNearDeadline(250)) {
                var edged = image.WithBinaryEdge(1);
                if (!ReferenceEquals(edged.Gray, image.Gray)) {
                    if (TryDecodeFromTemplateFinders(scale, edged.Threshold, edged, invert, accept, aggressive, stylized, budget, out result, out diagTemplate)) {
                        diagnostics = diagTemplate;
                        return true;
                    }
                    diagnostics = Better(diagnostics, diagTemplate);
                }
            }

            if (stylized && !budget.IsNearDeadline(250)) {
                var edged2 = image.WithBinaryEdge(2);
                if (!ReferenceEquals(edged2.Gray, image.Gray)) {
                    if (TryDecodeFromTemplateFinders(scale, edged2.Threshold, edged2, invert, accept, aggressive, stylized, budget, out result, out diagTemplate)) {
                        diagnostics = diagTemplate;
                        return true;
                    }
                    diagnostics = Better(diagnostics, diagTemplate);
                }
            }
        }

        if (candidates.Count > 0) {
            if (budget.Enabled && budget.MaxMilliseconds <= 800) {
                return false;
            }
            var maxSingleFinder = stylized ? (aggressive ? 80 : 64) : (aggressive ? 20 : 30);
            if (candidates.Count <= maxSingleFinder) {
                if (TryDecodeBySingleFinder(scale, threshold, image, invert, candidates, accept, aggressive, stylized, budget, out result, out var diagS)) {
                    diagnostics = diagS;
                    return true;
                }
                diagnostics = Better(diagnostics, diagS);
            }
        }

        if (budget.Enabled && budget.MaxMilliseconds <= 800) {
            return false;
        }

        var skipFallbacks = candidates.Count >= (aggressive ? 48 : 64);
        if (skipFallbacks) {
            return false;
        }

        // Connected-components fallback (helps when finder detection fails but a clean symbol exists).
        var diagCC = default(QrPixelDecodeDiagnostics);
        if (!budget.IsExpired && TryDecodeByConnectedComponents(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagCC)) {
            diagnostics = diagCC;
            return true;
        }
        diagnostics = Better(diagnostics, diagCC);

        if (aggressive && !budget.IsNearDeadline(200)) {
            var closed = image.WithBinaryClose(1);
            var diagClosed = default(QrPixelDecodeDiagnostics);
            if (!budget.IsExpired && TryDecodeByConnectedComponents(scale, closed.Threshold, closed, invert, accept, aggressive, stylized, budget, out result, out diagClosed)) {
                diagnostics = diagClosed;
                return true;
            }
            diagnostics = Better(diagnostics, diagClosed);

            if (!budget.IsNearDeadline(200)) {
                var closed2 = image.WithBinaryClose(2);
                var diagClosed2 = default(QrPixelDecodeDiagnostics);
                if (!budget.IsExpired && TryDecodeByConnectedComponents(scale, closed2.Threshold, closed2, invert, accept, aggressive, stylized, budget, out result, out diagClosed2)) {
                    diagnostics = diagClosed2;
                    return true;
                }
                diagnostics = Better(diagnostics, diagClosed2);
            }
        }

        if (aggressive && !budget.IsNearDeadline(200)) {
            var edge = image.WithBinaryEdge(1);
            var diagEdge = default(QrPixelDecodeDiagnostics);
            if (!budget.IsExpired && TryDecodeByConnectedComponents(scale, edge.Threshold, edge, invert, accept, aggressive, stylized, budget, out result, out diagEdge)) {
                diagnostics = diagEdge;
                return true;
            }
            diagnostics = Better(diagnostics, diagEdge);

            if (!budget.IsNearDeadline(200)) {
                var edgeClosed = edge.WithBinaryClose(1);
                var diagEdgeClosed = default(QrPixelDecodeDiagnostics);
                if (!budget.IsExpired && TryDecodeByConnectedComponents(scale, edgeClosed.Threshold, edgeClosed, invert, accept, aggressive, stylized, budget, out result, out diagEdgeClosed)) {
                    diagnostics = diagEdgeClosed;
                    return true;
                }
                diagnostics = Better(diagnostics, diagEdgeClosed);
            }
        }

        if (aggressive && !budget.IsNearDeadline(250)) {
            var diagSquares = default(QrPixelDecodeDiagnostics);
            if (TryDecodeFromEdgeSquares(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagSquares)) {
                diagnostics = diagSquares;
                return true;
            }
            diagnostics = Better(diagnostics, diagSquares);
        }

        // Fallback: bounding box exact-fit (works for perfectly cropped/generated images).
        var diagB = default(QrPixelDecodeDiagnostics);
        if (!budget.IsExpired && TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagB)) {
            diagnostics = diagB;
            return true;
        }
        diagnostics = Better(diagnostics, diagB);

        return false;
    }

    private static bool TryDecodeByCandidateBounds(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Count == 0) return false;
        if (budget.IsExpired || budget.IsNearDeadline(120)) return false;

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        var maxModule = 0.0;

        for (var i = 0; i < candidates.Count; i++) {
            var c = candidates[i];
            if (c.ModuleSize > maxModule) maxModule = c.ModuleSize;
            if (c.X < minX) minX = c.X;
            if (c.Y < minY) minY = c.Y;
            if (c.X > maxX) maxX = c.X;
            if (c.Y > maxY) maxY = c.Y;
        }

        if (double.IsInfinity(minX) || double.IsInfinity(minY)) return false;

        var pad = maxModule > 0 ? maxModule * (stylized ? 8.0 : 6.0) : 12.0;

        static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        var bminX = Clamp(QrMath.RoundToInt(minX - pad), 0, image.Width - 1);
        var bminY = Clamp(QrMath.RoundToInt(minY - pad), 0, image.Height - 1);
        var bmaxX = Clamp(QrMath.RoundToInt(maxX + pad), 0, image.Width - 1);
        var bmaxY = Clamp(QrMath.RoundToInt(maxY + pad), 0, image.Height - 1);

        if (bmaxX <= bminX || bmaxY <= bminY) return false;

        return TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagnostics, candidates.Count, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY);
    }

    private static bool TryDecodeByCandidateClusters(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Count < 3) return false;
        if (budget.IsExpired || budget.IsNearDeadline(120)) return false;

        Span<double> sizes = stackalloc double[Math.Min(candidates.Count, 64)];
        var sizeCount = 0;
        for (var i = 0; i < candidates.Count && sizeCount < sizes.Length; i++) {
            var ms = candidates[i].ModuleSize;
            if (ms > 0) sizes[sizeCount++] = ms;
        }
        if (sizeCount == 0) return false;

        var ordered = sizes.Slice(0, sizeCount).ToArray();
        Array.Sort(ordered);
        var medianSize = ordered[ordered.Length / 2];
        if (medianSize <= 0) return false;

        var useTighterGate = stylized && candidates.Count >= 10;
        var stylizedMin = useTighterGate ? 20.0 : 26.0;
        var stylizedMax = useTighterGate ? 32.0 : 42.0;
        var minLink = Math.Max(24.0, medianSize * (stylized ? stylizedMin : 22.0));
        var maxLink = Math.Max(minLink, medianSize * (stylized ? stylizedMax : 34.0));
        var minLink2 = minLink * minLink;
        var maxLink2 = maxLink * maxLink;
        var linkScale = useTighterGate ? 1.45 : (stylized ? 1.6 : 1.45);
        var linkScale2 = linkScale * linkScale;

        var nnDist2 = new double[candidates.Count];
        for (var i = 0; i < candidates.Count; i++) {
            nnDist2[i] = double.PositiveInfinity;
        }
        for (var i = 0; i < candidates.Count - 1; i++) {
            var a = candidates[i];
            for (var j = i + 1; j < candidates.Count; j++) {
                var b = candidates[j];
                var dX = b.X - a.X;
                var dY = b.Y - a.Y;
                var d2 = dX * dX + dY * dY;
                if (d2 < nnDist2[i]) nnDist2[i] = d2;
                if (d2 < nnDist2[j]) nnDist2[j] = d2;
            }
        }

        var visited = new bool[candidates.Count];
        var queue = new int[candidates.Count];

        for (var i = 0; i < candidates.Count; i++) {
            if (visited[i]) continue;
            if (budget.IsExpired || budget.IsNearDeadline(140)) break;

            var qHead = 0;
            var qTail = 0;
            visited[i] = true;
            queue[qTail++] = i;

            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            var maxModule = 0.0;
            var clusterCount = 0;

            while (qHead < qTail) {
                var idx = queue[qHead++];
                var c = candidates[idx];
                clusterCount++;
                if (c.ModuleSize > maxModule) maxModule = c.ModuleSize;
                if (c.X < minX) minX = c.X;
                if (c.Y < minY) minY = c.Y;
                if (c.X > maxX) maxX = c.X;
                if (c.Y > maxY) maxY = c.Y;

                for (var j = 0; j < candidates.Count; j++) {
                    if (visited[j]) continue;
                    var dX = candidates[j].X - c.X;
                    var dY = candidates[j].Y - c.Y;
                    var d2 = dX * dX + dY * dY;
                    var link2 = Math.Min(nnDist2[idx], nnDist2[j]);
                    if (double.IsInfinity(link2) || link2 <= 0) link2 = maxLink2;
                    link2 *= linkScale2;
                    if (link2 < minLink2) link2 = minLink2;
                    if (link2 > maxLink2) link2 = maxLink2;
                    if (d2 <= link2) {
                        visited[j] = true;
                        queue[qTail++] = j;
                    }
                }
            }

            if (clusterCount < 3 || double.IsInfinity(minX) || double.IsInfinity(minY)) continue;

            var clusterList = new List<QrFinderPatternDetector.FinderPattern>(clusterCount);
            for (var q = 0; q < qTail; q++) {
                clusterList.Add(candidates[queue[q]]);
            }
            if (clusterList.Count >= 3) {
                if (TryDecodeFromFinderCandidates(scale, threshold, image, invert, clusterList, candidatesSorted: false, accept, aggressive, stylized, budget, out result, out var diagCluster)) {
                    diagnostics = diagCluster;
                    return true;
                }
                diagnostics = Better(diagnostics, diagCluster);
            }

            var pad = maxModule > 0 ? maxModule * (stylized ? 8.0 : 6.0) : 12.0;
            static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

            var bminX = Clamp(QrMath.RoundToInt(minX - pad), 0, image.Width - 1);
            var bminY = Clamp(QrMath.RoundToInt(minY - pad), 0, image.Height - 1);
            var bmaxX = Clamp(QrMath.RoundToInt(maxX + pad), 0, image.Width - 1);
            var bmaxY = Clamp(QrMath.RoundToInt(maxY + pad), 0, image.Height - 1);
            if (bmaxX <= bminX || bmaxY <= bminY) continue;

            if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagnostics, clusterCount, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY)) {
                return true;
            }
        }
        return false;
    }

    private static void CollectFromCandidateClusters(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        ReadOnlySpan<QrFinderPatternDetector.FinderPattern> candidates,
        int totalCandidates,
        PooledList<QrDecoded> results,
        HashSet<byte[]> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        bool aggressive,
        bool stylized) {
        if (candidates.Length < 3) return;
        if (budget.IsExpired || budget.IsNearDeadline(200)) return;

        Span<double> sizes = stackalloc double[Math.Min(candidates.Length, 64)];
        var sizeCount = 0;
        for (var i = 0; i < candidates.Length && sizeCount < sizes.Length; i++) {
            var ms = candidates[i].ModuleSize;
            if (ms > 0) sizes[sizeCount++] = ms;
        }
        if (sizeCount == 0) return;

        var ordered = sizes.Slice(0, sizeCount).ToArray();
        Array.Sort(ordered);
        var medianSize = ordered[ordered.Length / 2];
        if (medianSize <= 0) return;

        var minLink = Math.Max(24.0, medianSize * (stylized ? 20.0 : 22.0));
        var maxLink = Math.Max(minLink, medianSize * (stylized ? 32.0 : 34.0));
        var minLink2 = minLink * minLink;
        var maxLink2 = maxLink * maxLink;
        var linkScale = stylized ? 1.4 : 1.45;
        var linkScale2 = linkScale * linkScale;

        var nnDist2 = new double[candidates.Length];
        for (var i = 0; i < candidates.Length; i++) {
            nnDist2[i] = double.PositiveInfinity;
        }
        for (var i = 0; i < candidates.Length - 1; i++) {
            var a = candidates[i];
            for (var j = i + 1; j < candidates.Length; j++) {
                var b = candidates[j];
                var dX = b.X - a.X;
                var dY = b.Y - a.Y;
                var d2 = dX * dX + dY * dY;
                if (d2 < nnDist2[i]) nnDist2[i] = d2;
                if (d2 < nnDist2[j]) nnDist2[j] = d2;
            }
        }

        var visited = new bool[candidates.Length];
        var queue = new int[candidates.Length];

        for (var i = 0; i < candidates.Length; i++) {
            if (visited[i]) continue;
            if (budget.IsExpired || budget.IsNearDeadline(220)) break;

            var qHead = 0;
            var qTail = 0;
            visited[i] = true;
            queue[qTail++] = i;

            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            var maxModule = 0.0;
            var clusterCount = 0;

            while (qHead < qTail) {
                var idx = queue[qHead++];
                var c = candidates[idx];
                clusterCount++;
                if (c.ModuleSize > maxModule) maxModule = c.ModuleSize;
                if (c.X < minX) minX = c.X;
                if (c.Y < minY) minY = c.Y;
                if (c.X > maxX) maxX = c.X;
                if (c.Y > maxY) maxY = c.Y;

                for (var j = 0; j < candidates.Length; j++) {
                    if (visited[j]) continue;
                    var dX = candidates[j].X - c.X;
                    var dY = candidates[j].Y - c.Y;
                    var d2 = dX * dX + dY * dY;
                    var link2 = Math.Min(nnDist2[idx], nnDist2[j]);
                    if (double.IsInfinity(link2) || link2 <= 0) link2 = maxLink2;
                    link2 *= linkScale2;
                    if (link2 < minLink2) link2 = minLink2;
                    if (link2 > maxLink2) link2 = maxLink2;
                    if (d2 <= link2) {
                        visited[j] = true;
                        queue[qTail++] = j;
                    }
                }
            }

            if (clusterCount < 3 || double.IsInfinity(minX) || double.IsInfinity(minY)) continue;

            var clusterList = new List<QrFinderPatternDetector.FinderPattern>(clusterCount);
            for (var q = 0; q < qTail; q++) {
                clusterList.Add(candidates[queue[q]]);
            }
            if (clusterList.Count >= 3) {
                if (TryDecodeFromFinderCandidates(scale, threshold, image, invert, clusterList, candidatesSorted: false, accept, aggressive, stylized, budget, out var clusterDecoded, out _)) {
                    AddResult(results, seen, clusterDecoded, accept);
                }
            }

            var pad = maxModule > 0 ? maxModule * (stylized ? 8.0 : 6.0) : 12.0;
            static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

            var bminX = Clamp(QrMath.RoundToInt(minX - pad), 0, image.Width - 1);
            var bminY = Clamp(QrMath.RoundToInt(minY - pad), 0, image.Height - 1);
            var bmaxX = Clamp(QrMath.RoundToInt(maxX + pad), 0, image.Width - 1);
            var bmaxY = Clamp(QrMath.RoundToInt(maxY + pad), 0, image.Height - 1);
            if (bmaxX <= bminX || bmaxY <= bminY) continue;

            if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, stylized, budget, out var decoded, out _, totalCandidates, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY)) {
                AddResult(results, seen, decoded, accept);
            }
        }
    }

    private static bool TryDecodeBySingleFinder(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Count == 0) return false;
        if (budget.IsExpired) return false;

        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        var n = Math.Min(candidates.Count, 4);

        Span<SingleFinderOrientation> orientationsBuf = stackalloc SingleFinderOrientation[3];
        Span<int> dimsBuf = stackalloc int[12];

        for (var i = 0; i < n; i++) {
            var c = candidates[i];
            var moduleSize = c.ModuleSize;
            if (moduleSize <= 0) continue;

            var orientations = orientationsBuf;
            var oCount = 0;
            var fx = c.X / image.Width;
            var fy = c.Y / image.Height;

            if (fx <= 0.55 && fy <= 0.55) orientations[oCount++] = SingleFinderOrientation.TopLeft;
            if (fx >= 0.45 && fy <= 0.55) orientations[oCount++] = SingleFinderOrientation.TopRight;
            if (fx <= 0.55 && fy >= 0.45) orientations[oCount++] = SingleFinderOrientation.BottomLeft;
            if (oCount == 0) {
                orientations[oCount++] = SingleFinderOrientation.TopLeft;
                orientations[oCount++] = SingleFinderOrientation.TopRight;
                orientations[oCount++] = SingleFinderOrientation.BottomLeft;
            }

            for (var oi = 0; oi < oCount; oi++) {
                var orientation = orientations[oi];
                if (!TryGetMaxDimension(image, c, moduleSize, orientation, out var maxDim)) continue;

                var dims = dimsBuf;
                var dimsCount = 0;
                var baseDim = NearestValidDimension(maxDim);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 4);
                if (stylized) {
                    AddDimensionCandidate(ref dims, ref dimsCount, baseDim + 4);
                }
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 8);
                if (stylized) {
                    AddDimensionCandidate(ref dims, ref dimsCount, baseDim + 8);
                }
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 12);
                if (stylized) {
                    AddDimensionCandidate(ref dims, ref dimsCount, baseDim + 12);
                }
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 16);
                if (stylized) {
                    AddDimensionCandidate(ref dims, ref dimsCount, baseDim + 16);
                }
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 20);

                var maxDims = budget.Enabled ? Math.Min(dimsCount, stylized ? 5 : 3) : dimsCount;
                for (var di = 0; di < maxDims; di++) {
                    if (budget.IsExpired) return false;
                    var dim = dims[di];
                    if (!TryGetBoundingBoxFromSingleFinder(image, c, moduleSize, orientation, dim, out var minX, out var minY, out var maxX, out var maxY)) {
                        continue;
                    }

                    if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagnostics, candidates.Count, candidateTriplesTried: 0, minX, minY, maxX, maxY)) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private enum SingleFinderOrientation {
        TopLeft,
        TopRight,
        BottomLeft
    }

    private static bool TryGetMaxDimension(
        QrGrayImage image,
        QrFinderPatternDetector.FinderPattern candidate,
        double moduleSize,
        SingleFinderOrientation orientation,
        out int maxDim) {
        maxDim = 0;
        if (moduleSize <= 0) return false;

        var halfFinder = moduleSize * 3.5;
        var left = candidate.X - halfFinder;
        var right = candidate.X + halfFinder;
        var top = candidate.Y - halfFinder;
        var bottom = candidate.Y + halfFinder;

        double availX;
        double availY;
        switch (orientation) {
            case SingleFinderOrientation.TopRight:
                availX = right;
                availY = image.Height - top;
                break;
            case SingleFinderOrientation.BottomLeft:
                availX = image.Width - left;
                availY = bottom;
                break;
            default:
                availX = image.Width - left;
                availY = image.Height - top;
                break;
        }

        var dim = (int)Math.Floor(Math.Min(availX, availY) / moduleSize);
        if (dim < 21) return false;
        if (dim > 177) dim = 177;
        maxDim = dim;
        return true;
    }

    private static bool TryGetBoundingBoxFromSingleFinder(
        QrGrayImage image,
        QrFinderPatternDetector.FinderPattern candidate,
        double moduleSize,
        SingleFinderOrientation orientation,
        int dimension,
        out int minX,
        out int minY,
        out int maxX,
        out int maxY) {
        minX = minY = maxX = maxY = 0;
        if (dimension is < 21 or > 177) return false;
        if ((dimension & 3) != 1) return false;

        var halfFinder = moduleSize * 3.5;
        var dimPx = dimension * moduleSize;

        double minXf;
        double minYf;
        double maxXf;
        double maxYf;

        switch (orientation) {
            case SingleFinderOrientation.TopRight:
                maxXf = candidate.X + halfFinder;
                minXf = maxXf - dimPx;
                minYf = candidate.Y - halfFinder;
                maxYf = minYf + dimPx;
                break;
            case SingleFinderOrientation.BottomLeft:
                minXf = candidate.X - halfFinder;
                maxXf = minXf + dimPx;
                maxYf = candidate.Y + halfFinder;
                minYf = maxYf - dimPx;
                break;
            default:
                minXf = candidate.X - halfFinder;
                maxXf = minXf + dimPx;
                minYf = candidate.Y - halfFinder;
                maxYf = minYf + dimPx;
                break;
        }

        var slack = moduleSize * 4.0;
        if (minXf < -slack || minYf < -slack || maxXf > image.Width - 1 + slack || maxYf > image.Height - 1 + slack) return false;

        minX = Math.Clamp(QrMath.RoundToInt(minXf), 0, image.Width - 1);
        maxX = Math.Clamp(QrMath.RoundToInt(maxXf), 0, image.Width - 1);
        minY = Math.Clamp(QrMath.RoundToInt(minYf), 0, image.Height - 1);
        maxY = Math.Clamp(QrMath.RoundToInt(maxYf), 0, image.Height - 1);

        return minX < maxX && minY < maxY;
    }

    private static bool TryDecodeByConnectedComponents(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var w = image.Width;
        var h = image.Height;

        if (budget.IsExpired) return false;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        using var comps = FindComponents(image, invert, shouldStop, minAreaDivisor: 400, minComponentSize: 21);
        if (comps.Count == 0) return false;

        comps.Sort(static (a, b) => b.Area.CompareTo(a.Area));
        var maxTry = Math.Min(comps.Count, 6);

        for (var i = 0; i < maxTry; i++) {
            if (budget.IsExpired || budget.IsNearDeadline(120)) return false;
            var c = comps[i];
            var pad = Math.Max(2, (int)Math.Round(Math.Min(c.Width, c.Height) * 0.05));
            var bminX = c.MinX - pad;
            var bminY = c.MinY - pad;
            var bmaxX = c.MaxX + pad;
            var bmaxY = c.MaxY + pad;

            if (bminX < 0) bminX = 0;
            if (bminY < 0) bminY = 0;
            if (bmaxX >= w) bmaxX = w - 1;
            if (bmaxY >= h) bmaxY = h - 1;

            if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagnostics, candidateCount: 0, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY)) {
                return true;
            }
        }

        return false;
    }

}
#endif
