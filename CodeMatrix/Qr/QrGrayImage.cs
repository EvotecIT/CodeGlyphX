#if NET8_0_OR_GREATER
using System;

namespace CodeMatrix.Qr;

internal readonly struct QrGrayImage {
    public readonly int Width;
    public readonly int Height;
    public readonly byte[] Gray;
    public readonly byte Threshold;

    private QrGrayImage(int width, int height, byte[] gray, byte threshold) {
        Width = width;
        Height = height;
        Gray = gray;
        Threshold = threshold;
    }

    public bool IsBlack(int x, int y, bool invert) {
        var lum = Gray[y * Width + x];
        var black = lum < Threshold;
        return invert ? !black : black;
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

        byte min = 255;
        byte max = 0;

        var sampleOffset = scale / 2;
        for (var y = 0; y < outH; y++) {
            var py = y * scale + sampleOffset;
            if (py >= height) py = height - 1;
            var row = py * stride;

            for (var x = 0; x < outW; x++) {
                var px = x * scale + sampleOffset;
                if (px >= width) px = width - 1;
                var p = row + px * 4;

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

                if (l < min) min = l;
                if (l > max) max = l;
            }
        }

        if (max - min < 24) return false;

        var threshold = (byte)((min + max) / 2);
        image = new QrGrayImage(outW, outH, gray, threshold);
        return true;
    }
}
#endif

