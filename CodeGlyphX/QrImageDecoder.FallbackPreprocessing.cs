using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class QrImageDecoder {
    private static void ApplyMaxDimension(ref byte[] rgba, ref int width, ref int height, ref int stride, QrPixelDecodeOptions? options, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var maxDim = options?.MaxDimension ?? 0;
        if (maxDim <= 0) return;

        var currentMax = width > height ? width : height;
        if (currentMax <= maxDim) return;

        var scale = maxDim / (double)currentMax;
        var dstWidth = Math.Max(1, (int)Math.Round(width * scale));
        var dstHeight = Math.Max(1, (int)Math.Round(height * scale));
        var background = new CodeGlyphX.Rendering.Png.Rgba32(255, 255, 255, 255);
        rgba = ImageScaler.ResizeToFitNearest(rgba, width, height, stride, dstWidth, dstHeight, background, preserveAspectRatio: true, cancellationToken);
        width = dstWidth;
        height = dstHeight;
        stride = width * 4;
    }

    private static byte[] ConvertBgraToRgba(byte[] pixels, int width, int height, int stride, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var rgba = new byte[checked(width * height * 4)];
        var dst = 0;
        for (var y = 0; y < height; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var i = row + (x * 4);
                rgba[dst + 0] = pixels[i + 2];
                rgba[dst + 1] = pixels[i + 1];
                rgba[dst + 2] = pixels[i + 0];
                rgba[dst + 3] = pixels[i + 3];
                dst += 4;
            }
        }
        return rgba;
    }

    private static byte[] BuildGrayscale(byte[] rgba, int width, int height, int stride, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var gray = new byte[checked(width * height)];
        var dst = 0;
        for (var y = 0; y < height; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var i = row + (x * 4);
                ReadCompositedRgba(rgba, i, out var r, out var g, out var b);
                var lum = (299 * r) + (587 * g) + (114 * b);
                gray[dst++] = (byte)(lum / 1000);
            }
        }
        return gray;
    }

    private static byte[] BuildChannelGrayscale(byte[] rgba, int width, int height, int stride, int variant, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var gray = new byte[checked(width * height)];
        var dst = 0;
        for (var y = 0; y < height; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var i = row + (x * 4);
                ReadCompositedRgba(rgba, i, out var r, out var g, out var b);
                gray[dst++] = variant switch {
                    0 => r,
                    1 => g,
                    2 => b,
                    _ => (byte)(Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)))
                };
            }
        }
        return gray;
    }

    private static void ReadCompositedRgba(byte[] rgba, int index, out byte r, out byte g, out byte b) {
        r = rgba[index + 0];
        g = rgba[index + 1];
        b = rgba[index + 2];
        var a = rgba[index + 3];
        if (a == 255) return;

        var invA = 255 - a;
        r = (byte)((r * a + 255 * invA + 127) / 255);
        g = (byte)((g * a + 255 * invA + 127) / 255);
        b = (byte)((b * a + 255 * invA + 127) / 255);
    }

    private static byte[] InvertGrayscale(byte[] gray, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var inverted = new byte[gray.Length];
        for (var i = 0; i < gray.Length; i++) {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            inverted[i] = (byte)(255 - gray[i]);
        }
        return inverted;
    }

    private static void ComputeGrayStats(byte[] gray, out byte min, out byte max, out int mean, out byte otsuThreshold, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var histogram = new int[256];
        long sum = 0;
        min = 255;
        max = 0;

        for (var i = 0; i < gray.Length; i++) {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            var value = gray[i];
            histogram[value]++;
            sum += value;
            if (value < min) min = value;
            if (value > max) max = value;
        }

        mean = (int)(sum / Math.Max(1, gray.Length));
        otsuThreshold = ComputeOtsuThreshold(histogram, gray.Length);
    }

    private static byte[] BuildThresholds(int mean, byte min, byte max, byte otsuThreshold) {
        var thresholds = new List<byte>(10);
        var range = max - min;
        AddThreshold(thresholds, otsuThreshold);
        AddThreshold(thresholds, (min + max) / 2);
        AddThreshold(thresholds, mean);
        AddThreshold(thresholds, mean - 12);
        AddThreshold(thresholds, mean - 4);
        AddThreshold(thresholds, mean + 4);
        if (range > 0) {
            AddThreshold(thresholds, min + (range / 3));
            AddThreshold(thresholds, min + ((range * 2) / 3));
        }
        AddThreshold(thresholds, min + 12);
        AddThreshold(thresholds, max - 12);
        if (thresholds.Count == 0) thresholds.Add((byte)ClampByte(mean));
        return thresholds.ToArray();
    }

    private static void AddThreshold(List<byte> thresholds, int value) {
        var clamped = (byte)ClampByte(value);
        for (var i = 0; i < thresholds.Count; i++) {
            if (thresholds[i] == clamped) return;
        }
        thresholds.Add(clamped);
    }

    private static byte ComputeOtsuThreshold(int[] histogram, int total) {
        long sum = 0;
        for (var i = 0; i < 256; i++) {
            sum += i * (long)histogram[i];
        }

        long sumB = 0;
        var weightBackground = 0;
        var bestThreshold = 0;
        var bestVariance = -1.0;

        for (var threshold = 0; threshold < 256; threshold++) {
            weightBackground += histogram[threshold];
            if (weightBackground == 0) continue;

            var weightForeground = total - weightBackground;
            if (weightForeground == 0) break;

            sumB += threshold * (long)histogram[threshold];
            var meanBackground = sumB / (double)weightBackground;
            var meanForeground = (sum - sumB) / (double)weightForeground;
            var diff = meanBackground - meanForeground;
            var betweenClassVariance = weightBackground * (double)weightForeground * diff * diff;
            if (betweenClassVariance > bestVariance) {
                bestVariance = betweenClassVariance;
                bestThreshold = threshold;
            }
        }

        return (byte)bestThreshold;
    }

    private static bool TryContrastStretch(byte[] gray, out byte[] stretched, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        stretched = Array.Empty<byte>();
        if (gray.Length == 0) return false;

        byte min = 255;
        byte max = 0;
        for (var i = 0; i < gray.Length; i++) {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            var value = gray[i];
            if (value < min) min = value;
            if (value > max) max = value;
        }

        var range = max - min;
        if (range <= 0 || range >= 224) return false;

        stretched = new byte[gray.Length];
        var scale = 255.0 / range;
        for (var i = 0; i < gray.Length; i++) {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            var value = (int)((gray[i] - min) * scale + 0.5);
            stretched[i] = (byte)ClampByte(value);
        }
        return true;
    }

    private static bool TryLocalNormalize(byte[] gray, int width, int height, out byte[] normalized, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        normalized = Array.Empty<byte>();
        if (gray.Length == 0 || width <= 0 || height <= 0) return false;

        var minDim = width < height ? width : height;
        if (minDim < 21) return false;

        var windowSize = minDim >= 720 ? 31 : minDim >= 360 ? 21 : 15;
        if ((windowSize & 1) == 0) windowSize++;
        var radius = windowSize / 2;
        var stride = width + 1;
        var integral = new int[stride * (height + 1)];

        for (var y = 1; y <= height; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            var rowSum = 0;
            var srcRow = (y - 1) * width;
            var baseIndex = y * stride;
            var prevIndex = (y - 1) * stride;
            for (var x = 1; x <= width; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                rowSum += gray[srcRow + (x - 1)];
                integral[baseIndex + x] = integral[prevIndex + x] + rowSum;
            }
        }

        normalized = new byte[gray.Length];
        for (var y = 0; y < height; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            var y0 = y - radius;
            var y1 = y + radius;
            if (y0 < 0) y0 = 0;
            if (y1 >= height) y1 = height - 1;

            var y0i = y0 * stride;
            var y1i = (y1 + 1) * stride;

            for (var x = 0; x < width; x++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var x0 = x - radius;
                var x1 = x + radius;
                if (x0 < 0) x0 = 0;
                if (x1 >= width) x1 = width - 1;

                var x1i = x1 + 1;
                var area = (x1 - x0 + 1) * (y1 - y0 + 1);
                var sum = integral[y1i + x1i] - integral[y0i + x1i] - integral[y1i + x0] + integral[y0i + x0];
                var mean = sum / area;
                var index = y * width + x;
                normalized[index] = (byte)ClampByte(gray[index] - mean + 128);
            }
        }

        return true;
    }

    private static bool TryFindDarkBounds(byte[] gray, int width, int height, byte threshold, out int minX, out int minY, out int maxX, out int maxY, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        minX = width;
        minY = height;
        maxX = -1;
        maxY = -1;

        var idx = 0;
        for (var y = 0; y < height; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < width; x++, idx++) {
                if ((x & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                if (gray[idx] >= threshold) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        return maxX >= minX && maxY >= minY;
    }

}
