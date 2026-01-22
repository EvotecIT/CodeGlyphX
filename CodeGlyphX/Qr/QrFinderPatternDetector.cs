#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;

namespace CodeGlyphX.Qr;

internal static class QrFinderPatternDetector {
    internal readonly struct FinderPattern {
        public readonly double X;
        public readonly double Y;
        public readonly double ModuleSize;
        public readonly int Count;

        public FinderPattern(double x, double y, double moduleSize, int count) {
            X = x;
            Y = y;
            ModuleSize = moduleSize;
            Count = count;
        }
    }

    public static bool TryFind(QrGrayImage image, bool invert, out FinderPattern topLeft, out FinderPattern topRight, out FinderPattern bottomLeft) {
        topLeft = default;
        topRight = default;
        bottomLeft = default;

        var possibleCenters = FindCandidates(image, invert);
        if (possibleCenters.Count < 3) return false;
        return TrySelectBestThree(possibleCenters, out topLeft, out topRight, out bottomLeft);
    }

    public static List<FinderPattern> FindCandidates(QrGrayImage image, bool invert) {
        var output = new List<FinderPattern>(8);
        FindCandidates(image, invert, output, aggressive: false, shouldStop: null);
        return output;
    }

    public static List<FinderPattern> FindCandidates(QrGrayImage image, bool invert, bool aggressive, Func<bool>? shouldStop = null, int rowStepOverride = 0, int maxCandidates = 0, bool allowFullScan = true, bool requireDiagonalCheck = true) {
        var output = new List<FinderPattern>(8);
        FindCandidates(image, invert, output, aggressive, shouldStop, rowStepOverride, maxCandidates, allowFullScan, requireDiagonalCheck);
        return output;
    }

    internal static void FindCandidates(QrGrayImage image, bool invert, List<FinderPattern> output) {
        FindCandidates(image, invert, output, aggressive: false, shouldStop: null);
    }

    internal static void FindCandidates(QrGrayImage image, bool invert, List<FinderPattern> output, bool aggressive, Func<bool>? shouldStop = null, int rowStepOverride = 0, int maxCandidates = 0, bool allowFullScan = true, bool requireDiagonalCheck = true) {
        if (output is null) throw new ArgumentNullException(nameof(output));
        output.Clear();

        using var pooled = new PooledList<FinderPattern>(8);
        var step = rowStepOverride > 0 ? rowStepOverride : GetRowStep(image);
        FindCandidatesWithStep(image, invert, step, pooled, aggressive, shouldStop, maxCandidates, requireDiagonalCheck);
        if (allowFullScan && step > 1 && pooled.Count < 3) {
            using var full = new PooledList<FinderPattern>(8);
            FindCandidatesWithStep(image, invert, rowStep: 1, full, aggressive, shouldStop, maxCandidates, requireDiagonalCheck);
            if (full.Count > pooled.Count) {
                output.Clear();
                output.EnsureCapacity(full.Count);
                full.CopyTo(output);
                return;
            }
        }

        pooled.CopyTo(output);
    }

    private static int GetRowStep(QrGrayImage image) {
        var minDim = image.Width < image.Height ? image.Width : image.Height;
        if (minDim >= 1400) return 3;
        if (minDim >= 900) return 2;
        return 1;
    }

    private static void FindCandidatesWithStep(QrGrayImage image, bool invert, int rowStep, PooledList<FinderPattern> possibleCenters, bool aggressive, Func<bool>? shouldStop, int maxCandidates, bool requireDiagonalCheck) {
        // Scan rows for 1:1:3:1:1 run-length patterns and cross-check vertically/horizontally.
        Span<int> stateCount = stackalloc int[5];

        if (rowStep < 1) rowStep = 1;

        for (var y = 0; y < image.Height; y += rowStep) {
            if (shouldStop?.Invoke() == true) return;
            if (maxCandidates > 0 && possibleCenters.Count >= maxCandidates) return;
            stateCount.Clear();
            var currentState = 0;

            for (var x = 0; x < image.Width; x++) {
                if (image.IsBlack(x, y, invert)) {
                    if ((currentState & 1) == 1) currentState++;
                    if (currentState > 4) {
                        ShiftCounts(stateCount);
                        currentState = 3;
                    }
                    stateCount[currentState]++;
                } else {
                    if ((currentState & 1) == 0) {
                        if (currentState == 4) {
                            if (FoundPatternCross(stateCount, aggressive) && HandlePossibleCenter(image, invert, possibleCenters, stateCount, x, y, aggressive, requireDiagonalCheck)) {
                                if (maxCandidates > 0 && possibleCenters.Count >= maxCandidates) return;
                                currentState = 0;
                                stateCount.Clear();
                            } else {
                                ShiftCounts(stateCount);
                                currentState = 3;
                            }
                        } else {
                            currentState++;
                            stateCount[currentState]++;
                        }
                    } else {
                        stateCount[currentState]++;
                    }
                }
            }

            // Check for pattern at end of row.
            if (FoundPatternCross(stateCount, aggressive)) {
                HandlePossibleCenter(image, invert, possibleCenters, stateCount, image.Width, y, aggressive, requireDiagonalCheck);
            }
        }
    }

    private static bool TrySelectBestThree(List<FinderPattern> candidates, out FinderPattern topLeft, out FinderPattern topRight, out FinderPattern bottomLeft) {
        topLeft = default;
        topRight = default;
        bottomLeft = default;

        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        var n = Math.Min(candidates.Count, 10);

        var bestScore = double.NegativeInfinity;
        var bestA = default(FinderPattern);
        var bestB = default(FinderPattern);
        var bestC = default(FinderPattern);

        for (var i = 0; i < n - 2; i++) {
            for (var j = i + 1; j < n - 1; j++) {
                for (var k = j + 1; k < n; k++) {
                    var a = candidates[i];
                    var b = candidates[j];
                    var c = candidates[k];

                    var msAvg = (a.ModuleSize + b.ModuleSize + c.ModuleSize) / 3.0;
                    var msMin = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                    var msMax = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                    if (msMax > msMin * 1.75) continue;

                    var dAB = Dist2(a, b);
                    var dAC = Dist2(a, c);
                    var dBC = Dist2(b, c);

                    var maxD = dAB;
                    var maxP = 0;
                    if (dAC > maxD) { maxD = dAC; maxP = 1; }
                    if (dBC > maxD) { maxD = dBC; maxP = 2; }

                    // Side lengths are the two smaller distances; they should be similar for a QR finder triangle.
                    double side1, side2;
                    if (maxP == 0) { side1 = dAC; side2 = dBC; }
                    else if (maxP == 1) { side1 = dAB; side2 = dBC; }
                    else { side1 = dAB; side2 = dAC; }

                    if (side1 <= 0 || side2 <= 0) continue;
                    var ratio = side1 > side2 ? side1 / side2 : side2 / side1;
                    if (ratio > 1.8) continue;

                    // Prefer larger triangles (but bounded by image), higher counts, and similar side lengths.
                    var counts = a.Count + b.Count + c.Count;
                    var sizeScore = Math.Sqrt(maxD);
                    var symmetryPenalty = Math.Abs(Math.Sqrt(side1) - Math.Sqrt(side2));
                    var score = counts * 10 + sizeScore - symmetryPenalty * 4 - Math.Abs(msAvg - msMin) * 0.5;

                    if (score > bestScore) {
                        bestScore = score;
                        bestA = a;
                        bestB = b;
                        bestC = c;
                    }
                }
            }
        }

        if (double.IsNegativeInfinity(bestScore)) return false;

        // Assign points: the point shared by the two shorter distances is top-left.
        var d01 = Dist2(bestA, bestB);
        var d02 = Dist2(bestA, bestC);
        var d12 = Dist2(bestB, bestC);

        FinderPattern tl, p1, p2;
        if (d01 >= d02 && d01 >= d12) {
            tl = bestC;
            p1 = bestA;
            p2 = bestB;
        } else if (d02 >= d01 && d02 >= d12) {
            tl = bestB;
            p1 = bestA;
            p2 = bestC;
        } else {
            tl = bestA;
            p1 = bestB;
            p2 = bestC;
        }

        // Ensure p1 is top-right and p2 is bottom-left (clockwise orientation).
        var cross = Cross(p1.X - tl.X, p1.Y - tl.Y, p2.X - tl.X, p2.Y - tl.Y);
        if (cross < 0) {
            (p1, p2) = (p2, p1);
        }

        topLeft = tl;
        topRight = p1;
        bottomLeft = p2;
        return true;
    }

    private static double Dist2(FinderPattern a, FinderPattern b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;

    private static void ShiftCounts(Span<int> stateCount) {
        stateCount[0] = stateCount[2];
        stateCount[1] = stateCount[3];
        stateCount[2] = stateCount[4];
        stateCount[3] = 1;
        stateCount[4] = 0;
    }

    private static bool HandlePossibleCenter(QrGrayImage image, bool invert, PooledList<FinderPattern> possibleCenters, ReadOnlySpan<int> stateCount, int endX, int y, bool aggressive, bool requireDiagonalCheck) {
        var stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
        var centerX = CenterFromEnd(stateCount, endX);
        if (centerX < 0 || centerX >= image.Width) return false;

        var maxCount = stateCount[2];
        if (!CrossCheckVertical(image, invert, QrMath.RoundToInt(centerX), y, maxCount, stateCountTotal, aggressive, out var centerY, out var moduleSizeV)) {
            return false;
        }
        if (!CrossCheckHorizontal(image, invert, QrMath.RoundToInt(centerX), QrMath.RoundToInt(centerY), maxCount, stateCountTotal, aggressive, out centerX, out var moduleSizeH)) {
            return false;
        }
        if (requireDiagonalCheck && !CrossCheckDiagonal(image, invert, QrMath.RoundToInt(centerX), QrMath.RoundToInt(centerY), maxCount, stateCountTotal, aggressive)) {
            return false;
        }

        var moduleSize = (moduleSizeV + moduleSizeH) / 2.0;
        AddOrMerge(possibleCenters, new FinderPattern(centerX, centerY, moduleSize, 1));
        return true;
    }

    private static void AddOrMerge(PooledList<FinderPattern> centers, FinderPattern candidate) {
        for (var i = 0; i < centers.Count; i++) {
            var existing = centers[i];
            var dx = Math.Abs(existing.X - candidate.X);
            var dy = Math.Abs(existing.Y - candidate.Y);
            var dm = Math.Abs(existing.ModuleSize - candidate.ModuleSize);

            var tol = existing.ModuleSize * 1.5;
            if (dx <= tol && dy <= tol && dm <= existing.ModuleSize) {
                var newCount = existing.Count + 1;
                var x = (existing.X * existing.Count + candidate.X) / newCount;
                var y = (existing.Y * existing.Count + candidate.Y) / newCount;
                var ms = (existing.ModuleSize * existing.Count + candidate.ModuleSize) / newCount;
                centers[i] = new FinderPattern(x, y, ms, newCount);
                return;
            }
        }

        centers.Add(candidate);
    }

    private sealed class PooledList<T> : IDisposable {
        private T[] _buffer;
        public int Count { get; private set; }

        public PooledList(int capacity) {
            if (capacity < 1) capacity = 1;
            _buffer = ArrayPool<T>.Shared.Rent(capacity);
        }

        public T this[int index] {
            get => _buffer[index];
            set => _buffer[index] = value;
        }

        public void Add(T item) {
            if (Count == _buffer.Length) Grow();
            _buffer[Count++] = item;
        }

        public List<T> ToList() {
            if (Count == 0) return new List<T>();
            var list = new List<T>(Count);
            for (var i = 0; i < Count; i++) list.Add(_buffer[i]);
            return list;
        }

        public void CopyTo(List<T> output) {
            if (output is null) throw new ArgumentNullException(nameof(output));
            output.EnsureCapacity(output.Count + Count);
            for (var i = 0; i < Count; i++) output.Add(_buffer[i]);
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

    private static bool FoundPatternCross(ReadOnlySpan<int> stateCount, bool aggressive) {
        var total = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
        if (total < 7) return false;
        if (stateCount[0] == 0 || stateCount[1] == 0 || stateCount[2] == 0 || stateCount[3] == 0 || stateCount[4] == 0) return false;

        var moduleSize = total / 7.0;
        var maxVariance = moduleSize * (aggressive ? 0.8 : 0.5);
        var centerVariance = aggressive ? 3.5 * maxVariance : 3.0 * maxVariance;

        return Math.Abs(moduleSize - stateCount[0]) <= maxVariance &&
               Math.Abs(moduleSize - stateCount[1]) <= maxVariance &&
               Math.Abs(3.0 * moduleSize - stateCount[2]) <= centerVariance &&
               Math.Abs(moduleSize - stateCount[3]) <= maxVariance &&
               Math.Abs(moduleSize - stateCount[4]) <= maxVariance;
    }

    private static double CenterFromEnd(ReadOnlySpan<int> stateCount, int end) {
        return end - stateCount[4] - stateCount[3] - stateCount[2] / 2.0;
    }

    private static bool CrossCheckVertical(QrGrayImage image, bool invert, int centerX, int startY, int maxCount, int originalTotal, bool aggressive, out double centerY, out double moduleSize) {
        centerY = 0;
        moduleSize = 0;

        Span<int> stateCount = stackalloc int[5];

        var y = startY;
        while (y >= 0 && image.IsBlack(centerX, y, invert)) {
            stateCount[2]++;
            y--;
        }
        if (y < 0) return false;

        while (y >= 0 && !image.IsBlack(centerX, y, invert) && stateCount[1] <= maxCount) {
            stateCount[1]++;
            y--;
        }
        if (y < 0 || stateCount[1] > maxCount) return false;

        while (y >= 0 && image.IsBlack(centerX, y, invert) && stateCount[0] <= maxCount) {
            stateCount[0]++;
            y--;
        }
        if (stateCount[0] > maxCount) return false;

        y = startY + 1;
        while (y < image.Height && image.IsBlack(centerX, y, invert)) {
            stateCount[2]++;
            y++;
        }
        if (y == image.Height) return false;

        while (y < image.Height && !image.IsBlack(centerX, y, invert) && stateCount[3] < maxCount) {
            stateCount[3]++;
            y++;
        }
        if (y == image.Height || stateCount[3] >= maxCount) return false;

        while (y < image.Height && image.IsBlack(centerX, y, invert) && stateCount[4] < maxCount) {
            stateCount[4]++;
            y++;
        }
        if (stateCount[4] >= maxCount) return false;

        var total = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
        if (Math.Abs(total - originalTotal) * 5 >= originalTotal * 2) return false;
        if (!FoundPatternCross(stateCount, aggressive)) return false;

        centerY = CenterFromEnd(stateCount, y);
        moduleSize = total / 7.0;
        return true;
    }

    private static bool CrossCheckHorizontal(QrGrayImage image, bool invert, int startX, int centerY, int maxCount, int originalTotal, bool aggressive, out double centerX, out double moduleSize) {
        centerX = 0;
        moduleSize = 0;

        Span<int> stateCount = stackalloc int[5];

        var x = startX;
        while (x >= 0 && image.IsBlack(x, centerY, invert)) {
            stateCount[2]++;
            x--;
        }
        if (x < 0) return false;

        while (x >= 0 && !image.IsBlack(x, centerY, invert) && stateCount[1] <= maxCount) {
            stateCount[1]++;
            x--;
        }
        if (x < 0 || stateCount[1] > maxCount) return false;

        while (x >= 0 && image.IsBlack(x, centerY, invert) && stateCount[0] <= maxCount) {
            stateCount[0]++;
            x--;
        }
        if (stateCount[0] > maxCount) return false;

        x = startX + 1;
        while (x < image.Width && image.IsBlack(x, centerY, invert)) {
            stateCount[2]++;
            x++;
        }
        if (x == image.Width) return false;

        while (x < image.Width && !image.IsBlack(x, centerY, invert) && stateCount[3] < maxCount) {
            stateCount[3]++;
            x++;
        }
        if (x == image.Width || stateCount[3] >= maxCount) return false;

        while (x < image.Width && image.IsBlack(x, centerY, invert) && stateCount[4] < maxCount) {
            stateCount[4]++;
            x++;
        }
        if (stateCount[4] >= maxCount) return false;

        var total = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
        if (Math.Abs(total - originalTotal) * 5 >= originalTotal * 2) return false;
        if (!FoundPatternCross(stateCount, aggressive)) return false;

        centerX = CenterFromEnd(stateCount, x);
        moduleSize = total / 7.0;
        return true;
    }

    private static bool CrossCheckDiagonal(QrGrayImage image, bool invert, int centerX, int centerY, int maxCount, int originalTotal, bool aggressive) {
        Span<int> stateCount = stackalloc int[5];

        var x = centerX;
        var y = centerY;

        while (x >= 0 && y >= 0 && image.IsBlack(x, y, invert)) {
            stateCount[2]++;
            x--;
            y--;
        }
        if (x < 0 || y < 0) return false;

        while (x >= 0 && y >= 0 && !image.IsBlack(x, y, invert) && stateCount[1] <= maxCount) {
            stateCount[1]++;
            x--;
            y--;
        }
        if (x < 0 || y < 0 || stateCount[1] > maxCount) return false;

        while (x >= 0 && y >= 0 && image.IsBlack(x, y, invert) && stateCount[0] <= maxCount) {
            stateCount[0]++;
            x--;
            y--;
        }
        if (stateCount[0] > maxCount) return false;

        x = centerX + 1;
        y = centerY + 1;
        while (x < image.Width && y < image.Height && image.IsBlack(x, y, invert)) {
            stateCount[2]++;
            x++;
            y++;
        }
        if (x >= image.Width || y >= image.Height) return false;

        while (x < image.Width && y < image.Height && !image.IsBlack(x, y, invert) && stateCount[3] < maxCount) {
            stateCount[3]++;
            x++;
            y++;
        }
        if (x >= image.Width || y >= image.Height || stateCount[3] >= maxCount) return false;

        while (x < image.Width && y < image.Height && image.IsBlack(x, y, invert) && stateCount[4] < maxCount) {
            stateCount[4]++;
            x++;
            y++;
        }
        if (stateCount[4] >= maxCount) return false;

        var total = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
        if (Math.Abs(total - originalTotal) * 5 >= originalTotal * 2) return false;
        if (!FoundPatternCross(stateCount, aggressive)) return false;

        return true;
    }
}
#endif
