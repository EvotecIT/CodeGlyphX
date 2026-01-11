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

    public QrGrayImage WithContrastStretch(int minRange = 40) {
        var range = Max - Min;
        if (range <= 0 || range >= minRange) return this;

        var w = Width;
        var h = Height;
        var stretched = new byte[w * h];
        var histogram = new int[256];

        byte min = 255;
        byte max = 0;

        var scale = 255.0 / range;
        for (var i = 0; i < Gray.Length; i++) {
            var v = (int)((Gray[i] - Min) * scale + 0.5);
            if (v < 0) v = 0;
            else if (v > 255) v = 255;
            var b = (byte)v;
            stretched[i] = b;
            histogram[b]++;
            if (b < min) min = b;
            if (b > max) max = b;
        }

        var threshold = ComputeOtsuThreshold(histogram, stretched.Length);
        return new QrGrayImage(w, h, stretched, min, max, threshold, null);
    }

    public QrGrayImage WithBoxBlur(int radius) {
        if (radius <= 0) return this;

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

        var blurred = new byte[w * h];
        var histogram = new int[256];
        byte min = 255;
        byte max = 0;

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
                var l = (byte)mean;

                var idx = y * w + x;
                blurred[idx] = l;
                histogram[l]++;

                if (l < min) min = l;
                if (l > max) max = l;
            }
        }

        var threshold = ComputeOtsuThreshold(histogram, blurred.Length);
        return new QrGrayImage(w, h, blurred, min, max, threshold, null);
    }

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

    public QrGrayImage Rotate90() {
        var w = Width;
        var h = Height;
        var rotW = h;
        var rotH = w;
        var rotated = new byte[rotW * rotH];

        for (var y = 0; y < h; y++) {
            var row = y * w;
            for (var x = 0; x < w; x++) {
                var nx = h - 1 - y;
                var ny = x;
                rotated[ny * rotW + nx] = Gray[row + x];
            }
        }

        return new QrGrayImage(rotW, rotH, rotated, Min, Max, Threshold, null);
    }

    public QrGrayImage Rotate180() {
        var w = Width;
        var h = Height;
        var rotated = new byte[w * h];

        for (var y = 0; y < h; y++) {
            var row = y * w;
            var ny = h - 1 - y;
            var nrow = ny * w;
            for (var x = 0; x < w; x++) {
                var nx = w - 1 - x;
                rotated[nrow + nx] = Gray[row + x];
            }
        }

        return new QrGrayImage(w, h, rotated, Min, Max, Threshold, null);
    }

    public QrGrayImage Rotate270() {
        var w = Width;
        var h = Height;
        var rotW = h;
        var rotH = w;
        var rotated = new byte[rotW * rotH];

        for (var y = 0; y < h; y++) {
            var row = y * w;
            for (var x = 0; x < w; x++) {
                var nx = y;
                var ny = w - 1 - x;
                rotated[ny * rotW + nx] = Gray[row + x];
            }
        }

        return new QrGrayImage(rotW, rotH, rotated, Min, Max, Threshold, null);
    }

    public QrGrayImage MirrorX() {
        var w = Width;
        var h = Height;
        var mirrored = new byte[w * h];

        for (var y = 0; y < h; y++) {
            var row = y * w;
            for (var x = 0; x < w; x++) {
                var nx = w - 1 - x;
                mirrored[row + nx] = Gray[row + x];
            }
        }

        return new QrGrayImage(w, h, mirrored, Min, Max, Threshold, null);
    }

    public static bool TryCreate(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, int scale, out QrGrayImage image) {
        return TryCreate(pixels, width, height, stride, fmt, scale, minContrast: 24, out image);
    }

    public static bool TryCreate(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, int scale, int minContrast, out QrGrayImage image) {
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

        if (minContrast > 0 && max - min < minContrast) return false;

        var threshold = ComputeOtsuThreshold(histogram, gray.Length);
        image = new QrGrayImage(outW, outH, gray, min, max, threshold, null);
        return true;
    }

    public QrGrayImage WithLocalNormalize(int windowSize) {
        if (windowSize < 3) windowSize = 3;
        if ((windowSize & 1) == 0) windowSize++;

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

        var normalized = new byte[w * h];
        var histogram = new int[256];
        byte min = 255;
        byte max = 0;
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
                var idx = y * w + x;
                var value = Gray[idx] - mean + 128;
                if (value < 0) value = 0;
                else if (value > 255) value = 255;
                var v = (byte)value;
                normalized[idx] = v;
                histogram[v]++;
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        var threshold = ComputeOtsuThreshold(histogram, normalized.Length);
        return new QrGrayImage(w, h, normalized, min, max, threshold, null);
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
