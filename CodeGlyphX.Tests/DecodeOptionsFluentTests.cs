using System.Threading;
using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DecodeOptionsFluentTests {
    [Fact]
    public void QrPixelDecodeOptions_Fluent_Configures_Options() {
        var options = new QrPixelDecodeOptions()
            .WithProfile(QrDecodeProfile.Balanced)
            .WithBudget(200, 420)
            .WithMaxScale(2)
            .WithoutTransforms()
            .WithAggressiveSampling();

        Assert.Equal(QrDecodeProfile.Balanced, options.Profile);
        Assert.Equal(200, options.BudgetMilliseconds);
        Assert.Equal(420, options.MaxDimension);
        Assert.Equal(2, options.MaxScale);
        Assert.True(options.DisableTransforms);
        Assert.True(options.AggressiveSampling);
    }

    [Fact]
    public void ImageDecodeOptions_Fluent_Configures_Options() {
        var options = new ImageDecodeOptions()
            .WithRecognitionBudget(150, 320)
            .WithMaxPixels(5000)
            .WithMaxBytes(8192);

        Assert.Equal(150, options.RecognitionBudgetMilliseconds);
        Assert.Equal(320, options.MaxDimension);
        Assert.Equal(5000, options.MaxPixels);
        Assert.Equal(8192, options.MaxBytes);
    }
}
