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

                    if (bestDist == 0 && bestTiming > (dimension - 16) * 2 - 4 && bestScalePenalty == 0) return;
                }
            }
        }
    }

    public static bool SampleModule9(QrGrayImage image, double sx, double sy, double oxX, double oxY, double oyX, double oyY, bool invert) {
        var black = 0;

        if (IsBlackBilinear(image, sx, sy, invert)) black++;

        // Axes
        if (IsBlackBilinear(image, sx + oxX, sy + oxY, invert)) black++;
        if (IsBlackBilinear(image, sx - oxX, sy - oxY, invert)) black++;
        if (IsBlackBilinear(image, sx + oyX, sy + oyY, invert)) black++;
        if (IsBlackBilinear(image, sx - oyX, sy - oyY, invert)) black++;

        // Diagonals
        if (IsBlackBilinear(image, sx + oxX + oyX, sy + oxY + oyY, invert)) black++;
        if (IsBlackBilinear(image, sx + oxX - oyX, sy + oxY - oyY, invert)) black++;
        if (IsBlackBilinear(image, sx - oxX + oyX, sy - oxY + oyY, invert)) black++;
        if (IsBlackBilinear(image, sx - oxX - oyX, sy - oxY - oyY, invert)) black++;

        return black >= 5;
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
        for (var i = 0; i <= 5; i++) if (SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, 8, i)) bitsA |= 1 << i;
        if (SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, 8, 7)) bitsA |= 1 << 6;
        if (SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, 8, 8)) bitsA |= 1 << 7;
        if (SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, 7, 8)) bitsA |= 1 << 8;
        for (var i = 9; i < 15; i++) if (SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, 14 - i, 8)) bitsA |= 1 << i;

        var bitsB = 0;
        for (var i = 0; i < 8; i++) if (SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, size - 1 - i, 8)) bitsB |= 1 << i;
        for (var i = 8; i < 15; i++) if (SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, 8, size - 15 + i)) bitsB |= 1 << i;

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
            var prev = SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, start, 6);
            for (var x = start + 1; x <= end; x++) {
                var cur = SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, x, 6);
                if (cur != prev) max++;
                prev = cur;
            }
        }

        // Vertical timing pattern at x=6, y=8..size-9
        if (end > start) {
            var prev = SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, 6, start);
            for (var y = start + 1; y <= end; y++) {
                var cur = SampleAt(image, invert, tlX, tlY, vxX, vxY, vyX, vyY, phaseX, phaseY, 6, y);
                if (cur != prev) max++;
                prev = cur;
            }
        }

        return max;
    }

    private static bool SampleAt(
        QrGrayImage image,
        bool invert,
        double tlX,
        double tlY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        double phaseX,
        double phaseY,
        int mx,
        int my) {
        var dx = mx - 3 + phaseX;
        var dy = my - 3 + phaseY;

        var sx = tlX + vxX * dx + vyX * dy;
        var sy = tlY + vxY * dx + vyY * dy;

        return IsBlackBilinear(image, sx, sy, invert);
    }

    public static bool IsBlackBilinear(QrGrayImage image, double x, double y, bool invert) {
        var lum = SampleLumaBilinear(image, x, y);
        var threshold = image.ThresholdMap is null ? image.Threshold : SampleThresholdBilinear(image, x, y);
        var black = lum <= threshold;
        return invert ? !black : black;
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

    public static bool SampleModule25Px(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta5x5(moduleSizePx);

        var black = 0;
        for (var iy = -2; iy <= 2; iy++) {
            for (var ix = -2; ix <= 2; ix++) {
                if (IsBlackBilinear(image, sx + ix * d, sy + iy * d, invert)) black++;
            }
        }

        return black >= 13;
    }

    public static bool SampleModule25Nearest(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta5x5(moduleSizePx);

        var black = 0;
        for (var iy = -2; iy <= 2; iy++) {
            for (var ix = -2; ix <= 2; ix++) {
                if (IsBlackNearest(image, sx + ix * d, sy + iy * d, invert)) black++;
            }
        }

        return black >= 13;
    }

    public static bool SampleModule25NearestLoose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta5x5(moduleSizePx);

        var black = 0;
        for (var iy = -2; iy <= 2; iy++) {
            for (var ix = -2; ix <= 2; ix++) {
                if (IsBlackNearest(image, sx + ix * d, sy + iy * d, invert)) black++;
            }
        }

        return black >= 7;
    }

    public static bool SampleModule9Px(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        // For non-integer module sizes (common on UI-scaled QR) we need to sample a larger fraction of a module.
        // If the delta is too large at downscaled resolutions, samples spill into neighbors and decoding fails.
        var d = GetSampleDelta(moduleSizePx);

        var black = 0;
        for (var iy = -1; iy <= 1; iy++) {
            for (var ix = -1; ix <= 1; ix++) {
                var x = sx + ix * d;
                var y = sy + iy * d;
                if (IsBlackBilinear(image, x, y, invert)) black++;
            }
        }
        return black >= 5;
    }

    public static bool SampleModule9PxLoose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta(moduleSizePx);

        var black = 0;
        for (var iy = -1; iy <= 1; iy++) {
            for (var ix = -1; ix <= 1; ix++) {
                var x = sx + ix * d;
                var y = sy + iy * d;
                if (IsBlackBilinear(image, x, y, invert)) black++;
            }
        }
        return black >= 3;
    }

    public static bool SampleModule9Nearest(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta(moduleSizePx);

        var black = 0;
        for (var iy = -1; iy <= 1; iy++) {
            for (var ix = -1; ix <= 1; ix++) {
                var x = sx + ix * d;
                var y = sy + iy * d;
                if (IsBlackNearest(image, x, y, invert)) black++;
            }
        }
        return black >= 5;
    }

    public static bool SampleModule9NearestLoose(QrGrayImage image, double sx, double sy, bool invert, double moduleSizePx) {
        var d = GetSampleDelta(moduleSizePx);

        var black = 0;
        for (var iy = -1; iy <= 1; iy++) {
            for (var ix = -1; ix <= 1; ix++) {
                var x = sx + ix * d;
                var y = sy + iy * d;
                if (IsBlackNearest(image, x, y, invert)) black++;
            }
        }
        return black >= 3;
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
        if (x < 0) x = 0;
        else if (x > image.Width - 1) x = image.Width - 1;

        if (y < 0) y = 0;
        else if (y > image.Height - 1) y = image.Height - 1;

        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        if (x1 >= image.Width) x1 = image.Width - 1;
        if (y1 >= image.Height) y1 = image.Height - 1;

        var fx = x - x0;
        var fy = y - y0;

        var w = image.Width;
        var g = image.Gray;

        var l00 = g[y0 * w + x0];
        var l10 = g[y0 * w + x1];
        var l01 = g[y1 * w + x0];
        var l11 = g[y1 * w + x1];

        var l0 = l00 + (l10 - l00) * fx;
        var l1 = l01 + (l11 - l01) * fx;
        var lum = l0 + (l1 - l0) * fy;

        return (byte)Math.Clamp((int)Math.Round(lum), 0, 255);
    }

    private static byte SampleThresholdBilinear(QrGrayImage image, double x, double y) {
        if (image.ThresholdMap is null) return image.Threshold;

        if (x < 0) x = 0;
        else if (x > image.Width - 1) x = image.Width - 1;

        if (y < 0) y = 0;
        else if (y > image.Height - 1) y = image.Height - 1;

        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        if (x1 >= image.Width) x1 = image.Width - 1;
        if (y1 >= image.Height) y1 = image.Height - 1;

        var fx = x - x0;
        var fy = y - y0;

        var w = image.Width;
        var t = image.ThresholdMap;
        var t00 = t[y0 * w + x0];
        var t10 = t[y0 * w + x1];
        var t01 = t[y1 * w + x0];
        var t11 = t[y1 * w + x1];

        var t0 = t00 + (t10 - t00) * fx;
        var t1 = t01 + (t11 - t01) * fx;
        var thr = t0 + (t1 - t0) * fy;

        return (byte)Math.Clamp((int)Math.Round(thr), 0, 255);
    }
}
#endif
