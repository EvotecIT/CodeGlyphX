#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelSampling {
    public static bool SampleModule9Px(QrGrayImage image, double sx, double sy, bool invert) {
        return SampleModule9Px(image, sx, sy, invert, moduleSizePx: 0);
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
}
#endif
