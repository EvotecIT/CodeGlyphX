#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using CodeGlyphX.Rendering;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBlack(int x, int y, bool invert) {
        var idx = y * Width + x;
        var lum = Gray[idx];
        var t = ThresholdMap is null ? Threshold : ThresholdMap[idx];
        var black = lum <= t;
        return invert ? !black : black;
    }

    public QrGrayImage WithThreshold(byte threshold) => new(Width, Height, Gray, Min, Max, threshold, null);

    public QrGrayImage WithBinaryBoost(int delta) => WithBinaryBoost(delta, pool: null);

    public QrGrayImage WithBinaryBoost(int delta, QrGrayImagePool? pool) {
        if (delta <= 0) return this;

        var w = Width;
        var h = Height;
        var total = w * h;
        var boosted = RentBuffer(total, pool);
        Span<int> histogram = stackalloc int[256];

        byte min = 255;
        byte max = 0;

        var t = Threshold;
        for (var i = 0; i < total; i++) {
            var v = Gray[i];
            var b = v <= t ? v - delta : v + delta;
            if (b < 0) b = 0;
            else if (b > 255) b = 255;
            var bv = (byte)b;
            boosted[i] = bv;
            histogram[bv]++;
            if (bv < min) min = bv;
            if (bv > max) max = bv;
        }

        var threshold = ComputeOtsuThreshold(histogram, total);
        return new QrGrayImage(w, h, boosted, min, max, threshold, null);
    }

    public QrGrayImage WithContrastStretch(int minRange = 40) => WithContrastStretch(minRange, pool: null);

    public QrGrayImage WithContrastStretch(int minRange, QrGrayImagePool? pool) {
        var range = Max - Min;
        if (range <= 0 || range >= minRange) return this;

        var w = Width;
        var h = Height;
        var total = w * h;
        var stretched = RentBuffer(total, pool);
        Span<int> histogram = stackalloc int[256];

        byte min = 255;
        byte max = 0;

        var scale = 255.0 / range;
        for (var i = 0; i < total; i++) {
            var v = (int)((Gray[i] - Min) * scale + 0.5);
            if (v < 0) v = 0;
            else if (v > 255) v = 255;
            var b = (byte)v;
            stretched[i] = b;
            histogram[b]++;
            if (b < min) min = b;
            if (b > max) max = b;
        }

        var threshold = ComputeOtsuThreshold(histogram, total);
        return new QrGrayImage(w, h, stretched, min, max, threshold, null);
    }

    public QrGrayImage WithBoxBlur(int radius) => WithBoxBlur(radius, pool: null);

    public QrGrayImage WithBoxBlur(int radius, QrGrayImagePool? pool) {
        if (radius <= 0) return this;

        var w = Width;
        var h = Height;
        var total = w * h;
        var stride = w + 1;
        var integral = ArrayPool<int>.Shared.Rent(stride * (h + 1));
        Array.Clear(integral, 0, stride);

        try {
            for (var y = 1; y <= h; y++) {
                var rowSum = 0;
                var row = (y - 1) * w;
                var baseIdx = y * stride;
                var prevIdx = (y - 1) * stride;
                integral[baseIdx] = 0;
                for (var x = 1; x <= w; x++) {
                    rowSum += Gray[row + (x - 1)];
                    integral[baseIdx + x] = integral[prevIdx + x] + rowSum;
                }
            }

            var blurred = RentBuffer(total, pool);
            Span<int> histogram = stackalloc int[256];
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

            var threshold = ComputeOtsuThreshold(histogram, total);
            return new QrGrayImage(w, h, blurred, min, max, threshold, null);
        } finally {
            ArrayPool<int>.Shared.Return(integral);
        }
    }

    public QrGrayImage WithAdaptiveThreshold(int windowSize, int offset) => WithAdaptiveThreshold(windowSize, offset, pool: null);

    public QrGrayImage WithAdaptiveThreshold(int windowSize, int offset, QrGrayImagePool? pool) {
        if (windowSize < 3) windowSize = 3;
        if ((windowSize & 1) == 0) windowSize++;
        if (offset < 0) offset = 0;
        if (offset > 255) offset = 255;

        var w = Width;
        var h = Height;
        var stride = w + 1;
        var integral = ArrayPool<int>.Shared.Rent(stride * (h + 1));
        Array.Clear(integral, 0, stride);

        try {
            for (var y = 1; y <= h; y++) {
                var rowSum = 0;
                var row = (y - 1) * w;
                var baseIdx = y * stride;
                var prevIdx = (y - 1) * stride;
                integral[baseIdx] = 0;
                for (var x = 1; x <= w; x++) {
                    rowSum += Gray[row + (x - 1)];
                    integral[baseIdx + x] = integral[prevIdx + x] + rowSum;
                }
            }

            var total = w * h;
            var thresholds = RentBuffer(total, pool);
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
        } finally {
            ArrayPool<int>.Shared.Return(integral);
        }
    }

    public QrGrayImage Rotate90() => Rotate90(pool: null);

    public QrGrayImage Rotate90(QrGrayImagePool? pool) {
        var w = Width;
        var h = Height;
        var rotW = h;
        var rotH = w;
        var rotated = RentBuffer(rotW * rotH, pool);

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

    public QrGrayImage Rotate180() => Rotate180(pool: null);

    public QrGrayImage Rotate180(QrGrayImagePool? pool) {
        var w = Width;
        var h = Height;
        var rotated = RentBuffer(w * h, pool);

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

    public QrGrayImage Rotate270() => Rotate270(pool: null);

    public QrGrayImage Rotate270(QrGrayImagePool? pool) {
        var w = Width;
        var h = Height;
        var rotW = h;
        var rotH = w;
        var rotated = RentBuffer(rotW * rotH, pool);

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

    public QrGrayImage MirrorX() => MirrorX(pool: null);

    public QrGrayImage MirrorX(QrGrayImagePool? pool) {
        var w = Width;
        var h = Height;
        var mirrored = RentBuffer(w * h, pool);

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
        return TryCreate(pixels, width, height, stride, fmt, scale, minContrast: 24, shouldStop: null, pool: null, out image);
    }

    public static bool TryCreate(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, int scale, int minContrast, out QrGrayImage image) {
        return TryCreate(pixels, width, height, stride, fmt, scale, minContrast, shouldStop: null, pool: null, out image);
    }

    public static bool TryCreate(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, int scale, int minContrast, Func<bool>? shouldStop, out QrGrayImage image) {
        return TryCreate(pixels, width, height, stride, fmt, scale, minContrast, shouldStop, pool: null, out image);
    }

    public static bool TryCreate(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, int scale, int minContrast, Func<bool>? shouldStop, QrGrayImagePool? pool, out QrGrayImage image) {
        image = default;

        if (width <= 0 || height <= 0) return false;
        if (stride < width * 4) return false;
        if (pixels.Length < (height - 1) * stride + width * 4) return false;
        if (scale is < 1 or > 8) return false;
        if (width < scale || height < scale) return false;

        var outW = width / scale;
        var outH = height / scale;
        if (outW <= 0 || outH <= 0) return false;

        var total = outW * outH;
        var gray = RentBuffer(total, pool);
        Span<int> histogram = stackalloc int[256];

        byte min = 255;
        byte max = 0;

        if (scale == 1) {
            if (fmt == PixelFormat.Bgra32) {
                for (var y = 0; y < outH; y++) {
                    if (shouldStop?.Invoke() == true) return false;
                    var p = y * stride;
                    var idx = y * outW;
                    for (var x = 0; x < outW; x++) {
                        var b = pixels[p + 0];
                        var g = pixels[p + 1];
                        var r = pixels[p + 2];
                        var a = pixels[p + 3];
                        p += 4;

                        if (a != 255) {
                            var invA = 255 - a;
                            r = (byte)((r * a + 255 * invA + 127) / 255);
                            g = (byte)((g * a + 255 * invA + 127) / 255);
                            b = (byte)((b * a + 255 * invA + 127) / 255);
                        }

                        var lum = (299 * r + 587 * g + 114 * b + 500) / 1000;
                        var l = (byte)lum;

                        gray[idx++] = l;
                        histogram[l]++;

                        if (l < min) min = l;
                        if (l > max) max = l;
                    }
                }
            } else {
                for (var y = 0; y < outH; y++) {
                    if (shouldStop?.Invoke() == true) return false;
                    var p = y * stride;
                    var idx = y * outW;
                    for (var x = 0; x < outW; x++) {
                        var r = pixels[p + 0];
                        var g = pixels[p + 1];
                        var b = pixels[p + 2];
                        var a = pixels[p + 3];
                        p += 4;

                        if (a != 255) {
                            var invA = 255 - a;
                            r = (byte)((r * a + 255 * invA + 127) / 255);
                            g = (byte)((g * a + 255 * invA + 127) / 255);
                            b = (byte)((b * a + 255 * invA + 127) / 255);
                        }

                        var lum = (299 * r + 587 * g + 114 * b + 500) / 1000;
                        var l = (byte)lum;

                        gray[idx++] = l;
                        histogram[l]++;

                        if (l < min) min = l;
                        if (l > max) max = l;
                    }
                }
            }
        } else {
            var blockCount = scale * scale;

            if (fmt == PixelFormat.Bgra32) {
                for (var y = 0; y < outH; y++) {
                    if (shouldStop?.Invoke() == true) return false;
                    var baseY = y * scale;
                    var baseRow = baseY * stride;
                    var rowIdx = y * outW;
                    for (var x = 0; x < outW; x++) {
                        var baseX = x * scale;
                        var pBase = baseRow + baseX * 4;

                        var sumR = 0;
                        var sumG = 0;
                        var sumB = 0;

                        for (var dy = 0; dy < scale; dy++) {
                            var p = pBase + dy * stride;

                            for (var dx = 0; dx < scale; dx++) {
                                var b = pixels[p + 0];
                                var g = pixels[p + 1];
                                var r = pixels[p + 2];
                                var a = pixels[p + 3];
                                if (a != 255) {
                                    var invA = 255 - a;
                                    b = (byte)((b * a + 255 * invA + 127) / 255);
                                    g = (byte)((g * a + 255 * invA + 127) / 255);
                                    r = (byte)((r * a + 255 * invA + 127) / 255);
                                }
                                sumB += b;
                                sumG += g;
                                sumR += r;
                                p += 4;
                            }
                        }

                        var lum = (299 * sumR + 587 * sumG + 114 * sumB + 500 * blockCount) / (1000 * blockCount);
                        var l = (byte)lum;

                        gray[rowIdx + x] = l;
                        histogram[l]++;

                        if (l < min) min = l;
                        if (l > max) max = l;
                    }
                }
            } else {
                for (var y = 0; y < outH; y++) {
                    if (shouldStop?.Invoke() == true) return false;
                    var baseY = y * scale;
                    var baseRow = baseY * stride;
                    var rowIdx = y * outW;
                    for (var x = 0; x < outW; x++) {
                        var baseX = x * scale;
                        var pBase = baseRow + baseX * 4;

                        var sumR = 0;
                        var sumG = 0;
                        var sumB = 0;

                        for (var dy = 0; dy < scale; dy++) {
                            var p = pBase + dy * stride;

                            for (var dx = 0; dx < scale; dx++) {
                                var r = pixels[p + 0];
                                var g = pixels[p + 1];
                                var b = pixels[p + 2];
                                var a = pixels[p + 3];
                                if (a != 255) {
                                    var invA = 255 - a;
                                    r = (byte)((r * a + 255 * invA + 127) / 255);
                                    g = (byte)((g * a + 255 * invA + 127) / 255);
                                    b = (byte)((b * a + 255 * invA + 127) / 255);
                                }
                                sumR += r;
                                sumG += g;
                                sumB += b;
                                p += 4;
                            }
                        }

                        var lum = (299 * sumR + 587 * sumG + 114 * sumB + 500 * blockCount) / (1000 * blockCount);
                        var l = (byte)lum;

                        gray[rowIdx + x] = l;
                        histogram[l]++;

                        if (l < min) min = l;
                        if (l > max) max = l;
                    }
                }
            }
        }

        if (minContrast > 0 && max - min < minContrast) return false;

        var threshold = ComputeOtsuThreshold(histogram, total);
        image = new QrGrayImage(outW, outH, gray, min, max, threshold, null);
        return true;
    }

    public QrGrayImage WithLocalNormalize(int windowSize) => WithLocalNormalize(windowSize, pool: null);

    public QrGrayImage WithLocalNormalize(int windowSize, QrGrayImagePool? pool) {
        if (windowSize < 3) windowSize = 3;
        if ((windowSize & 1) == 0) windowSize++;

        var w = Width;
        var h = Height;
        var total = w * h;
        var stride = w + 1;
        var integral = ArrayPool<int>.Shared.Rent(stride * (h + 1));
        Array.Clear(integral, 0, stride);

        try {
            for (var y = 1; y <= h; y++) {
                var rowSum = 0;
                var row = (y - 1) * w;
                var baseIdx = y * stride;
                var prevIdx = (y - 1) * stride;
                integral[baseIdx] = 0;
                for (var x = 1; x <= w; x++) {
                    rowSum += Gray[row + (x - 1)];
                    integral[baseIdx + x] = integral[prevIdx + x] + rowSum;
                }
            }

            var normalized = RentBuffer(total, pool);
            Span<int> histogram = stackalloc int[256];
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

            var threshold = ComputeOtsuThreshold(histogram, total);
            return new QrGrayImage(w, h, normalized, min, max, threshold, null);
        } finally {
            ArrayPool<int>.Shared.Return(integral);
        }
    }

    private static byte[] RentBuffer(int size, QrGrayImagePool? pool) {
        return pool is null ? new byte[size] : pool.Rent(size);
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
