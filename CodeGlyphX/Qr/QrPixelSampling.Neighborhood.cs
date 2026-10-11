#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelSampling {
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
}
#endif
