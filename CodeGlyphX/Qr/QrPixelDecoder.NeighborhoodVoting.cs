#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SampleMajority3x3(QrGrayImage image, int px, int py, bool invert) {
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;

        var x0 = px - 1;
        if (x0 < 0) x0 = 0;
        else if (x0 >= width) x0 = width - 1;
        var x1 = px;
        if (x1 < 0) x1 = 0;
        else if (x1 >= width) x1 = width - 1;
        var x2 = px + 1;
        if (x2 < 0) x2 = 0;
        else if (x2 >= width) x2 = width - 1;

        var y0 = py - 1;
        if (y0 < 0) y0 = 0;
        else if (y0 >= height) y0 = height - 1;
        var y1 = py;
        if (y1 < 0) y1 = 0;
        else if (y1 >= height) y1 = height - 1;
        var y2 = py + 1;
        if (y2 < 0) y2 = 0;
        else if (y2 >= height) y2 = height - 1;

        var row0 = y0 * width;
        var row1 = y1 * width;
        var row2 = y2 * width;
        var black = 0;
        var remaining = 9;
        const int required = 5;
        if (thresholdMap is null) {
            if (!invert) {
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
            } else {
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
            }
        } else {
            if (!invert) {
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
            } else {
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
            }
        }

        return black >= required;
    }

}
#endif
