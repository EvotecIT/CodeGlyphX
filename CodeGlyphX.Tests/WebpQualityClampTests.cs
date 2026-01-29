using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpQualityClampTests {
    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(25, 25)]
    [InlineData(100, 100)]
    [InlineData(150, 100)]
    public void WebpQuality_Clamps_For_All_Options(int input, int expected) {
        Assert.Equal(expected, new QrEasyOptions { WebpQuality = input }.WebpQuality);
        Assert.Equal(expected, new MatrixOptions { WebpQuality = input }.WebpQuality);
        Assert.Equal(expected, new BarcodeOptions { WebpQuality = input }.WebpQuality);
    }
}
