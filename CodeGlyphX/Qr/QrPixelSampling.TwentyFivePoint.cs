#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelSampling {
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
}
#endif
