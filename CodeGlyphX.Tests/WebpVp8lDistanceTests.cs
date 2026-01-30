using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpVp8lDistanceTests {
    [Fact]
    public void Vp8l_DistanceMapping_UsesNeighborhoodTable() {
        Assert.True(WebpVp8lDecoder.TryMapDistanceCode(distanceCode: 1, width: 10, out var d1));
        Assert.Equal(10, d1);

        Assert.True(WebpVp8lDecoder.TryMapDistanceCode(distanceCode: 2, width: 10, out var d2));
        Assert.Equal(1, d2);
    }

    [Fact]
    public void Vp8l_DistanceMapping_AboveTable_IsLinear() {
        Assert.True(WebpVp8lDecoder.TryMapDistanceCode(distanceCode: 121, width: 25, out var d));
        Assert.Equal(1, d);
    }

    [Fact]
    public void Vp8l_Lz77Copy_HandlesOverlap() {
        var buffer = new int[4];
        buffer[0] = 7;

        var next = WebpVp8lDecoder.CopyLz77(buffer, pos: 1, distance: 1, length: 3, pixelCount: buffer.Length);
        Assert.Equal(4, next);
        Assert.Equal(new[] { 7, 7, 7, 7 }, buffer);
    }
}

