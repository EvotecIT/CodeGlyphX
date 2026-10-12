#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static bool TrySampleAndDecode(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, bool aggressive, bool stylized, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics, bool allowPerspectiveRecovery = false) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        var moduleSize = (tl.ModuleSize + tr.ModuleSize + bl.ModuleSize) / 3.0;
        if (moduleSize <= 0) return false;

        var tightBudget = budget.Enabled && budget.BudgetMilliseconds <= 800;
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
            if (TrySampleAndDecodeDimension(scale, threshold, image, invert, tl, tr, bl, dimension, candidateCount, candidateTriplesTried, accept, aggressive, stylized, budget, out result, out diagnostics, allowPerspectiveRecovery)) return true;
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

    private static bool TrySampleAndDecodeDimension(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int dimension, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, bool aggressive, bool stylized, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics, bool allowPerspectiveRecovery = false) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        using var scratch = new SamplingScratch(dimension);
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
        // A bounded photograph retry can retain perspective precision within a
        // short allowance. Its caller and every sampling attempt share this deadline.
        var tightBudget = budget.Enabled && budget.BudgetMilliseconds <= 800 && !allowPerspectiveRecovery;

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
                    if (TrySampleWithAlignmentLandmarks(image, invert, dimension, scratch, tl, tr, bl,
                        a, ax, ay, moduleSize, accept, aggressive, stylized, budget, out result, out var landmarkDiag)) {
                        diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, landmarkDiag);
                        return true;
                    }
                    best = Better(best, landmarkDiag);

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


}
#endif
