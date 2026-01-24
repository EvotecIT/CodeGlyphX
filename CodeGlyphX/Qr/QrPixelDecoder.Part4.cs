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
    private static PooledList<Component> FindComponents(QrGrayImage image, bool invert, Func<bool>? shouldStop) {
        var w = image.Width;
        var h = image.Height;
        var comps = new PooledList<Component>(8);
        if (w <= 0 || h <= 0) return comps;

        var total = w * h;
        var minArea = Math.Max(16, total / 400); // ~0.25% of image area
        var visited = ArrayPool<bool>.Shared.Rent(total);
        Array.Clear(visited, 0, total);
        var stack = ArrayPool<int>.Shared.Rent(Math.Max(64, total / 16));
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        var compareThreshold = threshold;

        try {
            if (thresholdMap is null) {
                if (!invert) {
                    for (var y = 0; y < h; y++) {
                        if (shouldStop?.Invoke() == true) break;
                        var row = y * w;
                        for (int x = 0, idx = row; x < w; x++, idx++) {
                            if (visited[idx]) continue;
                            if (gray[idx] > compareThreshold) {
                                visited[idx] = true;
                                continue;
                            }
                            visited[idx] = true;

                            var minX = x;
                            var maxX = x;
                            var minY = y;
                            var maxY = y;
                            var area = 0;

                            var sp = 0;
                            stack[sp++] = idx;

                            while (sp > 0) {
                                if (sp + 4 >= stack.Length) {
                                    GrowStack(ref stack, sp + 4);
                                }
                                var cur = stack[--sp];

                                var cy = cur / w;
                                var cx = cur - cy * w;

                                area++;
                                if (cx < minX) minX = cx;
                                if (cx > maxX) maxX = cx;
                                if (cy < minY) minY = cy;
                                if (cy > maxY) maxY = cy;

                                if (cx > 0) {
                                    var ni = cur - 1;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] <= compareThreshold) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cx + 1 < w) {
                                    var ni = cur + 1;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] <= compareThreshold) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cy > 0) {
                                    var ni = cur - w;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] <= compareThreshold) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cy + 1 < h) {
                                    var ni = cur + w;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] <= compareThreshold) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                            }

                            if (area < minArea) continue;
                            var cw = maxX - minX + 1;
                            var ch = maxY - minY + 1;
                            if (cw < 21 || ch < 21) continue;

                            var ratio = cw > ch ? (double)cw / ch : (double)ch / cw;
                            if (ratio > 2.2) continue;

                            comps.Add(new Component(minX, minY, maxX, maxY, area));
                        }
                    }
                } else {
                    for (var y = 0; y < h; y++) {
                        if (shouldStop?.Invoke() == true) break;
                        var row = y * w;
                        for (int x = 0, idx = row; x < w; x++, idx++) {
                            if (visited[idx]) continue;
                            if (gray[idx] <= compareThreshold) {
                                visited[idx] = true;
                                continue;
                            }
                            visited[idx] = true;

                            var minX = x;
                            var maxX = x;
                            var minY = y;
                            var maxY = y;
                            var area = 0;

                            var sp = 0;
                            stack[sp++] = idx;

                            while (sp > 0) {
                                if (sp + 4 >= stack.Length) {
                                    GrowStack(ref stack, sp + 4);
                                }
                                var cur = stack[--sp];

                                var cy = cur / w;
                                var cx = cur - cy * w;

                                area++;
                                if (cx < minX) minX = cx;
                                if (cx > maxX) maxX = cx;
                                if (cy < minY) minY = cy;
                                if (cy > maxY) maxY = cy;

                                if (cx > 0) {
                                    var ni = cur - 1;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] > compareThreshold) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cx + 1 < w) {
                                    var ni = cur + 1;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] > compareThreshold) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cy > 0) {
                                    var ni = cur - w;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] > compareThreshold) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cy + 1 < h) {
                                    var ni = cur + w;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] > compareThreshold) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                            }

                            if (area < minArea) continue;
                            var cw = maxX - minX + 1;
                            var ch = maxY - minY + 1;
                            if (cw < 21 || ch < 21) continue;

                            var ratio = cw > ch ? (double)cw / ch : (double)ch / cw;
                            if (ratio > 2.2) continue;

                            comps.Add(new Component(minX, minY, maxX, maxY, area));
                        }
                    }
                }
            } else {
                var thresholds = thresholdMap;
                if (!invert) {
                    for (var y = 0; y < h; y++) {
                        if (shouldStop?.Invoke() == true) break;
                        var row = y * w;
                        for (int x = 0, idx = row; x < w; x++, idx++) {
                            if (visited[idx]) continue;
                            if (gray[idx] > thresholds![idx]) {
                                visited[idx] = true;
                                continue;
                            }
                            visited[idx] = true;

                            var minX = x;
                            var maxX = x;
                            var minY = y;
                            var maxY = y;
                            var area = 0;

                            var sp = 0;
                            stack[sp++] = idx;

                            while (sp > 0) {
                                if (sp + 4 >= stack.Length) {
                                    GrowStack(ref stack, sp + 4);
                                }
                                var cur = stack[--sp];

                                var cy = cur / w;
                                var cx = cur - cy * w;

                                area++;
                                if (cx < minX) minX = cx;
                                if (cx > maxX) maxX = cx;
                                if (cy < minY) minY = cy;
                                if (cy > maxY) maxY = cy;

                                if (cx > 0) {
                                    var ni = cur - 1;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] <= thresholds[ni]) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cx + 1 < w) {
                                    var ni = cur + 1;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] <= thresholds[ni]) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cy > 0) {
                                    var ni = cur - w;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] <= thresholds[ni]) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cy + 1 < h) {
                                    var ni = cur + w;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] <= thresholds[ni]) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                            }

                            if (area < minArea) continue;
                            var cw = maxX - minX + 1;
                            var ch = maxY - minY + 1;
                            if (cw < 21 || ch < 21) continue;

                            var ratio = cw > ch ? (double)cw / ch : (double)ch / cw;
                            if (ratio > 2.2) continue;

                            comps.Add(new Component(minX, minY, maxX, maxY, area));
                        }
                    }
                } else {
                    for (var y = 0; y < h; y++) {
                        if (shouldStop?.Invoke() == true) break;
                        var row = y * w;
                        for (int x = 0, idx = row; x < w; x++, idx++) {
                            if (visited[idx]) continue;
                            if (gray[idx] <= thresholds![idx]) {
                                visited[idx] = true;
                                continue;
                            }
                            visited[idx] = true;

                            var minX = x;
                            var maxX = x;
                            var minY = y;
                            var maxY = y;
                            var area = 0;

                            var sp = 0;
                            stack[sp++] = idx;

                            while (sp > 0) {
                                if (sp + 4 >= stack.Length) {
                                    GrowStack(ref stack, sp + 4);
                                }
                                var cur = stack[--sp];

                                var cy = cur / w;
                                var cx = cur - cy * w;

                                area++;
                                if (cx < minX) minX = cx;
                                if (cx > maxX) maxX = cx;
                                if (cy < minY) minY = cy;
                                if (cy > maxY) maxY = cy;

                                if (cx > 0) {
                                    var ni = cur - 1;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] > thresholds[ni]) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cx + 1 < w) {
                                    var ni = cur + 1;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] > thresholds[ni]) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cy > 0) {
                                    var ni = cur - w;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] > thresholds[ni]) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                                if (cy + 1 < h) {
                                    var ni = cur + w;
                                    if (!visited[ni]) {
                                        visited[ni] = true;
                                        if (gray[ni] > thresholds[ni]) {
                                            stack[sp++] = ni;
                                        }
                                    }
                                }
                            }

                            if (area < minArea) continue;
                            var cw = maxX - minX + 1;
                            var ch = maxY - minY + 1;
                            if (cw < 21 || ch < 21) continue;

                            var ratio = cw > ch ? (double)cw / ch : (double)ch / cw;
                            if (ratio > 2.2) continue;

                            comps.Add(new Component(minX, minY, maxX, maxY, area));
                        }
                    }
                }
            }
        } finally {
            ArrayPool<bool>.Shared.Return(visited);
            ArrayPool<int>.Shared.Return(stack);
        }

        return comps;
    }

    private static void GrowStack(ref int[] stack, int size) {
        var newSize = stack.Length * 2;
        if (newSize < size + 8) newSize = size + 8;
        var next = ArrayPool<int>.Shared.Rent(newSize);
        Array.Copy(stack, next, stack.Length);
        ArrayPool<int>.Shared.Return(stack);
        stack = next;
    }

    private static bool TryDecodeFromFinderCandidates(int scale, byte threshold, QrGrayImage image, bool invert, List<QrFinderPatternDetector.FinderPattern> candidates, bool candidatesSorted, Func<QrDecoded, bool>? accept, bool aggressive, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (!candidatesSorted) {
            candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        }
        var tightBudget = budget.Enabled && budget.MaxMilliseconds <= 800;
        var n = Math.Min(candidates.Count, tightBudget ? 5 : (budget.Enabled ? 8 : 12));
        var triedTriples = 0;
        var bboxAttempts = 0;
        var maxTriples = aggressive ? 80 : 40;
        if (budget.Enabled) {
            maxTriples = Math.Min(maxTriples, aggressive ? 40 : 20);
        }
        if (tightBudget && candidates.Count > 12) {
            maxTriples = Math.Min(maxTriples, aggressive ? 16 : 12);
        }

        for (var i = 0; i < n - 2; i++) {
            for (var j = i + 1; j < n - 1; j++) {
                for (var k = j + 1; k < n; k++) {
                    if (budget.IsExpired || budget.IsNearDeadline(120)) return false;
                    triedTriples++;
                    if (triedTriples > maxTriples) return false;
                    var a = candidates[i];
                    var b = candidates[j];
                    var c = candidates[k];

                    var msMin = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                    var msMax = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                    if (msMin <= 0) continue;
                    if (msMax > msMin * 1.75) continue;

                    if (!TryOrderAsTlTrBl(a, b, c, out var tl, out var tr, out var bl)) continue;
                    if (TrySampleAndDecode(scale, threshold, image, invert, tl, tr, bl, candidates.Count, triedTriples, accept, aggressive, budget, out result, out var diag)) {
                        diagnostics = diag;
                        return true;
                    }
                    diagnostics = Better(diagnostics, diag);

                    // If the finder triple looks reasonable but decoding fails (false positives are common in UI),
                    // try a bounded bbox decode around the candidate region before moving on.
                    if (bboxAttempts < 4 && TryGetCandidateBounds(tl, tr, bl, image.Width, image.Height, out var bminX, out var bminY, out var bmaxX, out var bmaxY)) {
                        bboxAttempts++;
                        if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out var diagB, candidates.Count, triedTriples, bminX, bminY, bmaxX, bmaxY)) {
                            diagnostics = diagB;
                            return true;
                        }
                        diagnostics = Better(diagnostics, diagB);
                    }
                }
            }
        }

        return false;
    }

    private readonly struct Component {
        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }
        public int Area { get; }
        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;

        public Component(int minX, int minY, int maxX, int maxY, int area) {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            Area = area;
        }
    }

    private static bool TryOrderAsTlTrBl(
        QrFinderPatternDetector.FinderPattern a,
        QrFinderPatternDetector.FinderPattern b,
        QrFinderPatternDetector.FinderPattern c,
        out QrFinderPatternDetector.FinderPattern tl,
        out QrFinderPatternDetector.FinderPattern tr,
        out QrFinderPatternDetector.FinderPattern bl) {
        tl = default;
        tr = default;
        bl = default;

        var dAB = Dist2(a, b);
        var dAC = Dist2(a, c);
        var dBC = Dist2(b, c);

        // Side lengths are the two smaller distances; they should be similar for a QR finder triangle.
        var maxD = dAB;
        var maxP = 0;
        if (dAC > maxD) { maxD = dAC; maxP = 1; }
        if (dBC > maxD) { maxD = dBC; maxP = 2; }

        double side1, side2;
        if (maxP == 0) { side1 = dAC; side2 = dBC; }
        else if (maxP == 1) { side1 = dAB; side2 = dBC; }
        else { side1 = dAB; side2 = dAC; }

        if (side1 <= 0 || side2 <= 0) return false;
        var ratio = side1 > side2 ? side1 / side2 : side2 / side1;
        if (ratio > 1.8) return false;

        // The point shared by the two shorter distances is top-left.
        if (maxP == 0) {
            tl = c;
            tr = a;
            bl = b;
        } else if (maxP == 1) {
            tl = b;
            tr = a;
            bl = c;
        } else {
            tl = a;
            tr = b;
            bl = c;
        }

        // Ensure clockwise orientation: tr should be to the right and bl below.
        var cross = Cross(tr.X - tl.X, tr.Y - tl.Y, bl.X - tl.X, bl.Y - tl.Y);
        if (cross < 0) (tr, bl) = (bl, tr);

        return true;
    }

    private static double Dist2(QrFinderPatternDetector.FinderPattern a, QrFinderPatternDetector.FinderPattern b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;

    private static bool TrySampleAndDecode(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, bool aggressive, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        var moduleSize = (tl.ModuleSize + tr.ModuleSize + bl.ModuleSize) / 3.0;
        if (moduleSize <= 0) return false;

        var tightBudget = budget.Enabled && budget.MaxMilliseconds <= 800;
        var distX = Distance(tl.X, tl.Y, tr.X, tr.Y);
        var distY = Distance(tl.X, tl.Y, bl.X, bl.Y);
        if (distX <= 0 || distY <= 0) return false;

        var dimH = QrMath.RoundToInt(distX / moduleSize) + 7;
        var dimV = QrMath.RoundToInt(distY / moduleSize) + 7;

        // Try a few nearby dimensions (estimation can be off on UI-scaled QR).
        var baseDim = NearestValidDimension((dimH + dimV) / 2);
        Span<int> candidates = stackalloc int[10];
        var candidatesCount = 0;
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim);
        var dimHc = NearestValidDimension(dimH);
        var dimVc = NearestValidDimension(dimV);
        if (tightBudget) {
            if (Math.Abs(dimHc - baseDim) >= 4) {
                AddDimensionCandidate(ref candidates, ref candidatesCount, dimHc);
            }
            if (Math.Abs(dimVc - baseDim) >= 4) {
                AddDimensionCandidate(ref candidates, ref candidatesCount, dimVc);
            }
        } else {
            AddDimensionCandidate(ref candidates, ref candidatesCount, dimHc);
            AddDimensionCandidate(ref candidates, ref candidatesCount, dimVc);
            AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 4);
            AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 8);
            AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 12);
            AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 4);
            AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 8);
            AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 12);
        }

        for (var i = 0; i < candidatesCount; i++) {
            if (budget.IsExpired) return false;
            var dimension = candidates[i];
            if (dimension is < 21 or > 177) continue;
            if (TrySampleAndDecodeDimension(scale, threshold, image, invert, tl, tr, bl, dimension, candidateCount, candidateTriplesTried, accept, aggressive, budget, out result, out diagnostics)) return true;
        }

        return false;
    }

    private static void AddDimensionCandidate(ref Span<int> list, ref int count, int dimension) {
        if (dimension is < 21 or > 177) return;
        if ((dimension & 3) != 1) return;
        for (var i = 0; i < count; i++) {
            if (list[i] == dimension) return;
        }
        list[count++] = dimension;
    }

    private static bool TrySampleAndDecodeDimension(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int dimension, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, bool aggressive, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        var scratch = new global::CodeGlyphX.BitMatrix(dimension, dimension);
        var modulesBetweenCenters = dimension - 7;
        if (modulesBetweenCenters <= 0) return false;

        // Use affine mapping based on the three finder centers (no perspective correction yet).
        var vxX = (tr.X - tl.X) / modulesBetweenCenters;
        var vxY = (tr.Y - tl.Y) / modulesBetweenCenters;
        var vyX = (bl.X - tl.X) / modulesBetweenCenters;
        var vyY = (bl.Y - tl.Y) / modulesBetweenCenters;

        // First try sampling using only the raw finder centers (fast path for clean, axis-aligned on-screen QR).
        const double finderCenterToCorner = 3.5;

        var moduleSize0 = (Math.Sqrt(vxX * vxX + vxY * vxY) + Math.Sqrt(vyX * vyX + vyY * vyY)) / 2.0;

        var cornerTlX0 = tl.X - (vxX + vyX) * finderCenterToCorner;
        var cornerTlY0 = tl.Y - (vxY + vyY) * finderCenterToCorner;

        var cornerTrX0 = cornerTlX0 + vxX * dimension;
        var cornerTrY0 = cornerTlY0 + vxY * dimension;

        var cornerBlX0 = cornerTlX0 + vyX * dimension;
        var cornerBlY0 = cornerTlY0 + vyY * dimension;

        var cornerBrX0 = cornerTlX0 + (vxX + vyX) * dimension;
        var cornerBrY0 = cornerTlY0 + (vxY + vyY) * dimension;

        if (TrySampleWithCorners(image, invert, phaseX: 0, phaseY: 0, dimension, scratch, cornerTlX0, cornerTlY0, cornerTrX0, cornerTrY0, cornerBrX0, cornerBrY0, cornerBlX0, cornerBlY0, moduleSize0, accept, aggressive, budget, out result, out var moduleDiag0)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiag0);
            return true;
        }

        var best = moduleDiag0;
        var tightBudget = budget.Enabled && budget.MaxMilliseconds <= 800;

        // Refine sampling using format/timing patterns as a score:
        // - phase (sub-module offsets) + small scale adjustment of vx/vy (finder centers can be slightly off).
        double phaseX;
        double phaseY;
        if (tightBudget) {
            QrPixelSampling.RefinePhase(image, invert, tl.X, tl.Y, vxX, vxY, vyX, vyY, dimension, out phaseX, out phaseY);
        } else {
            QrPixelSampling.RefineTransform(image, invert, tl.X, tl.Y, vxX, vxY, vyX, vyY, dimension, out vxX, out vxY, out vyX, out vyY, out phaseX, out phaseY);
        }

        var moduleSize = (Math.Sqrt(vxX * vxX + vxY * vxY) + Math.Sqrt(vyX * vyX + vyY * vyY)) / 2.0;

        // Build an initial estimate of the QR outer corners from the three finder centers and the per-module vectors.
        var cornerTlX = tl.X - (vxX + vyX) * finderCenterToCorner;
        var cornerTlY = tl.Y - (vxY + vyY) * finderCenterToCorner;

        var cornerTrX = cornerTlX + vxX * dimension;
        var cornerTrY = cornerTlY + vxY * dimension;

        var cornerBlX = cornerTlX + vyX * dimension;
        var cornerBlY = cornerTlY + vyY * dimension;

        var cornerBrXr0 = cornerTlX + (vxX + vyX) * dimension;
        var cornerBrYr0 = cornerTlY + (vxY + vyY) * dimension;

        // Then try with refined phase/scale. Alignment pattern detection can produce false positives on busy UI regions;
        // use it as a fallback only.
        if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, scratch, cornerTlX, cornerTlY, cornerTrX, cornerTrY, cornerBrXr0, cornerBrYr0, cornerBlX, cornerBlY, moduleSize, accept, aggressive, budget, out result, out var moduleDiagR)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagR);
            return true;
        }

        best = Better(best, moduleDiagR);
        if (tightBudget) {
            if (aggressive &&
                moduleDiagR.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload &&
                !budget.IsNearDeadline(150)) {
                QrPixelSampling.RefineTransform(image, invert, tl.X, tl.Y, vxX, vxY, vyX, vyY, dimension, out var rvxX, out var rvxY, out var rvyX, out var rvyY, out var rPhaseX, out var rPhaseY);

                var rModuleSize = (Math.Sqrt(rvxX * rvxX + rvxY * rvxY) + Math.Sqrt(rvyX * rvyX + rvyY * rvyY)) / 2.0;
                var rCornerTlX = tl.X - (rvxX + rvyX) * finderCenterToCorner;
                var rCornerTlY = tl.Y - (rvxY + rvyY) * finderCenterToCorner;
                var rCornerTrX = rCornerTlX + rvxX * dimension;
                var rCornerTrY = rCornerTlY + rvxY * dimension;
                var rCornerBlX = rCornerTlX + rvyX * dimension;
                var rCornerBlY = rCornerTlY + rvyY * dimension;
                var rCornerBrX = rCornerTlX + (rvxX + rvyX) * dimension;
                var rCornerBrY = rCornerTlY + (rvxY + rvyY) * dimension;

                if (TrySampleWithCorners(image, invert, rPhaseX, rPhaseY, dimension, scratch, rCornerTlX, rCornerTlY, rCornerTrX, rCornerTrY, rCornerBrX, rCornerBrY, rCornerBlX, rCornerBlY, rModuleSize, accept, aggressive, budget, out result, out var moduleDiagT)) {
                    diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagT);
                    return true;
                }

                best = Better(best, moduleDiagT);
            }

            if (aggressive &&
                moduleDiagR.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload &&
                !budget.IsNearDeadline(150)) {
                if (TrySampleWithPhaseJitterLite(
                        image,
                        invert,
                        phaseX,
                        phaseY,
                        dimension,
                        scratch,
                        cornerTlX,
                        cornerTlY,
                        cornerTrX,
                        cornerTrY,
                        cornerBrXr0,
                        cornerBrYr0,
                        cornerBlX,
                        cornerBlY,
                        moduleSize,
                        accept,
                        aggressive,
                        budget,
                        out result,
                        out var moduleDiagLite)) {
                    diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagLite);
                    return true;
                }

                best = Better(best, moduleDiagLite);
            }

            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, best);
            return false;
        }

        // Small perspective tuning: jitter the bottom-right corner in module-space to handle mild skew.
        if (TrySampleWithCornerJitter(
                image,
                invert,
                phaseX,
                phaseY,
                dimension,
                scratch,
                cornerTlX,
                cornerTlY,
                cornerTrX,
                cornerTrY,
                cornerBlX,
                cornerBlY,
                cornerBrXr0,
                cornerBrYr0,
                vxX,
                vxY,
                vyX,
                vyY,
                moduleSize,
                accept,
                aggressive,
                budget,
                out result,
                out var moduleDiagJ)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagJ);
            return true;
        }

        best = Better(best, moduleDiagJ);

        // Try to find the bottom-right alignment pattern (helps a lot on UI-scaled QR where module pitch isn't perfectly uniform).
        var version = (dimension - 17) / 4;
        if (version >= 2) {
            var align = QrTables.GetAlignmentPatternPositions(version);
            if (align.Length > 0) {
                var a = align[align.Length - 1]; // bottom-right
                var dxA = a - 3 + phaseX;
                var dyA = a - 3 + phaseY;
                var predX = tl.X + vxX * dxA + vyX * dyA;
                var predY = tl.Y + vxY * dxA + vyY * dyA;

                if (QrAlignmentPatternFinder.TryFind(image, invert, predX, predY, vxX, vxY, vyX, vyY, moduleSize, out var ax, out var ay)) {
                    // Convert the alignment center back into an estimated outer bottom-right corner.
                    // The bottom-right alignment center is at (dimension-6.5, dimension-6.5) in module-center coordinates,
                    // i.e. 6.5 modules inward from the outer corner along both axes.
                    var cornerBrXA = ax + (vxX + vyX) * 6.5;
                    var cornerBrYA = ay + (vxY + vyY) * 6.5;

                    if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, scratch, cornerTlX, cornerTlY, cornerTrX, cornerTrY, cornerBrXA, cornerBrYA, cornerBlX, cornerBlY, moduleSize, accept, aggressive, budget, out result, out var moduleDiagA)) {
                        diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagA);
                        return true;
                    }

                    best = Better(best, moduleDiagA);
                }
            }
        }

        var moduleDiagP = default(global::CodeGlyphX.QrDecodeDiagnostics);
        if (aggressive && TrySampleWithPhaseJitter(
                image,
                invert,
                phaseX,
                phaseY,
                dimension,
                scratch,
                cornerTlX,
                cornerTlY,
                cornerTrX,
                cornerTrY,
                cornerBrXr0,
                cornerBrYr0,
                cornerBlX,
                cornerBlY,
                moduleSize,
                accept,
                aggressive,
                budget,
                out result,
                out moduleDiagP)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagP);
            return true;
        }

        if (aggressive) {
            best = Better(best, moduleDiagP);
        }

        diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, best);
        return false;
    }

    private static bool TrySampleWithCornerJitter(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        global::CodeGlyphX.BitMatrix scratch,
        double cornerTlX,
        double cornerTlY,
        double cornerTrX,
        double cornerTrY,
        double cornerBlX,
        double cornerBlY,
        double cornerBrX,
        double cornerBrY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        double moduleSizePx,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        if (budget.IsExpired) return false;

        Span<double> offsets = stackalloc double[3];
        offsets[0] = -0.60;
        offsets[1] = 0.0;
        offsets[2] = 0.60;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);
        const double jitterEpsilon = 1e-9;

        for (var yi = 0; yi < offsets.Length; yi++) {
            if (budget.IsExpired) return false;
            var oy = offsets[yi];
            for (var xi = 0; xi < offsets.Length; xi++) {
                if (budget.IsExpired) return false;
                var ox = offsets[xi];
                if (Math.Abs(ox) <= jitterEpsilon && Math.Abs(oy) <= jitterEpsilon) continue;

                var jx = cornerBrX + vxX * ox + vyX * oy;
                var jy = cornerBrY + vxY * ox + vyY * oy;

                if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, scratch, cornerTlX, cornerTlY, cornerTrX, cornerTrY, jx, jy, cornerBlX, cornerBlY, moduleSizePx, accept, aggressive, budget, out result, out var diag)) {
                    moduleDiagnostics = diag;
                    return true;
                }

                best = Better(best, diag);
            }
        }

        moduleDiagnostics = best;
        return false;
    }

    private static bool TrySampleWithPhaseJitterLite(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        global::CodeGlyphX.BitMatrix scratch,
        double cornerTlX,
        double cornerTlY,
        double cornerTrX,
        double cornerTrY,
        double cornerBrX,
        double cornerBrY,
        double cornerBlX,
        double cornerBlY,
        double moduleSizePx,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        if (budget.IsExpired) return false;

        Span<double> offsets = stackalloc double[2];
        offsets[0] = -0.25;
        offsets[1] = 0.25;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);

        for (var i = 0; i < offsets.Length; i++) {
            if (budget.IsExpired) return false;
            var ox = phaseX + offsets[i];
            if (TrySampleWithCorners(
                    image,
                    invert,
                    ox,
                    phaseY,
                    dimension,
                    scratch,
                    cornerTlX,
                    cornerTlY,
                    cornerTrX,
                    cornerTrY,
                    cornerBrX,
                    cornerBrY,
                    cornerBlX,
                    cornerBlY,
                    moduleSizePx,
                    accept,
                    aggressive,
                    budget,
                    out result,
                    out var diag)) {
                moduleDiagnostics = diag;
                return true;
            }

            best = Better(best, diag);
        }

        for (var i = 0; i < offsets.Length; i++) {
            if (budget.IsExpired) return false;
            var oy = phaseY + offsets[i];
            if (TrySampleWithCorners(
                    image,
                    invert,
                    phaseX,
                    oy,
                    dimension,
                    scratch,
                    cornerTlX,
                    cornerTlY,
                    cornerTrX,
                    cornerTrY,
                    cornerBrX,
                    cornerBrY,
                    cornerBlX,
                    cornerBlY,
                    moduleSizePx,
                    accept,
                    aggressive,
                    budget,
                    out result,
                    out var diag)) {
                moduleDiagnostics = diag;
                return true;
            }

            best = Better(best, diag);
        }

        moduleDiagnostics = best;
        return false;
    }

    private static bool TrySampleWithPhaseJitter(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        global::CodeGlyphX.BitMatrix scratch,
        double cornerTlX,
        double cornerTlY,
        double cornerTrX,
        double cornerTrY,
        double cornerBrX,
        double cornerBrY,
        double cornerBlX,
        double cornerBlY,
        double moduleSizePx,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        if (budget.IsExpired) return false;

        Span<double> offsets = stackalloc double[3];
        offsets[0] = -0.35;
        offsets[1] = 0.0;
        offsets[2] = 0.35;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);
        const double jitterEpsilon = 1e-9;

        for (var yi = 0; yi < offsets.Length; yi++) {
            if (budget.IsExpired) return false;
            var oy = phaseY + offsets[yi];
            for (var xi = 0; xi < offsets.Length; xi++) {
                if (budget.IsExpired) return false;
                var ox = phaseX + offsets[xi];
                if (Math.Abs(ox - phaseX) <= jitterEpsilon && Math.Abs(oy - phaseY) <= jitterEpsilon) continue;

                if (TrySampleWithCorners(
                        image,
                        invert,
                        ox,
                        oy,
                        dimension,
                        scratch,
                        cornerTlX,
                        cornerTlY,
                        cornerTrX,
                        cornerTrY,
                        cornerBrX,
                        cornerBrY,
                        cornerBlX,
                        cornerBlY,
                        moduleSizePx,
                        accept,
                        aggressive,
                        budget,
                        out result,
                        out var diag)) {
                    moduleDiagnostics = diag;
                    return true;
                }

                best = Better(best, diag);
            }
        }

        moduleDiagnostics = best;
        return false;
    }

}
#endif
