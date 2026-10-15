using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.MaxiCode;

internal static partial class MaxiCodeImageDecoder {
    private readonly struct GrayImage {
        internal readonly int Width, Height;
        private readonly byte[] _gray;
        private readonly int _threshold;
        internal GrayImage(int width, int height, byte[] gray, int threshold) {
            Width = width; Height = height; _gray = gray; _threshold = threshold;
        }
        internal bool Dark(int x, int y) => _gray[y * Width + x] <= _threshold;
    }

    // The unified scanner supplies validated packed RGBA with stride, row order and channel order normalized.
    private static bool TryCreateGray(byte[] rgba, int width, int height, CancellationToken token,
        DecodeBudgetState budget, out GrayImage image) {
        image = default;
        if (width < 30 || height < 30 || Aborted(token, budget)) return false;
        var gray = new byte[checked(width * height)];
        var histogram = new int[256];
        for (var y = 0; y < height; y++) {
            if (Aborted(token, budget)) return false;
            for (var x = 0; x < width; x++) {
                if ((x & 1023) == 0 && Aborted(token, budget)) return false;
                var at = (y * width + x) * 4;
                var luminance = (rgba[at] * 77 + rgba[at + 1] * 150 + rgba[at + 2] * 29) >> 8;
                var alpha = rgba[at + 3];
                var value = (byte)((luminance * alpha + 255 * (255 - alpha) + 127) / 255);
                gray[y * width + x] = value;
                histogram[value]++;
            }
        }
        long sum = 0;
        for (var i = 0; i < 256; i++) sum += (long)i * histogram[i];
        var weight = 0;
        long partial = 0;
        var best = -1.0;
        var threshold = 0;
        for (var i = 0; i < 255; i++) {
            weight += histogram[i];
            partial += (long)i * histogram[i];
            if (weight == 0 || weight == gray.Length) continue;
            var difference = (double)partial / weight - (double)(sum - partial) / (gray.Length - weight);
            var variance = (double)weight * (gray.Length - weight) * difference * difference;
            if (variance > best) { best = variance; threshold = i; }
        }
        if (best < 0 || Aborted(token, budget)) return false;
        image = new GrayImage(width, height, gray, threshold);
        return true;
    }
}
