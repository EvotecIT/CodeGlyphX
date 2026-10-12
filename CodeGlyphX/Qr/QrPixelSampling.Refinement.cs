#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelSampling {
    private static readonly double[] PhaseOffsetsCoarse = { -0.35, -0.15, 0.0, 0.15, 0.35 };
    private static readonly double[] ScaleFactors = { 0.94, 0.97, 1.0, 1.03, 1.06 };
    private static readonly double[] ScaleFactorsWide = { 0.9, 0.95, 1.0, 1.05, 1.1 };

    public static void RefineTransform(
        QrGrayImage image,
        bool invert,
        double tlX,
        double tlY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        int dimension,
        out double bestVxX,
        out double bestVxY,
        out double bestVyX,
        out double bestVyY,
        out double bestPhaseX,
        out double bestPhaseY) {
        bestVxX = vxX;
        bestVxY = vxY;
        bestVyX = vyX;
        bestVyY = vyY;
        bestPhaseX = 0;
        bestPhaseY = 0;

        var bestDist = int.MaxValue;
        var bestTiming = int.MinValue;
        var bestScalePenalty = double.PositiveInfinity;
        const double scaleEpsilon = 1e-6;

        for (var sy = 0; sy < ScaleFactors.Length; sy++) {
            var scaleY = ScaleFactors[sy];
            var svyX = vyX * scaleY;
            var svyY = vyY * scaleY;

            for (var sx = 0; sx < ScaleFactors.Length; sx++) {
                var scaleX = ScaleFactors[sx];
                var svxX = vxX * scaleX;
                var svxY = vxY * scaleX;

                FindBestPhase(image, invert, tlX, tlY, svxX, svxY, svyX, svyY, dimension, out var phX, out var phY, out var dist, out var timing);

                // Prefer lower format distance, then higher timing alternation. Break ties by staying closer to the original scale.
                var scalePenalty = Math.Abs(scaleX - 1.0) + Math.Abs(scaleY - 1.0);

                if (dist < bestDist ||
                    (dist == bestDist && timing > bestTiming) ||
                    (dist == bestDist && timing == bestTiming && scalePenalty < bestScalePenalty)) {
                    bestDist = dist;
                    bestTiming = timing;
                    bestScalePenalty = scalePenalty;

                    bestVxX = svxX;
                    bestVxY = svxY;
                    bestVyX = svyX;
                    bestVyY = svyY;
                    bestPhaseX = phX;
                    bestPhaseY = phY;
                }

                if (bestDist == 0 && bestTiming > (dimension - 16) * 2 - 4 && bestScalePenalty <= scaleEpsilon) return;
            }
        }
    }

    public static void RefineTransformWide(
        QrGrayImage image,
        bool invert,
        double tlX,
        double tlY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        int dimension,
        out double bestVxX,
        out double bestVxY,
        out double bestVyX,
        out double bestVyY,
        out double bestPhaseX,
        out double bestPhaseY) {
        bestVxX = vxX;
        bestVxY = vxY;
        bestVyX = vyX;
        bestVyY = vyY;
        bestPhaseX = 0;
        bestPhaseY = 0;

        var bestDist = int.MaxValue;
        var bestTiming = int.MinValue;
        var bestScalePenalty = double.PositiveInfinity;
        const double scaleEpsilon = 1e-6;

        for (var sy = 0; sy < ScaleFactorsWide.Length; sy++) {
            var scaleY = ScaleFactorsWide[sy];
            var svyX = vyX * scaleY;
            var svyY = vyY * scaleY;

            for (var sx = 0; sx < ScaleFactorsWide.Length; sx++) {
                var scaleX = ScaleFactorsWide[sx];
                var svxX = vxX * scaleX;
                var svxY = vxY * scaleX;

                FindBestPhase(image, invert, tlX, tlY, svxX, svxY, svyX, svyY, dimension, out var phX, out var phY, out var dist, out var timing);

                var scalePenalty = Math.Abs(scaleX - 1.0) + Math.Abs(scaleY - 1.0);

                if (dist < bestDist ||
                    (dist == bestDist && timing > bestTiming) ||
                    (dist == bestDist && timing == bestTiming && scalePenalty < bestScalePenalty)) {
                    bestDist = dist;
                    bestTiming = timing;
                    bestScalePenalty = scalePenalty;

                    bestVxX = svxX;
                    bestVxY = svxY;
                    bestVyX = svyX;
                    bestVyY = svyY;
                    bestPhaseX = phX;
                    bestPhaseY = phY;

                    if (bestDist == 0 && bestTiming > (dimension - 16) * 2 - 4 && bestScalePenalty <= scaleEpsilon) return;
                }
            }
        }
    }

    public static void RefinePhase(
        QrGrayImage image,
        bool invert,
        double tlX,
        double tlY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        int dimension,
        out double bestPhaseX,
        out double bestPhaseY) {
        FindBestPhase(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, dimension, out bestPhaseX, out bestPhaseY, out _, out _);
    }

    private static void FindBestPhase(
        QrGrayImage image, bool invert, double tlX, double tlY,
        double vxX, double vxY, double vyX, double vyY, int dimension,
        out double bestX, out double bestY, out int bestDist, out int bestTiming) {
        bestX = bestY = 0;
        bestDist = int.MaxValue;
        bestTiming = int.MinValue;
        if (ScorePhaseCandidates(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, dimension,
            PhaseOffsetsCoarse, PhaseOffsetsCoarse, preferFirstEqual: false,
            ref bestX, ref bestY, ref bestDist, ref bestTiming)) return;

        Span<double> fineX = stackalloc double[5];
        Span<double> fineY = stackalloc double[5];
        for (var i = 0; i < 5; i++) {
            // Preserve the original arithmetic and fine-grid tie order.
            var offset = i switch { 0 => -0.10, 1 => -0.05, 2 => 0, 3 => 0.05, _ => 0.10 };
            fineX[i] = Math.Clamp(bestX + offset, -0.49, 0.49);
            fineY[i] = Math.Clamp(bestY + offset, -0.49, 0.49);
        }
        ScorePhaseCandidates(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, dimension,
            fineX, fineY, preferFirstEqual: true, ref bestX, ref bestY, ref bestDist, ref bestTiming);
    }

    /// <summary>Defers positive-distance timing scores until the final minimum format distance is known.</summary>
    private static bool ScorePhaseCandidates(
        QrGrayImage image, bool invert, double tlX, double tlY,
        double vxX, double vxY, double vyX, double vyY, int dimension,
        ReadOnlySpan<double> phasesX, ReadOnlySpan<double> phasesY, bool preferFirstEqual,
        ref double bestX, ref double bestY, ref int bestDist, ref int bestTiming) {
        Span<int> distances = stackalloc int[25];
        var minimum = bestDist;
        var hasWinner = !preferFirstEqual;
        var index = 0;
        for (var y = 0; y < phasesY.Length; y++) {
            for (var x = 0; x < phasesX.Length; x++) {
                var distance = ComputeFormatDistance(image, invert, tlX, tlY, vxX, vxY, vyX, vyY,
                    dimension, phasesX[x], phasesY[y]);
                distances[index++] = distance;
                if (distance < minimum) minimum = distance;
                // Evaluate exact format matches immediately in the original order.
                // They alone can trigger the existing perfect-score early return.
                if (distance != 0) continue;
                var timing = ComputeTimingAlternations(image, invert, tlX, tlY, vxX, vxY, vyX, vyY,
                    dimension, phasesX[x], phasesY[y]);
                if (distance < bestDist || timing > bestTiming || (!hasWinner && timing == bestTiming)) {
                    bestDist = distance; bestTiming = timing;
                    bestX = phasesX[x]; bestY = phasesY[y]; hasWinner = true;
                }
                if (bestTiming > (dimension - 16) * 2 - 4) return true;
            }
        }
        if (minimum == 0) return false;

        index = 0;
        for (var y = 0; y < phasesY.Length; y++) {
            for (var x = 0; x < phasesX.Length; x++) {
                if (distances[index++] != minimum) continue;
                var timing = ComputeTimingAlternations(image, invert, tlX, tlY, vxX, vxY, vyX, vyY,
                    dimension, phasesX[x], phasesY[y]);
                if (minimum < bestDist || timing > bestTiming || (!hasWinner && timing == bestTiming)) {
                    bestDist = minimum; bestTiming = timing;
                    bestX = phasesX[x]; bestY = phasesY[y]; hasWinner = true;
                }
            }
        }
        return false;
    }

    private static int ComputeFormatDistance(
        QrGrayImage image,
        bool invert,
        double tlX,
        double tlY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        int dimension,
        double phaseX,
        double phaseY) {
        var size = dimension;

        var bitsA = 0;
        var bitsB = 0;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;

        var dx8 = 5 + phaseX; // 8 - 3 + phaseX
        var dy8 = 5 + phaseY; // 8 - 3 + phaseY
        var dx7 = 4 + phaseX; // 7 - 3 + phaseX
        var vxXdx8 = vxX * dx8;
        var vxYdx8 = vxY * dx8;
        var vxXdx7 = vxX * dx7;
        var vxYdx7 = vxY * dx7;
        var vyXdy8 = vyX * dy8;
        var vyYdy8 = vyY * dy8;

        var baseXdx8 = tlX + vxXdx8;
        var baseYdx8 = tlY + vxYdx8;
        var baseXdx7 = tlX + vxXdx7;
        var baseYdx7 = tlY + vxYdx7;
        var baseXdy8 = tlX + vyXdy8;
        var baseYdy8 = tlY + vyYdy8;

        if (thresholdMap is null) {
            for (var i = 0; i <= 5; i++) {
                var dy = (i - 3) + phaseY;
                var sx = baseXdx8 + vyX * dy;
                var sy = baseYdx8 + vyY * dy;
                var isBlack = (SampleLumaBilinear(image, sx, sy) <= threshold) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << i;
            }

            {
                var dy = 4 + phaseY; // 7 - 3 + phaseY
                var sx = baseXdx8 + vyX * dy;
                var sy = baseYdx8 + vyY * dy;
                var isBlack = (SampleLumaBilinear(image, sx, sy) <= threshold) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << 6;
            }

            {
                var sx = baseXdx8 + vyXdy8;
                var sy = baseYdx8 + vyYdy8;
                var isBlack = (SampleLumaBilinear(image, sx, sy) <= threshold) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << 7;
            }

            {
                var sx = baseXdx7 + vyXdy8;
                var sy = baseYdx7 + vyYdy8;
                var isBlack = (SampleLumaBilinear(image, sx, sy) <= threshold) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << 8;
            }

            for (var i = 9; i < 15; i++) {
                var dx = (11 - i) + phaseX; // (14 - i) - 3 + phaseX
                var sx = baseXdy8 + vxX * dx;
                var sy = baseYdy8 + vxY * dx;
                var isBlack = (SampleLumaBilinear(image, sx, sy) <= threshold) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << i;
            }

            for (var i = 0; i < 8; i++) {
                var dx = (size - 4 - i) + phaseX; // (size - 1 - i) - 3 + phaseX
                var sx = baseXdy8 + vxX * dx;
                var sy = baseYdy8 + vxY * dx;
                var isBlack = (SampleLumaBilinear(image, sx, sy) <= threshold) ^ invert;
                bitsB |= (isBlack ? 1 : 0) << i;
            }

            for (var i = 8; i < 15; i++) {
                var dy = (size - 18 + i) + phaseY; // (size - 15 + i) - 3 + phaseY
                var sx = baseXdx8 + vyX * dy;
                var sy = baseYdx8 + vyY * dy;
                var isBlack = (SampleLumaBilinear(image, sx, sy) <= threshold) ^ invert;
                bitsB |= (isBlack ? 1 : 0) << i;
            }
        } else {
            for (var i = 0; i <= 5; i++) {
                var dy = (i - 3) + phaseY;
                var sx = baseXdx8 + vyX * dy;
                var sy = baseYdx8 + vyY * dy;
                SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                var isBlack = (lum <= thr) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << i;
            }

            {
                var dy = 4 + phaseY;
                var sx = baseXdx8 + vyX * dy;
                var sy = baseYdx8 + vyY * dy;
                SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                var isBlack = (lum <= thr) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << 6;
            }

            {
                var sx = baseXdx8 + vyXdy8;
                var sy = baseYdx8 + vyYdy8;
                SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                var isBlack = (lum <= thr) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << 7;
            }

            {
                var sx = baseXdx7 + vyXdy8;
                var sy = baseYdx7 + vyYdy8;
                SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                var isBlack = (lum <= thr) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << 8;
            }

            for (var i = 9; i < 15; i++) {
                var dx = (11 - i) + phaseX;
                var sx = baseXdy8 + vxX * dx;
                var sy = baseYdy8 + vxY * dx;
                SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                var isBlack = (lum <= thr) ^ invert;
                bitsA |= (isBlack ? 1 : 0) << i;
            }

            for (var i = 0; i < 8; i++) {
                var dx = (size - 4 - i) + phaseX;
                var sx = baseXdy8 + vxX * dx;
                var sy = baseYdy8 + vxY * dx;
                SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                var isBlack = (lum <= thr) ^ invert;
                bitsB |= (isBlack ? 1 : 0) << i;
            }

            for (var i = 8; i < 15; i++) {
                var dy = (size - 18 + i) + phaseY;
                var sx = baseXdx8 + vyX * dy;
                var sy = baseYdx8 + vyY * dy;
                SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                var isBlack = (lum <= thr) ^ invert;
                bitsB |= (isBlack ? 1 : 0) << i;
            }
        }

        return global::CodeGlyphX.QrDecoder.GetBestFormatDistance(bitsA, bitsB);
    }

    private static int ComputeTimingAlternations(
        QrGrayImage image,
        bool invert,
        double tlX,
        double tlY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        int dimension,
        double phaseX,
        double phaseY) {
        var max = 0;

        // Horizontal timing pattern at y=6, x=8..size-9
        var start = 8;
        var end = dimension - 9;
        if (end > start) {
            var thresholdMap = image.ThresholdMap;
            var threshold = image.Threshold;
            var dxStart = start - 3 + phaseX;
            var dyStart = start - 3 + phaseY;
            var dxFixed = 3 + phaseX;
            var dyFixed = 3 + phaseY;
            var vxXdxStart = vxX * dxStart;
            var vxYdxStart = vxY * dxStart;
            var vyXdyFixed = vyX * dyFixed;
            var vyYdyFixed = vyY * dyFixed;
            var vxXdxFixed = vxX * dxFixed;
            var vxYdxFixed = vxY * dxFixed;
            var vyXdyStart = vyX * dyStart;
            var vyYdyStart = vyY * dyStart;
            var baseHX = tlX + vxXdxStart + vyXdyFixed;
            var baseHY = tlY + vxYdxStart + vyYdyFixed;
            var baseVX = tlX + vxXdxFixed + vyXdyStart;
            var baseVY = tlY + vxYdxFixed + vyYdyStart;
            var steps = end - start;

            if (thresholdMap is null) {
                max += CountAlternationsNoMap(image, baseHX, baseHY, vxX, vxY, steps, invert, threshold);
                max += CountAlternationsNoMap(image, baseVX, baseVY, vyX, vyY, steps, invert, threshold);
            } else {
                max += CountAlternationsWithMap(image, baseHX, baseHY, vxX, vxY, steps, invert);
                max += CountAlternationsWithMap(image, baseVX, baseVY, vyX, vyY, steps, invert);
            }
        }

        return max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountAlternationsNoMap(QrGrayImage image, double sx, double sy, double stepX, double stepY, int steps, bool invert, int threshold) {
        var prev = (SampleLumaBilinear(image, sx, sy) <= threshold) ^ invert;
        var alternations = 0;
        for (var i = 0; i < steps; i++) {
            sx += stepX;
            sy += stepY;
            var cur = (SampleLumaBilinear(image, sx, sy) <= threshold) ^ invert;
            if (cur != prev) alternations++;
            prev = cur;
        }
        return alternations;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountAlternationsWithMap(QrGrayImage image, double sx, double sy, double stepX, double stepY, int steps, bool invert) {
        SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
        var prev = (lum <= thr) ^ invert;
        var alternations = 0;
        for (var i = 0; i < steps; i++) {
            sx += stepX;
            sy += stepY;
            SampleLumaThresholdBilinear(image, sx, sy, out lum, out thr);
            var cur = (lum <= thr) ^ invert;
            if (cur != prev) alternations++;
            prev = cur;
        }
        return alternations;
    }
}
#endif
