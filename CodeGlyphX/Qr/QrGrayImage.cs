#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal readonly struct QrGrayImage {
    public readonly int Width;
    public readonly int Height;
    public readonly byte[] Gray;
    public readonly byte Min;
    public readonly byte Max;
    public readonly byte Threshold;
    public readonly byte[]? ThresholdMap;

    private QrGrayImage(int width, int height, byte[] gray, byte min, byte max, byte threshold, byte[]? thresholdMap) {
        Width = width;
        Height = height;
        Gray = gray;
        Min = min;
        Max = max;
        Threshold = threshold;
        ThresholdMap = thresholdMap;
    }

    public bool IsBlack(int x, int y, bool invert) {
        var idx = y * Width + x;
        var lum = Gray[idx];
        var t = ThresholdMap is null ? Threshold : ThresholdMap[idx];
        var black = lum <= t;
        return invert ? !black : black;
    }

    public QrGrayImage WithThreshold(byte threshold) => new(Width, Height, Gray, Min, Max, threshold, null);

    public QrGrayImage WithAdaptiveThreshold(int windowSize, int offset) {
        if (windowSize < 3) windowSize = 3;
        if ((windowSize & 1) == 0) windowSize++;
        if (offset < 0) offset = 0;
        if (offset > 255) offset = 255;

        var w = Width;
        var h = Height;
        var stride = w + 1;
        var integral = new int[(w + 1) * (h + 1)];

        for (var y = 1; y <= h; y++) {
            var rowSum = 0;
            var row = (y - 1) * w;
            var baseIdx = y * stride;
            var prevIdx = (y - 1) * stride;
            for (var x = 1; x <= w; x++) {
                rowSum += Gray[row + (x - 1)];
                integral[baseIdx + x] = integral[prevIdx + x] + rowSum;
            }
        }

        var thresholds = new byte[w * h];
        var radius = windowSize / 2;

        for (var y = 0; y < h; y++) {
            var y0 = y - radius;
            var y1 = y + radius;
            if (y0 < 0) y0 = 0;
            if (y1 >= h) y1 = h - 1;

            var y0i = y0 * stride;
            var y1i = (y1 + 1) * stride;
            for (var x = 0; x < w; x++) {
                var x0 = x - radius;
                var x1 = x + radius;
                if (x0 < 0) x0 = 0;
                if (x1 >= w) x1 = w - 1;

                var x0i = x0;
                var x1i = x1 + 1;

                var sum = integral[y1i + x1i] - integral[y0i + x1i] - integral[y1i + x0i] + integral[y0i + x0i];
                var area = (x1 - x0 + 1) * (y1 - y0 + 1);
                var mean = sum / area;
                var t = mean - offset;
                if (t < 0) t = 0;
                else if (t > 255) t = 255;
                thresholds[y * w + x] = (byte)t;
            }
        }

        return new QrGrayImage(w, h, Gray, Min, Max, Threshold, thresholds);
    }

    public static bool TryCreate(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, int scale, out QrGrayImage image) {
        image = default;

        if (width <= 0 || height <= 0) return false;
        if (stride < width * 4) return false;
        if (pixels.Length < (height - 1) * stride + width * 4) return false;
        if (scale is < 1 or > 8) return false;
        if (width < scale || height < scale) return false;

        var outW = width / scale;
        var outH = height / scale;
        if (outW <= 0 || outH <= 0) return false;

        var gray = new byte[outW * outH];
        var histogram = new int[256];

        byte min = 255;
        byte max = 0;

        if (scale == 1) {
            for (var y = 0; y < outH; y++) {
                var row = y * stride;
                for (var x = 0; x < outW; x++) {
                    var p = row + x * 4;

                    byte r, g, b;
                    if (fmt == PixelFormat.Bgra32) {
                        b = pixels[p + 0];
                        g = pixels[p + 1];
                        r = pixels[p + 2];
                    } else {
                        r = pixels[p + 0];
                        g = pixels[p + 1];
                        b = pixels[p + 2];
                    }

                    var lum = (r * 299 + g * 587 + b * 114 + 500) / 1000;
                    var l = (byte)lum;

                    var idx = y * outW + x;
                    gray[idx] = l;
                    histogram[l]++;

                    if (l < min) min = l;
                    if (l > max) max = l;
                }
            }
        } else {
            var blockCount = scale * scale;

            for (var y = 0; y < outH; y++) {
                var baseY = y * scale;
                for (var x = 0; x < outW; x++) {
                    var baseX = x * scale;

                    var sumR = 0;
                    var sumG = 0;
                    var sumB = 0;

                    for (var dy = 0; dy < scale; dy++) {
                        var row = (baseY + dy) * stride;
                        var p = row + baseX * 4;

                        for (var dx = 0; dx < scale; dx++) {
                            byte r, g, b;
                            if (fmt == PixelFormat.Bgra32) {
                                b = pixels[p + 0];
                                g = pixels[p + 1];
                                r = pixels[p + 2];
                            } else {
                                r = pixels[p + 0];
                                g = pixels[p + 1];
                                b = pixels[p + 2];
                            }

                            sumR += r;
                            sumG += g;
                            sumB += b;

                            p += 4;
                        }
                    }

                    var lum = (299 * sumR + 587 * sumG + 114 * sumB + 500 * blockCount) / (1000 * blockCount);
                    var l = (byte)lum;

                    var idx = y * outW + x;
                    gray[idx] = l;
                    histogram[l]++;

                    if (l < min) min = l;
                    if (l > max) max = l;
                }
            }
        }

        if (max - min < 24) return false;

        var threshold = ComputeOtsuThreshold(histogram, gray.Length);
        image = new QrGrayImage(outW, outH, gray, min, max, threshold, null);
        return true;
    }

    private static byte ComputeOtsuThreshold(ReadOnlySpan<int> hist, int total) {
        if (total <= 0) return 128;

        long sum = 0;
        for (var i = 0; i < 256; i++) sum += (long)i * hist[i];

        long sumB = 0;
        var wB = 0;
        var best = 0;
        double bestVar = double.NegativeInfinity;

        for (var t = 0; t < 256; t++) {
            wB += hist[t];
            if (wB == 0) continue;

            var wF = total - wB;
            if (wF == 0) break;

            sumB += (long)t * hist[t];
            var mB = sumB / (double)wB;
            var mF = (sum - sumB) / (double)wF;
            var diff = mB - mF;
            var between = wB * (double)wF * diff * diff;

            if (between > bestVar) {
                bestVar = between;
                best = t;
            }
        }

        return (byte)best;
    }
}
#endif
