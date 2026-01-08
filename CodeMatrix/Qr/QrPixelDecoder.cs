#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;

namespace CodeMatrix.Qr;

internal static class QrPixelDecoder {
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result) {
        return TryDecode(pixels, width, height, stride, fmt, out result, out _);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        return TryDecode(pixels, width, height, stride, fmt, accept: null, out result, out diagnostics);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, Func<QrDecoded, bool>? accept, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (width <= 0 || height <= 0) return false;
        if (stride < width * 4) return false;
        if (pixels.Length < (height - 1) * stride + width * 4) return false;

        var best = default(QrPixelDecodeDiagnostics);

        // Prefer a full finder-pattern based decode (robust to extra background/noise).
        if (TryDecodeAtScale(pixels, width, height, stride, fmt, 1, accept, out result, out var diag1)) {
            diagnostics = diag1;
            return true;
        }
        best = Better(best, diag1);

        // Optional downscale passes (often helps with UI-scaled / anti-aliased QR).
        var minDim = Math.Min(width, height);
        if (minDim >= 160) {
            if (TryDecodeAtScale(pixels, width, height, stride, fmt, 2, accept, out result, out var diag2)) {
                diagnostics = diag2;
                return true;
            }
            best = Better(best, diag2);
        }
        if (minDim >= 320) {
            if (TryDecodeAtScale(pixels, width, height, stride, fmt, 3, accept, out result, out var diag3)) {
                diagnostics = diag3;
                return true;
            }
            best = Better(best, diag3);
        }

        diagnostics = best;
        return false;
    }

    private static bool TryDecodeAtScale(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, int scale, Func<QrDecoded, bool>? accept, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (!QrGrayImage.TryCreate(pixels, width, height, stride, fmt, scale, out var baseImage)) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeMatrix.QrDecodeDiagnostics(global::CodeMatrix.QrDecodeFailure.InvalidInput));
            return false;
        }

        // Try a few thresholds (helps with anti-aliasing and mixed UI backgrounds).
        Span<byte> thresholds = stackalloc byte[8];
        var thresholdCount = 0;
        var mid = (baseImage.Min + baseImage.Max) / 2;
        var range = baseImage.Max - baseImage.Min;

        // Midpoint is often more stable on UI screenshots (Otsu can get skewed by gray UI text).
        AddThresholdCandidate(ref thresholds, ref thresholdCount, mid);
        AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold);

        AddThresholdCandidate(ref thresholds, ref thresholdCount, mid - 16);
        AddThresholdCandidate(ref thresholds, ref thresholdCount, mid + 16);
        AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold - 16);
        AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold + 16);

        if (range > 0) {
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Min + range / 3);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Min + (range * 2) / 3);
        }
        AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Min + 12);
        AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Max - 12);

        var best = default(QrPixelDecodeDiagnostics);

        for (var i = 0; i < thresholdCount; i++) {
            var image = baseImage.WithThreshold(thresholds[i]);

            // Try normal polarity first, then inverted.
            if (TryDecodeFromGray(scale, thresholds[i], image, invert: false, accept, out result, out var diagN)) {
                diagnostics = diagN;
                return true;
            }
            best = Better(best, diagN);

            if (TryDecodeFromGray(scale, thresholds[i], image, invert: true, accept, out result, out var diagI)) {
                diagnostics = diagI;
                return true;
            }
            best = Better(best, diagI);
        }

        // Adaptive threshold pass (helps with uneven lighting / gradients).
        var adaptive = baseImage.WithAdaptiveThreshold(windowSize: 15, offset: 8);
        if (TryDecodeFromGray(scale, baseImage.Threshold, adaptive, invert: false, accept, out result, out var diagA)) {
            diagnostics = diagA;
            return true;
        }
        best = Better(best, diagA);

        if (TryDecodeFromGray(scale, baseImage.Threshold, adaptive, invert: true, accept, out result, out var diagAI)) {
            diagnostics = diagAI;
            return true;
        }
        best = Better(best, diagAI);

        // Second adaptive pass biased for softer UI gradients.
        var adaptiveSoft = baseImage.WithAdaptiveThreshold(windowSize: 25, offset: 4);
        if (TryDecodeFromGray(scale, baseImage.Threshold, adaptiveSoft, invert: false, accept, out result, out var diagAS)) {
            diagnostics = diagAS;
            return true;
        }
        best = Better(best, diagAS);

        if (TryDecodeFromGray(scale, baseImage.Threshold, adaptiveSoft, invert: true, accept, out result, out var diagASI)) {
            diagnostics = diagASI;
            return true;
        }
        best = Better(best, diagASI);

        diagnostics = best;
        return false;
    }

    private static void AddThresholdCandidate(ref Span<byte> list, ref int count, int threshold) {
        if (count >= list.Length) return;
        if (threshold < 0) threshold = 0;
        else if (threshold > 255) threshold = 255;
        var t = (byte)threshold;
        for (var i = 0; i < count; i++) {
            if (list[i] == t) return;
        }
        list[count++] = t;
    }

    private static bool TryDecodeFromGray(int scale, byte threshold, QrGrayImage image, bool invert, Func<QrDecoded, bool>? accept, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        // Finder-based sampling (robust to extra background/noise). Try multiple triples when the region contains UI/text.
        var candidates = QrFinderPatternDetector.FindCandidates(image, invert);
        if (candidates.Count >= 3) {
            if (TryDecodeFromFinderCandidates(scale, threshold, image, invert, candidates, accept, out result, out var diagF)) {
                diagnostics = diagF;
                return true;
            }
            diagnostics = Better(diagnostics, diagF);

            if (TryDecodeByCandidateBounds(scale, threshold, image, invert, candidates, accept, out result, out var diagC)) {
                diagnostics = diagC;
                return true;
            }
            diagnostics = Better(diagnostics, diagC);
        }

        // Fallback: bounding box exact-fit (works for perfectly cropped/generated images).
        if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, out result, out var diagB)) {
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
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Count == 0) return false;

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

        return TryDecodeByBoundingBox(scale, threshold, image, invert, accept, out result, out diagnostics, candidates.Count, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY);
    }

    private static bool TryDecodeFromFinderCandidates(int scale, byte threshold, QrGrayImage image, bool invert, List<QrFinderPatternDetector.FinderPattern> candidates, Func<QrDecoded, bool>? accept, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        var n = Math.Min(candidates.Count, 12);
        var triedTriples = 0;
        var bboxAttempts = 0;

        for (var i = 0; i < n - 2; i++) {
            for (var j = i + 1; j < n - 1; j++) {
                for (var k = j + 1; k < n; k++) {
                    triedTriples++;
                    var a = candidates[i];
                    var b = candidates[j];
                    var c = candidates[k];

                    var msMin = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                    var msMax = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                    if (msMin <= 0) continue;
                    if (msMax > msMin * 1.75) continue;

                    if (!TryOrderAsTlTrBl(a, b, c, out var tl, out var tr, out var bl)) continue;
                    if (TrySampleAndDecode(scale, threshold, image, invert, tl, tr, bl, candidates.Count, triedTriples, accept, out result, out var diag)) {
                        diagnostics = diag;
                        return true;
                    }
                    diagnostics = Better(diagnostics, diag);

                    // If the finder triple looks reasonable but decoding fails (false positives are common in UI),
                    // try a bounded bbox decode around the candidate region before moving on.
                    if (bboxAttempts < 4 && TryGetCandidateBounds(tl, tr, bl, image.Width, image.Height, out var bminX, out var bminY, out var bmaxX, out var bmaxY)) {
                        bboxAttempts++;
                        if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, out result, out var diagB, candidates.Count, triedTriples, bminX, bminY, bmaxX, bmaxY)) {
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

    private static bool TrySampleAndDecode(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var moduleSize = (tl.ModuleSize + tr.ModuleSize + bl.ModuleSize) / 3.0;
        if (moduleSize <= 0) return false;

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
        AddDimensionCandidate(ref candidates, ref candidatesCount, NearestValidDimension(dimH));
        AddDimensionCandidate(ref candidates, ref candidatesCount, NearestValidDimension(dimV));
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 4);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 8);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 12);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 4);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 8);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 12);

        for (var i = 0; i < candidatesCount; i++) {
            var dimension = candidates[i];
            if (dimension is < 21 or > 177) continue;
            if (TrySampleAndDecodeDimension(scale, threshold, image, invert, tl, tr, bl, dimension, candidateCount, candidateTriplesTried, accept, out result, out diagnostics)) return true;
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

    private static bool TrySampleAndDecodeDimension(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int dimension, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

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

        if (TrySampleWithCorners(image, invert, phaseX: 0, phaseY: 0, dimension, cornerTlX0, cornerTlY0, cornerTrX0, cornerTrY0, cornerBrX0, cornerBrY0, cornerBlX0, cornerBlY0, moduleSize0, accept, out result, out var moduleDiag0)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiag0);
            return true;
        }

        var best = moduleDiag0;

        // Refine sampling using format/timing patterns as a score:
        // - phase (sub-module offsets) + small scale adjustment of vx/vy (finder centers can be slightly off).
        QrPixelSampling.RefineTransform(image, invert, tl.X, tl.Y, vxX, vxY, vyX, vyY, dimension, out vxX, out vxY, out vyX, out vyY, out var phaseX, out var phaseY);

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
        if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, cornerTlX, cornerTlY, cornerTrX, cornerTrY, cornerBrXr0, cornerBrYr0, cornerBlX, cornerBlY, moduleSize, accept, out result, out var moduleDiagR)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagR);
            return true;
        }

        best = Better(best, moduleDiagR);

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

                    if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, cornerTlX, cornerTlY, cornerTrX, cornerTrY, cornerBrXA, cornerBrYA, cornerBlX, cornerBlY, moduleSize, accept, out result, out var moduleDiagA)) {
                        diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagA);
                        return true;
                    }

                    best = Better(best, moduleDiagA);
                }
            }
        }

        diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, best);
        return false;
    }

    private static bool TrySampleWithCorners(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
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
        out QrDecoded result,
        out global::CodeMatrix.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        // Build a perspective transform from module-corner space (0..dimension) to image-space and sample using it.
        var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0,
            dimension, 0,
            dimension, dimension,
            0, dimension,
            cornerTlX, cornerTlY,
            cornerTrX, cornerTrY,
            cornerBrX, cornerBrY,
            cornerBlX, cornerBlY);

        var bm = new global::CodeMatrix.BitMatrix(dimension, dimension);

        var clamped = 0;

        for (var my = 0; my < dimension; my++) {
            for (var mx = 0; mx < dimension; mx++) {
                var mxc = mx + 0.5 + phaseX;
                var myc = my + 0.5 + phaseY;

                transform.Transform(mxc, myc, out var sx, out var sy);
                if (double.IsNaN(sx) || double.IsNaN(sy)) return false;

                if (sx < 0) { sx = 0; clamped++; }
                else if (sx > image.Width - 1) { sx = image.Width - 1; clamped++; }

                if (sy < 0) { sy = 0; clamped++; }
                else if (sy > image.Height - 1) { sy = image.Height - 1; clamped++; }

                // When modules are reasonably large, nearest-neighbor majority sampling is more stable than bilinear
                // (bilinear can blur binary UI edges into mid-gray values around the threshold).
                // Prefer a tighter sampling pattern for typical UI-rendered QRs (3–6 px/module).
                // 5x5 sampling is more sensitive to small transform errors; use it only when modules are large.
                bm[mx, my] = moduleSizePx >= 6.0
                    ? QrPixelSampling.SampleModule25Nearest(image, sx, sy, invert, moduleSizePx)
                    : moduleSizePx >= 1.25
                        ? QrPixelSampling.SampleModule9Nearest(image, sx, sy, invert, moduleSizePx)
                        : QrPixelSampling.SampleModule9Px(image, sx, sy, invert, moduleSizePx);
            }
        }

        // If we had to clamp too many samples, the region is likely cropped too tight or the estimate is wrong.
        if (clamped > dimension * 2) return false;

        if (global::CodeMatrix.QrDecoder.TryDecode(bm, out result, out var moduleDiag)) {
            moduleDiagnostics = moduleDiag;
            if (accept == null || accept(result)) return true;
            return false;
        }

        var inv = bm.Clone();
        Invert(inv);
        if (global::CodeMatrix.QrDecoder.TryDecode(inv, out result, out var moduleDiagInv)) {
            moduleDiagnostics = moduleDiagInv;
            if (accept == null || accept(result)) return true;
            return false;
        }

        moduleDiagnostics = Better(moduleDiag, moduleDiagInv);
        return false;
    }

    private static bool TryDecodeByBoundingBox(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        Func<QrDecoded, bool>? accept,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics,
        int candidateCount = 0,
        int candidateTriplesTried = 0,
        int scanMinX = 0,
        int scanMinY = 0,
        int scanMaxX = -1,
        int scanMaxY = -1) {
        result = null!;
        diagnostics = default;

        if (scanMaxX < 0) scanMaxX = image.Width - 1;
        if (scanMaxY < 0) scanMaxY = image.Height - 1;
        if (scanMinX < 0) scanMinX = 0;
        if (scanMinY < 0) scanMinY = 0;
        if (scanMaxX >= image.Width) scanMaxX = image.Width - 1;
        if (scanMaxY >= image.Height) scanMaxY = image.Height - 1;
        if (scanMinX > scanMaxX || scanMinY > scanMaxY) return false;

        var minX = scanMaxX;
        var minY = scanMaxY;
        var maxX = -1;
        var maxY = -1;

        for (var y = scanMinY; y <= scanMaxY; y++) {
            for (var x = scanMinX; x <= scanMaxX; x++) {
                if (!image.IsBlack(x, y, invert)) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < 0) return false;

        TrimBoundingBox(image, invert, ref minX, ref minY, ref maxX, ref maxY);
        if (maxX < minX || maxY < minY) return false;

        // Expand a touch to counter anti-aliasing that can shrink the detected black bbox.
        if (minX > 0) minX--;
        if (minY > 0) minY--;
        if (maxX < image.Width - 1) maxX++;
        if (maxY < image.Height - 1) maxY++;

        var boxW = maxX - minX + 1;
        var boxH = maxY - minY + 1;
        if (boxW <= 0 || boxH <= 0) return false;

        var maxModules = Math.Min(boxW, boxH);
        var maxVersion = Math.Min(40, (maxModules - 17) / 4);
        if (maxVersion < 1) return false;

        // Try smaller versions first (more likely for OTP QR), but accept non-integer module sizes.
        var best = default(QrPixelDecodeDiagnostics);
        for (var version = 1; version <= maxVersion; version++) {
            var modulesCount = version * 4 + 17;
            var moduleSizeX = boxW / (double)modulesCount;
            var moduleSizeY = boxH / (double)modulesCount;
            if (moduleSizeX < 1.0 || moduleSizeY < 1.0) continue;

            var relDiff = Math.Abs(moduleSizeX - moduleSizeY) / Math.Max(moduleSizeX, moduleSizeY);
            if (relDiff > 0.20) continue;

            var bm = new global::CodeMatrix.BitMatrix(modulesCount, modulesCount);
            for (var my = 0; my < modulesCount; my++) {
                var sy = minY + (my + 0.5) * moduleSizeY;
                var py = QrMath.RoundToInt(sy);
                if (py < 0) py = 0;
                else if (py >= image.Height) py = image.Height - 1;

                for (var mx = 0; mx < modulesCount; mx++) {
                    var sx = minX + (mx + 0.5) * moduleSizeX;
                    var px = QrMath.RoundToInt(sx);
                    if (px < 0) px = 0;
                    else if (px >= image.Width) px = image.Width - 1;

                    bm[mx, my] = SampleMajority3x3(image, px, py, invert);
                }
            }

            if (global::CodeMatrix.QrDecoder.TryDecode(bm, out result, out var moduleDiag)) {
                diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag);
                if (accept == null || accept(result)) return true;
            }
            best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag));

            var inv = bm.Clone();
            Invert(inv);
            if (global::CodeMatrix.QrDecoder.TryDecode(inv, out result, out var moduleDiagInv)) {
                diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagInv);
                if (accept == null || accept(result)) return true;
            }
            best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagInv));
        }

        diagnostics = best;
        return false;
    }

    private static bool TryGetCandidateBounds(
        QrFinderPatternDetector.FinderPattern tl,
        QrFinderPatternDetector.FinderPattern tr,
        QrFinderPatternDetector.FinderPattern bl,
        int width,
        int height,
        out int minX,
        out int minY,
        out int maxX,
        out int maxY) {
        var moduleSize = (tl.ModuleSize + tr.ModuleSize + bl.ModuleSize) / 3.0;
        if (moduleSize <= 0) {
            minX = minY = maxX = maxY = 0;
            return false;
        }

        var pad = moduleSize * 8.0;
        var minXF = Math.Min(tl.X, Math.Min(tr.X, bl.X)) - pad;
        var maxXF = Math.Max(tl.X, Math.Max(tr.X, bl.X)) + pad;
        var minYF = Math.Min(tl.Y, Math.Min(tr.Y, bl.Y)) - pad;
        var maxYF = Math.Max(tl.Y, Math.Max(tr.Y, bl.Y)) + pad;

        minX = Math.Clamp(QrMath.RoundToInt(minXF), 0, width - 1);
        maxX = Math.Clamp(QrMath.RoundToInt(maxXF), 0, width - 1);
        minY = Math.Clamp(QrMath.RoundToInt(minYF), 0, height - 1);
        maxY = Math.Clamp(QrMath.RoundToInt(maxYF), 0, height - 1);

        return minX < maxX && minY < maxY;
    }

    private static void TrimBoundingBox(QrGrayImage image, bool invert, ref int minX, ref int minY, ref int maxX, ref int maxY) {
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        if (width <= 0 || height <= 0) return;

        var rowThreshold = Math.Max(2, width / 40);
        var colThreshold = Math.Max(2, height / 40);

        while (minY <= maxY && CountDarkRow(image, invert, minX, maxX, minY) <= rowThreshold) minY++;
        while (minY <= maxY && CountDarkRow(image, invert, minX, maxX, maxY) <= rowThreshold) maxY--;
        while (minX <= maxX && CountDarkCol(image, invert, minX, minY, maxY) <= colThreshold) minX++;
        while (minX <= maxX && CountDarkCol(image, invert, maxX, minY, maxY) <= colThreshold) maxX--;
    }

    private static int CountDarkRow(QrGrayImage image, bool invert, int minX, int maxX, int y) {
        var count = 0;
        for (var x = minX; x <= maxX; x++) {
            if (image.IsBlack(x, y, invert)) count++;
        }
        return count;
    }

    private static int CountDarkCol(QrGrayImage image, bool invert, int x, int minY, int maxY) {
        var count = 0;
        for (var y = minY; y <= maxY; y++) {
            if (image.IsBlack(x, y, invert)) count++;
        }
        return count;
    }

    private static int NearestValidDimension(int dimension) {
        // Valid sizes are 17 + 4*version => dimension mod 4 == 1.
        var best = -1;
        var bestDiff = int.MaxValue;

        for (var delta = -2; delta <= 2; delta++) {
            var d = dimension + delta;
            if (d < 21 || d > 177) continue;
            if ((d & 3) != 1) continue;

            var diff = Math.Abs(delta);
            if (diff < bestDiff) {
                bestDiff = diff;
                best = d;
            }
        }

        return best;
    }

    private static double Distance(double x1, double y1, double x2, double y2) {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool SampleMajority3x3(QrGrayImage image, int px, int py, bool invert) {
        var black = 0;

        for (var dy = -1; dy <= 1; dy++) {
            var y = py + dy;
            if (y < 0) y = 0;
            else if (y >= image.Height) y = image.Height - 1;

            for (var dx = -1; dx <= 1; dx++) {
                var x = px + dx;
                if (x < 0) x = 0;
                else if (x >= image.Width) x = image.Width - 1;

                if (image.IsBlack(x, y, invert)) black++;
            }
        }

        return black >= 5;
    }

    private static void Invert(global::CodeMatrix.BitMatrix matrix) {
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                matrix[x, y] = !matrix[x, y];
            }
        }
    }

    private static QrPixelDecodeDiagnostics Better(QrPixelDecodeDiagnostics a, QrPixelDecodeDiagnostics b) {
        if (IsEmpty(a)) return b;
        if (IsEmpty(b)) return a;

        // Pick the attempt that got "furthest" (format ok > RS > payload), then the one with lower format distance.
        var sa = Score(a.ModuleDiagnostics);
        var sb = Score(b.ModuleDiagnostics);
        if (sb > sa) return b;
        if (sa > sb) return a;

        var da = a.ModuleDiagnostics.FormatBestDistance;
        var db = b.ModuleDiagnostics.FormatBestDistance;
        if (db >= 0 && (da < 0 || db < da)) return b;
        return a;
    }

    private static global::CodeMatrix.QrDecodeDiagnostics Better(global::CodeMatrix.QrDecodeDiagnostics a, global::CodeMatrix.QrDecodeDiagnostics b) {
        if (IsEmpty(a)) return b;
        if (IsEmpty(b)) return a;

        var sa = Score(a);
        var sb = Score(b);
        if (sb > sa) return b;
        if (sa > sb) return a;

        var da = a.FormatBestDistance;
        var db = b.FormatBestDistance;
        if (db >= 0 && (da < 0 || db < da)) return b;
        return a;
    }

    private static bool IsEmpty(QrPixelDecodeDiagnostics d) {
        return d.Scale == 0 && d.Dimension == 0 && d.CandidateCount == 0 && d.CandidateTriplesTried == 0 &&
               d.ModuleDiagnostics.Version == 0 && d.ModuleDiagnostics.Failure == global::CodeMatrix.QrDecodeFailure.None;
    }

    private static bool IsEmpty(global::CodeMatrix.QrDecodeDiagnostics d) {
        return d.Version == 0 && d.Failure == global::CodeMatrix.QrDecodeFailure.None;
    }

    private static int Score(global::CodeMatrix.QrDecodeDiagnostics d) {
        return d.Failure switch {
            global::CodeMatrix.QrDecodeFailure.None => 5,
            global::CodeMatrix.QrDecodeFailure.Payload => 4,
            global::CodeMatrix.QrDecodeFailure.ReedSolomon => 3,
            global::CodeMatrix.QrDecodeFailure.FormatInfo => 2,
            global::CodeMatrix.QrDecodeFailure.InvalidSize => 1,
            _ => 0,
        };
    }
}
#endif
