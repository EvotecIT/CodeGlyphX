using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrWebsiteFlowRoundTripTests {
    [Fact]
    public void RoundTrip_DefaultWebsiteExample() {
        const string payload = "https://example.com/website-basic";
        var png = QrCode.Render(payload, OutputFormat.Png).Data;
        AssertRoundTrip(payload, png);
    }

    [Fact]
    public void RoundTrip_DocsStylingOptions() {
        const string payload = "https://example.com/website-styled";
        var options = new QrEasyOptions {
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleCornerRadiusPx = 3,
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                OuterShape = QrPngModuleShape.Circle,
                InnerShape = QrPngModuleShape.Circle,
                OuterColor = new Rgba32(220, 20, 60),
                InnerColor = new Rgba32(220, 20, 60)
            }
        };

        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        AssertRoundTrip(payload, png);
    }

    [Fact]
    public void RoundTrip_DocsLogoBuilder() {
        const string payload = "https://example.com/website-logo";
        var logo = LogoBuilder.CreateCirclePng(
            size: 96,
            color: new Rgba32(24, 24, 24),
            accent: new Rgba32(240, 240, 240),
            out _,
            out _);

        var logoOptions = QrPresets.Logo(logo);
        logoOptions.LogoScale = 0.10;
        logoOptions.LogoDrawBackground = false;
        var png = QR.Create(payload, logoOptions).Png();

        Assert.True(ImageReader.TryDecodeRgba32(png, out var rgba, out var width, out var height));
        var decodeOptions = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            AggressiveSampling = true,
            MaxMilliseconds = TestBudget.Adjust(2500),
            BudgetMilliseconds = TestBudget.Adjust(2500),
            MaxDimension = 1400
        };
        var stride = width * 4;
        var ok = QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded, out var info, decodeOptions);
        Assert.True(ok, info.ToString());
        Assert.Equal(payload, decoded.Text);
    }

    private static void AssertRoundTrip(string payload, byte[] png, QrPixelDecodeOptions? qrOptions = null, ImageDecodeOptions? imageOptions = null) {
        imageOptions ??= ImageDecodeOptions.Safe();
        if (qrOptions is null) {
            qrOptions = QrPixelDecodeOptions.Balanced();
            qrOptions.AutoCrop = true;
            qrOptions.MaxMilliseconds = 2000;
            qrOptions.BudgetMilliseconds = 2000;
        }

        var success = QrImageDecoder.TryDecodeImage(png, imageOptions, out var decoded, out var info, qrOptions);
        Assert.True(success, QrDiagnosticsDump.Build(info, "Decode failed"));
        Assert.Equal(payload, decoded.Text);
    }
}
