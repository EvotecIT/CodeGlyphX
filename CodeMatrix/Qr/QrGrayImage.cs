#if NET8_0_OR_GREATER
using System;

namespace CodeMatrix.Qr;

internal readonly struct QrGrayImage {
    public readonly int Width;
    public readonly int Height;
    public readonly byte[] Gray;
    public readonly byte Min;
    public readonly byte Max;
    public readonly byte Threshold;

    private QrGrayImage(int width, int height, byte[] gray, byte min, byte max, byte threshold) {
        Width = width;
        Height = height;
        Gray = gray;
        Min = min;
        Max = max;
        Threshold = threshold;
    }

    public bool IsBlack(int x, int y, bool invert) {
        var lum = Gray[y * Width + x];
        var black = lum <= Threshold;
        return invert ? !black : black;
    }

    public QrGrayImage WithThreshold(byte threshold) => new(Width, Height, Gray, Min, Max, threshold);

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
        image = new QrGrayImage(outW, outH, gray, min, max, threshold);
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
