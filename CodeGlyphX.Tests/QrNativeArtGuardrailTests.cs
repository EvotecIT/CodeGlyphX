using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrNativeArtGuardrailTests {
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
