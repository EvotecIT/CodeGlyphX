#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

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
        var thresholdMap = image.ThresholdMap;
        if (thresholdMap is null) {
            return SampleModule9NoMap(image, sx, sy, oxX, oxY, oyX, oyY, invert);
        }
        return SampleModule9WithMap(image, sx, sy, oxX, oxY, oyX, oyY, invert);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SampleModule9NoMap(QrGrayImage image, double sx, double sy, double oxX, double oxY, double oyX, double oyY, bool invert) {
        const int required = 5;
        Span<double> dx = stackalloc double[9];
        Span<double> dy = stackalloc double[9];
        FillSampleOffsets(oxX, oxY, oyX, oyY, dx, dy);
        var threshold = image.Threshold;
        var black = 0;
        var remaining = 9;
        if (!invert) {
            for (var i = 0; i < 9; i++) {
                if (SampleLumaBilinear(image, sx + dx[i], sy + dy[i]) <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
            }
            return black >= required;
        }

        for (var i = 0; i < 9; i++) {
            if (SampleLumaBilinear(image, sx + dx[i], sy + dy[i]) > threshold) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;
        }
        return black >= required;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SampleModule9WithMap(QrGrayImage image, double sx, double sy, double oxX, double oxY, double oyX, double oyY, bool invert) {
        const int required = 5;
        Span<double> dx = stackalloc double[9];
        Span<double> dy = stackalloc double[9];
        FillSampleOffsets(oxX, oxY, oyX, oyY, dx, dy);
        var black = 0;
        var remaining = 9;
        if (!invert) {
            for (var i = 0; i < 9; i++) {
                SampleLumaThresholdBilinear(image, sx + dx[i], sy + dy[i], out var lum, out var thr);
                if (lum <= thr) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
            }
            return black >= required;
        }

        for (var i = 0; i < 9; i++) {
            SampleLumaThresholdBilinear(image, sx + dx[i], sy + dy[i], out var lum, out var thr);
            if (lum > thr) black++;
            remaining--;
            if (black >= required) return true;
            if (black + remaining < required) return false;
        }
        return black >= required;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FillSampleOffsets(double oxX, double oxY, double oyX, double oyY, Span<double> dx, Span<double> dy) {
        dx[0] = 0;
        dy[0] = 0;
        dx[1] = oxX;
        dy[1] = oxY;
        dx[2] = -oxX;
        dy[2] = -oxY;
        dx[3] = oyX;
        dy[3] = oyY;
        dx[4] = -oyX;
        dy[4] = -oyY;
        dx[5] = oxX + oyX;
        dy[5] = oxY + oyY;
        dx[6] = oxX - oyX;
        dy[6] = oxY - oyY;
        dx[7] = -oxX + oyX;
        dy[7] = -oxY + oyY;
        dx[8] = -oxX - oyX;
        dy[8] = -oxY - oyY;
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
        var maxX = width - 1;
        var maxY = height - 1;
        var inBounds = px > 0 && px < maxX && py > 0 && py < maxY;

        if (thresholdMap is null) {
            if (!invert) {
                if (inBounds) {
                    var x0 = px - 1;
                    var x1 = px;
                    var x2 = px + 1;
                    var row0 = (py - 1) * width;
                    var row1 = py * width;
                    var row2 = (py + 1) * width;

                    if (gray[row0 + x0] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row0 + x1] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row0 + x2] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row1 + x0] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row1 + x1] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row1 + x2] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row2 + x0] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row2 + x1] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row2 + x2] <= threshold) black++;
                    return black >= required;
                }

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
                if (inBounds) {
                    var x0 = px - 1;
                    var x1 = px;
                    var x2 = px + 1;
                    var row0 = (py - 1) * width;
                    var row1 = py * width;
                    var row2 = (py + 1) * width;

                    if (gray[row0 + x0] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row0 + x1] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row0 + x2] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row1 + x0] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row1 + x1] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row1 + x2] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row2 + x0] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row2 + x1] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row2 + x2] > threshold) black++;
                    return black >= required;
                }

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
                if (inBounds) {
                    var x0 = px - 1;
                    var x1 = px;
                    var x2 = px + 1;
                    var row0 = (py - 1) * width;
                    var row1 = py * width;
                    var row2 = (py + 1) * width;

                    var idx = row0 + x0;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row0 + x1;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row0 + x2;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row1 + x0;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row1 + x1;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row1 + x2;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row2 + x0;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row2 + x1;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row2 + x2;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    return black >= required;
                }

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
                if (inBounds) {
                    var x0 = px - 1;
                    var x1 = px;
                    var x2 = px + 1;
                    var row0 = (py - 1) * width;
                    var row1 = py * width;
                    var row2 = (py + 1) * width;

                    var idx = row0 + x0;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row0 + x1;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row0 + x2;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row1 + x0;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row1 + x1;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row1 + x2;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row2 + x0;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row2 + x1;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row2 + x2;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    return black >= required;
                }

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
        return SampleModule9PxCore(image, sx, sy, invert, d, required: 5);
    }

    internal static bool SampleModuleCenter3x3WithDelta(QrGrayImage image, double sx, double sy, bool invert, double d) {
        return SampleModule9PxCore(image, sx, sy, invert, d, required: 5);
    }

    public static bool SampleModuleCenter3x3Loose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDeltaCenter(moduleSizePx);
        return SampleModule9PxCore(image, sx, sy, invert, d, required: 3);
    }

    internal static bool SampleModuleCenter3x3LooseWithDelta(QrGrayImage image, double sx, double sy, bool invert, double d) {
        return SampleModule9PxCore(image, sx, sy, invert, d, required: 3);
    }

    private static bool SampleModule9PxCore(QrGrayImage image, double sx, double sy, bool invert, double d, int required) {
        var black = 0;
        var remaining = 9;
        var startY = sy - d;
        var startX = sx - d;
        var x0 = startX;
        var x1 = startX + d;
        var x2 = x1 + d;
        var y0 = startY;
        var y1 = startY + d;
        var y2 = y1 + d;
        var maxX = image.Width - 1;
        var maxY = image.Height - 1;
        var inBounds = x0 >= 0 && x2 < maxX && y0 >= 0 && y2 < maxY;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        if (thresholdMap is null) {
            if (!invert) {
                if (inBounds) {
                    if (SampleLumaBilinearUnchecked(image, x0, y0) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x1, y0) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x2, y0) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x0, y1) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x1, y1) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x2, y1) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x0, y2) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x1, y2) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x2, y2) <= threshold) black++;
                } else {
                    if (SampleLumaBilinear(image, x0, y0) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x1, y0) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x2, y0) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x0, y1) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x1, y1) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x2, y1) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x0, y2) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x1, y2) <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x2, y2) <= threshold) black++;
                }
            } else {
                if (inBounds) {
                    if (SampleLumaBilinearUnchecked(image, x0, y0) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x1, y0) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x2, y0) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x0, y1) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x1, y1) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x2, y1) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x0, y2) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x1, y2) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinearUnchecked(image, x2, y2) > threshold) black++;
                } else {
                    if (SampleLumaBilinear(image, x0, y0) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x1, y0) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x2, y0) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x0, y1) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x1, y1) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x2, y1) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x0, y2) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x1, y2) > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (SampleLumaBilinear(image, x2, y2) > threshold) black++;
                }
            }
        } else {
            if (!invert) {
                byte lum;
                byte thr;
                if (inBounds) {
                    SampleLumaThresholdBilinearUnchecked(image, x0, y0, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x1, y0, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x2, y0, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x0, y1, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x1, y1, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x2, y1, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x0, y2, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x1, y2, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x2, y2, out lum, out thr);
                    if (lum <= thr) black++;
                } else {
                    SampleLumaThresholdBilinear(image, x0, y0, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x1, y0, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x2, y0, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x0, y1, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x1, y1, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x2, y1, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x0, y2, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x1, y2, out lum, out thr);
                    if (lum <= thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x2, y2, out lum, out thr);
                    if (lum <= thr) black++;
                }
            } else {
                byte lum;
                byte thr;
                if (inBounds) {
                    SampleLumaThresholdBilinearUnchecked(image, x0, y0, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x1, y0, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x2, y0, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x0, y1, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x1, y1, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x2, y1, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x0, y2, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x1, y2, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinearUnchecked(image, x2, y2, out lum, out thr);
                    if (lum > thr) black++;
                } else {
                    SampleLumaThresholdBilinear(image, x0, y0, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x1, y0, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x2, y0, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x0, y1, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x1, y1, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x2, y1, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x0, y2, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x1, y2, out lum, out thr);
                    if (lum > thr) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    SampleLumaThresholdBilinear(image, x2, y2, out lum, out thr);
                    if (lum > thr) black++;
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
        var maxX = image.Width - 1;
        var maxY = image.Height - 1;
        var inBounds = startX >= 0 && startX + d * 4 < maxX && startY >= 0 && startY + d * 4 < maxY;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        if (thresholdMap is null) {
            if (!invert) {
                if (inBounds) {
                    for (var iy = 0; iy < 5; iy++) {
                        var y = startY + iy * d;
                        for (var ix = 0; ix < 5; ix++) {
                            var x = startX + ix * d;
                            if (SampleLumaBilinearUnchecked(image, x, y) <= threshold) black++;
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
                            if (SampleLumaBilinear(image, x, y) <= threshold) black++;
                            remaining--;
                            if (black >= required) return true;
                            if (black + remaining < required) return false;
                        }
                    }
                }
            } else {
                if (inBounds) {
                    for (var iy = 0; iy < 5; iy++) {
                        var y = startY + iy * d;
                        for (var ix = 0; ix < 5; ix++) {
                            var x = startX + ix * d;
                            if (SampleLumaBilinearUnchecked(image, x, y) > threshold) black++;
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
            }
        } else {
            if (!invert) {
                if (inBounds) {
                    for (var iy = 0; iy < 5; iy++) {
                        var y = startY + iy * d;
                        for (var ix = 0; ix < 5; ix++) {
                            var x = startX + ix * d;
                            SampleLumaThresholdBilinearUnchecked(image, x, y, out var lum, out var thr);
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
                            if (lum <= thr) black++;
                            remaining--;
                            if (black >= required) return true;
                            if (black + remaining < required) return false;
                        }
                    }
                }
            } else {
                if (inBounds) {
                    for (var iy = 0; iy < 5; iy++) {
                        var y = startY + iy * d;
                        for (var ix = 0; ix < 5; ix++) {
                            var x = startX + ix * d;
                            SampleLumaThresholdBilinearUnchecked(image, x, y, out var lum, out var thr);
                            if (lum > thr) black++;
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
        }

        return black >= required;
    }

    public static bool SampleModule25Nearest(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta5x5(moduleSizePx);
        return SampleModule25NearestCore(image, sx, sy, invert, d, required: 13);
    }

    internal static bool SampleModule25NearestWithDelta(QrGrayImage image, double sx, double sy, bool invert, double d) {
        return SampleModule25NearestCore(image, sx, sy, invert, d, required: 13);
    }

    public static bool SampleModule25NearestLoose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta5x5(moduleSizePx);
        return SampleModule25NearestCore(image, sx, sy, invert, d, required: 7);
    }

    internal static bool SampleModule25NearestLooseWithDelta(QrGrayImage image, double sx, double sy, bool invert, double d) {
        return SampleModule25NearestCore(image, sx, sy, invert, d, required: 7);
    }

    private static bool SampleModule25NearestCore(QrGrayImage image, double sx, double sy, bool invert, double d, int required) {
        var black = 0;
        var remaining = 25;
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        var startY = sy - (2 * d);
        var startX = sx - (2 * d);
        var endX = startX + d * 4;
        var endY = startY + d * 4;
        var inBounds = startX >= -0.5 && endX < maxX + 0.5 && startY >= -0.5 && endY < maxY + 0.5;
        int px0;
        int px1;
        int px2;
        int px3;
        int px4;
        int py0;
        int py1;
        int py2;
        int py3;
        int py4;
        if (inBounds) {
            px0 = QrMath.RoundToInt(startX);
            px1 = QrMath.RoundToInt(startX + d);
            px2 = QrMath.RoundToInt(startX + d * 2);
            px3 = QrMath.RoundToInt(startX + d * 3);
            px4 = QrMath.RoundToInt(endX);
            py0 = QrMath.RoundToInt(startY);
            py1 = QrMath.RoundToInt(startY + d);
            py2 = QrMath.RoundToInt(startY + d * 2);
            py3 = QrMath.RoundToInt(startY + d * 3);
            py4 = QrMath.RoundToInt(endY);
        } else {
            px0 = ClampRound(startX, maxX);
            px1 = ClampRound(startX + d, maxX);
            px2 = ClampRound(startX + d * 2, maxX);
            px3 = ClampRound(startX + d * 3, maxX);
            px4 = ClampRound(endX, maxX);
            py0 = ClampRound(startY, maxY);
            py1 = ClampRound(startY + d, maxY);
            py2 = ClampRound(startY + d * 2, maxY);
            py3 = ClampRound(startY + d * 3, maxY);
            py4 = ClampRound(endY, maxY);
        }
        Span<int> rows = stackalloc int[5];
        rows[0] = py0 * width;
        rows[1] = py1 * width;
        rows[2] = py2 * width;
        rows[3] = py3 * width;
        rows[4] = py4 * width;

        if (thresholdMap is null) {
            if (!invert) {
                for (var iy = 0; iy < 5; iy++) {
                    var row = rows[iy];
                    if (gray[row + px0] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row + px1] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row + px2] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row + px3] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row + px4] <= threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;
                }
            } else {
                for (var iy = 0; iy < 5; iy++) {
                    var row = rows[iy];
                    if (gray[row + px0] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row + px1] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row + px2] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row + px3] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    if (gray[row + px4] > threshold) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;
                }
            }
        } else {
            if (!invert) {
                for (var iy = 0; iy < 5; iy++) {
                    var row = rows[iy];
                    var idx = row + px0;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row + px1;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row + px2;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row + px3;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row + px4;
                    if (gray[idx] <= thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;
                }
            } else {
                for (var iy = 0; iy < 5; iy++) {
                    var row = rows[iy];
                    var idx = row + px0;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row + px1;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row + px2;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row + px3;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;

                    idx = row + px4;
                    if (gray[idx] > thresholdMap[idx]) black++;
                    remaining--;
                    if (black >= required) return true;
                    if (black + remaining < required) return false;
                }
            }
        }

        return black >= required;
    }

    public static bool SampleModule9Px(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        // For non-integer module sizes (common on UI-scaled QR) we need to sample a larger fraction of a module.
        // If the delta is too large at downscaled resolutions, samples spill into neighbors and decoding fails.
        var d = GetSampleDelta(moduleSizePx);
        return SampleModule9PxCore(image, sx, sy, invert, d, required: 5);
    }

    internal static bool SampleModule9PxWithDelta(QrGrayImage image, double sx, double sy, bool invert, double d) {
        return SampleModule9PxCore(image, sx, sy, invert, d, required: 5);
    }

    public static bool SampleModule9PxLoose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta(moduleSizePx);
        return SampleModule9PxCore(image, sx, sy, invert, d, required: 3);
    }

    internal static bool SampleModule9PxLooseWithDelta(QrGrayImage image, double sx, double sy, bool invert, double d) {
        return SampleModule9PxCore(image, sx, sy, invert, d, required: 3);
    }

    public static bool SampleModule9Nearest(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta(moduleSizePx);
        return SampleModule9NearestCore(image, sx, sy, invert, d, required: 5);
    }

    internal static bool SampleModule9NearestWithDelta(QrGrayImage image, double sx, double sy, bool invert, double d) {
        return SampleModule9NearestCore(image, sx, sy, invert, d, required: 5);
    }

    public static bool SampleModule9NearestLoose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta(moduleSizePx);
        return SampleModule9NearestCore(image, sx, sy, invert, d, required: 3);
    }

    internal static bool SampleModule9NearestLooseWithDelta(QrGrayImage image, double sx, double sy, bool invert, double d) {
        return SampleModule9NearestCore(image, sx, sy, invert, d, required: 3);
    }

    private static bool SampleModule9NearestCore(QrGrayImage image, double sx, double sy, bool invert, double d, int required) {
        var black = 0;
        var remaining = 9;
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;
        var startY = sy - d;
        var startX = sx - d;
        var endX = startX + d * 2;
        var endY = startY + d * 2;
        var inBounds = startX >= -0.5 && endX < maxX + 0.5 && startY >= -0.5 && endY < maxY + 0.5;
        int px0;
        int px1;
        int px2;
        int py0;
        int py1;
        int py2;
        if (inBounds) {
            px0 = QrMath.RoundToInt(startX);
            px1 = QrMath.RoundToInt(startX + d);
            px2 = QrMath.RoundToInt(endX);
            py0 = QrMath.RoundToInt(startY);
            py1 = QrMath.RoundToInt(startY + d);
            py2 = QrMath.RoundToInt(endY);
        } else {
            px0 = ClampRound(startX, maxX);
            px1 = ClampRound(startX + d, maxX);
            px2 = ClampRound(endX, maxX);
            py0 = ClampRound(startY, maxY);
            py1 = ClampRound(startY + d, maxY);
            py2 = ClampRound(endY, maxY);
        }
        var row0 = py0 * width;
        var row1 = py1 * width;
        var row2 = py2 * width;

        if (thresholdMap is null) {
            if (!invert) {
                if (gray[row0 + px0] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row0 + px1] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row0 + px2] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row1 + px0] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row1 + px1] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row1 + px2] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row2 + px0] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row2 + px1] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row2 + px2] <= threshold) black++;
            } else {
                if (gray[row0 + px0] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row0 + px1] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row0 + px2] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row1 + px0] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row1 + px1] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row1 + px2] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row2 + px0] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row2 + px1] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                if (gray[row2 + px2] > threshold) black++;
            }
        } else {
            if (!invert) {
                var idx = row0 + px0;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row0 + px1;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row0 + px2;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row1 + px0;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row1 + px1;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row1 + px2;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row2 + px0;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row2 + px1;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row2 + px2;
                if (gray[idx] <= thresholdMap[idx]) black++;
            } else {
                var idx = row0 + px0;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row0 + px1;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row0 + px2;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row1 + px0;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row1 + px1;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row1 + px2;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row2 + px0;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row2 + px1;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;

                idx = row2 + px2;
                if (gray[idx] > thresholdMap[idx]) black++;
            }
        }

        return black >= required;
    }


    internal static double GetSampleDeltaForModule(double moduleSizePx) => GetSampleDelta(moduleSizePx);

    internal static double GetSampleDeltaCenterForModule(double moduleSizePx) => GetSampleDeltaCenter(moduleSizePx);

    internal static double GetSampleDelta5x5ForModule(double moduleSizePx) => GetSampleDelta5x5(moduleSizePx);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ClampRound(double value, int max) {
        var v = QrMath.RoundToInt(value);
        if ((uint)v > (uint)max) return v < 0 ? 0 : max;
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte SampleLumaBilinear(QrGrayImage image, double x, double y) {
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;

        int x0;
        int y0;
        int x1;
        int y1;
        double fx;
        double fy;
        if (x >= 0 && x < maxX && y >= 0 && y < maxY) {
            x0 = (int)x;
            y0 = (int)y;
            x1 = x0 + 1;
            y1 = y0 + 1;
            fx = x - x0;
            fy = y - y0;
        } else {
            if (x < 0) x = 0;
            else if (x > maxX) x = maxX;

            if (y < 0) y = 0;
            else if (y > maxY) y = maxY;

            x0 = (int)x;
            y0 = (int)y;
            x1 = x0 + 1;
            y1 = y0 + 1;

            if (x1 > maxX) x1 = maxX;
            if (y1 > maxY) y1 = maxY;

            fx = x - x0;
            fy = y - y0;
        }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte SampleLumaBilinearUnchecked(QrGrayImage image, double x, double y) {
        var width = image.Width;
        var g = image.Gray;

        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var fx = x - x0;
        var fy = y - y0;

        var l00 = g[y0 * width + x0];
        var l10 = g[y0 * width + x1];
        var l01 = g[y1 * width + x0];
        var l11 = g[y1 * width + x1];

        var l0 = l00 + (l10 - l00) * fx;
        var l1 = l01 + (l11 - l01) * fx;
        var lum = l0 + (l1 - l0) * fy;

        var rounded = (int)(lum + 0.5);
        if (rounded < 0) rounded = 0;
        else if (rounded > 255) rounded = 255;
        return (byte)rounded;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SampleLumaThresholdBilinear(QrGrayImage image, double x, double y, out byte lum, out byte threshold) {
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;

        int x0;
        int y0;
        int x1;
        int y1;
        double fx;
        double fy;
        if (x >= 0 && x < maxX && y >= 0 && y < maxY) {
            x0 = (int)x;
            y0 = (int)y;
            x1 = x0 + 1;
            y1 = y0 + 1;
            fx = x - x0;
            fy = y - y0;
        } else {
            if (x < 0) x = 0;
            else if (x > maxX) x = maxX;

            if (y < 0) y = 0;
            else if (y > maxY) y = maxY;

            x0 = (int)x;
            y0 = (int)y;
            x1 = x0 + 1;
            y1 = y0 + 1;

            if (x1 > maxX) x1 = maxX;
            if (y1 > maxY) y1 = maxY;

            fx = x - x0;
            fy = y - y0;
        }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SampleLumaThresholdBilinearUnchecked(QrGrayImage image, double x, double y, out byte lum, out byte threshold) {
        var width = image.Width;
        var g = image.Gray;
        var t = image.ThresholdMap!;

        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var fx = x - x0;
        var fy = y - y0;

        var idx00 = y0 * width + x0;
        var idx10 = y0 * width + x1;
        var idx01 = y1 * width + x0;
        var idx11 = y1 * width + x1;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte SampleThresholdBilinear(QrGrayImage image, double x, double y) {
        if (image.ThresholdMap is null) return image.Threshold;

        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;

        int x0;
        int y0;
        int x1;
        int y1;
        double fx;
        double fy;
        if (x >= 0 && x < maxX && y >= 0 && y < maxY) {
            x0 = (int)x;
            y0 = (int)y;
            x1 = x0 + 1;
            y1 = y0 + 1;
            fx = x - x0;
            fy = y - y0;
        } else {
            if (x < 0) x = 0;
            else if (x > maxX) x = maxX;

            if (y < 0) y = 0;
            else if (y > maxY) y = maxY;

            x0 = (int)x;
            y0 = (int)y;
            x1 = x0 + 1;
            y1 = y0 + 1;

            if (x1 > maxX) x1 = maxX;
            if (y1 > maxY) y1 = maxY;

            fx = x - x0;
            fy = y - y0;
        }

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
