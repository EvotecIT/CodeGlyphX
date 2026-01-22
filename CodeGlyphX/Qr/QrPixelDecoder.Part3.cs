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
        HashSet<string> seen,
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

        var q25 = FindQuantile(histogram, total * 25 / 100);
        var q50 = FindQuantile(histogram, total / 2);
        var q75 = FindQuantile(histogram, total * 75 / 100);

        AddThresholdCandidate(ref list, ref count, q25);
        AddThresholdCandidate(ref list, ref count, q50);
        AddThresholdCandidate(ref list, ref count, q75);
    }

    private static byte FindQuantile(Span<int> histogram, int target) {
        if (target <= 0) return 0;
        var sum = 0;
        for (var i = 0; i < histogram.Length; i++) {
            sum += histogram[i];
            if (sum >= target) return (byte)i;
        }
        return 255;
    }

    private static void CollectFromImage(
        QrGrayImage image,
        bool invert,
        PooledList<QrDecoded> results,
        HashSet<string> seen,
        Func<QrDecoded, bool>? accept,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        DecodeBudget budget,
        bool aggressive) {
        if (budget.IsExpired) return;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        var rowStepOverride = budget.Enabled && budget.MaxMilliseconds <= 800 ? 2 : 0;
        var maxCandidates = 0;
        if (budget.Enabled && budget.MaxMilliseconds <= 800) {
            maxCandidates = aggressive ? 36 : 24;
        }
        QrFinderPatternDetector.FindCandidates(image, invert, candidates, aggressive, shouldStop, rowStepOverride, maxCandidates);
        if (budget.Enabled && candidates.Count > 64) {
            candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
            candidates.RemoveRange(64, candidates.Count - 64);
        }
        if (budget.IsExpired) return;
        if (candidates.Count >= 3) {
            CollectFromFinderCandidates(image, invert, candidates, results, seen, accept, budget, aggressive);
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
        HashSet<string> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        bool aggressive) {
        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        var n = Math.Min(candidates.Count, 10);
        var triedTriples = 0;
        var maxTriples = 48;
        if (budget.Enabled) {
            maxTriples = 24;
        }

        var stop = false;
        for (var i = 0; i < n - 2 && !stop; i++) {
            for (var j = i + 1; j < n - 1 && !stop; j++) {
                for (var k = j + 1; k < n; k++) {
                    if (budget.IsExpired) return;
                    if (triedTriples++ >= maxTriples) { stop = true; break; }

                    var a = candidates[i];
                    var b = candidates[j];
                    var c = candidates[k];

                    var msMin = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                    var msMax = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                    if (msMin <= 0) continue;
                    if (msMax > msMin * 1.75) continue;

                    if (!TryOrderAsTlTrBl(a, b, c, out var tl, out var tr, out var bl)) continue;
                    if (TrySampleAndDecode(
                            scale: 1,
                            threshold: image.Threshold,
                            image,
                            invert,
                            tl,
                            tr,
                            bl,
                            candidates.Count,
                            triedTriples,
                            accept,
                            aggressive,
                            budget,
                            out var decoded,
                            out _)) {
                        AddResult(results, seen, decoded, accept);
                    }
                }
            }
        }
    }

    private static void CollectFromComponents(
        QrGrayImage image,
        bool invert,
        PooledList<QrDecoded> results,
        HashSet<string> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget) {
        if (budget.IsExpired) return;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        using var comps = FindComponents(image, invert, shouldStop);
        if (comps.Count == 0) return;

        comps.Sort(static (a, b) => b.Area.CompareTo(a.Area));
        var maxTry = Math.Min(comps.Count, 12);

        for (var i = 0; i < maxTry; i++) {
            if (budget.IsExpired || budget.IsNearDeadline(120)) return;
            var c = comps[i];
            var pad = Math.Max(2, (int)Math.Round(Math.Min(c.Width, c.Height) * 0.05));
            var bminX = c.MinX - pad;
            var bminY = c.MinY - pad;
            var bmaxX = c.MaxX + pad;
            var bmaxY = c.MaxY + pad;

            if (bminX < 0) bminX = 0;
            if (bminY < 0) bminY = 0;
            if (bmaxX >= image.Width) bmaxX = image.Width - 1;
            if (bmaxY >= image.Height) bmaxY = image.Height - 1;

            if (TryDecodeByBoundingBox(scale: 1, threshold: image.Threshold, image, invert, accept, aggressive: false, budget, out var decoded, out _, candidateCount: 0, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY)) {
                AddResult(results, seen, decoded, accept);
            }
        }
    }

    private static void AddResult(PooledList<QrDecoded> results, HashSet<string> seen, QrDecoded decoded, Func<QrDecoded, bool>? accept) {
        if (accept is not null && !accept(decoded)) return;
        var key = Convert.ToBase64String(decoded.Bytes);
        if (!seen.Add(key)) return;
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

    private static bool TryDecodeFromGray(int scale, byte threshold, QrGrayImage image, bool invert, List<QrFinderPatternDetector.FinderPattern> candidates, Func<QrDecoded, bool>? accept, bool aggressive, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        // Finder-based sampling (robust to extra background/noise). Try multiple triples when the region contains UI/text.
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        var rowStepOverride = budget.Enabled && budget.MaxMilliseconds <= 800 ? 2 : 0;
        QrFinderPatternDetector.FindCandidates(image, invert, candidates, aggressive, shouldStop, rowStepOverride);
        if (budget.Enabled && candidates.Count > 64) {
            candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
            candidates.RemoveRange(64, candidates.Count - 64);
        }
        if (budget.Enabled && budget.MaxMilliseconds <= 800 && candidates.Count > 30) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidates.Count, candidateTriplesTried: 0, dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload));
            return false;
        }

        if (candidates.Count >= 3) {
            if (TryDecodeFromFinderCandidates(scale, threshold, image, invert, candidates, accept, aggressive, budget, out result, out var diagF)) {
                diagnostics = diagF;
                return true;
            }
            diagnostics = Better(diagnostics, diagF);

            if (TryDecodeByCandidateBounds(scale, threshold, image, invert, candidates, accept, aggressive, budget, out result, out var diagC)) {
                diagnostics = diagC;
                return true;
            }
            diagnostics = Better(diagnostics, diagC);
        }

        if (candidates.Count > 0) {
            if (budget.Enabled && budget.MaxMilliseconds <= 800) {
                return false;
            }
            if (TryDecodeBySingleFinder(scale, threshold, image, invert, candidates, accept, aggressive, budget, out result, out var diagS)) {
                diagnostics = diagS;
                return true;
            }
            diagnostics = Better(diagnostics, diagS);
        }

        if (budget.Enabled && budget.MaxMilliseconds <= 800) {
            return false;
        }

        // Connected-components fallback (helps when finder detection fails but a clean symbol exists).
        var diagCC = default(QrPixelDecodeDiagnostics);
        if (!budget.IsExpired && TryDecodeByConnectedComponents(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagCC)) {
            diagnostics = diagCC;
            return true;
        }
        diagnostics = Better(diagnostics, diagCC);

        // Fallback: bounding box exact-fit (works for perfectly cropped/generated images).
        var diagB = default(QrPixelDecodeDiagnostics);
        if (!budget.IsExpired && TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagB)) {
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

        var pad = maxModule > 0 ? maxModule * 6.0 : 12.0;

        static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        var bminX = Clamp(QrMath.RoundToInt(minX - pad), 0, image.Width - 1);
        var bminY = Clamp(QrMath.RoundToInt(minY - pad), 0, image.Height - 1);
        var bmaxX = Clamp(QrMath.RoundToInt(maxX + pad), 0, image.Width - 1);
        var bmaxY = Clamp(QrMath.RoundToInt(maxY + pad), 0, image.Height - 1);

        if (bmaxX <= bminX || bmaxY <= bminY) return false;

        return TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagnostics, candidates.Count, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY);
    }

    private static bool TryDecodeBySingleFinder(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Count == 0) return false;
        if (budget.IsExpired) return false;

        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        var n = Math.Min(candidates.Count, 4);

        var orientationsBuf = new SingleFinderOrientation[3];
        var dimsBuf = new int[8];

        for (var i = 0; i < n; i++) {
            var c = candidates[i];
            var moduleSize = c.ModuleSize;
            if (moduleSize <= 0) continue;

            var orientations = orientationsBuf.AsSpan();
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

                var dims = dimsBuf.AsSpan();
                var dimsCount = 0;
                var baseDim = NearestValidDimension(maxDim);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 4);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 8);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 12);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 16);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 20);

                var maxDims = budget.Enabled ? Math.Min(dimsCount, 3) : dimsCount;
                for (var di = 0; di < maxDims; di++) {
                    if (budget.IsExpired) return false;
                    var dim = dims[di];
                    if (!TryGetBoundingBoxFromSingleFinder(image, c, moduleSize, orientation, dim, out var minX, out var minY, out var maxX, out var maxY)) {
                        continue;
                    }

                    if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagnostics, candidates.Count, candidateTriplesTried: 0, minX, minY, maxX, maxY)) {
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
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var w = image.Width;
        var h = image.Height;

        if (budget.IsExpired) return false;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        using var comps = FindComponents(image, invert, shouldStop);
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

            if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagnostics, candidateCount: 0, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY)) {
                return true;
            }
        }

        return false;
    }

}
#endif
