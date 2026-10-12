#if NET8_0_OR_GREATER
using System;
using System.Buffers;

namespace CodeGlyphX.Qr;

internal readonly partial struct QrGrayImage {
    public QrGrayImage WithBinaryClose(int radius) => WithBinaryClose(radius, pool: null);

    public QrGrayImage WithBinaryClose(int radius, QrGrayImagePool? pool) {
        if (radius <= 0) return this;

        var w = Width;
        var h = Height;
        var total = w * h;
        var mask = RentBuffer(total, pool);
        var temp = RentBuffer(total, pool);
        var gray = RentBuffer(total, pool);

        var thresholds = ThresholdMap;
        var threshold = Threshold;
        for (var i = 0; i < total; i++) {
            var t = thresholds is null ? threshold : thresholds[i];
            mask[i] = Gray[i] <= t ? (byte)1 : (byte)0;
        }

        Dilate(mask, temp, w, h, radius);
        Erode(temp, mask, w, h, radius);

        for (var i = 0; i < total; i++) {
            gray[i] = mask[i] == 1 ? (byte)0 : (byte)255;
        }

        return new QrGrayImage(w, h, gray, min: 0, max: 255, threshold: 127, thresholdMap: null);
    }

    public QrGrayImage WithBinaryOpen(int radius) => WithBinaryOpen(radius, pool: null);

    public QrGrayImage WithBinaryOpen(int radius, QrGrayImagePool? pool) {
        if (radius <= 0) return this;

        var w = Width;
        var h = Height;
        var total = w * h;
        var mask = RentBuffer(total, pool);
        var temp = RentBuffer(total, pool);
        var gray = RentBuffer(total, pool);

        var thresholds = ThresholdMap;
        var threshold = Threshold;
        for (var i = 0; i < total; i++) {
            var t = thresholds is null ? threshold : thresholds[i];
            mask[i] = Gray[i] <= t ? (byte)1 : (byte)0;
        }

        Erode(mask, temp, w, h, radius);
        Dilate(temp, mask, w, h, radius);

        for (var i = 0; i < total; i++) {
            gray[i] = mask[i] == 1 ? (byte)0 : (byte)255;
        }

        return new QrGrayImage(w, h, gray, min: 0, max: 255, threshold: 127, thresholdMap: null);
    }

    public QrGrayImage WithBinaryEdge(int radius) => WithBinaryEdge(radius, pool: null);

    public QrGrayImage WithBinaryEdge(int radius, QrGrayImagePool? pool) {
        if (radius <= 0) return this;

        var w = Width;
        var h = Height;
        var total = w * h;
        var mask = RentBuffer(total, pool);
        var dilated = RentBuffer(total, pool);
        var eroded = RentBuffer(total, pool);
        var gray = RentBuffer(total, pool);

        var thresholds = ThresholdMap;
        var threshold = Threshold;
        for (var i = 0; i < total; i++) {
            var t = thresholds is null ? threshold : thresholds[i];
            mask[i] = Gray[i] <= t ? (byte)1 : (byte)0;
        }

        Dilate(mask, dilated, w, h, radius);
        Erode(mask, eroded, w, h, radius);

        for (var i = 0; i < total; i++) {
            gray[i] = dilated[i] != eroded[i] ? (byte)0 : (byte)255;
        }

        return new QrGrayImage(w, h, gray, min: 0, max: 255, threshold: 127, thresholdMap: null);
    }

    private static void Dilate(ReadOnlySpan<byte> src, Span<byte> dst, int width, int height, int radius) =>
        FilterBinarySquare(src, dst, width, height, radius, erode: false);

    private static void Erode(ReadOnlySpan<byte> src, Span<byte> dst, int width, int height, int radius) =>
        FilterBinarySquare(src, dst, width, height, radius, erode: true);

    /// <summary>Counts set pixels in a moving, clipped square without rescanning its interior.</summary>
    private static void FilterBinarySquare(ReadOnlySpan<byte> src, Span<byte> dst,
        int width, int height, int radius, bool erode) {
        // Each column holds the number of set pixels in the current vertical window.
        // Rent only one row of integers; the input and output buffers remain distinct.
        var columns = ArrayPool<int>.Shared.Rent(width);
        try {
            Array.Clear(columns, 0, width);
            var verticalRadius = Math.Min(radius, height - 1);
            var horizontalRadius = Math.Min(radius, width - 1);
            for (var y = 0; y <= verticalRadius; y++) {
                var row = y * width;
                for (var x = 0; x < width; x++) columns[x] += src[row + x] != 0 ? 1 : 0;
            }

            for (var y = 0; y < height; y++) {
                if (y > 0) {
                    var leaving = y - verticalRadius - 1;
                    if (leaving >= 0) {
                        var row = leaving * width;
                        for (var x = 0; x < width; x++) columns[x] -= src[row + x] != 0 ? 1 : 0;
                    }
                    if (verticalRadius < height - y) {
                        var row = (y + verticalRadius) * width;
                        for (var x = 0; x < width; x++) columns[x] += src[row + x] != 0 ? 1 : 0;
                    }
                }

                var windowHeight = Math.Min(verticalRadius, y) + Math.Min(verticalRadius, height - y - 1) + 1;
                var count = 0;
                for (var x = 0; x <= horizontalRadius; x++) count += columns[x];
                var outputRow = y * width;
                for (var x = 0; x < width; x++) {
                    var windowWidth = Math.Min(horizontalRadius, x) + Math.Min(horizontalRadius, width - x - 1) + 1;
                    dst[outputRow + x] = (erode ? count == windowWidth * windowHeight : count != 0) ? (byte)1 : (byte)0;
                    var leaving = x - horizontalRadius;
                    if (leaving >= 0) count -= columns[leaving];
                    if (horizontalRadius < width - x - 1) count += columns[x + horizontalRadius + 1];
                }
            }
        } finally {
            ArrayPool<int>.Shared.Return(columns);
        }
    }
}
#endif
