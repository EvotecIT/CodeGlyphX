using CodeGlyphX.Rendering;
using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpQualityClampTests {
    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(25, 25)]
    [InlineData(100, 100)]
    [InlineData(150, 100)]
    public void OutputQualityClampsToTheSupportedWebpRange(int input, int expected) {
        Assert.Equal(expected, new OutputOptions { WebpQuality = input }.WebpQuality);
    }
}
