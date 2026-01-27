#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        var step = rowStepOverride > 0 ? rowStepOverride : GetRowStep(image, aggressive);
        FindCandidatesWithStep(image, invert, step, pooled, aggressive, shouldStop, maxCandidates, requireDiagonalCheck);
        if (allowFullScan && step > 1 && (pooled.Count < 3 || aggressive)) {
            using var full = new PooledList<FinderPattern>(8);
            FindCandidatesWithStep(image, invert, rowStep: 1, full, aggressive, shouldStop, maxCandidates, requireDiagonalCheck);
            for (var i = 0; i < full.Count; i++) {
                var candidate = full[i];
                var weight = candidate.Count <= 1 ? 1 : Math.Min(candidate.Count, 8);
                for (var w = 0; w < weight; w++) {
                    AddOrMerge(pooled, new FinderPattern(candidate.X, candidate.Y, candidate.ModuleSize, 1));
                }
            }
        }

        pooled.CopyTo(output);
    }

    private static int GetRowStep(QrGrayImage image, bool aggressive) {
        var minDim = image.Width < image.Height ? image.Width : image.Height;
        var step = minDim >= 1400 ? 3 : minDim >= 900 ? 2 : 1;
        if (aggressive && step > 1) step--;
        return step;
    }

    private static void FindCandidatesWithStep(QrGrayImage image, bool invert, int rowStep, PooledList<FinderPattern> possibleCenters, bool aggressive, Func<bool>? shouldStop, int maxCandidates, bool requireDiagonalCheck) {
        // Scan rows for 1:1:3:1:1 run-length patterns and cross-check vertically/horizontally.
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholds = image.ThresholdMap;
        var threshold = image.Threshold;
        var maxCandidatesLimit = maxCandidates > 0 ? maxCandidates : int.MaxValue;

        if (rowStep < 1) rowStep = 1;

        if (thresholds is null) {
            if (!invert) {
                for (var y = 0; y < height; y += rowStep) {
                    if (shouldStop?.Invoke() == true) return;
                    if (possibleCenters.Count >= maxCandidatesLimit) return;
                    var s0 = 0;
                    var s1 = 0;
                    var s2 = 0;
                    var s3 = 0;
                    var s4 = 0;
                    var currentState = 0;
                    var rowOffset = y * width;
                    var idx = rowOffset;

                    for (var x = 0; x < width; x++, idx++) {
                        if (gray[idx] <= threshold) {
                            if ((currentState & 1) == 1) currentState++;
                            if (currentState > 4) {
                                ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                currentState = 3;
                            }
                            IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                        } else {
                            if ((currentState & 1) == 0) {
                                if (currentState == 4) {
                                    if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive) && HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, x, y, aggressive, requireDiagonalCheck)) {
                                        if (possibleCenters.Count >= maxCandidatesLimit) return;
                                        currentState = 0;
                                        ResetCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                    } else {
                                        ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                        currentState = 3;
                                    }
                                } else {
                                    currentState++;
                                    IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                                }
                            } else {
                                IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                            }
                        }
                    }

                    // Check for pattern at end of row.
                    if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) {
                        HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, width, y, aggressive, requireDiagonalCheck);
                    }
                }
            } else {
                for (var y = 0; y < height; y += rowStep) {
                    if (shouldStop?.Invoke() == true) return;
                    if (possibleCenters.Count >= maxCandidatesLimit) return;
                    var s0 = 0;
                    var s1 = 0;
                    var s2 = 0;
                    var s3 = 0;
                    var s4 = 0;
                    var currentState = 0;
                    var rowOffset = y * width;
                    var idx = rowOffset;

                    for (var x = 0; x < width; x++, idx++) {
                        if (gray[idx] > threshold) {
                            if ((currentState & 1) == 1) currentState++;
                            if (currentState > 4) {
                                ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                currentState = 3;
                            }
                            IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                        } else {
                            if ((currentState & 1) == 0) {
                                if (currentState == 4) {
                                    if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive) && HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, x, y, aggressive, requireDiagonalCheck)) {
                                        if (possibleCenters.Count >= maxCandidatesLimit) return;
                                        currentState = 0;
                                        ResetCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                    } else {
                                        ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                        currentState = 3;
                                    }
                                } else {
                                    currentState++;
                                    IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                                }
                            } else {
                                IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                            }
                        }
                    }

                    // Check for pattern at end of row.
                    if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) {
                        HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, width, y, aggressive, requireDiagonalCheck);
                    }
                }
            }
        } else {
            if (!invert) {
                for (var y = 0; y < height; y += rowStep) {
                    if (shouldStop?.Invoke() == true) return;
                    if (possibleCenters.Count >= maxCandidatesLimit) return;
                    var s0 = 0;
                    var s1 = 0;
                    var s2 = 0;
                    var s3 = 0;
                    var s4 = 0;
                    var currentState = 0;
                    var rowOffset = y * width;
                    var idx = rowOffset;

                    for (var x = 0; x < width; x++, idx++) {
                        if (gray[idx] <= thresholds[idx]) {
                            if ((currentState & 1) == 1) currentState++;
                            if (currentState > 4) {
                                ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                currentState = 3;
                            }
                            IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                        } else {
                            if ((currentState & 1) == 0) {
                                if (currentState == 4) {
                                    if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive) && HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, x, y, aggressive, requireDiagonalCheck)) {
                                        if (possibleCenters.Count >= maxCandidatesLimit) return;
                                        currentState = 0;
                                        ResetCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                    } else {
                                        ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                        currentState = 3;
                                    }
                                } else {
                                    currentState++;
                                    IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                                }
                            } else {
                                IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                            }
                        }
                    }

                    // Check for pattern at end of row.
                    if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) {
                        HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, width, y, aggressive, requireDiagonalCheck);
                    }
                }
            } else {
                for (var y = 0; y < height; y += rowStep) {
                    if (shouldStop?.Invoke() == true) return;
                    if (possibleCenters.Count >= maxCandidatesLimit) return;
                    var s0 = 0;
                    var s1 = 0;
                    var s2 = 0;
                    var s3 = 0;
                    var s4 = 0;
                    var currentState = 0;
                    var rowOffset = y * width;
                    var idx = rowOffset;

                    for (var x = 0; x < width; x++, idx++) {
                        if (gray[idx] > thresholds[idx]) {
                            if ((currentState & 1) == 1) currentState++;
                            if (currentState > 4) {
                                ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                currentState = 3;
                            }
                            IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                        } else {
                            if ((currentState & 1) == 0) {
                                if (currentState == 4) {
                                    if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive) && HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, x, y, aggressive, requireDiagonalCheck)) {
                                        if (possibleCenters.Count >= maxCandidatesLimit) return;
                                        currentState = 0;
                                        ResetCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                    } else {
                                        ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                                        currentState = 3;
                                    }
                                } else {
                                    currentState++;
                                    IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                                }
                            } else {
                                IncrementState(currentState, ref s0, ref s1, ref s2, ref s3, ref s4);
                            }
                        }
                    }

                    // Check for pattern at end of row.
                    if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) {
                        HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, width, y, aggressive, requireDiagonalCheck);
                    }
                }
            }
        }
    }

    private static bool TrySelectBestThree(List<FinderPattern> candidates, out FinderPattern topLeft, out FinderPattern topRight, out FinderPattern bottomLeft) {
        topLeft = default;
        topRight = default;
        bottomLeft = default;

        const int maxCandidates = 10;
        if (candidates.Count <= maxCandidates) {
            return TrySelectBestThreeCore(CollectionsMarshal.AsSpan(candidates), out topLeft, out topRight, out bottomLeft);
        }

        Span<FinderPattern> top = stackalloc FinderPattern[maxCandidates];
        var topCount = 0;
        foreach (var candidate in candidates) {
            var count = candidate.Count;
            if (topCount == maxCandidates && count <= top[topCount - 1].Count) continue;
            var insertPos = topCount < maxCandidates ? topCount : maxCandidates - 1;
            while (insertPos > 0 && count > top[insertPos - 1].Count) {
                if (insertPos < maxCandidates) {
                    top[insertPos] = top[insertPos - 1];
                }
                insertPos--;
            }
            top[insertPos] = candidate;
            if (topCount < maxCandidates) topCount++;
        }

        return TrySelectBestThreeCore(top.Slice(0, topCount), out topLeft, out topRight, out bottomLeft);
    }

    private static bool TrySelectBestThreeCore(ReadOnlySpan<FinderPattern> source, out FinderPattern topLeft, out FinderPattern topRight, out FinderPattern bottomLeft) {
        topLeft = default;
        topRight = default;
        bottomLeft = default;

        var n = source.Length;

        var bestScore = double.NegativeInfinity;
        var bestA = default(FinderPattern);
        var bestB = default(FinderPattern);
        var bestC = default(FinderPattern);

        for (var i = 0; i < n - 2; i++) {
            for (var j = i + 1; j < n - 1; j++) {
                for (var k = j + 1; k < n; k++) {
                    var a = source[i];
                    var b = source[j];
                    var c = source[k];

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Dist2(FinderPattern a, FinderPattern b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ResetCounts(ref int s0, ref int s1, ref int s2, ref int s3, ref int s4) {
        s0 = 0;
        s1 = 0;
        s2 = 0;
        s3 = 0;
        s4 = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ShiftCounts(ref int s0, ref int s1, ref int s2, ref int s3, ref int s4) {
        s0 = s2;
        s1 = s3;
        s2 = s4;
        s3 = 1;
        s4 = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void IncrementState(int state, ref int s0, ref int s1, ref int s2, ref int s3, ref int s4) {
        switch (state) {
            case 0:
                s0++;
                break;
            case 1:
                s1++;
                break;
            case 2:
                s2++;
                break;
            case 3:
                s3++;
                break;
            default:
                s4++;
                break;
        }
    }

    private static bool HandlePossibleCenter(QrGrayImage image, bool invert, PooledList<FinderPattern> possibleCenters, int s0, int s1, int s2, int s3, int s4, int endX, int y, bool aggressive, bool requireDiagonalCheck) {
        var stateCountTotal = s0 + s1 + s2 + s3 + s4;
        var centerX = CenterFromEnd(s0, s1, s2, s3, s4, endX);
        if (centerX < 0 || centerX >= image.Width) return false;

        var maxCount = s2;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FoundPatternCross(ReadOnlySpan<int> stateCount, bool aggressive) {
        var total = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
        if (total < 7) return false;
        if (stateCount[0] == 0 || stateCount[1] == 0 || stateCount[2] == 0 || stateCount[3] == 0 || stateCount[4] == 0) return false;
        var outerLimit = aggressive ? 20 : 5;   // 2.0x or 0.5x, scaled by 10.
        var centerLimit = aggressive ? 60 : 15; // 6.0x or 1.5x, scaled by 10.

        var diff0 = 7 * stateCount[0] - total;
        if (diff0 < 0) diff0 = -diff0;
        if (diff0 * 10 > total * outerLimit) return false;

        var diff1 = 7 * stateCount[1] - total;
        if (diff1 < 0) diff1 = -diff1;
        if (diff1 * 10 > total * outerLimit) return false;

        var diff2 = 7 * stateCount[2] - (3 * total);
        if (diff2 < 0) diff2 = -diff2;
        if (diff2 * 10 > total * centerLimit) return false;

        var diff3 = 7 * stateCount[3] - total;
        if (diff3 < 0) diff3 = -diff3;
        if (diff3 * 10 > total * outerLimit) return false;

        var diff4 = 7 * stateCount[4] - total;
        if (diff4 < 0) diff4 = -diff4;
        return diff4 * 10 <= total * outerLimit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FoundPatternCross(int s0, int s1, int s2, int s3, int s4, bool aggressive) {
        var total = s0 + s1 + s2 + s3 + s4;
        if (total < 7) return false;
        if (s0 == 0 || s1 == 0 || s2 == 0 || s3 == 0 || s4 == 0) return false;
        var outerLimit = aggressive ? 20 : 5;
        var centerLimit = aggressive ? 60 : 15;

        var diff0 = 7 * s0 - total;
        if (diff0 < 0) diff0 = -diff0;
        if (diff0 * 10 > total * outerLimit) return false;

        var diff1 = 7 * s1 - total;
        if (diff1 < 0) diff1 = -diff1;
        if (diff1 * 10 > total * outerLimit) return false;

        var diff2 = 7 * s2 - (3 * total);
        if (diff2 < 0) diff2 = -diff2;
        if (diff2 * 10 > total * centerLimit) return false;

        var diff3 = 7 * s3 - total;
        if (diff3 < 0) diff3 = -diff3;
        if (diff3 * 10 > total * outerLimit) return false;

        var diff4 = 7 * s4 - total;
        if (diff4 < 0) diff4 = -diff4;
        return diff4 * 10 <= total * outerLimit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double CenterFromEnd(ReadOnlySpan<int> stateCount, int end) {
        return end - stateCount[4] - stateCount[3] - stateCount[2] / 2.0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double CenterFromEnd(int s0, int s1, int s2, int s3, int s4, int end) {
        return end - s4 - s3 - s2 / 2.0;
    }

    private static bool CrossCheckVertical(QrGrayImage image, bool invert, int centerX, int startY, int maxCount, int originalTotal, bool aggressive, out double centerY, out double moduleSize) {
        centerY = 0;
        moduleSize = 0;

        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholds = image.ThresholdMap;
        var threshold = image.Threshold;
        var s0 = 0;
        var s1 = 0;
        var s2 = 0;
        var s3 = 0;
        var s4 = 0;

        var y = startY;
        var idx = (y * width) + centerX;
        if (thresholds is null) {
            if (!invert) {
                while (y >= 0 && gray[idx] <= threshold) {
                    s2++;
                    y--;
                    idx -= width;
                }
                if (y < 0) return false;

                while (y >= 0 && gray[idx] > threshold && s1 <= maxCount) {
                    s1++;
                    y--;
                    idx -= width;
                }
                if (y < 0 || s1 > maxCount) return false;

                while (y >= 0 && gray[idx] <= threshold && s0 <= maxCount) {
                    s0++;
                    y--;
                    idx -= width;
                }
                if (s0 > maxCount) return false;

                y = startY + 1;
                idx = (y * width) + centerX;
                while (y < height && gray[idx] <= threshold) {
                    s2++;
                    y++;
                    idx += width;
                }
                if (y == height) return false;

                while (y < height && gray[idx] > threshold && s3 < maxCount) {
                    s3++;
                    y++;
                    idx += width;
                }
                if (y == height || s3 >= maxCount) return false;

                while (y < height && gray[idx] <= threshold && s4 < maxCount) {
                    s4++;
                    y++;
                    idx += width;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (y >= 0 && gray[idx] > threshold) {
                    s2++;
                    y--;
                    idx -= width;
                }
                if (y < 0) return false;

                while (y >= 0 && gray[idx] <= threshold && s1 <= maxCount) {
                    s1++;
                    y--;
                    idx -= width;
                }
                if (y < 0 || s1 > maxCount) return false;

                while (y >= 0 && gray[idx] > threshold && s0 <= maxCount) {
                    s0++;
                    y--;
                    idx -= width;
                }
                if (s0 > maxCount) return false;

                y = startY + 1;
                idx = (y * width) + centerX;
                while (y < height && gray[idx] > threshold) {
                    s2++;
                    y++;
                    idx += width;
                }
                if (y == height) return false;

                while (y < height && gray[idx] <= threshold && s3 < maxCount) {
                    s3++;
                    y++;
                    idx += width;
                }
                if (y == height || s3 >= maxCount) return false;

                while (y < height && gray[idx] > threshold && s4 < maxCount) {
                    s4++;
                    y++;
                    idx += width;
                }
                if (s4 >= maxCount) return false;
            }
        } else {
            if (!invert) {
                while (y >= 0 && gray[idx] <= thresholds[idx]) {
                    s2++;
                    y--;
                    idx -= width;
                }
                if (y < 0) return false;

                while (y >= 0 && gray[idx] > thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    y--;
                    idx -= width;
                }
                if (y < 0 || s1 > maxCount) return false;

                while (y >= 0 && gray[idx] <= thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    y--;
                    idx -= width;
                }
                if (s0 > maxCount) return false;

                y = startY + 1;
                idx = (y * width) + centerX;
                while (y < height && gray[idx] <= thresholds[idx]) {
                    s2++;
                    y++;
                    idx += width;
                }
                if (y == height) return false;

                while (y < height && gray[idx] > thresholds[idx] && s3 < maxCount) {
                    s3++;
                    y++;
                    idx += width;
                }
                if (y == height || s3 >= maxCount) return false;

                while (y < height && gray[idx] <= thresholds[idx] && s4 < maxCount) {
                    s4++;
                    y++;
                    idx += width;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (y >= 0 && gray[idx] > thresholds[idx]) {
                    s2++;
                    y--;
                    idx -= width;
                }
                if (y < 0) return false;

                while (y >= 0 && gray[idx] <= thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    y--;
                    idx -= width;
                }
                if (y < 0 || s1 > maxCount) return false;

                while (y >= 0 && gray[idx] > thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    y--;
                    idx -= width;
                }
                if (s0 > maxCount) return false;

                y = startY + 1;
                idx = (y * width) + centerX;
                while (y < height && gray[idx] > thresholds[idx]) {
                    s2++;
                    y++;
                    idx += width;
                }
                if (y == height) return false;

                while (y < height && gray[idx] <= thresholds[idx] && s3 < maxCount) {
                    s3++;
                    y++;
                    idx += width;
                }
                if (y == height || s3 >= maxCount) return false;

                while (y < height && gray[idx] > thresholds[idx] && s4 < maxCount) {
                    s4++;
                    y++;
                    idx += width;
                }
                if (s4 >= maxCount) return false;
            }
        }

        var total = s0 + s1 + s2 + s3 + s4;
        if (Math.Abs(total - originalTotal) * 5 >= originalTotal * 2) return false;
        if (!FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) return false;

        centerY = CenterFromEnd(s0, s1, s2, s3, s4, y);
        moduleSize = total / 7.0;
        return true;
    }

    private static bool CrossCheckHorizontal(QrGrayImage image, bool invert, int startX, int centerY, int maxCount, int originalTotal, bool aggressive, out double centerX, out double moduleSize) {
        centerX = 0;
        moduleSize = 0;

        var width = image.Width;
        var gray = image.Gray;
        var thresholds = image.ThresholdMap;
        var threshold = image.Threshold;
        var s0 = 0;
        var s1 = 0;
        var s2 = 0;
        var s3 = 0;
        var s4 = 0;

        var x = startX;
        var rowOffset = centerY * width;
        var idx = rowOffset + x;
        if (thresholds is null) {
            if (!invert) {
                while (x >= 0 && gray[idx] <= threshold) {
                    s2++;
                    x--;
                    idx--;
                }
                if (x < 0) return false;

                while (x >= 0 && gray[idx] > threshold && s1 <= maxCount) {
                    s1++;
                    x--;
                    idx--;
                }
                if (x < 0 || s1 > maxCount) return false;

                while (x >= 0 && gray[idx] <= threshold && s0 <= maxCount) {
                    s0++;
                    x--;
                    idx--;
                }
                if (s0 > maxCount) return false;

                x = startX + 1;
                idx = rowOffset + x;
                while (x < width && gray[idx] <= threshold) {
                    s2++;
                    x++;
                    idx++;
                }
                if (x == width) return false;

                while (x < width && gray[idx] > threshold && s3 < maxCount) {
                    s3++;
                    x++;
                    idx++;
                }
                if (x == width || s3 >= maxCount) return false;

                while (x < width && gray[idx] <= threshold && s4 < maxCount) {
                    s4++;
                    x++;
                    idx++;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (x >= 0 && gray[idx] > threshold) {
                    s2++;
                    x--;
                    idx--;
                }
                if (x < 0) return false;

                while (x >= 0 && gray[idx] <= threshold && s1 <= maxCount) {
                    s1++;
                    x--;
                    idx--;
                }
                if (x < 0 || s1 > maxCount) return false;

                while (x >= 0 && gray[idx] > threshold && s0 <= maxCount) {
                    s0++;
                    x--;
                    idx--;
                }
                if (s0 > maxCount) return false;

                x = startX + 1;
                idx = rowOffset + x;
                while (x < width && gray[idx] > threshold) {
                    s2++;
                    x++;
                    idx++;
                }
                if (x == width) return false;

                while (x < width && gray[idx] <= threshold && s3 < maxCount) {
                    s3++;
                    x++;
                    idx++;
                }
                if (x == width || s3 >= maxCount) return false;

                while (x < width && gray[idx] > threshold && s4 < maxCount) {
                    s4++;
                    x++;
                    idx++;
                }
                if (s4 >= maxCount) return false;
            }
        } else {
            if (!invert) {
                while (x >= 0 && gray[idx] <= thresholds[idx]) {
                    s2++;
                    x--;
                    idx--;
                }
                if (x < 0) return false;

                while (x >= 0 && gray[idx] > thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    x--;
                    idx--;
                }
                if (x < 0 || s1 > maxCount) return false;

                while (x >= 0 && gray[idx] <= thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    x--;
                    idx--;
                }
                if (s0 > maxCount) return false;

                x = startX + 1;
                idx = rowOffset + x;
                while (x < width && gray[idx] <= thresholds[idx]) {
                    s2++;
                    x++;
                    idx++;
                }
                if (x == width) return false;

                while (x < width && gray[idx] > thresholds[idx] && s3 < maxCount) {
                    s3++;
                    x++;
                    idx++;
                }
                if (x == width || s3 >= maxCount) return false;

                while (x < width && gray[idx] <= thresholds[idx] && s4 < maxCount) {
                    s4++;
                    x++;
                    idx++;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (x >= 0 && gray[idx] > thresholds[idx]) {
                    s2++;
                    x--;
                    idx--;
                }
                if (x < 0) return false;

                while (x >= 0 && gray[idx] <= thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    x--;
                    idx--;
                }
                if (x < 0 || s1 > maxCount) return false;

                while (x >= 0 && gray[idx] > thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    x--;
                    idx--;
                }
                if (s0 > maxCount) return false;

                x = startX + 1;
                idx = rowOffset + x;
                while (x < width && gray[idx] > thresholds[idx]) {
                    s2++;
                    x++;
                    idx++;
                }
                if (x == width) return false;

                while (x < width && gray[idx] <= thresholds[idx] && s3 < maxCount) {
                    s3++;
                    x++;
                    idx++;
                }
                if (x == width || s3 >= maxCount) return false;

                while (x < width && gray[idx] > thresholds[idx] && s4 < maxCount) {
                    s4++;
                    x++;
                    idx++;
                }
                if (s4 >= maxCount) return false;
            }
        }

        var total = s0 + s1 + s2 + s3 + s4;
        if (Math.Abs(total - originalTotal) * 5 >= originalTotal * 2) return false;
        if (!FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) return false;

        centerX = CenterFromEnd(s0, s1, s2, s3, s4, x);
        moduleSize = total / 7.0;
        return true;
    }

    private static bool CrossCheckDiagonal(QrGrayImage image, bool invert, int centerX, int centerY, int maxCount, int originalTotal, bool aggressive) {
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholds = image.ThresholdMap;
        var threshold = image.Threshold;

        var s0 = 0;
        var s1 = 0;
        var s2 = 0;
        var s3 = 0;
        var s4 = 0;

        var x = centerX;
        var y = centerY;
        var idx = (y * width) + x;
        if (thresholds is null) {
            if (!invert) {
                while (x >= 0 && y >= 0 && gray[idx] <= threshold) {
                    s2++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0) return false;

                while (x >= 0 && y >= 0 && gray[idx] > threshold && s1 <= maxCount) {
                    s1++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0 || s1 > maxCount) return false;

                while (x >= 0 && y >= 0 && gray[idx] <= threshold && s0 <= maxCount) {
                    s0++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (s0 > maxCount) return false;

                x = centerX + 1;
                y = centerY + 1;
                idx = (y * width) + x;
                while (x < width && y < height && gray[idx] <= threshold) {
                    s2++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height) return false;

                while (x < width && y < height && gray[idx] > threshold && s3 < maxCount) {
                    s3++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height || s3 >= maxCount) return false;

                while (x < width && y < height && gray[idx] <= threshold && s4 < maxCount) {
                    s4++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (x >= 0 && y >= 0 && gray[idx] > threshold) {
                    s2++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0) return false;

                while (x >= 0 && y >= 0 && gray[idx] <= threshold && s1 <= maxCount) {
                    s1++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0 || s1 > maxCount) return false;

                while (x >= 0 && y >= 0 && gray[idx] > threshold && s0 <= maxCount) {
                    s0++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (s0 > maxCount) return false;

                x = centerX + 1;
                y = centerY + 1;
                idx = (y * width) + x;
                while (x < width && y < height && gray[idx] > threshold) {
                    s2++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height) return false;

                while (x < width && y < height && gray[idx] <= threshold && s3 < maxCount) {
                    s3++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height || s3 >= maxCount) return false;

                while (x < width && y < height && gray[idx] > threshold && s4 < maxCount) {
                    s4++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (s4 >= maxCount) return false;
            }
        } else {
            if (!invert) {
                while (x >= 0 && y >= 0 && gray[idx] <= thresholds[idx]) {
                    s2++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0) return false;

                while (x >= 0 && y >= 0 && gray[idx] > thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0 || s1 > maxCount) return false;

                while (x >= 0 && y >= 0 && gray[idx] <= thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (s0 > maxCount) return false;

                x = centerX + 1;
                y = centerY + 1;
                idx = (y * width) + x;
                while (x < width && y < height && gray[idx] <= thresholds[idx]) {
                    s2++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height) return false;

                while (x < width && y < height && gray[idx] > thresholds[idx] && s3 < maxCount) {
                    s3++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height || s3 >= maxCount) return false;

                while (x < width && y < height && gray[idx] <= thresholds[idx] && s4 < maxCount) {
                    s4++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (x >= 0 && y >= 0 && gray[idx] > thresholds[idx]) {
                    s2++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0) return false;

                while (x >= 0 && y >= 0 && gray[idx] <= thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0 || s1 > maxCount) return false;

                while (x >= 0 && y >= 0 && gray[idx] > thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (s0 > maxCount) return false;

                x = centerX + 1;
                y = centerY + 1;
                idx = (y * width) + x;
                while (x < width && y < height && gray[idx] > thresholds[idx]) {
                    s2++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height) return false;

                while (x < width && y < height && gray[idx] <= thresholds[idx] && s3 < maxCount) {
                    s3++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height || s3 >= maxCount) return false;

                while (x < width && y < height && gray[idx] > thresholds[idx] && s4 < maxCount) {
                    s4++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (s4 >= maxCount) return false;
            }
        }

        var total = s0 + s1 + s2 + s3 + s4;
        if (Math.Abs(total - originalTotal) * 5 >= originalTotal * 2) return false;
        if (!FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) return false;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBlackAt(byte[] gray, byte[]? thresholds, byte threshold, int index, bool invert) {
        var lum = gray[index];
        var t = thresholds is null ? threshold : thresholds[index];
        var black = lum <= t;
        return invert ? !black : black;
    }
}
#endif
