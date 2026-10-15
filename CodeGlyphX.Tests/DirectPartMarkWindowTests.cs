using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DirectPartMarkWindowTests {
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 9, 1)]
    [InlineData(9, 1, 128)]
    [InlineData(9, 7, 0)]
    [InlineData(9, 7, 1)]
    [InlineData(9, 7, 128)]
    [InlineData(257, 3, 128)]
    [InlineData(3, 257, 128)]
    public void AdaptiveRetries_UseExactClippedWindowMeans(int width, int height, int requestedRadius) {
        var gray = new byte[width * height];
        var rgba = new byte[gray.Length * 4];
        for (var i = 0; i < gray.Length; i++) {
            // A deterministic nonuniform field exercises floor division and
            // strict threshold boundaries without relying on barcode decoding.
            gray[i] = (byte)((i * 73 + i / width * 19 + 31) % 256);
            rgba[i * 4] = rgba[i * 4 + 1] = rgba[i * 4 + 2] = gray[i];
            rgba[i * 4 + 3] = 255;
        }
        const int bias = 4;
        var options = new DirectPartMarkOptions { AdaptiveWindowRadius = requestedRadius, ThresholdBias = bias, MaxAttempts = 3 };
        var retries = DirectPartMarkPreprocessor.CreateVariants(rgba, width, height, options, CancellationToken.None).ToArray();
        Assert.Equal(3, retries.Length);
        var radius = requestedRadius > 0 ? requestedRadius : Math.Max(2, Math.Min(24, Math.Min(width, height) / 24));
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            // Independent reference: enumerate every pixel in the clipped
            // neighborhood rather than reusing rolling or integral sums.
            long sum = 0;
            var count = 0;
            for (var row = Math.Max(0, y - radius); row <= Math.Min(height - 1, y + radius); row++)
                for (var column = Math.Max(0, x - radius); column <= Math.Min(width - 1, x + radius); column++) {
                    sum += gray[row * width + column];
                    count++;
                }
            var mean = sum / count;
            var at = y * width + x;
            Assert.Equal(gray[at] < mean - bias ? (byte)0 : (byte)255, retries[1][at]);
            Assert.Equal(gray[at] > mean + bias ? (byte)0 : (byte)255, retries[2][at]);
        }
    }

    [Fact]
    public void MaximumRadius_PreservesUniformPixelsWhenTheCompleteImageSumExceedsInt32() {
        const int width = 2902, height = 2902;
        Assert.True((long)width * height * 255 > int.MaxValue);
        var rgba = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        var options = new DirectPartMarkOptions { AdaptiveWindowRadius = 128, MaxAttempts = 3 };
        var retries = DirectPartMarkPreprocessor.CreateVariants(rgba, width, height, options, CancellationToken.None).ToArray();
        Assert.Equal(3, retries.Length);
        // A uniform image has neither polarity of mark, including at clipped
        // edges. Local averages remain correct at this full-resolution boundary.
        for (var attempt = 1; attempt <= 2; attempt++) {
            Assert.Equal(width * height, retries[attempt].Length);
            Assert.DoesNotContain(retries[attempt], pixel => pixel != 255);
        }
    }
}
