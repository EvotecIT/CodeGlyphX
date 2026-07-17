#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = System.Byte[];
#endif

using System;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class MicroQrPixelDecoder {
    private readonly struct GrayImage {
        private GrayImage(int width, int height, byte[] gray, byte min, byte max, byte otsuThreshold) {
            Width = width;
            Height = height;
            Gray = gray;
            Min = min;
            Max = max;
            OtsuThreshold = otsuThreshold;
        }

        internal int Width { get; }
        internal int Height { get; }
        internal byte[] Gray { get; }
        internal byte Min { get; }
        internal byte Max { get; }
        internal byte OtsuThreshold { get; }

        internal bool IsForeground(int x, int y, byte threshold, bool inverted) {
            var dark = Gray[y * Width + x] <= threshold;
            return inverted ? !dark : dark;
        }

        internal static bool TryCreate(
            PixelSpan pixels,
            int width,
            int height,
            int stride,
            PixelFormat format,
            CancellationToken cancellationToken,
            out GrayImage image) {
            image = default;
            if (format != PixelFormat.Rgba32 && format != PixelFormat.Bgra32) return false;
            if (width <= 0 || height <= 0 || stride < width * 4) return false;
            var required = (long)(height - 1) * stride + (long)width * 4;
            if (required > pixels.Length || (long)width * height > int.MaxValue) return false;

            var gray = new byte[width * height];
            var histogram = new int[256];
            byte min = 255;
            byte max = 0;
            for (var y = 0; y < height; y++) {
                if ((y & 31) == 0 && cancellationToken.IsCancellationRequested) return false;
                var sourceRow = y * stride;
                var targetRow = y * width;
                for (var x = 0; x < width; x++) {
                    var offset = sourceRow + x * 4;
                    byte red;
                    byte green;
                    byte blue;
                    if (format == PixelFormat.Bgra32) {
                        blue = pixels[offset];
                        green = pixels[offset + 1];
                        red = pixels[offset + 2];
                    } else {
                        red = pixels[offset];
                        green = pixels[offset + 1];
                        blue = pixels[offset + 2];
                    }
                    var sourceLuminance = (red * 77 + green * 150 + blue * 29) >> 8;
                    var alpha = pixels[offset + 3];
                    var luminance = (byte)((sourceLuminance * alpha + 255 * (255 - alpha) + 127) / 255);
                    gray[targetRow + x] = luminance;
                    histogram[luminance]++;
                    if (luminance < min) min = luminance;
                    if (luminance > max) max = luminance;
                }
            }
            if (min == max) return false;
            image = new GrayImage(width, height, gray, min, max, ComputeOtsuThreshold(histogram, gray.Length));
            return true;
        }

        private static byte ComputeOtsuThreshold(int[] histogram, int total) {
            long sum = 0;
            for (var i = 0; i < histogram.Length; i++) sum += (long)i * histogram[i];
            long backgroundSum = 0;
            var backgroundWeight = 0;
            var bestThreshold = 0;
            var bestVariance = -1.0;
            for (var threshold = 0; threshold < histogram.Length; threshold++) {
                backgroundWeight += histogram[threshold];
                if (backgroundWeight == 0) continue;
                var foregroundWeight = total - backgroundWeight;
                if (foregroundWeight == 0) break;
                backgroundSum += (long)threshold * histogram[threshold];
                var backgroundMean = backgroundSum / (double)backgroundWeight;
                var foregroundMean = (sum - backgroundSum) / (double)foregroundWeight;
                var difference = backgroundMean - foregroundMean;
                var variance = backgroundWeight * (double)foregroundWeight * difference * difference;
                if (variance <= bestVariance) continue;
                bestVariance = variance;
                bestThreshold = threshold;
            }
            return (byte)bestThreshold;
        }
    }
}
