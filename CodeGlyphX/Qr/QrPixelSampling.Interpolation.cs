#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelSampling {
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
