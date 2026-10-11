#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelSampling {
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

    internal static double GetSampleDeltaForModule(double moduleSizePx) => GetSampleDelta(moduleSizePx);

    internal static double GetSampleDeltaCenterForModule(double moduleSizePx) => GetSampleDeltaCenter(moduleSizePx);

    internal static double GetSampleDelta5x5ForModule(double moduleSizePx) => GetSampleDelta5x5(moduleSizePx);

    internal static double GetSampleDeltaRingForModule(double moduleSizePx) => GetSampleDeltaRing(moduleSizePx);

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

    private static double GetSampleDeltaRing(double moduleSizePx) {
        if (!(moduleSizePx > 0)) return 0.9;

        // Sample closer to the module edge to handle hollow/stylized shapes.
        var d = moduleSizePx * 0.42;
        if (d < 0.35) d = 0.35;

        var max = moduleSizePx * 0.49;
        if (d > max) d = max;

        return d;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ClampRound(double value, int max) {
        var v = QrMath.RoundToInt(value);
        if ((uint)v > (uint)max) return v < 0 ? 0 : max;
        return v;
    }
}
#endif
