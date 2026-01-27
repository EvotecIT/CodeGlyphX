using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpColorCacheTests {
    [Fact]
    public void Vp8l_ColorCacheIndex_KnownPixel_IsStable() {
        var pixel = PackArgb(a: 255, r: 1, g: 2, b: 3);
        var index = WebpVp8lDecoder.ComputeColorCacheIndex(pixel, cacheBits: 5);
        Assert.Equal(22, index);
    }

    [Fact]
    public void Vp8l_ColorCache_IsUpdatedDuringCopy() {
        var pixel = PackArgb(a: 255, r: 1, g: 2, b: 3);
        var buffer = new[] { pixel, 0 };
        var cache = new int[2];

        var next = WebpVp8lDecoder.CopyLz77(buffer, pos: 1, distance: 1, length: 1, pixelCount: buffer.Length, cache, colorCacheBits: 1);
        Assert.Equal(2, next);
        Assert.Equal(pixel, cache[1]);
    }

    private static int PackArgb(int a, int r, int g, int b) {
        return ((a & 0xFF) << 24)
            | ((r & 0xFF) << 16)
            | ((g & 0xFF) << 8)
            | (b & 0xFF);
    }
}

