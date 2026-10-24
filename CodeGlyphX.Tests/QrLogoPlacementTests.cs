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

    [Fact]
    public void InvisibleOrWhiteOnWhiteLogos_AreRejected_InsteadOfSilentlyDisappearing() {
        var qr = QR.Encode("BRAND", Encoding(4));
        var transparent = PngImageEncoder.EncodeRgba32(new byte[4], 1, 1);
        var white = PngImageEncoder.EncodeRgba32(new byte[] { 255, 255, 255, 255 }, 1, 1);
        Assert.Throws<InvalidOperationException>(() => QrArt.RenderLogo(qr, transparent));
        Assert.Throws<InvalidOperationException>(() => QrArt.RenderLogo(qr, white));
        Assert.Throws<InvalidOperationException>(() => QrArt.RenderLogo(qr, white, new QrLogoRenderOptions {
            Appearance = new QrRenderOptions { ModuleSize = 16, LogoBackground = new Rgba32(255, 255, 255, 100) }
        }));
        var result = QrArt.RenderLogo(qr, white, new QrLogoRenderOptions {
            Appearance = new QrRenderOptions { ModuleSize = 16, LogoBackground = Rgba32.Black }
        });
        Assert.Equal("BRAND", IndependentDecode(result.GetPng()));
    }

    [Theory]
    [InlineData(4, 1)]
    [InlineData(1, 4)]
    public void RectangularArtwork_IsCentredWithinTheBudget(int sourceWidth, int sourceHeight) {
        var rgba = new byte[sourceWidth * sourceHeight * 4];
        for (var i = 0; i < rgba.Length; i += 4) { rgba[i] = 20; rgba[i + 1] = 60; rgba[i + 2] = 110; rgba[i + 3] = 255; }
        var result = QrArt.RenderLogo(QR.Encode("BRAND", Encoding(7)), PngImageEncoder.EncodeRgba32(rgba, sourceWidth, sourceHeight));
        var pixels = ImageReader.DecodeRgba32(result.GetPng(), out var width, out var height);
        var minX = width; var minY = height; var maxX = -1; var maxY = -1;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var p = (y * width + x) * 4;
            if (pixels[p] != 20 || pixels[p + 1] != 60 || pixels[p + 2] != 110) continue;
            minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
        }
        var b = result.Budget;
        Assert.InRange(Math.Abs(minX + maxX + 1 - (2 * (4 + b.XModules) + b.KnockoutSideModules) * 16), 0, 1);
        Assert.InRange(Math.Abs(minY + maxY + 1 - (2 * (4 + b.YModules) + b.KnockoutSideModules) * 16), 0, 1);
        Assert.Equal("BRAND", IndependentDecode(result.GetPng()));
    }

    [Fact]
    public void HtmlAndSvg_PreserveTheSameOffCentreLogoCoverage() {
        var qr = QR.Encode("BRAND", Encoding(7));
        var budget = QrArt.FitLogo(qr);
        var options = new QrRenderOptions { ModuleSize = 8, LogoPng = Logo(),
            LogoScale = budget.LogoSideModules / (double)qr.Size, LogoPaddingPx = budget.PaddingModules * 8,
            LogoOffsetXPx = (budget.XModules + budget.PaddingModules) * 8,
            LogoOffsetYPx = (budget.YModules + budget.PaddingModules) * 8 };
        var svg = qr.Render(OutputFormat.Svg, options).GetText();
        var html = qr.Render(OutputFormat.Html, options).GetText();
        Assert.Equal(svg, html);
        Assert.Contains("#143C6E", html, StringComparison.OrdinalIgnoreCase);
        options.LogoOffsetXPx = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => qr.Render(OutputFormat.Html, options));
    }

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
