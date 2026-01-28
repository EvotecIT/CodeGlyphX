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
    public void QrEasyOptions_WebpQuality_Clamps(int input, int expected) {
        var options = new QrEasyOptions { WebpQuality = input };
        Assert.Equal(expected, options.WebpQuality);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(25, 25)]
    [InlineData(100, 100)]
    [InlineData(150, 100)]
    public void MatrixOptions_WebpQuality_Clamps(int input, int expected) {
        var options = new MatrixOptions { WebpQuality = input };
        Assert.Equal(expected, options.WebpQuality);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(25, 25)]
    [InlineData(100, 100)]
    [InlineData(150, 100)]
    public void BarcodeOptions_WebpQuality_Clamps(int input, int expected) {
        var options = new BarcodeOptions { WebpQuality = input };
        Assert.Equal(expected, options.WebpQuality);
    }
}
