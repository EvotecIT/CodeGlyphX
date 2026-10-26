#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static partial class QrFinderPatternDetector {
    private static bool CrossCheckVertical(QrGrayImage image, bool invert, int centerX, int startY, int maxCount, int originalTotal, bool aggressive, out double centerY, out double moduleSize) {
        centerY = 0;
        moduleSize = 0;

        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholds = image.ThresholdMap;
        var threshold = image.Threshold;
        var s0 = 0;
        var s1 = 0;
        var s2 = 0;
        var s3 = 0;
        var s4 = 0;

        var y = startY;
        var idx = (y * width) + centerX;
        if (thresholds is null) {
            if (!invert) {
                while (y >= 0 && gray[idx] <= threshold) {
                    s2++;
                    y--;
                    idx -= width;
                }
                if (y < 0) return false;

                while (y >= 0 && gray[idx] > threshold && s1 <= maxCount) {
                    s1++;
                    y--;
                    idx -= width;
                }
                if (y < 0 || s1 > maxCount) return false;

                while (y >= 0 && gray[idx] <= threshold && s0 <= maxCount) {
                    s0++;
                    y--;
                    idx -= width;
                }
                if (s0 > maxCount) return false;

                y = startY + 1;
                idx = (y * width) + centerX;
                while (y < height && gray[idx] <= threshold) {
                    s2++;
                    y++;
                    idx += width;
                }
                if (y == height) return false;

                while (y < height && gray[idx] > threshold && s3 < maxCount) {
                    s3++;
                    y++;
                    idx += width;
                }
                if (y == height || s3 >= maxCount) return false;

                while (y < height && gray[idx] <= threshold && s4 < maxCount) {
                    s4++;
                    y++;
                    idx += width;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (y >= 0 && gray[idx] > threshold) {
                    s2++;
                    y--;
                    idx -= width;
                }
                if (y < 0) return false;

                while (y >= 0 && gray[idx] <= threshold && s1 <= maxCount) {
                    s1++;
                    y--;
                    idx -= width;
                }
                if (y < 0 || s1 > maxCount) return false;

                while (y >= 0 && gray[idx] > threshold && s0 <= maxCount) {
                    s0++;
                    y--;
                    idx -= width;
                }
                if (s0 > maxCount) return false;

                y = startY + 1;
                idx = (y * width) + centerX;
                while (y < height && gray[idx] > threshold) {
                    s2++;
                    y++;
                    idx += width;
                }
                if (y == height) return false;

                while (y < height && gray[idx] <= threshold && s3 < maxCount) {
                    s3++;
                    y++;
                    idx += width;
                }
                if (y == height || s3 >= maxCount) return false;

                while (y < height && gray[idx] > threshold && s4 < maxCount) {
                    s4++;
                    y++;
                    idx += width;
                }
                if (s4 >= maxCount) return false;
            }
        } else {
            if (!invert) {
                while (y >= 0 && gray[idx] <= thresholds[idx]) {
                    s2++;
                    y--;
                    idx -= width;
                }
                if (y < 0) return false;

                while (y >= 0 && gray[idx] > thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    y--;
                    idx -= width;
                }
                if (y < 0 || s1 > maxCount) return false;

                while (y >= 0 && gray[idx] <= thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    y--;
                    idx -= width;
                }
                if (s0 > maxCount) return false;

                y = startY + 1;
                idx = (y * width) + centerX;
                while (y < height && gray[idx] <= thresholds[idx]) {
                    s2++;
                    y++;
                    idx += width;
                }
                if (y == height) return false;

                while (y < height && gray[idx] > thresholds[idx] && s3 < maxCount) {
                    s3++;
                    y++;
                    idx += width;
                }
                if (y == height || s3 >= maxCount) return false;

                while (y < height && gray[idx] <= thresholds[idx] && s4 < maxCount) {
                    s4++;
                    y++;
                    idx += width;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (y >= 0 && gray[idx] > thresholds[idx]) {
                    s2++;
                    y--;
                    idx -= width;
                }
                if (y < 0) return false;

                while (y >= 0 && gray[idx] <= thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    y--;
                    idx -= width;
                }
                if (y < 0 || s1 > maxCount) return false;

                while (y >= 0 && gray[idx] > thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    y--;
                    idx -= width;
                }
                if (s0 > maxCount) return false;

                y = startY + 1;
                idx = (y * width) + centerX;
                while (y < height && gray[idx] > thresholds[idx]) {
                    s2++;
                    y++;
                    idx += width;
                }
                if (y == height) return false;

                while (y < height && gray[idx] <= thresholds[idx] && s3 < maxCount) {
                    s3++;
                    y++;
                    idx += width;
                }
                if (y == height || s3 >= maxCount) return false;

                while (y < height && gray[idx] > thresholds[idx] && s4 < maxCount) {
                    s4++;
                    y++;
                    idx += width;
                }
                if (s4 >= maxCount) return false;
            }
        }

        var total = s0 + s1 + s2 + s3 + s4;
        if (Math.Abs(total - originalTotal) * 5 >= originalTotal * 2) return false;
        if (!FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) return false;

        centerY = CenterFromEnd(s0, s1, s2, s3, s4, y);
        moduleSize = total / 7.0;
        return true;
    }

    private static bool CrossCheckHorizontal(QrGrayImage image, bool invert, int startX, int centerY, int maxCount, int originalTotal, bool aggressive, out double centerX, out double moduleSize) {
        centerX = 0;
        moduleSize = 0;

        var width = image.Width;
        var gray = image.Gray;
        var thresholds = image.ThresholdMap;
        var threshold = image.Threshold;
        var s0 = 0;
        var s1 = 0;
        var s2 = 0;
        var s3 = 0;
        var s4 = 0;

        var x = startX;
        var rowOffset = centerY * width;
        var idx = rowOffset + x;
        if (thresholds is null) {
            if (!invert) {
                while (x >= 0 && gray[idx] <= threshold) {
                    s2++;
                    x--;
                    idx--;
                }
                if (x < 0) return false;

                while (x >= 0 && gray[idx] > threshold && s1 <= maxCount) {
                    s1++;
                    x--;
                    idx--;
                }
                if (x < 0 || s1 > maxCount) return false;

                while (x >= 0 && gray[idx] <= threshold && s0 <= maxCount) {
                    s0++;
                    x--;
                    idx--;
                }
                if (s0 > maxCount) return false;

                x = startX + 1;
                idx = rowOffset + x;
                while (x < width && gray[idx] <= threshold) {
                    s2++;
                    x++;
                    idx++;
                }
                if (x == width) return false;

                while (x < width && gray[idx] > threshold && s3 < maxCount) {
                    s3++;
                    x++;
                    idx++;
                }
                if (x == width || s3 >= maxCount) return false;

                while (x < width && gray[idx] <= threshold && s4 < maxCount) {
                    s4++;
                    x++;
                    idx++;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (x >= 0 && gray[idx] > threshold) {
                    s2++;
                    x--;
                    idx--;
                }
                if (x < 0) return false;

                while (x >= 0 && gray[idx] <= threshold && s1 <= maxCount) {
                    s1++;
                    x--;
                    idx--;
                }
                if (x < 0 || s1 > maxCount) return false;

                while (x >= 0 && gray[idx] > threshold && s0 <= maxCount) {
                    s0++;
                    x--;
                    idx--;
                }
                if (s0 > maxCount) return false;

                x = startX + 1;
                idx = rowOffset + x;
                while (x < width && gray[idx] > threshold) {
                    s2++;
                    x++;
                    idx++;
                }
                if (x == width) return false;

                while (x < width && gray[idx] <= threshold && s3 < maxCount) {
                    s3++;
                    x++;
                    idx++;
                }
                if (x == width || s3 >= maxCount) return false;

                while (x < width && gray[idx] > threshold && s4 < maxCount) {
                    s4++;
                    x++;
                    idx++;
                }
                if (s4 >= maxCount) return false;
            }
        } else {
            if (!invert) {
                while (x >= 0 && gray[idx] <= thresholds[idx]) {
                    s2++;
                    x--;
                    idx--;
                }
                if (x < 0) return false;

                while (x >= 0 && gray[idx] > thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    x--;
                    idx--;
                }
                if (x < 0 || s1 > maxCount) return false;

                while (x >= 0 && gray[idx] <= thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    x--;
                    idx--;
                }
                if (s0 > maxCount) return false;

                x = startX + 1;
                idx = rowOffset + x;
                while (x < width && gray[idx] <= thresholds[idx]) {
                    s2++;
                    x++;
                    idx++;
                }
                if (x == width) return false;

                while (x < width && gray[idx] > thresholds[idx] && s3 < maxCount) {
                    s3++;
                    x++;
                    idx++;
                }
                if (x == width || s3 >= maxCount) return false;

                while (x < width && gray[idx] <= thresholds[idx] && s4 < maxCount) {
                    s4++;
                    x++;
                    idx++;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (x >= 0 && gray[idx] > thresholds[idx]) {
                    s2++;
                    x--;
                    idx--;
                }
                if (x < 0) return false;

                while (x >= 0 && gray[idx] <= thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    x--;
                    idx--;
                }
                if (x < 0 || s1 > maxCount) return false;

                while (x >= 0 && gray[idx] > thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    x--;
                    idx--;
                }
                if (s0 > maxCount) return false;

                x = startX + 1;
                idx = rowOffset + x;
                while (x < width && gray[idx] > thresholds[idx]) {
                    s2++;
                    x++;
                    idx++;
                }
                if (x == width) return false;

                while (x < width && gray[idx] <= thresholds[idx] && s3 < maxCount) {
                    s3++;
                    x++;
                    idx++;
                }
                if (x == width || s3 >= maxCount) return false;

                while (x < width && gray[idx] > thresholds[idx] && s4 < maxCount) {
                    s4++;
                    x++;
                    idx++;
                }
                if (s4 >= maxCount) return false;
            }
        }

        var total = s0 + s1 + s2 + s3 + s4;
        if (Math.Abs(total - originalTotal) * 5 >= originalTotal * 2) return false;
        if (!FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) return false;

        centerX = CenterFromEnd(s0, s1, s2, s3, s4, x);
        moduleSize = total / 7.0;
        return true;
    }

    private static bool CrossCheckDiagonal(QrGrayImage image, bool invert, int centerX, int centerY, int maxCount, int originalTotal, bool aggressive) {
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholds = image.ThresholdMap;
        var threshold = image.Threshold;

        var s0 = 0;
        var s1 = 0;
        var s2 = 0;
        var s3 = 0;
        var s4 = 0;

        var x = centerX;
        var y = centerY;
        var idx = (y * width) + x;
        if (thresholds is null) {
            if (!invert) {
                while (x >= 0 && y >= 0 && gray[idx] <= threshold) {
                    s2++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0) return false;

                while (x >= 0 && y >= 0 && gray[idx] > threshold && s1 <= maxCount) {
                    s1++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0 || s1 > maxCount) return false;

                while (x >= 0 && y >= 0 && gray[idx] <= threshold && s0 <= maxCount) {
                    s0++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (s0 > maxCount) return false;

                x = centerX + 1;
                y = centerY + 1;
                idx = (y * width) + x;
                while (x < width && y < height && gray[idx] <= threshold) {
                    s2++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height) return false;

                while (x < width && y < height && gray[idx] > threshold && s3 < maxCount) {
                    s3++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height || s3 >= maxCount) return false;

                while (x < width && y < height && gray[idx] <= threshold && s4 < maxCount) {
                    s4++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (x >= 0 && y >= 0 && gray[idx] > threshold) {
                    s2++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0) return false;

                while (x >= 0 && y >= 0 && gray[idx] <= threshold && s1 <= maxCount) {
                    s1++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0 || s1 > maxCount) return false;

                while (x >= 0 && y >= 0 && gray[idx] > threshold && s0 <= maxCount) {
                    s0++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (s0 > maxCount) return false;

                x = centerX + 1;
                y = centerY + 1;
                idx = (y * width) + x;
                while (x < width && y < height && gray[idx] > threshold) {
                    s2++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height) return false;

                while (x < width && y < height && gray[idx] <= threshold && s3 < maxCount) {
                    s3++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height || s3 >= maxCount) return false;

                while (x < width && y < height && gray[idx] > threshold && s4 < maxCount) {
                    s4++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (s4 >= maxCount) return false;
            }
        } else {
            if (!invert) {
                while (x >= 0 && y >= 0 && gray[idx] <= thresholds[idx]) {
                    s2++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0) return false;

                while (x >= 0 && y >= 0 && gray[idx] > thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0 || s1 > maxCount) return false;

                while (x >= 0 && y >= 0 && gray[idx] <= thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (s0 > maxCount) return false;

                x = centerX + 1;
                y = centerY + 1;
                idx = (y * width) + x;
                while (x < width && y < height && gray[idx] <= thresholds[idx]) {
                    s2++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height) return false;

                while (x < width && y < height && gray[idx] > thresholds[idx] && s3 < maxCount) {
                    s3++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height || s3 >= maxCount) return false;

                while (x < width && y < height && gray[idx] <= thresholds[idx] && s4 < maxCount) {
                    s4++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (s4 >= maxCount) return false;
            } else {
                while (x >= 0 && y >= 0 && gray[idx] > thresholds[idx]) {
                    s2++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0) return false;

                while (x >= 0 && y >= 0 && gray[idx] <= thresholds[idx] && s1 <= maxCount) {
                    s1++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (x < 0 || y < 0 || s1 > maxCount) return false;

                while (x >= 0 && y >= 0 && gray[idx] > thresholds[idx] && s0 <= maxCount) {
                    s0++;
                    x--;
                    y--;
                    idx -= width + 1;
                }
                if (s0 > maxCount) return false;

                x = centerX + 1;
                y = centerY + 1;
                idx = (y * width) + x;
                while (x < width && y < height && gray[idx] > thresholds[idx]) {
                    s2++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height) return false;

                while (x < width && y < height && gray[idx] <= thresholds[idx] && s3 < maxCount) {
                    s3++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (x >= width || y >= height || s3 >= maxCount) return false;

                while (x < width && y < height && gray[idx] > thresholds[idx] && s4 < maxCount) {
                    s4++;
                    x++;
                    y++;
                    idx += width + 1;
                }
                if (s4 >= maxCount) return false;
            }
        }

        var total = s0 + s1 + s2 + s3 + s4;
        // At a 45-degree rotation, each diagonal pixel step advances along
        // both finder axes, so this run count can be half the horizontal count.
        // Retain the upper mismatch bound and the five-run ratio check below.
        if ((long)total * 2 < originalTotal || (long)total * 5 >= (long)originalTotal * 7) return false;
        if (!FoundPatternCross(s0, s1, s2, s3, s4, aggressive)
            && !CircularBallDiagonal(s0, s1, s2, s3, s4, originalTotal)
            && !SquareBallDiagonal(s0, s1, s2, s3, s4, originalTotal)) return false;

        return true;
    }

}
#endif
