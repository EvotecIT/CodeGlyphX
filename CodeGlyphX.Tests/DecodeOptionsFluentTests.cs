using System.Threading;
using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DecodeOptionsFluentTests {
    [Fact]
    public void CodeGlyphDecodeOptions_Fluent_Configures_Options() {
        using var cts = new CancellationTokenSource();
        var options = new CodeGlyphDecodeOptions()
            .WithExpectedBarcode(BarcodeType.Code128)
            .PreferBarcodeFirst()
            .IncludeBarcodeResults(false)
            .WithCancellation(cts.Token)
            .WithQrBudget(250, 640)
            .WithQrProfile(QrDecodeProfile.Fast)
            .WithAggressiveQrSampling()
            .WithImageBudget(300, 500)
            .WithImageMaxPixels(1234)
            .WithImageMaxBytes(2048)
            .WithBarcode(b => b.WithCode39Checksum(Code39ChecksumPolicy.StripIfValid));

        Assert.Equal(BarcodeType.Code128, options.ExpectedBarcode);
        Assert.True(options.PreferBarcode);
        Assert.False(options.IncludeBarcode);
        Assert.Equal(cts.Token, options.CancellationToken);
        Assert.NotNull(options.Qr);
        Assert.NotNull(options.Image);
        Assert.NotNull(options.Barcode);
        Assert.Equal(QrDecodeProfile.Fast, options.Qr!.Profile);
        Assert.Equal(250, options.Qr.MaxMilliseconds);
        Assert.Equal(640, options.Qr.MaxDimension);
        Assert.True(options.Qr.AggressiveSampling);
        Assert.Equal(300, options.Image!.MaxMilliseconds);
        Assert.Equal(500, options.Image.MaxDimension);
        Assert.Equal(1234, options.Image.MaxPixels);
        Assert.Equal(2048, options.Image.MaxBytes);
        Assert.Equal(Code39ChecksumPolicy.StripIfValid, options.Barcode!.Code39Checksum);
    }

    [Fact]
    public void QrPixelDecodeOptions_Fluent_Configures_Options() {
        var options = new QrPixelDecodeOptions()
            .WithProfile(QrDecodeProfile.Balanced)
            .WithBudget(200, 420)
            .WithMaxScale(2)
            .WithoutTransforms()
            .WithAggressiveSampling();

        Assert.Equal(QrDecodeProfile.Balanced, options.Profile);
        Assert.Equal(200, options.MaxMilliseconds);
        Assert.Equal(420, options.MaxDimension);
        Assert.Equal(2, options.MaxScale);
        Assert.True(options.DisableTransforms);
        Assert.True(options.AggressiveSampling);
    }

    [Fact]
    public void ImageDecodeOptions_Fluent_Configures_Options() {
        var options = new ImageDecodeOptions()
            .WithBudget(150, 320)
            .WithMaxPixels(5000)
            .WithMaxBytes(8192);

        Assert.Equal(150, options.MaxMilliseconds);
        Assert.Equal(320, options.MaxDimension);
        Assert.Equal(5000, options.MaxPixels);
        Assert.Equal(8192, options.MaxBytes);
    }
}
