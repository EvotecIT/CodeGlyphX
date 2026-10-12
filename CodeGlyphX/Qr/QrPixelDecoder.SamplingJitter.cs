#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
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
