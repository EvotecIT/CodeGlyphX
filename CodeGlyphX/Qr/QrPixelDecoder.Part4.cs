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
    private static PooledList<Component> FindComponents(
        QrGrayImage image,
        bool invert,
        Func<bool>? shouldStop,
        int minAreaDivisor,
        int minComponentSize) {
        var w = image.Width;
        var h = image.Height;
        var comps = new PooledList<Component>(8);
        if (w <= 0 || h <= 0) return comps;

        var total = w * h;
        if (minAreaDivisor <= 0) minAreaDivisor = 400;
        if (minComponentSize < 6) minComponentSize = 6;
        var minArea = Math.Max(16, total / minAreaDivisor); // ~0.25% default
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
                            if (cw < minComponentSize || ch < minComponentSize) continue;

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
                            if (cw < minComponentSize || ch < minComponentSize) continue;

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
                            if (cw < minComponentSize || ch < minComponentSize) continue;

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
                            if (cw < minComponentSize || ch < minComponentSize) continue;

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

    private static bool TryDecodeFromFinderCandidates(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        bool candidatesSorted,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics,
        double moduleRatioLimit = 1.75,
        double sideRatioLimit = 1.8,
        int maxTriplesOverride = 0,
        int maxCandidatesOverride = 0) {
        result = null!;
        diagnostics = default;

        if (!candidatesSorted) {
            candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        }
        var tightBudget = budget.Enabled && budget.MaxMilliseconds <= 800;
        var nLimit = maxCandidatesOverride > 0 ? maxCandidatesOverride : (budget.Enabled ? (aggressive ? 16 : 8) : (aggressive ? 20 : 12));
        var n = Math.Min(candidates.Count, tightBudget ? 5 : nLimit);
        var triedTriples = 0;
        var bboxAttempts = 0;
        var maxTriples = maxTriplesOverride > 0 ? maxTriplesOverride : (aggressive ? 160 : 40);
        if (budget.Enabled && maxTriplesOverride <= 0) {
            maxTriples = Math.Min(maxTriples, aggressive ? 80 : 20);
        }
        if (tightBudget && candidates.Count > 12) {
            maxTriples = Math.Min(maxTriples, aggressive ? 24 : 12);
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
                    if (msMax > msMin * moduleRatioLimit) continue;

                    if (!TryOrderAsTlTrBl(a, b, c, sideRatioLimit, out var tl, out var tr, out var bl)) continue;
                    if (TrySampleAndDecode(scale, threshold, image, invert, tl, tr, bl, candidates.Count, triedTriples, accept, aggressive, stylized, budget, out result, out var diag)) {
                        diagnostics = diag;
                        return true;
                    }
                    diagnostics = Better(diagnostics, diag);

                    // If the finder triple looks reasonable but decoding fails (false positives are common in UI),
                    // try a bounded bbox decode around the candidate region before moving on.
                    if (bboxAttempts < 4 && TryGetCandidateBounds(tl, tr, bl, image.Width, image.Height, out var bminX, out var bminY, out var bmaxX, out var bmaxY)) {
                        bboxAttempts++;
                        if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out var diagB, candidates.Count, triedTriples, bminX, bminY, bmaxX, bmaxY)) {
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

    private static bool TryDecodeFromComponentFinders(
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
        if (TryDecodeFromComponentFindersCore(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagnostics)) {
            return true;
        }

        if (aggressive && !budget.IsNearDeadline(200)) {
            var edge = image.WithBinaryEdge(1);
            if (TryDecodeFromComponentFindersCore(scale, edge.Threshold, edge, invert, accept, aggressive, stylized, budget, out result, out diagnostics)) {
                return true;
            }
        }

        return false;
    }

    private static bool TryDecodeFromComponentFindersCore(
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

        if (budget.IsExpired) return false;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        var minDim = Math.Min(image.Width, image.Height);
        var minAreaDivisor = aggressive ? 2000 : 400;
        var minComponentSize = aggressive ? Math.Max(12, minDim / 90) : 21;
        using var comps = FindComponents(image, invert, shouldStop, minAreaDivisor, minComponentSize);
        if (comps.Count < 3) return false;

        var minSize = Math.Max(10, (int)Math.Round(minDim * (aggressive ? 0.03 : 0.05)));
        var maxSize = Math.Max(minSize + 4, (int)Math.Round(minDim * 0.65));

        Span<QrFinderPatternDetector.FinderPattern> top = stackalloc QrFinderPatternDetector.FinderPattern[20];
        var topCount = 0;

        for (var i = 0; i < comps.Count; i++) {
            if (budget.IsExpired || budget.IsNearDeadline(120)) return false;
            var c = comps[i];
            var w = c.Width;
            var h = c.Height;
            if (w < minSize || h < minSize) continue;
            if (w > maxSize || h > maxSize) continue;

            var ratio = w > h ? (double)w / h : (double)h / w;
            if (ratio > (aggressive ? 1.7 : 1.45)) continue;

            var fillRatio = c.Area / (double)(w * h);
            if (fillRatio < (aggressive ? 0.04 : 0.08)) continue;

            var moduleSize = (w + h) * 0.5 / 7.0;
            if (moduleSize < 0.9) continue;

            var cx = (c.MinX + c.MaxX) * 0.5;
            var cy = (c.MinY + c.MaxY) * 0.5;
            var candidate = new QrFinderPatternDetector.FinderPattern(cx, cy, moduleSize, c.Area);

            if (topCount < top.Length) {
                top[topCount++] = candidate;
                continue;
            }
            var replaceIndex = -1;
            var minArea = int.MaxValue;
            for (var t = 0; t < top.Length; t++) {
                if (top[t].Count < minArea) {
                    minArea = top[t].Count;
                    replaceIndex = t;
                }
            }
            if (replaceIndex >= 0 && candidate.Count > minArea) {
                top[replaceIndex] = candidate;
            }
        }

        if (topCount < 3) return false;
        var list = RentCandidateList();
        try {
            list.Clear();
            for (var i = 0; i < topCount; i++) {
                list.Add(top[i]);
            }
            return TryDecodeFromFinderCandidates(scale, threshold, image, invert, list, candidatesSorted: false, accept, aggressive, stylized, budget, out result, out diagnostics);
        } finally {
            ReturnCandidateList(list);
        }
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
        double ratioLimit,
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
        if (ratio > ratioLimit) return false;

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

    private static bool TryDecodeFromTemplateFinders(
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

        if (budget.IsExpired || budget.IsNearDeadline(250)) return false;
        var w = image.Width;
        var h = image.Height;
        if (w < 64 || h < 64) return false;

        var minDim = Math.Min(w, h);
        var minMs = Math.Max(2, minDim / 200);
        var maxMs = Math.Min(40, Math.Max(minMs + 1, minDim / 8));

        var stride = w + 1;
        var length = stride * (h + 1);
        var integral = ArrayPool<int>.Shared.Rent(length);
        Array.Clear(integral, 0, length);

        try {
            var gray = image.Gray;
            var thresholds = image.ThresholdMap;
            var t = image.Threshold;

            for (var y = 1; y <= h; y++) {
                if (budget.IsNearDeadline(200)) return false;
                var rowSum = 0;
                var row = (y - 1) * w;
                var baseIdx = y * stride;
                var prevIdx = (y - 1) * stride;
                for (var x = 1; x <= w; x++) {
                    var idx = row + (x - 1);
                    var th = thresholds is null ? t : thresholds[idx];
                    var isBlack = invert ? gray[idx] > th : gray[idx] <= th;
                    if (isBlack) rowSum++;
                    integral[baseIdx + x] = integral[prevIdx + x] + rowSum;
                }
            }

            Span<Candidate> candidates = stackalloc Candidate[64];
            var candidateCount = 0;

            var minOuterRatio = aggressive ? 0.25 : 0.35;
            var minCenterRatio = aggressive ? 0.25 : 0.35;
            var maxInnerRatio = aggressive ? 0.85 : 0.75;
            var minScore = aggressive ? 0.30 : 0.45;

            for (var ms = minMs; ms <= maxMs; ms += ms < 6 ? 1 : 2) {
                if (budget.IsNearDeadline(180)) break;
                var halfOuter = (int)Math.Round(ms * 3.5);
                var halfInner = (int)Math.Round(ms * 2.5);
                var halfCenter = (int)Math.Round(ms * 1.5);
                if (halfCenter < 1) continue;
                var step = Math.Max(2, (int)Math.Round(ms * 1.0));

                var minX = halfOuter;
                var maxX = w - 1 - halfOuter;
                var minY = halfOuter;
                var maxY = h - 1 - halfOuter;
                if (minX >= maxX || minY >= maxY) continue;

                for (var cy = minY; cy <= maxY; cy += step) {
                    if (budget.IsNearDeadline(180)) break;
                    for (var cx = minX; cx <= maxX; cx += step) {
                        var outer = SumRect(integral, stride, cx - halfOuter, cy - halfOuter, cx + halfOuter, cy + halfOuter);
                        var inner = SumRect(integral, stride, cx - halfInner, cy - halfInner, cx + halfInner, cy + halfInner);
                        var center = SumRect(integral, stride, cx - halfCenter, cy - halfCenter, cx + halfCenter, cy + halfCenter);

                        var outerArea = (2 * halfOuter + 1) * (2 * halfOuter + 1);
                        var innerArea = (2 * halfInner + 1) * (2 * halfInner + 1);
                        var centerArea = (2 * halfCenter + 1) * (2 * halfCenter + 1);
                        var ringOuterArea = outerArea - innerArea;
                        var ringInnerArea = innerArea - centerArea;
                        if (ringOuterArea <= 0 || ringInnerArea <= 0 || centerArea <= 0) continue;

                        var outerRatio = outerArea > 0 ? (outer - inner) / (double)ringOuterArea : 0.0;
                        var centerRatio = center / (double)centerArea;
                        var innerRatio = ringInnerArea > 0 ? (inner - center) / (double)ringInnerArea : 1.0;

                        if (outerRatio < minOuterRatio || centerRatio < minCenterRatio || innerRatio > maxInnerRatio) continue;
                        var score = outerRatio + centerRatio - innerRatio;
                        if (score < minScore) continue;

                        var candidate = new Candidate(cx + 0.5, cy + 0.5, ms, score);
                        AddCandidate(ref candidates, ref candidateCount, candidate);
                    }
                }
            }

            if (candidateCount < 3) return false;

            var list = RentCandidateList();
            try {
                list.Clear();
                for (var i = 0; i < candidateCount; i++) {
                    var c = candidates[i];
                    list.Add(new QrFinderPatternDetector.FinderPattern(c.X, c.Y, c.ModuleSize, (int)(c.Score * 1000)));
                }
                return TryDecodeFromFinderCandidates(scale, threshold, image, invert, list, candidatesSorted: false, accept, aggressive, stylized, budget, out result, out diagnostics);
            } finally {
                ReturnCandidateList(list);
            }
        } finally {
            ArrayPool<int>.Shared.Return(integral, clearArray: false);
        }
    }

    private static int SumRect(int[] integral, int stride, int x0, int y0, int x1, int y1) {
        var sx0 = x0;
        var sy0 = y0;
        var sx1 = x1 + 1;
        var sy1 = y1 + 1;
        var a = integral[sy0 * stride + sx0];
        var b = integral[sy0 * stride + sx1];
        var c = integral[sy1 * stride + sx0];
        var d = integral[sy1 * stride + sx1];
        return d - b - c + a;
    }

    private readonly struct Candidate {
        public double X { get; }
        public double Y { get; }
        public double ModuleSize { get; }
        public double Score { get; }

        public Candidate(double x, double y, double moduleSize, double score) {
            X = x;
            Y = y;
            ModuleSize = moduleSize;
            Score = score;
        }
    }

    private static void AddCandidate(ref Span<Candidate> list, ref int count, Candidate candidate) {
        var maxCount = list.Length;
        if (count < maxCount) {
            list[count++] = candidate;
            return;
        }
        var minIndex = 0;
        var minScore = list[0].Score;
        for (var i = 1; i < maxCount; i++) {
            if (list[i].Score < minScore) {
                minScore = list[i].Score;
                minIndex = i;
            }
        }
        if (candidate.Score > minScore) {
            list[minIndex] = candidate;
        }
    }

    private static bool TryEstimateModuleSizeFromTiming(
        QrGrayImage image,
        bool invert,
        QrFinderPatternDetector.FinderPattern tl,
        QrFinderPatternDetector.FinderPattern tr,
        QrFinderPatternDetector.FinderPattern bl,
        double moduleSizeSeed,
        out double moduleSize) {
        moduleSize = moduleSizeSeed;
        if (moduleSizeSeed <= 0) return false;

        var dx = tr.X - tl.X;
        var dy = tr.Y - tl.Y;
        var distH = Math.Sqrt(dx * dx + dy * dy);
        if (distH <= 0) return false;

        var vx = dx / distH;
        var vy = dy / distH;
        var nx = -vy;
        var ny = vx;

        var offset = (6.0 - 3.5) * moduleSizeSeed;
        var startPad = moduleSizeSeed * 7.0;

        var hStartX = tl.X + vx * startPad + nx * offset;
        var hStartY = tl.Y + vy * startPad + ny * offset;
        var hEndX = tr.X - vx * startPad + nx * offset;
        var hEndY = tr.Y - vy * startPad + ny * offset;

        var hSizeOk = TryEstimateRunLength(image, invert, hStartX, hStartY, hEndX, hEndY, moduleSizeSeed, out var hModule);

        var dxV = bl.X - tl.X;
        var dyV = bl.Y - tl.Y;
        var distV = Math.Sqrt(dxV * dxV + dyV * dyV);
        if (distV <= 0) {
            if (hSizeOk) {
                moduleSize = hModule;
                return true;
            }
            return false;
        }

        var vxV = dxV / distV;
        var vyV = dyV / distV;
        var nxV = -vyV;
        var nyV = vxV;

        var vStartX = tl.X + vxV * startPad - nxV * offset;
        var vStartY = tl.Y + vyV * startPad - nyV * offset;
        var vEndX = bl.X - vxV * startPad - nxV * offset;
        var vEndY = bl.Y - vyV * startPad - nyV * offset;

        var vSizeOk = TryEstimateRunLength(image, invert, vStartX, vStartY, vEndX, vEndY, moduleSizeSeed, out var vModule);

        if (hSizeOk && vSizeOk) {
            moduleSize = (hModule + vModule) * 0.5;
            return true;
        }
        if (hSizeOk) {
            moduleSize = hModule;
            return true;
        }
        if (vSizeOk) {
            moduleSize = vModule;
            return true;
        }
        return false;
    }

    private static bool TryDecodeFromEdgeSquares(
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

        if (budget.IsExpired || budget.IsNearDeadline(250)) return false;
        var edge = image.WithBinaryEdge(1);
        var w = edge.Width;
        var h = edge.Height;
        if (w < 48 || h < 48) return false;

        var stride = w + 1;
        var length = stride * (h + 1);
        var integral = ArrayPool<int>.Shared.Rent(length);
        Array.Clear(integral, 0, length);

        try {
            var gray = edge.Gray;
            var thresholdE = edge.Threshold;

            for (var y = 1; y <= h; y++) {
                if (budget.IsNearDeadline(200)) return false;
                var rowSum = 0;
                var row = (y - 1) * w;
                var baseIdx = y * stride;
                var prevIdx = (y - 1) * stride;
                for (var x = 1; x <= w; x++) {
                    var idx = row + (x - 1);
                    if (gray[idx] <= thresholdE) rowSum++;
                    integral[baseIdx + x] = integral[prevIdx + x] + rowSum;
                }
            }

            Span<int> sizes = stackalloc int[8];
            var sizeCount = 0;
            var minDim = Math.Min(w, h);
            AddSquareSize(ref sizes, ref sizeCount, 80);
            AddSquareSize(ref sizes, ref sizeCount, 120);
            AddSquareSize(ref sizes, ref sizeCount, 160);
            AddSquareSize(ref sizes, ref sizeCount, minDim / 4);
            AddSquareSize(ref sizes, ref sizeCount, minDim / 3);
            AddSquareSize(ref sizes, ref sizeCount, minDim / 2);

            Span<SquareCandidate> candidates = stackalloc SquareCandidate[24];
            var candidateCount = 0;

            for (var s = 0; s < sizeCount; s++) {
                if (budget.IsNearDeadline(200)) break;
                var size = sizes[s];
                if (size < 48 || size > minDim) continue;
                var ring = Math.Max(2, size / 12);
                var inner = size - ring * 2;
                if (inner < 8) continue;
                var ringArea = size * size - inner * inner;
                var step = Math.Max(4, size / 4);

                for (var y = 0; y + size <= h; y += step) {
                    if (budget.IsNearDeadline(200)) break;
                    for (var x = 0; x + size <= w; x += step) {
                        var outer = SumRect(integral, stride, x, y, x + size - 1, y + size - 1);
                        var innerSum = SumRect(integral, stride, x + ring, y + ring, x + ring + inner - 1, y + ring + inner - 1);
                        var ringEdges = outer - innerSum;
                        if (ringEdges <= 0) continue;
                        var score = ringEdges / (double)ringArea;
                        if (score < 0.18) continue;
                        var candidate = new SquareCandidate(x, y, size, score);
                        AddSquareCandidate(ref candidates, ref candidateCount, candidate);
                    }
                }
            }

            if (candidateCount == 0) return false;
            SortCandidates(ref candidates, candidateCount);

            for (var i = 0; i < candidateCount; i++) {
                if (budget.IsExpired || budget.IsNearDeadline(150)) return false;
                var c = candidates[i];
                var pad = Math.Max(2, (int)Math.Round(c.Size * 0.06));
                var x0 = Math.Max(0, c.X - pad);
                var y0 = Math.Max(0, c.Y - pad);
                var x1 = Math.Min(w - 1, c.X + c.Size - 1 + pad);
                var y1 = Math.Min(h - 1, c.Y + c.Size - 1 + pad);

                if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, stylized, budget, out result, out diagnostics, candidateCount: 0, candidateTriplesTried: 0, x0, y0, x1, y1)) {
                    return true;
                }
            }
        } finally {
            ArrayPool<int>.Shared.Return(integral, clearArray: false);
        }

        return false;
    }

    private readonly struct SquareCandidate {
        public int X { get; }
        public int Y { get; }
        public int Size { get; }
        public double Score { get; }

        public SquareCandidate(int x, int y, int size, double score) {
            X = x;
            Y = y;
            Size = size;
            Score = score;
        }
    }

    private static void AddSquareSize(ref Span<int> list, ref int count, int size) {
        if (size <= 0) return;
        for (var i = 0; i < count; i++) {
            if (list[i] == size) return;
        }
        if (count < list.Length) {
            list[count++] = size;
        }
    }

    private static void AddSquareCandidate(ref Span<SquareCandidate> list, ref int count, SquareCandidate candidate) {
        var maxCount = list.Length;
        if (count < maxCount) {
            list[count++] = candidate;
            return;
        }
        var minIndex = 0;
        var minScore = list[0].Score;
        for (var i = 1; i < maxCount; i++) {
            if (list[i].Score < minScore) {
                minScore = list[i].Score;
                minIndex = i;
            }
        }
        if (candidate.Score > minScore) {
            list[minIndex] = candidate;
        }
    }

    private static void SortCandidates(ref Span<SquareCandidate> list, int count) {
        for (var i = 0; i < count - 1; i++) {
            var maxIdx = i;
            var maxScore = list[i].Score;
            for (var j = i + 1; j < count; j++) {
                if (list[j].Score > maxScore) {
                    maxScore = list[j].Score;
                    maxIdx = j;
                }
            }
            if (maxIdx != i) {
                (list[i], list[maxIdx]) = (list[maxIdx], list[i]);
            }
        }
    }

    private static bool TryEstimateRunLength(
        QrGrayImage image,
        bool invert,
        double x0,
        double y0,
        double x1,
        double y1,
        double moduleSizeSeed,
        out double moduleSize) {
        moduleSize = moduleSizeSeed;
        var dx = x1 - x0;
        var dy = y1 - y0;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < moduleSizeSeed * 6.0) return false;

        var steps = (int)Math.Floor(len);
        if (steps < 8) return false;

        var runs = new List<int>(64);
        var last = false;
        var run = 0;
        var init = false;

        for (var i = 0; i <= steps; i++) {
            var t = i / len;
            var x = x0 + dx * t;
            var y = y0 + dy * t;
            var ix = (int)Math.Round(x);
            var iy = (int)Math.Round(y);
            if (ix < 0 || iy < 0 || ix >= image.Width || iy >= image.Height) continue;

            var isBlack = image.IsBlack(ix, iy, invert);
            if (!init) {
                last = isBlack;
                run = 1;
                init = true;
                continue;
            }

            if (isBlack == last) {
                run++;
            } else {
                if (run > 0) runs.Add(run);
                last = isBlack;
                run = 1;
            }
        }
        if (run > 0) runs.Add(run);

        if (runs.Count < 6) return false;

        runs.Sort();
        var median = runs[runs.Count / 2];
        if (median <= 0) return false;

        var minRun = Math.Max(1, (int)Math.Round(moduleSizeSeed * 0.4));
        var maxRun = (int)Math.Round(moduleSizeSeed * 3.5);
        if (median < minRun || median > maxRun) return false;

        moduleSize = median;
        return true;
    }

    private static bool TrySampleAndDecode(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, bool aggressive, bool stylized, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        var moduleSize = (tl.ModuleSize + tr.ModuleSize + bl.ModuleSize) / 3.0;
        if (moduleSize <= 0) return false;

        var tightBudget = budget.Enabled && budget.MaxMilliseconds <= 800;
        var distX = Distance(tl.X, tl.Y, tr.X, tr.Y);
        var distY = Distance(tl.X, tl.Y, bl.X, bl.Y);
        if (distX <= 0 || distY <= 0) return false;

        if (aggressive && !budget.IsNearDeadline(120) &&
            TryEstimateModuleSizeFromTiming(image, invert, tl, tr, bl, moduleSize, out var refinedModuleSize)) {
            moduleSize = refinedModuleSize;
        }

        var dimH = QrMath.RoundToInt(distX / moduleSize) + 7;
        var dimV = QrMath.RoundToInt(distY / moduleSize) + 7;

        // Try a few nearby dimensions (estimation can be off on UI-scaled QR).
        var baseDim = NearestValidDimension((dimH + dimV) / 2);
        Span<int> candidates = stackalloc int[16];
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
            if (aggressive) {
                AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 16);
                AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 20);
                AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 16);
                AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 20);
            }
        }

        for (var i = 0; i < candidatesCount; i++) {
            if (budget.IsExpired) return false;
            var dimension = candidates[i];
            if (dimension is < 21 or > 177) continue;
            if (TrySampleAndDecodeDimension(scale, threshold, image, invert, tl, tr, bl, dimension, candidateCount, candidateTriplesTried, accept, aggressive, stylized, budget, out result, out diagnostics)) return true;
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

    private static bool TrySampleAndDecodeDimension(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int dimension, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, bool aggressive, bool stylized, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
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

        if (TrySampleWithCorners(image, invert, phaseX: 0, phaseY: 0, dimension, scratch, cornerTlX0, cornerTlY0, cornerTrX0, cornerTrY0, cornerBrX0, cornerBrY0, cornerBlX0, cornerBlY0, moduleSize0, accept, aggressive, stylized, budget, out result, out var moduleDiag0)) {
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
        if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, scratch, cornerTlX, cornerTlY, cornerTrX, cornerTrY, cornerBrXr0, cornerBrYr0, cornerBlX, cornerBlY, moduleSize, accept, aggressive, stylized, budget, out result, out var moduleDiagR)) {
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

                if (TrySampleWithCorners(image, invert, rPhaseX, rPhaseY, dimension, scratch, rCornerTlX, rCornerTlY, rCornerTrX, rCornerTrY, rCornerBrX, rCornerBrY, rCornerBlX, rCornerBlY, rModuleSize, accept, aggressive, stylized, budget, out result, out var moduleDiagT)) {
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
                    stylized,
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

        if (aggressive &&
            moduleDiagR.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload &&
            !budget.IsNearDeadline(200)) {
            QrPixelSampling.RefineTransformWide(image, invert, tl.X, tl.Y, vxX, vxY, vyX, vyY, dimension, out var wvxX, out var wvxY, out var wvyX, out var wvyY, out var wPhaseX, out var wPhaseY);

            var wModuleSize = (Math.Sqrt(wvxX * wvxX + wvxY * wvxY) + Math.Sqrt(wvyX * wvyX + wvyY * wvyY)) / 2.0;
            var wCornerTlX = tl.X - (wvxX + wvyX) * finderCenterToCorner;
            var wCornerTlY = tl.Y - (wvxY + wvyY) * finderCenterToCorner;
            var wCornerTrX = wCornerTlX + wvxX * dimension;
            var wCornerTrY = wCornerTlY + wvxY * dimension;
            var wCornerBlX = wCornerTlX + wvyX * dimension;
            var wCornerBlY = wCornerTlY + wvyY * dimension;
            var wCornerBrX = wCornerTlX + (wvxX + wvyX) * dimension;
            var wCornerBrY = wCornerTlY + (wvxY + wvyY) * dimension;

            if (TrySampleWithCorners(image, invert, wPhaseX, wPhaseY, dimension, scratch, wCornerTlX, wCornerTlY, wCornerTrX, wCornerTrY, wCornerBrX, wCornerBrY, wCornerBlX, wCornerBlY, wModuleSize, accept, aggressive, stylized, budget, out result, out var moduleDiagWide)) {
                diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagWide);
                return true;
            }

            best = Better(best, moduleDiagWide);
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
                    stylized,
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

                    if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, scratch, cornerTlX, cornerTlY, cornerTrX, cornerTrY, cornerBrXA, cornerBrYA, cornerBlX, cornerBlY, moduleSize, accept, aggressive, stylized, budget, out result, out var moduleDiagA)) {
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
                    stylized,
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
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        var checkBudget = budget.Enabled || budget.CanCancel;
        if (checkBudget && budget.IsExpired) return false;

        Span<double> offsets = stackalloc double[3];
        offsets[0] = -0.60;
        offsets[1] = 0.0;
        offsets[2] = 0.60;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);
        const double jitterEpsilon = 1e-9;

        for (var yi = 0; yi < offsets.Length; yi++) {
            if (checkBudget && budget.IsExpired) return false;
            var oy = offsets[yi];
            for (var xi = 0; xi < offsets.Length; xi++) {
                if (checkBudget && budget.IsExpired) return false;
                var ox = offsets[xi];
                if (Math.Abs(ox) <= jitterEpsilon && Math.Abs(oy) <= jitterEpsilon) continue;

                var jx = cornerBrX + vxX * ox + vyX * oy;
                var jy = cornerBrY + vxY * ox + vyY * oy;

                if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, scratch, cornerTlX, cornerTlY, cornerTrX, cornerTrY, jx, jy, cornerBlX, cornerBlY, moduleSizePx, accept, aggressive, stylized, budget, out result, out var diag)) {
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
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        var checkBudget = budget.Enabled || budget.CanCancel;
        if (checkBudget && budget.IsExpired) return false;

        Span<double> offsets = stackalloc double[2];
        offsets[0] = -0.25;
        offsets[1] = 0.25;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);

        for (var i = 0; i < offsets.Length; i++) {
            if (checkBudget && budget.IsExpired) return false;
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
                    stylized,
                    budget,
                    out result,
                    out var diag)) {
                moduleDiagnostics = diag;
                return true;
            }

            best = Better(best, diag);
        }

        for (var i = 0; i < offsets.Length; i++) {
            if (checkBudget && budget.IsExpired) return false;
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
                    stylized,
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
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        var checkBudget = budget.Enabled || budget.CanCancel;
        if (checkBudget && budget.IsExpired) return false;

        Span<double> offsets = stackalloc double[3];
        offsets[0] = -0.35;
        offsets[1] = 0.0;
        offsets[2] = 0.35;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);
        const double jitterEpsilon = 1e-9;

        for (var yi = 0; yi < offsets.Length; yi++) {
            if (checkBudget && budget.IsExpired) return false;
            var oy = phaseY + offsets[yi];
            for (var xi = 0; xi < offsets.Length; xi++) {
                if (checkBudget && budget.IsExpired) return false;
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
                    stylized,
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
