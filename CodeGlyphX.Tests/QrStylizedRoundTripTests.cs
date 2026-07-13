using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrStylizedRoundTripTests {
    [Fact]
    public void QrDecode_ArtFeatures_RoundTrip_Smoke() {
        const string payload = "https://example.com/art-smoke";
        var options = new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            TargetSizePx = 1000,
            TargetSizeIncludesQuietZone = true,
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = new Rgba32(30, 60, 120),
            Background = new Rgba32(255, 255, 255),
            BackgroundSupersample = 2,
            ModuleShape = QrPngModuleShape.ConnectedRounded,
            ModuleScale = 0.95,
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.Glow,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterColor = new Rgba32(30, 60, 120),
                InnerColor = new Rgba32(60, 140, 255),
                OuterCornerRadiusPx = 6,
                InnerCornerRadiusPx = 4,
                GlowRadiusPx = 18,
                GlowAlpha = 90,
            },
        };

        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        Assert.True(ImageReader.TryDecodeRgba32(png, out var rgba, out var width, out var height));

        var decodeOptions = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            AggressiveSampling = true,
            StylizedSampling = true,
            BudgetMilliseconds = 6000,
            MaxDimension = 1200,
            AutoCrop = true
        };

        Assert.True(
            QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out var decoded, decodeOptions, CancellationToken.None),
            "Multi-decode should recover the rendered art-smoke payload.");
        Assert.Contains(decoded, result => result.Text == payload);
    }
}
