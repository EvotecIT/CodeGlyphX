using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;
using ZXing;
using ZXing.Common;

namespace CodeGlyphX.Tests;

public sealed class QrLogoPlacementTests {
    [Theory]
    [InlineData(7)]
    [InlineData(10)]
    public void Auto_KeepsTheLogoVisible_AndReportsItsActualPatternFreePlacement(int version) {
        var qr = QR.Encode("https://example.com/brand", Encoding(version));
        var result = QrArt.RenderLogo(qr, Logo());
        Assert.Equal(LogoPlacement.Auto, result.Budget.Placement);
        Assert.False(result.Budget.CoversAlignment);
        Assert.Empty(result.Warnings);
        Assert.Contains("CodeGlyphX", result.VerifiedDecoders);
        Assert.True(result.Budget.LogoSideModules >= 3);
        Assert.True(result.Budget.BlocksTouched > 0);
        Assert.All(result.Budget.MarginPerBlock, margin => Assert.True(margin >= result.Budget.ReservedErrorsPerBlock));
        var pixels = ImageReader.DecodeRgba32(result.GetPng(), out var width, out _);
        var x = (4 + result.Budget.XModules + result.Budget.PaddingModules) * 16 + 8;
        var y = (4 + result.Budget.YModules + result.Budget.PaddingModules) * 16 + 8;
        var p = (y * width + x) * 4;
        Assert.Equal(20, pixels[p]);
        Assert.Equal(60, pixels[p + 1]);
        Assert.Equal(110, pixels[p + 2]);
        Assert.Equal("https://example.com/brand", IndependentDecode(result.GetPng()));
    }

    [Fact]
    public void CenterOverAlignment_RequiresIndependentVerification_AndReturnsAWarning() {
        var qr = QR.Encode("https://example.com/brand", Encoding(7));
        var options = new QrLogoRenderOptions { Placement = LogoPlacement.CenterOverAlignment };
        Assert.Throws<ArgumentException>(() => QrArt.RenderLogo(qr, Logo(), options));
        options.IndependentDecoderName = "ZXing.Net";
        options.IndependentDecoder = IndependentDecode;
        var result = QrArt.RenderLogo(qr, Logo(), options);
        Assert.True(result.Budget.CoversAlignment);
        Assert.Equal(qr.Size, result.Budget.XModules * 2 + result.Budget.KnockoutSideModules);
        Assert.Equal(qr.Size, result.Budget.YModules * 2 + result.Budget.KnockoutSideModules);
        Assert.Single(result.Warnings);
        Assert.Contains("alignment", result.Warnings[0]);
        Assert.Equal(new[] { "CodeGlyphX", "ZXing.Net" }, result.VerifiedDecoders);
        Assert.Equal("https://example.com/brand", IndependentDecode(result.GetPng()));
    }

    [Fact]
    public void FailedIndependentDecoder_CannotReturnAnUnverifiedAlignmentOverlay() {
        var qr = QR.Encode("https://example.com/brand", Encoding(7));
        var options = new QrLogoRenderOptions {
            Placement = LogoPlacement.CenterOverAlignment,
            IndependentDecoderName = "OtherEngine",
            IndependentDecoder = _ => "wrong payload"
        };
        Assert.Throws<InvalidOperationException>(() => QrArt.RenderLogo(qr, Logo(), options));
        options.IndependentDecoderName = "CodeGlyphX";
        Assert.Throws<ArgumentException>(() => QrArt.RenderLogo(qr, Logo(), options));
    }

    [Fact]
    public void IndependentDecoder_ReceivesACopy_AndCannotCorruptTheReturnedImage() {
        var qr = QR.Encode("https://example.com/brand", Encoding(7));
        var options = new QrLogoRenderOptions {
            Placement = LogoPlacement.CenterOverAlignment,
            IndependentDecoderName = "ZXing.Net",
            IndependentDecoder = bytes => {
                var text = IndependentDecode(bytes);
                Array.Clear(bytes, 0, bytes.Length);
                return text;
            }
        };
        var result = QrArt.RenderLogo(qr, Logo(), options);
        Assert.Equal("https://example.com/brand", IndependentDecode(result.GetPng()));
        var copy = result.GetPng();
        Array.Clear(copy, 0, copy.Length);
        Assert.Equal("https://example.com/brand", IndependentDecode(result.GetPng()));
    }

    [Fact]
    public void ReadableLogoFailure_Throws_UnlessEncodingUpgradesAreExplicitlyAllowed() {
        var encoding = new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.L, MaxVersion = 6 };
        var options = new QrLogoRenderOptions { MinimumLogoSideModules = 7 };
        Assert.Throws<InvalidOperationException>(() => QrArt.RenderLogo("BRAND", Logo(), options, encoding));
        options.AllowErrorCorrectionUpgrade = true;
        options.AllowVersionUpgrade = true;
        var result = QrArt.RenderLogo("BRAND", Logo(), options, encoding);
        Assert.InRange(result.Symbol.Version, 2, 6);
        Assert.True(result.Budget.LogoSideModules >= 7);
        Assert.Equal("BRAND", IndependentDecode(result.GetPng()));
        Assert.Equal(QrErrorCorrectionLevel.L, encoding.ErrorCorrectionLevel);
        Assert.Equal(1, encoding.MinVersion);
        Assert.Equal(6, encoding.MaxVersion);
        encoding.MaxVersion = 1;
        Assert.Throws<InvalidOperationException>(() => QrArt.RenderLogo("BRAND", Logo(), options, encoding));
    }

    private static QrEncodingOptions Encoding(int version) => new() {
        ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = version, MaxVersion = version
    };

    private static byte[] Logo() => PngImageEncoder.EncodeRgba32(new byte[] { 20, 60, 110, 255 }, 1, 1);

    internal static string? IndependentDecode(byte[] png) {
        var pixels = ImageReader.DecodeRgba32(png, out var width, out var height);
        var reader = new BarcodeReaderGeneric {
            AutoRotate = false,
            Options = new DecodingOptions { TryHarder = false, PureBarcode = false, TryInverted = false,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
        };
        return reader.Decode(new RGBLuminanceSource(pixels, width, height, RGBLuminanceSource.BitmapFormat.RGBA32))?.Text;
    }
}
