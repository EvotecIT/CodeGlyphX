#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using CodeGlyphX;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static bool TrySampleWithCorners(
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
        if (budget.IsExpired) {
            result = null!;
            moduleDiagnostics = default;
            return false;
        }

        if (TrySampleWithCornersInternal(
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
                cornerBrX,
                cornerBrY,
                cornerBlX,
                cornerBlY,
                moduleSizePx,
                accept,
                budget,
                aggressive,
                loose: false,
                centerSampling: false,
                ringSampling: false,
                out result,
                out moduleDiagnostics)) {
            return true;
        }

        var strictDiag = moduleDiagnostics;
        var looseDiag = default(global::CodeGlyphX.QrDecodeDiagnostics);
        if (aggressive && ShouldTryLooseSampling(moduleDiagnostics, moduleSizePx) &&
            TrySampleWithCornersInternal(
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
                cornerBrX,
                cornerBrY,
                cornerBlX,
                cornerBlY,
                moduleSizePx,
                accept,
                budget,
                aggressive,
                loose: true,
                centerSampling: false,
                ringSampling: false,
                out result,
                out looseDiag)) {
            moduleDiagnostics = looseDiag;
            return true;
        }

        var best = Better(strictDiag, looseDiag);
        if (aggressive && ShouldTryCenterSampling(best, moduleSizePx)) {
            if (TrySampleWithCornersInternal(
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
                    cornerBrX,
                    cornerBrY,
                    cornerBlX,
                    cornerBlY,
                    moduleSizePx,
                    accept,
                    budget,
                    aggressive,
                    loose: false,
                    centerSampling: true,
                    ringSampling: false,
                    out result,
                    out var centerDiag)) {
                moduleDiagnostics = centerDiag;
                return true;
            }

            var centerLooseDiag = default(global::CodeGlyphX.QrDecodeDiagnostics);
            if (ShouldTryLooseSampling(centerDiag, moduleSizePx) &&
                TrySampleWithCornersInternal(
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
                    cornerBrX,
                    cornerBrY,
                    cornerBlX,
                    cornerBlY,
                    moduleSizePx,
                    accept,
                    budget,
                    aggressive,
                    loose: true,
                    centerSampling: true,
                    ringSampling: false,
                    out result,
                    out centerLooseDiag)) {
                moduleDiagnostics = centerLooseDiag;
                return true;
            }

            best = Better(best, centerDiag);
            best = Better(best, centerLooseDiag);
        }

        if (stylized && moduleSizePx >= 1.1 && !budget.IsNearDeadline(150)) {
            if (TrySampleWithCornersInternal(
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
                    cornerBrX,
                    cornerBrY,
                    cornerBlX,
                    cornerBlY,
                    moduleSizePx,
                    accept,
                    budget,
                    aggressive,
                    loose: false,
                    centerSampling: false,
                    ringSampling: true,
                    out result,
                    out var ringDiag)) {
                moduleDiagnostics = ringDiag;
                return true;
            }

            var ringLooseDiag = default(global::CodeGlyphX.QrDecodeDiagnostics);
            if (ShouldTryLooseSampling(ringDiag, moduleSizePx) &&
                TrySampleWithCornersInternal(
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
                    cornerBrX,
                    cornerBrY,
                    cornerBlX,
                    cornerBlY,
                    moduleSizePx,
                    accept,
                    budget,
                    aggressive,
                    loose: true,
                    centerSampling: false,
                    ringSampling: true,
                    out result,
                    out ringLooseDiag)) {
                moduleDiagnostics = ringLooseDiag;
                return true;
            }

            best = Better(best, ringDiag);
            best = Better(best, ringLooseDiag);
        }

        moduleDiagnostics = best;
        return false;
    }

    private static bool TrySampleWithCornersInternal(
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
        DecodeBudget budget,
        bool aggressive,
        bool loose,
        bool centerSampling,
        bool ringSampling,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
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

        var bm = scratch;
        bm.Clear();
        var clampedLimit = dimension * 2;
        var useRing = ringSampling && moduleSizePx >= 1.1;
        var mode = (centerSampling && moduleSizePx >= 1.25)
            ? 0
            : moduleSizePx >= 6.0
                ? 1
                : moduleSizePx >= 1.25
                    ? 2
                    : 3;
        var delta = useRing
            ? QrPixelSampling.GetSampleDeltaRingForModule(moduleSizePx)
            : mode switch {
                0 => QrPixelSampling.GetSampleDeltaCenterForModule(moduleSizePx),
                1 => QrPixelSampling.GetSampleDelta5x5ForModule(moduleSizePx),
                _ => QrPixelSampling.GetSampleDeltaForModule(moduleSizePx)
            };

        var sampledOk = useRing
            ? (loose
                ? SampleModules<NinePxLooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                : SampleModules<NinePxSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _))
            : mode switch {
                0 => loose
                    ? SampleModules<Center3x3LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                    : SampleModules<Center3x3Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _),
                1 => loose
                    ? SampleModules<Nearest25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                    : SampleModules<Nearest25Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _),
                2 => loose
                    ? SampleModules<Nearest9LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                    : SampleModules<Nearest9Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _),
                _ => loose
                    ? SampleModules<NinePxLooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                    : SampleModules<NinePxSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
            };

        if (!sampledOk) return false;

        if (budget.IsNearDeadline(120)) return false;
        Func<bool>? shouldStop = budget.CanAbort ? () => budget.IsExpired : null;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag)) {
            moduleDiagnostics = moduleDiag;
            if (accept == null || accept(result)) return true;
            return false;
        }

        if (budget.Enabled && budget.BudgetMilliseconds <= 800) {
            moduleDiagnostics = moduleDiag;
            return false;
        }

        global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv;
        bm.Invert();
        try {
            if (budget.IsNearDeadline(120)) return false;
            if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out moduleDiagInv)) {
                moduleDiagnostics = moduleDiagInv;
                if (accept == null || accept(result)) return true;
                return false;
            }
        } finally {
            bm.Invert();
        }

        moduleDiagnostics = Better(moduleDiag, moduleDiagInv);
        return false;
    }

    private interface IModuleSampler {
        static abstract bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta);
    }

    private readonly struct Center3x3Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModuleCenter3x3WithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Center3x3LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModuleCenter3x3LooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest25Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25NearestWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest25LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25NearestLooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest9Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9NearestWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest9LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9NearestLooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct NinePxSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9PxWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct NinePxLooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9PxLooseWithDelta(image, sx, sy, invert, delta);
    }

    private static bool SampleModules<TSampler>(
        QrGrayImage image,
        bool invert,
        in QrPerspectiveTransform transform,
        int dimension,
        double phaseX,
        double phaseY,
        global::CodeGlyphX.BitMatrix bm,
        DecodeBudget budget,
        int clampedLimit,
        double delta,
        out int clamped)
        where TSampler : struct, IModuleSampler {
        clamped = 0;

        var bmWords = bm.Words;
        var bmWidth = dimension;
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;
        var checkBudget = budget.CanAbort;
        var budgetCounter = 0;
        var xStart = 0.5 + phaseX;

        for (var my = 0; my < dimension; my++) {
            if (checkBudget && budget.IsExpired) return false;
            var myc = my + 0.5 + phaseY;
            transform.GetRowParameters(
                xStart,
                myc,
                out var numX,
                out var numY,
                out var denom,
                out var stepNumX,
                out var stepNumY,
                out var stepDenom);

            if (!double.IsFinite(numX + numY + denom + stepNumX + stepNumY + stepDenom)) {
                return false;
            }

            var denomEnd = denom + stepDenom * (dimension - 1);
            if (!double.IsFinite(denomEnd)) return false;
            var absDenom = denom < 0 ? -denom : denom;
            var absDenomEnd = denomEnd < 0 ? -denomEnd : denomEnd;
            if (absDenom < 1e-12 || absDenomEnd < 1e-12) return false;
            if (denom * denomEnd < 0) return false;

            var rowOffset = my * bmWidth;
            var absStepDenom = stepDenom < 0 ? -stepDenom : stepDenom;
            if (absStepDenom < 1e-12) {
                var inv = 1.0 / denom;
                var sx = numX * inv;
                var sy = numY * inv;
                var sxStep = stepNumX * inv;
                var syStep = stepNumY * inv;
                var wordIndex = rowOffset >> 5;
                var bitMask = 1u << (rowOffset & 31);

                var sxEnd = sx + sxStep * (dimension - 1);
                var syEnd = sy + syStep * (dimension - 1);
                if (sx >= 0 && sx <= maxX && sxEnd >= 0 && sxEnd <= maxX &&
                    sy >= 0 && sy <= maxY && syEnd >= 0 && syEnd <= maxY) {
                    if (!checkBudget) {
                        for (var mx = 0; mx < dimension; mx++) {
                            if (TSampler.Sample(image, sx, sy, invert, delta)) {
                                bmWords[wordIndex] |= bitMask;
                            }

                            bitMask <<= 1;
                            if (bitMask == 0) {
                                bitMask = 1u;
                                wordIndex++;
                            }

                            sx += sxStep;
                            sy += syStep;
                        }
                    } else {
                        for (var mx = 0; mx < dimension; mx++) {
                            if (((budgetCounter++ & 63) == 0) && budget.IsExpired) return false;

                            if (TSampler.Sample(image, sx, sy, invert, delta)) {
                                bmWords[wordIndex] |= bitMask;
                            }

                            bitMask <<= 1;
                            if (bitMask == 0) {
                                bitMask = 1u;
                                wordIndex++;
                            }

                            sx += sxStep;
                            sy += syStep;
                        }
                    }
                } else {
                    if (!checkBudget) {
                        for (var mx = 0; mx < dimension; mx++) {
                            var sampleX = sx;
                            var sampleY = sy;
                            if (sampleX < 0) { sampleX = 0; clamped++; }
                            else if (sampleX > maxX) { sampleX = maxX; clamped++; }

                            if (sampleY < 0) { sampleY = 0; clamped++; }
                            else if (sampleY > maxY) { sampleY = maxY; clamped++; }

                            if (TSampler.Sample(image, sampleX, sampleY, invert, delta)) {
                                bmWords[wordIndex] |= bitMask;
                            }

                            bitMask <<= 1;
                            if (bitMask == 0) {
                                bitMask = 1u;
                                wordIndex++;
                            }

                            sx += sxStep;
                            sy += syStep;
                        }
                    } else {
                        for (var mx = 0; mx < dimension; mx++) {
                            if (((budgetCounter++ & 63) == 0) && budget.IsExpired) return false;

                            var sampleX = sx;
                            var sampleY = sy;
                            if (sampleX < 0) { sampleX = 0; clamped++; }
                            else if (sampleX > maxX) { sampleX = maxX; clamped++; }

                            if (sampleY < 0) { sampleY = 0; clamped++; }
                            else if (sampleY > maxY) { sampleY = maxY; clamped++; }

                            if (TSampler.Sample(image, sampleX, sampleY, invert, delta)) {
                                bmWords[wordIndex] |= bitMask;
                            }

                            bitMask <<= 1;
                            if (bitMask == 0) {
                                bitMask = 1u;
                                wordIndex++;
                            }

                            sx += sxStep;
                            sy += syStep;
                        }
                    }
                }
            } else {
                var wordIndex = rowOffset >> 5;
                var bitMask = 1u << (rowOffset & 31);
                if (!checkBudget) {
                    for (var mx = 0; mx < dimension; mx++) {
                        var inv = 1.0 / denom;
                        var sx = numX * inv;
                        var sy = numY * inv;

                        if (sx < 0) { sx = 0; clamped++; }
                        else if (sx > maxX) { sx = maxX; clamped++; }

                        if (sy < 0) { sy = 0; clamped++; }
                        else if (sy > maxY) { sy = maxY; clamped++; }

                        if (TSampler.Sample(image, sx, sy, invert, delta)) {
                            bmWords[wordIndex] |= bitMask;
                        }

                        bitMask <<= 1;
                        if (bitMask == 0) {
                            bitMask = 1u;
                            wordIndex++;
                        }

                        numX += stepNumX;
                        numY += stepNumY;
                        denom += stepDenom;
                    }
                } else {
                    for (var mx = 0; mx < dimension; mx++) {
                        if (((budgetCounter++ & 63) == 0) && budget.IsExpired) return false;

                        var inv = 1.0 / denom;
                        var sx = numX * inv;
                        var sy = numY * inv;

                        if (sx < 0) { sx = 0; clamped++; }
                        else if (sx > maxX) { sx = maxX; clamped++; }

                        if (sy < 0) { sy = 0; clamped++; }
                        else if (sy > maxY) { sy = maxY; clamped++; }

                        if (TSampler.Sample(image, sx, sy, invert, delta)) {
                            bmWords[wordIndex] |= bitMask;
                        }

                        bitMask <<= 1;
                        if (bitMask == 0) {
                            bitMask = 1u;
                            wordIndex++;
                        }

                        numX += stepNumX;
                        numY += stepNumY;
                        denom += stepDenom;
                    }
                }
            }

            if (clamped > clampedLimit) return false;
        }

        return clamped <= clampedLimit;
    }

    private static bool ShouldTryLooseSampling(global::CodeGlyphX.QrDecodeDiagnostics diag, double moduleSizePx) {
        if (moduleSizePx < 1.0) return false;
        return diag.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload;
    }

    private static bool ShouldTryCenterSampling(global::CodeGlyphX.QrDecodeDiagnostics diag, double moduleSizePx) {
        if (moduleSizePx < 1.5) return false;
        return diag.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Distance(double x1, double y1, double x2, double y2) {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
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

}
#endif
