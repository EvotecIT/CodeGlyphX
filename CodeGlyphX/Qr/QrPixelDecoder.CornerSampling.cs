#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static bool TrySampleWithCorners(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        SamplingScratch scratch,
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
        SamplingScratch scratch,
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

        var bm = scratch.Matrix;
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
        if (scratch.TryDecode(budget, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag)) {
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
            if (scratch.TryDecode(budget, out result, out moduleDiagInv)) {
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

}
#endif
