using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]
public sealed class WebpLargeRoundTripTests {
    [Theory]
    [InlineData(4, 4, "checker")]
    [InlineData(31, 33, "gradient")]
    [InlineData(32, 32, "checker")]
    [InlineData(257, 129, "checker")]
    [InlineData(257, 129, "gradient")]
    [InlineData(840, 420, "solid")]
    [InlineData(840, 420, "checker")]
    [InlineData(840, 420, "gradient")]
    [InlineData(1, 421, "gradient")]
    [InlineData(841, 1, "checker")]
    public void WriteRgba32_RoundTripsAcrossSizesAndPatterns(int width, int height, string pattern) {
        var rgba = CreatePixels(width, height, pattern, withAlpha: false);

        var webp = WebpWriter.WriteRgba32(width, height, rgba, width * 4);

        AssertDecodedPixels(webp, width, height, rgba);
    }

    [Theory]
    [InlineData(257, 129)]
    [InlineData(840, 420)]
    public void WriteRgba32_RoundTripsWhenCodebooksRequireLiteralFallback(int width, int height) {
        // All 256 green values plus cache/back-reference symbols exceed the
        // encoder's 256-symbol codebook strategy for this deterministic input.
        var rgba = CreatePixels(width, height, "noise", withAlpha: false);

        var webp = WebpWriter.WriteRgba32(width, height, rgba, width * 4);

        AssertDecodedPixels(webp, width, height, rgba);
    }

    [Theory]
    [InlineData(4, 4, "checker", 3)]
    [InlineData(257, 129, "gradient", 13)]
    [InlineData(840, 420, "checker", 7)]
    [InlineData(840, 420, "gradient", 19)]
    [InlineData(257, 129, "noise", 11)]
    public void WriteRgba32_RoundTripsPaddedStrideAndAlpha(int width, int height, string pattern, int padding) {
        var expected = CreatePixels(width, height, pattern, withAlpha: true);
        var rowBytes = width * 4;
        var stride = rowBytes + padding;
        // The public API permits omitting padding after the final row.
        var padded = new byte[(height - 1) * stride + rowBytes];
        for (var i = 0; i < padded.Length; i++) padded[i] = 0xCD;
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(expected, y * rowBytes, padded, y * stride, rowBytes);
        }

        var webp = WebpWriter.WriteRgba32(width, height, padded.AsSpan(), stride);

        AssertDecodedPixels(webp, width, height, expected);
    }

    private static byte[] CreatePixels(int width, int height, string pattern, bool withAlpha) {
        var rgba = new byte[width * height * 4];
        var random = new Random(12345);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var offset = (y * width + x) * 4;
                var value = pattern switch {
                    "solid" => 255,
                    "checker" => ((x / 7 + y / 7) & 1) * 255,
                    "gradient" => (x + y) & 255,
                    "noise" => random.Next(256),
                    _ => throw new ArgumentOutOfRangeException(nameof(pattern))
                };
                rgba[offset] = (byte)value;
                rgba[offset + 1] = (byte)(pattern == "gradient" ? (x * 3 + y * 7) & 255 : value);
                rgba[offset + 2] = (byte)value;
                rgba[offset + 3] = withAlpha
                    ? (byte)(pattern == "checker" ? (value == 0 ? 0 : 255) : (x * 13 + y * 17) & 255)
                    : (byte)255;
            }
        }
        return rgba;
    }

    private static void AssertDecodedPixels(byte[] webp, int width, int height, byte[] expected) {
        Assert.True(ImageReader.TryDecodeRgba32(webp, out var actual, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        // This includes RGB values beneath fully transparent pixels.
        Assert.Equal(expected, actual);
    }
}
