#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static class QrPixelSampling {
    private static readonly double[] PhaseOffsetsCoarse = { -0.35, -0.15, 0.0, 0.15, 0.35 };
    private static readonly double[] ScaleFactors = { 0.94, 0.97, 1.0, 1.03, 1.06 };

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

    public static bool SampleModule9(QrGrayImage image, double sx, double sy, double oxX, double oxY, double oyX, double oyY, bool invert) {
        const int required = 5;
        var black = 0;
        var remaining = 9;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;

        if (thresholdMap is null) {
            if (!invert) {
                if (SampleLumaBilinear(image, sx, sy) <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                // Axes
                if (SampleLumaBilinear(image, sx + oxX, sy + oxY) <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (SampleLumaBilinear(image, sx - oxX, sy - oxY) <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (SampleLumaBilinear(image, sx + oyX, sy + oyY) <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (SampleLumaBilinear(image, sx - oyX, sy - oyY) <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                // Diagonals
                if (SampleLumaBilinear(image, sx + oxX + oyX, sy + oxY + oyY) <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (SampleLumaBilinear(image, sx + oxX - oyX, sy + oxY - oyY) <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (SampleLumaBilinear(image, sx - oxX + oyX, sy - oxY + oyY) <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (SampleLumaBilinear(image, sx - oxX - oyX, sy - oxY - oyY) <= threshold) black++;
                return black >= required;
            }

            if (SampleLumaBilinear(image, sx, sy) > threshold) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            // Axes
            if (SampleLumaBilinear(image, sx + oxX, sy + oxY) > threshold) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            if (SampleLumaBilinear(image, sx - oxX, sy - oxY) > threshold) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            if (SampleLumaBilinear(image, sx + oyX, sy + oyY) > threshold) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            if (SampleLumaBilinear(image, sx - oyX, sy - oyY) > threshold) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            // Diagonals
            if (SampleLumaBilinear(image, sx + oxX + oyX, sy + oxY + oyY) > threshold) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            if (SampleLumaBilinear(image, sx + oxX - oyX, sy + oxY - oyY) > threshold) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            if (SampleLumaBilinear(image, sx - oxX + oyX, sy - oxY + oyY) > threshold) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            if (SampleLumaBilinear(image, sx - oxX - oyX, sy - oxY - oyY) > threshold) black++;
            return black >= required;
        }

        if (!invert) {
            SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
            if (lum <= thr) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            // Axes
            SampleLumaThresholdBilinear(image, sx + oxX, sy + oxY, out lum, out thr);
            if (lum <= thr) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            SampleLumaThresholdBilinear(image, sx - oxX, sy - oxY, out lum, out thr);
            if (lum <= thr) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            SampleLumaThresholdBilinear(image, sx + oyX, sy + oyY, out lum, out thr);
            if (lum <= thr) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            SampleLumaThresholdBilinear(image, sx - oyX, sy - oyY, out lum, out thr);
            if (lum <= thr) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            // Diagonals
            SampleLumaThresholdBilinear(image, sx + oxX + oyX, sy + oxY + oyY, out lum, out thr);
            if (lum <= thr) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            SampleLumaThresholdBilinear(image, sx + oxX - oyX, sy + oxY - oyY, out lum, out thr);
            if (lum <= thr) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            SampleLumaThresholdBilinear(image, sx - oxX + oyX, sy - oxY + oyY, out lum, out thr);
            if (lum <= thr) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;

            SampleLumaThresholdBilinear(image, sx - oxX - oyX, sy - oxY - oyY, out lum, out thr);
            if (lum <= thr) black++;
            return black >= required;
        }

        SampleLumaThresholdBilinear(image, sx, sy, out var lumInv, out var thrInv);
        if (lumInv > thrInv) black++;
        remaining--;
        if (black >= required) return true;
        if (black + remaining < required) return false;

        // Axes
        SampleLumaThresholdBilinear(image, sx + oxX, sy + oxY, out lumInv, out thrInv);
        if (lumInv > thrInv) black++;
        remaining--;
        if (black >= required) return true;
        if (black + remaining < required) return false;

        SampleLumaThresholdBilinear(image, sx - oxX, sy - oxY, out lumInv, out thrInv);
        if (lumInv > thrInv) black++;
        remaining--;
        if (black >= required) return true;
        if (black + remaining < required) return false;

        SampleLumaThresholdBilinear(image, sx + oyX, sy + oyY, out lumInv, out thrInv);
        if (lumInv > thrInv) black++;
        remaining--;
        if (black >= required) return true;
        if (black + remaining < required) return false;

        SampleLumaThresholdBilinear(image, sx - oyX, sy - oyY, out lumInv, out thrInv);
        if (lumInv > thrInv) black++;
        remaining--;
        if (black >= required) return true;
        if (black + remaining < required) return false;

        // Diagonals
        SampleLumaThresholdBilinear(image, sx + oxX + oyX, sy + oxY + oyY, out lumInv, out thrInv);
        if (lumInv > thrInv) black++;
        remaining--;
        if (black >= required) return true;
        if (black + remaining < required) return false;

        SampleLumaThresholdBilinear(image, sx + oxX - oyX, sy + oxY - oyY, out lumInv, out thrInv);
        if (lumInv > thrInv) black++;
        remaining--;
        if (black >= required) return true;
        if (black + remaining < required) return false;

        SampleLumaThresholdBilinear(image, sx - oxX + oyX, sy - oxY + oyY, out lumInv, out thrInv);
        if (lumInv > thrInv) black++;
        remaining--;
        if (black >= required) return true;
        if (black + remaining < required) return false;

        SampleLumaThresholdBilinear(image, sx - oxX - oyX, sy - oxY - oyY, out lumInv, out thrInv);
        if (lumInv > thrInv) black++;
        return black >= required;
    }

    private static void FindBestPhase(
        QrGrayImage image,
        bool invert,
        double tlX,
        double tlY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        int dimension,
        out double bestX,
        out double bestY,
        out int bestDist,
        out int bestTiming) {
        bestX = 0;
        bestY = 0;
        bestDist = int.MaxValue;
        bestTiming = int.MinValue;

        for (var yi = 0; yi < PhaseOffsetsCoarse.Length; yi++) {
            var oy = PhaseOffsetsCoarse[yi];
            for (var xi = 0; xi < PhaseOffsetsCoarse.Length; xi++) {
                var ox = PhaseOffsetsCoarse[xi];

                var dist = ComputeFormatDistance(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, dimension, ox, oy);
                var timing = ComputeTimingAlternations(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, dimension, ox, oy);

                if (dist < bestDist || (dist == bestDist && timing > bestTiming)) {
                    bestDist = dist;
                    bestTiming = timing;
                    bestX = ox;
                    bestY = oy;
                }

                if (bestDist == 0 && bestTiming > (dimension - 16) * 2 - 4) return;
            }
        }

        // Small refinement pass around the best coarse point.
        Span<double> fine = stackalloc double[5];
        fine[0] = bestX - 0.10;
        fine[1] = bestX - 0.05;
        fine[2] = bestX;
        fine[3] = bestX + 0.05;
        fine[4] = bestX + 0.10;

        Span<double> fineY = stackalloc double[5];
        fineY[0] = bestY - 0.10;
        fineY[1] = bestY - 0.05;
        fineY[2] = bestY;
        fineY[3] = bestY + 0.05;
        fineY[4] = bestY + 0.10;

        bestDist = int.MaxValue;
        bestTiming = int.MinValue;

        for (var yi = 0; yi < fineY.Length; yi++) {
            var oy = Math.Clamp(fineY[yi], -0.49, 0.49);
            for (var xi = 0; xi < fine.Length; xi++) {
                var ox = Math.Clamp(fine[xi], -0.49, 0.49);

                var dist = ComputeFormatDistance(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, dimension, ox, oy);
                var timing = ComputeTimingAlternations(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, dimension, ox, oy);

                if (dist < bestDist || (dist == bestDist && timing > bestTiming)) {
                    bestDist = dist;
                    bestTiming = timing;
                    bestX = ox;
                    bestY = oy;
                }

                if (bestDist == 0 && bestTiming > (dimension - 16) * 2 - 4) return;
            }
        }
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

        if (thresholdMap is null) {
            if (!invert) {
                for (var i = 0; i <= 5; i++) {
                    var dy = (i - 3) + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    if (SampleLumaBilinear(image, sx, sy) <= threshold) bitsA |= 1 << i;
                }

                {
                    var dy = 4 + phaseY; // 7 - 3 + phaseY
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    if (SampleLumaBilinear(image, sx, sy) <= threshold) bitsA |= 1 << 6;
                }

                {
                    var sx = tlX + vxX * dx8 + vyX * dy8;
                    var sy = tlY + vxY * dx8 + vyY * dy8;
                    if (SampleLumaBilinear(image, sx, sy) <= threshold) bitsA |= 1 << 7;
                }

                {
                    var sx = tlX + vxX * dx7 + vyX * dy8;
                    var sy = tlY + vxY * dx7 + vyY * dy8;
                    if (SampleLumaBilinear(image, sx, sy) <= threshold) bitsA |= 1 << 8;
                }

                for (var i = 9; i < 15; i++) {
                    var dx = (11 - i) + phaseX; // (14 - i) - 3 + phaseX
                    var sx = tlX + vxX * dx + vyX * dy8;
                    var sy = tlY + vxY * dx + vyY * dy8;
                    if (SampleLumaBilinear(image, sx, sy) <= threshold) bitsA |= 1 << i;
                }

                for (var i = 0; i < 8; i++) {
                    var dx = (size - 4 - i) + phaseX; // (size - 1 - i) - 3 + phaseX
                    var sx = tlX + vxX * dx + vyX * dy8;
                    var sy = tlY + vxY * dx + vyY * dy8;
                    if (SampleLumaBilinear(image, sx, sy) <= threshold) bitsB |= 1 << i;
                }

                for (var i = 8; i < 15; i++) {
                    var dy = (size - 18 + i) + phaseY; // (size - 15 + i) - 3 + phaseY
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    if (SampleLumaBilinear(image, sx, sy) <= threshold) bitsB |= 1 << i;
                }
            } else {
                for (var i = 0; i <= 5; i++) {
                    var dy = (i - 3) + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    if (SampleLumaBilinear(image, sx, sy) > threshold) bitsA |= 1 << i;
                }

                {
                    var dy = 4 + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    if (SampleLumaBilinear(image, sx, sy) > threshold) bitsA |= 1 << 6;
                }

                {
                    var sx = tlX + vxX * dx8 + vyX * dy8;
                    var sy = tlY + vxY * dx8 + vyY * dy8;
                    if (SampleLumaBilinear(image, sx, sy) > threshold) bitsA |= 1 << 7;
                }

                {
                    var sx = tlX + vxX * dx7 + vyX * dy8;
                    var sy = tlY + vxY * dx7 + vyY * dy8;
                    if (SampleLumaBilinear(image, sx, sy) > threshold) bitsA |= 1 << 8;
                }

                for (var i = 9; i < 15; i++) {
                    var dx = (11 - i) + phaseX;
                    var sx = tlX + vxX * dx + vyX * dy8;
                    var sy = tlY + vxY * dx + vyY * dy8;
                    if (SampleLumaBilinear(image, sx, sy) > threshold) bitsA |= 1 << i;
                }

                for (var i = 0; i < 8; i++) {
                    var dx = (size - 4 - i) + phaseX;
                    var sx = tlX + vxX * dx + vyX * dy8;
                    var sy = tlY + vxY * dx + vyY * dy8;
                    if (SampleLumaBilinear(image, sx, sy) > threshold) bitsB |= 1 << i;
                }

                for (var i = 8; i < 15; i++) {
                    var dy = (size - 18 + i) + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    if (SampleLumaBilinear(image, sx, sy) > threshold) bitsB |= 1 << i;
                }
            }
        } else {
            if (!invert) {
                for (var i = 0; i <= 5; i++) {
                    var dy = (i - 3) + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum <= thr) bitsA |= 1 << i;
                }

                {
                    var dy = 4 + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum <= thr) bitsA |= 1 << 6;
                }

                {
                    var sx = tlX + vxX * dx8 + vyX * dy8;
                    var sy = tlY + vxY * dx8 + vyY * dy8;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum <= thr) bitsA |= 1 << 7;
                }

                {
                    var sx = tlX + vxX * dx7 + vyX * dy8;
                    var sy = tlY + vxY * dx7 + vyY * dy8;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum <= thr) bitsA |= 1 << 8;
                }

                for (var i = 9; i < 15; i++) {
                    var dx = (11 - i) + phaseX;
                    var sx = tlX + vxX * dx + vyX * dy8;
                    var sy = tlY + vxY * dx + vyY * dy8;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum <= thr) bitsA |= 1 << i;
                }

                for (var i = 0; i < 8; i++) {
                    var dx = (size - 4 - i) + phaseX;
                    var sx = tlX + vxX * dx + vyX * dy8;
                    var sy = tlY + vxY * dx + vyY * dy8;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum <= thr) bitsB |= 1 << i;
                }

                for (var i = 8; i < 15; i++) {
                    var dy = (size - 18 + i) + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum <= thr) bitsB |= 1 << i;
                }
            } else {
                for (var i = 0; i <= 5; i++) {
                    var dy = (i - 3) + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum > thr) bitsA |= 1 << i;
                }

                {
                    var dy = 4 + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum > thr) bitsA |= 1 << 6;
                }

                {
                    var sx = tlX + vxX * dx8 + vyX * dy8;
                    var sy = tlY + vxY * dx8 + vyY * dy8;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum > thr) bitsA |= 1 << 7;
                }

                {
                    var sx = tlX + vxX * dx7 + vyX * dy8;
                    var sy = tlY + vxY * dx7 + vyY * dy8;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum > thr) bitsA |= 1 << 8;
                }

                for (var i = 9; i < 15; i++) {
                    var dx = (11 - i) + phaseX;
                    var sx = tlX + vxX * dx + vyX * dy8;
                    var sy = tlY + vxY * dx + vyY * dy8;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum > thr) bitsA |= 1 << i;
                }

                for (var i = 0; i < 8; i++) {
                    var dx = (size - 4 - i) + phaseX;
                    var sx = tlX + vxX * dx + vyX * dy8;
                    var sy = tlY + vxY * dx + vyY * dy8;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum > thr) bitsB |= 1 << i;
                }

                for (var i = 8; i < 15; i++) {
                    var dy = (size - 18 + i) + phaseY;
                    var sx = tlX + vxX * dx8 + vyX * dy;
                    var sy = tlY + vxY * dx8 + vyY * dy;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    if (lum > thr) bitsB |= 1 << i;
                }
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

            if (thresholdMap is null) {
                if (!invert) {
                    // Horizontal
                    var sx = tlX + vxX * dxStart + vyX * dyFixed;
                    var sy = tlY + vxY * dxStart + vyY * dyFixed;
                    var prev = SampleLumaBilinear(image, sx, sy) <= threshold;
                    for (var x = start + 1; x <= end; x++) {
                        sx += vxX;
                        sy += vxY;
                        var cur = SampleLumaBilinear(image, sx, sy) <= threshold;
                        if (cur != prev) max++;
                        prev = cur;
                    }

                    // Vertical
                    sx = tlX + vxX * dxFixed + vyX * dyStart;
                    sy = tlY + vxY * dxFixed + vyY * dyStart;
                    prev = SampleLumaBilinear(image, sx, sy) <= threshold;
                    for (var y = start + 1; y <= end; y++) {
                        sx += vyX;
                        sy += vyY;
                        var cur = SampleLumaBilinear(image, sx, sy) <= threshold;
                        if (cur != prev) max++;
                        prev = cur;
                    }
                } else {
                    // Horizontal
                    var sx = tlX + vxX * dxStart + vyX * dyFixed;
                    var sy = tlY + vxY * dxStart + vyY * dyFixed;
                    var prev = SampleLumaBilinear(image, sx, sy) > threshold;
                    for (var x = start + 1; x <= end; x++) {
                        sx += vxX;
                        sy += vxY;
                        var cur = SampleLumaBilinear(image, sx, sy) > threshold;
                        if (cur != prev) max++;
                        prev = cur;
                    }

                    // Vertical
                    sx = tlX + vxX * dxFixed + vyX * dyStart;
                    sy = tlY + vxY * dxFixed + vyY * dyStart;
                    prev = SampleLumaBilinear(image, sx, sy) > threshold;
                    for (var y = start + 1; y <= end; y++) {
                        sx += vyX;
                        sy += vyY;
                        var cur = SampleLumaBilinear(image, sx, sy) > threshold;
                        if (cur != prev) max++;
                        prev = cur;
                    }
                }
            } else {
                if (!invert) {
                    // Horizontal
                    var sx = tlX + vxX * dxStart + vyX * dyFixed;
                    var sy = tlY + vxY * dxStart + vyY * dyFixed;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    var prev = lum <= thr;
                    for (var x = start + 1; x <= end; x++) {
                        sx += vxX;
                        sy += vxY;
                        SampleLumaThresholdBilinear(image, sx, sy, out lum, out thr);
                        var cur = lum <= thr;
                        if (cur != prev) max++;
                        prev = cur;
                    }

                    // Vertical
                    sx = tlX + vxX * dxFixed + vyX * dyStart;
                    sy = tlY + vxY * dxFixed + vyY * dyStart;
                    SampleLumaThresholdBilinear(image, sx, sy, out lum, out thr);
                    prev = lum <= thr;
                    for (var y = start + 1; y <= end; y++) {
                        sx += vyX;
                        sy += vyY;
                        SampleLumaThresholdBilinear(image, sx, sy, out lum, out thr);
                        var cur = lum <= thr;
                        if (cur != prev) max++;
                        prev = cur;
                    }
                } else {
                    // Horizontal
                    var sx = tlX + vxX * dxStart + vyX * dyFixed;
                    var sy = tlY + vxY * dxStart + vyY * dyFixed;
                    SampleLumaThresholdBilinear(image, sx, sy, out var lum, out var thr);
                    var prev = lum > thr;
                    for (var x = start + 1; x <= end; x++) {
                        sx += vxX;
                        sy += vxY;
                        SampleLumaThresholdBilinear(image, sx, sy, out lum, out thr);
                        var cur = lum > thr;
                        if (cur != prev) max++;
                        prev = cur;
                    }

                    // Vertical
                    sx = tlX + vxX * dxFixed + vyX * dyStart;
                    sy = tlY + vxY * dxFixed + vyY * dyStart;
                    SampleLumaThresholdBilinear(image, sx, sy, out lum, out thr);
                    prev = lum > thr;
                    for (var y = start + 1; y <= end; y++) {
                        sx += vyX;
                        sy += vyY;
                        SampleLumaThresholdBilinear(image, sx, sy, out lum, out thr);
                        var cur = lum > thr;
                        if (cur != prev) max++;
                        prev = cur;
                    }
                }
            }
        }

        return max;
    }

    public static bool IsBlackBilinear(QrGrayImage image, double x, double y, bool invert) {
        if (image.ThresholdMap is null) {
            var lum = SampleLumaBilinear(image, x, y);
            var black = lum <= image.Threshold;
            return invert ? !black : black;
        }

        SampleLumaThresholdBilinear(image, x, y, out var lumBilinear, out var threshold);
        var blackBilinear = lumBilinear <= threshold;
        return invert ? !blackBilinear : blackBilinear;
    }

    public static bool IsBlackNearest(QrGrayImage image, double x, double y, bool invert) {
        var px = QrMath.RoundToInt(x);
        var py = QrMath.RoundToInt(y);

        if (px < 0) px = 0;
        else if (px >= image.Width) px = image.Width - 1;

        if (py < 0) py = 0;
        else if (py >= image.Height) py = image.Height - 1;

        return image.IsBlack(px, py, invert);
    }

    public static bool SampleModule9Px(QrGrayImage image, double sx, double sy, bool invert) {
        return SampleModule9Px(image, sx, sy, invert, moduleSizePx: 0);
    }

    public static bool SampleModuleMajority3x3(QrGrayImage image, double sx, double sy, bool invert) {
        var px = QrMath.RoundToInt(sx);
        var py = QrMath.RoundToInt(sy);

        const int required = 5;
        var black = 0;
        var remaining = 9;
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;

        if (thresholdMap is null) {
            if (!invert) {
                for (var dy = -1; dy <= 1; dy++) {
                    var y = py + dy;
                    if (y < 0) y = 0;
                    else if (y >= height) y = height - 1;
                    var row = y * width;

                    for (var dx = -1; dx <= 1; dx++) {
                        var x = px + dx;
                        if (x < 0) x = 0;
                        else if (x >= width) x = width - 1;

                        if (gray[row + x] <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var dy = -1; dy <= 1; dy++) {
                    var y = py + dy;
                    if (y < 0) y = 0;
                    else if (y >= height) y = height - 1;
                    var row = y * width;

                    for (var dx = -1; dx <= 1; dx++) {
                        var x = px + dx;
                        if (x < 0) x = 0;
                        else if (x >= width) x = width - 1;

                        if (gray[row + x] > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var dy = -1; dy <= 1; dy++) {
                    var y = py + dy;
                    if (y < 0) y = 0;
                    else if (y >= height) y = height - 1;
                    var row = y * width;

                    for (var dx = -1; dx <= 1; dx++) {
                        var x = px + dx;
                        if (x < 0) x = 0;
                        else if (x >= width) x = width - 1;

                        var idx = row + x;
                        if (gray[idx] <= thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var dy = -1; dy <= 1; dy++) {
                    var y = py + dy;
                    if (y < 0) y = 0;
                    else if (y >= height) y = height - 1;
                    var row = y * width;

                    for (var dx = -1; dx <= 1; dx++) {
                        var x = px + dx;
                        if (x < 0) x = 0;
                        else if (x >= width) x = width - 1;

                        var idx = row + x;
                        if (gray[idx] > thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }

        return black >= required;
    }

    public static bool SampleModuleCenter3x3(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDeltaCenter(moduleSizePx);
        const int required = 5;
        var black = 0;
        var remaining = 9;
        var startY = sy - d;
        var startX = sx - d;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum <= thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum > thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }
        return black >= required;
    }

    public static bool SampleModuleCenter3x3Loose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDeltaCenter(moduleSizePx);
        const int required = 3;
        var black = 0;
        var remaining = 9;
        var startY = sy - d;
        var startX = sx - d;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum <= thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum > thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }
        return black >= required;
    }

    public static bool SampleModule25Px(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta5x5(moduleSizePx);

        const int required = 13;
        var black = 0;
        var remaining = 25;
        var startY = sy - (2 * d);
        var startX = sx - (2 * d);
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum <= thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum > thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }

        return black >= required;
    }

    public static bool SampleModule25Nearest(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta5x5(moduleSizePx);

        const int required = 13;
        var black = 0;
        var remaining = 25;
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        var startY = sy - (2 * d);
        var startX = sx - (2 * d);

        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        if (gray[row + px] <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        if (gray[row + px] > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        var idx = row + px;
                        if (gray[idx] <= thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        var idx = row + px;
                        if (gray[idx] > thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }

        return black >= required;
    }

    public static bool SampleModule25NearestLoose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta5x5(moduleSizePx);

        const int required = 7;
        var black = 0;
        var remaining = 25;
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        var startY = sy - (2 * d);
        var startX = sx - (2 * d);

        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        if (gray[row + px] <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        if (gray[row + px] > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        var idx = row + px;
                        if (gray[idx] <= thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 5; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 5; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        var idx = row + px;
                        if (gray[idx] > thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }

        return black >= required;
    }

    public static bool SampleModule9Px(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        // For non-integer module sizes (common on UI-scaled QR) we need to sample a larger fraction of a module.
        // If the delta is too large at downscaled resolutions, samples spill into neighbors and decoding fails.
        var d = GetSampleDelta(moduleSizePx);

        const int required = 5;
        var black = 0;
        var remaining = 9;
        var startY = sy - d;
        var startX = sx - d;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum <= thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum > thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }
        return black >= required;
    }

    public static bool SampleModule9PxLoose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta(moduleSizePx);

        const int required = 3;
        var black = 0;
        var remaining = 9;
        var startY = sy - d;
        var startX = sx - d;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        if (SampleLumaBilinear(image, x, y) > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum <= thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        SampleLumaThresholdBilinear(image, x, y, out var lum, out var thr);
                        if (lum > thr) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }
        return black >= required;
    }

    public static bool SampleModule9Nearest(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta(moduleSizePx);

        const int required = 5;
        var black = 0;
        var remaining = 9;
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        var startY = sy - d;
        var startX = sx - d;

        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        if (gray[row + px] <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        if (gray[row + px] > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        var idx = row + px;
                        if (gray[idx] <= thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        var idx = row + px;
                        if (gray[idx] > thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }
        return black >= required;
    }

    public static bool SampleModule9NearestLoose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta(moduleSizePx);

        const int required = 3;
        var black = 0;
        var remaining = 9;
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        var startY = sy - d;
        var startX = sx - d;

        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        if (gray[row + px] <= threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        if (gray[row + px] > threshold) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        var idx = row + px;
                        if (gray[idx] <= thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            } else {
                for (var iy = 0; iy < 3; iy++) {
                    var y = startY + iy * d;
                    var py = QrMath.RoundToInt(y);
                    if (py < 0) py = 0;
                    else if (py >= height) py = height - 1;
                    var row = py * width;
                    for (var ix = 0; ix < 3; ix++) {
                        var x = startX + ix * d;
                        var px = QrMath.RoundToInt(x);
                        if (px < 0) px = 0;
                        else if (px >= width) px = width - 1;
                        var idx = row + px;
                        if (gray[idx] > thresholdMap[idx]) black++;
                        remaining--;
                        if (black >= required) return true;
                        if (black + remaining < required) return false;
                    }
                }
            }
        }
        return black >= required;
    }

    private static double GetSampleDelta(double moduleSizePx) {
        if (!(moduleSizePx > 0)) return 0.85;

        // Aim for ~1/3 of a module, but cap to <1/2 to avoid crossing module boundaries.
        var d = moduleSizePx * 0.33;
        if (d > 0.85) d = 0.85;

        // Ensure we actually move across pixel grid for typical module sizes.
        if (d < 0.35) d = 0.35;

        var max = moduleSizePx * 0.49;
        if (d > max) d = max;

        return d;
    }

    private static double GetSampleDeltaCenter(double moduleSizePx) {
        if (!(moduleSizePx > 0)) return 0.35;

        var d = moduleSizePx * 0.20;
        if (d < 0.25) d = 0.25;
        if (d > 0.65) d = 0.65;

        var max = moduleSizePx * 0.35;
        if (d > max) d = max;

        return d;
    }

    private static double GetSampleDelta5x5(double moduleSizePx) {
        if (!(moduleSizePx > 0)) return 0.85;

        // For a 5x5 grid, the max offset is 2*d, so keep d safely under 1/4 module to stay within a cell.
        var d = moduleSizePx * 0.20;
        if (d > 0.85) d = 0.85;

        var max = moduleSizePx * 0.24;
        if (d > max) d = max;

        // If modules are tiny, fall back to a small but non-zero delta.
        if (d < 0.20) d = 0.20;

        return d;
    }

    private static byte SampleLumaBilinear(QrGrayImage image, double x, double y) {
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;

        if (x < 0) x = 0;
        else if (x > maxX) x = maxX;

        if (y < 0) y = 0;
        else if (y > maxY) y = maxY;

        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        if (x1 > maxX) x1 = maxX;
        if (y1 > maxY) y1 = maxY;

        var fx = x - x0;
        var fy = y - y0;

        var w = width;
        var g = image.Gray;

        var l00 = g[y0 * w + x0];
        var l10 = g[y0 * w + x1];
        var l01 = g[y1 * w + x0];
        var l11 = g[y1 * w + x1];

        var l0 = l00 + (l10 - l00) * fx;
        var l1 = l01 + (l11 - l01) * fx;
        var lum = l0 + (l1 - l0) * fy;

        var rounded = (int)(lum + 0.5);
        if (rounded < 0) rounded = 0;
        else if (rounded > 255) rounded = 255;
        return (byte)rounded;
    }

    private static void SampleLumaThresholdBilinear(QrGrayImage image, double x, double y, out byte lum, out byte threshold) {
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;

        if (x < 0) x = 0;
        else if (x > maxX) x = maxX;

        if (y < 0) y = 0;
        else if (y > maxY) y = maxY;

        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        if (x1 > maxX) x1 = maxX;
        if (y1 > maxY) y1 = maxY;

        var fx = x - x0;
        var fy = y - y0;

        var w = width;
        var g = image.Gray;
        var t = image.ThresholdMap!;

        var idx00 = y0 * w + x0;
        var idx10 = y0 * w + x1;
        var idx01 = y1 * w + x0;
        var idx11 = y1 * w + x1;

        var l00 = g[idx00];
        var l10 = g[idx10];
        var l01 = g[idx01];
        var l11 = g[idx11];

        var l0 = l00 + (l10 - l00) * fx;
        var l1 = l01 + (l11 - l01) * fx;
        var lumValue = l0 + (l1 - l0) * fy;

        var t00 = t[idx00];
        var t10 = t[idx10];
        var t01 = t[idx01];
        var t11 = t[idx11];

        var t0 = t00 + (t10 - t00) * fx;
        var t1 = t01 + (t11 - t01) * fx;
        var thrValue = t0 + (t1 - t0) * fy;

        var lumRounded = (int)(lumValue + 0.5);
        if (lumRounded < 0) lumRounded = 0;
        else if (lumRounded > 255) lumRounded = 255;

        var thrRounded = (int)(thrValue + 0.5);
        if (thrRounded < 0) thrRounded = 0;
        else if (thrRounded > 255) thrRounded = 255;

        lum = (byte)lumRounded;
        threshold = (byte)thrRounded;
    }

    private static byte SampleThresholdBilinear(QrGrayImage image, double x, double y) {
        if (image.ThresholdMap is null) return image.Threshold;

        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;

        if (x < 0) x = 0;
        else if (x > maxX) x = maxX;

        if (y < 0) y = 0;
        else if (y > maxY) y = maxY;

        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        if (x1 > maxX) x1 = maxX;
        if (y1 > maxY) y1 = maxY;

        var fx = x - x0;
        var fy = y - y0;

        var w = width;
        var t = image.ThresholdMap;
        var t00 = t[y0 * w + x0];
        var t10 = t[y0 * w + x1];
        var t01 = t[y1 * w + x0];
        var t11 = t[y1 * w + x1];

        var t0 = t00 + (t10 - t00) * fx;
        var t1 = t01 + (t11 - t01) * fx;
        var thr = t0 + (t1 - t0) * fy;

        var rounded = (int)(thr + 0.5);
        if (rounded < 0) rounded = 0;
        else if (rounded > 255) rounded = 255;
        return (byte)rounded;
    }
}
#endif
