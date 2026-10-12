using System;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrMorphologyTests {
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 13, 3)]
    [InlineData(17, 1, 2)]
    [InlineData(19, 17, 1)]
    [InlineData(19, 17, 2)]
    [InlineData(19, 17, 3)]
    [InlineData(5, 7, 12)]
    [InlineData(35, 29, 3)]
    public void BinaryFiltersPreserveClippedNeighborhoods(int width, int height, int radius) {
        var random = new Random(431);
        var pixels = new byte[width * height * 4];
        foreach (var pattern in new[] { 0, 1, 2 }) {
            for (var i = 0; i < width * height; i++) {
                var gray = pattern == 0 ? (byte)0 : pattern == 1 ? (byte)255 : (byte)random.Next(256);
                pixels[i * 4] = pixels[i * 4 + 1] = pixels[i * 4 + 2] = gray;
                pixels[i * 4 + 3] = 255;
            }
            Assert.True(QrGrayImage.TryCreate(pixels, width, height, width * 4, PixelFormat.Rgba32, 1, minContrast: 0, out var source));
            foreach (var image in new[] { source.WithThreshold(128), source.WithHybridThreshold(8, 24, 0) }) {
                var mask = new byte[width * height];
                for (var i = 0; i < mask.Length; i++)
                    mask[i] = image.Gray[i] <= (image.ThresholdMap is null ? image.Threshold : image.ThresholdMap[i]) ? (byte)1 : (byte)0;
                var dilated = ReferenceFilter(mask, width, height, radius, false);
                var eroded = ReferenceFilter(mask, width, height, radius, true);
                var closed = ReferenceFilter(dilated, width, height, radius, true);
                var opened = ReferenceFilter(eroded, width, height, radius, false);
                var pool = new QrGrayImagePool();
                try {
                    AssertPixels(closed, image.WithBinaryClose(radius, pool));
                    AssertPixels(opened, image.WithBinaryOpen(radius, pool));
                    var edge = image.WithBinaryEdge(radius, pool);
                    for (var i = 0; i < mask.Length; i++) Assert.Equal(dilated[i] != eroded[i] ? (byte)0 : (byte)255, edge.Gray[i]);
                } finally {
                    pool.ReturnAll();
                }
            }
        }
    }

    private static void AssertPixels(byte[] expected, QrGrayImage actual) {
        Assert.Equal((byte)127, actual.Threshold);
        Assert.Null(actual.ThresholdMap);
        for (var i = 0; i < expected.Length; i++) Assert.Equal(expected[i] == 1 ? (byte)0 : (byte)255, actual.Gray[i]);
    }

    // Independent definition: dilation accepts any set neighbor, erosion requires all.
    private static byte[] ReferenceFilter(byte[] input, int width, int height, int radius, bool erode) {
        var output = new byte[input.Length];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var on = erode;
                for (var yy = Math.Max(0, y - radius); yy <= Math.Min(height - 1, y + radius); yy++) {
                    for (var xx = Math.Max(0, x - radius); xx <= Math.Min(width - 1, x + radius); xx++) {
                        if (erode) on &= input[yy * width + xx] != 0;
                        else on |= input[yy * width + xx] != 0;
                    }
                }
                output[y * width + x] = on ? (byte)1 : (byte)0;
            }
        }
        return output;
    }
}
