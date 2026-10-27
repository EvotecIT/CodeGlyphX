using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrNativeArtGuardrailTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GuardedExports_AcceptRoundedSquareRingWithCircularBall(bool dark) {
        const string payload = "https://evotec.xyz";
        var options = QrArt.Hero(QrHeroPreset.Brand, dark);
        var png = QR.Render(payload, OutputFormat.Png, options,
            new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        Assert.True(QrImageDecoder.TryDecodeImage(png, out var decoded, out _,
            new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced, DisableTransforms = true, BudgetMilliseconds = 1000 }));
        Assert.Equal(payload, decoded.Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MirroredLeafRingsWithSquareBalls_PreserveTheExactNativePayload(bool dark) {
        const string payload = "https://evotec.xyz/products/codeglyphx/docs/getting-started?utm_source=print&utm_medium=flyer&utm_campaign=autumn-2026&ref=gallery-audit";
        var options = QrArt.Hero(QrHeroPreset.Botanical, dark);
        // Geometry readback is independent of a deadline that can expire under instrumentation.
        // Guard acceptance/rejection is covered separately; the strict artwork matrix retains this delivery size.
        options.ArtGuardrailsEnabled = false;
        options.Eyes!.InnerShape = QrModuleShape.Square;
        var png = QR.Render(payload, OutputFormat.Png, options,
            new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        var pixels = ImageReader.DecodeRgba32(png, out var width, out var height);
        Assert.True(QrImageDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced, DisableTransforms = true }, out var decoded));
        Assert.Equal(payload, decoded.Text);
        var independent = new ZXing.BarcodeReaderGeneric { AutoRotate = false, Options = new ZXing.Common.DecodingOptions {
            TryHarder = false, TryInverted = false, PossibleFormats = new[] { ZXing.BarcodeFormat.QR_CODE }
        } };
        Assert.Equal(payload, independent.Decode(new ZXing.RGBLuminanceSource(pixels, width, height,
            ZXing.RGBLuminanceSource.BitmapFormat.RGBA32))?.Text);
    }
#if NET8_0_OR_GREATER
    [Fact]
    public void RobustReadback_RecoversRotatedDenseDotsWithinAnOrdinaryBudget() {
        const string payload = QrArtPremiumScanTests.LongUrl;
        var options = QrArt.Hero(QrHeroPreset.MinimalLine, true);
        options.TargetSizePx = 1024;
        var png = QR.Render(payload, OutputFormat.Png, options,
            new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        var rgba = ImageReader.DecodeRgba32(png, out var width, out var height);
        var resized = QrArtWave1ScanTests.ResizeArea(rgba, width, height, 256, 256);
        var rotated = QrArtWave1ScanTests.Rotate2Degrees(resized, 256, 256);
        Assert.True(QrImageDecoder.TryDecode(rotated, 256, 256, 256 * 4, PixelFormat.Rgba32,
            new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust,
                BudgetMilliseconds = TestBudget.Adjust(1000) }, out var decoded));
        Assert.Equal(payload, decoded.Text);
    }
#endif

    [Theory]
    [InlineData(OutputFormat.Png)]
    [InlineData(OutputFormat.Svg)]
    public void GuardedExports_RejectArtworkThatFailsStandardNativeDecode(OutputFormat format) {
        var qr = QR.Encode("https://example.com/native-guardrail");
        var options = UndersizedDecorativeOptions();
        var failure = Assert.Throws<InvalidOperationException>(() => qr.Render(format, options));
        Assert.Contains("native decode validation", failure.Message);
        // The failure concerns the artwork, not an invalid encoding or a PNG/SVG codec error.
        options.ArtGuardrailsEnabled = false;
        Assert.NotEmpty(qr.Render(format, options).Data.ToArray());
    }

    [Fact]
    public void GuardedPixels_RejectUndersizedDots_AndAllowExplicitDecorativeRendering() {
        var qr = QR.Encode("https://example.com/native-guardrail");
        var options = UndersizedDecorativeOptions();
        Assert.Throws<InvalidOperationException>(() => qr.RenderPixels(out _, out _, out _, options));
        options.ArtGuardrailsEnabled = false;
        var pixels = qr.RenderPixels(out var width, out var height, out var stride, options);
        Assert.False(QrImageDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32,
            new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced, DisableTransforms = true }, out _));
    }

    [Fact]
    public void GuardedLiquidPixels_PreserveTheRequestedGeometry_AndExactBinaryPayload() {
        var payload = new byte[] { 0, 255, 1, 128, 65, 0 };
        var qr = QrCodeEncoder.EncodeBytes(payload);
        var options = new QrRenderOptions { ModuleShape = QrModuleShape.Liquid, ModuleSize = 12 };
        var pixels = qr.RenderPixels(out var width, out var height, out var stride, options);
        Assert.True(QrImageDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32,
            new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced, DisableTransforms = true }, out var decoded));
        Assert.Equal(payload, decoded.Bytes);
        var direct = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleShape = QrModuleShape.Liquid, ModuleSize = 12
        }, out _, out _, out _);
        Assert.Equal(direct, pixels);
    }

    [Fact]
    public void StaticHeuristicReport_DoesNotPerformRuntimeDecodeValidation() {
        var qr = QR.Encode("https://example.com/native-guardrail");
        Assert.NotNull(qr.EvaluateScanHeuristics(UndersizedDecorativeOptions()));
    }

    private static QrRenderOptions UndersizedDecorativeOptions() => new() {
        ModuleShape = QrModuleShape.DotGrid,
        ModuleSize = 1
    };
}
