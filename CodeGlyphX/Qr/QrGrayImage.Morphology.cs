#if NET8_0_OR_GREATER
using System;
using System.Threading;
using CodeGlyphX.Internal;

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

    /// <summary>Uses the shared square filter with the QR decoder's clipped edges and existing unbudgeted transform scope.</summary>
    private static void FilterBinarySquare(ReadOnlySpan<byte> src, Span<byte> dst,
        int width, int height, int radius, bool erode) {
        BinaryMaskMorphology.FilterSquare(src, dst, width, height, radius, erode, zeroOutside: false,
            CancellationToken.None, default);
    }
}
#endif
