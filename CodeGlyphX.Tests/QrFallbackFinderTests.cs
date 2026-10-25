using System;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class QrFallbackFinderTests {
    private const string Payload = "https://example.com/legacy-art";

    [Theory]
    [Trait("Category", "CorpusTiming")]
    [InlineData(QrCanvasTemplate.ScanMePill)]
    [InlineData(QrCanvasTemplate.TicketStub)]
    [InlineData(QrCanvasTemplate.PhoneOutline)]
    [InlineData(QrCanvasTemplate.RoundedCardHeader)]
    public void Fallback_LocalizesTheActualQr_InsideCaptionAndFrameInk(QrCanvasTemplate template) {
        var options = QrArt.Hero(QrHeroPreset.Ink);
        options.ModuleSize = 20;
        options.ArtGuardrailsEnabled = false;
        options.Canvas = new QrCanvasOptions { PaddingPx = 32, Template = template,
            Caption = new QrCanvasCaptionOptions { Text = "SCAN TO EXPLORE" } };
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var pixels = qr.RenderPixels(out var width, out var height, out var stride, options);
        var original = (byte[])pixels.Clone();
        WithFallback(() => {
            Assert.True(QrImageDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32,
                new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced, DisableTransforms = true, BudgetMilliseconds = 1000 }, out var decoded));
            Assert.Equal(Payload, decoded.Text);
        });
        Assert.Equal(original, pixels);
    }

    [Theory]
    [Trait("Category", "CorpusTiming")]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(10)]
    public void Fallback_UsesFinderGeometry_WhenLogoDetailsDistortTheCentralRuns(int version) {
        const string logoPayload = "BRAND";
        var qr = QR.Encode(logoPayload, new QrEncodingOptions { MinVersion = version, MaxVersion = version,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var logo = LogoBuilder.CreateCirclePng(64, new Rgba32(31, 24, 39), new Rgba32(140, 30, 60), out _, out _);
        WithFallback(() => {
            var result = QrArt.RenderLogo(qr, logo, new CodeGlyphX.Rendering.Art.QrLogoRenderOptions { Appearance = QrArt.Hero(QrHeroPreset.Ink) });
            Assert.True(QrArt.ValidateImage(result.GetPng(), logoPayload, 1000).AllPassed);
            Assert.Equal(logoPayload, QrLogoPlacementTests.IndependentDecode(result.GetPng()));
        });
    }

    [Fact]
    public void FinderLikeDecorations_CannotBecomeAValidPayload() {
        var fake = new BitMatrix(29, 29);
        foreach (var origin in new[] { (X: 0, Y: 0), (X: 22, Y: 0), (X: 0, Y: 22) })
            for (var y = 0; y < 7; y++) for (var x = 0; x < 7; x++)
                fake[origin.X + x, origin.Y + y] = x == 0 || y == 0 || x == 6 || y == 6 || (x >= 2 && x <= 4 && y >= 2 && y <= 4);
        var pixels = QrPngRenderer.RenderPixels(fake, new QrPngRenderOptions { ModuleSize = 8 }, out var w, out var h, out var stride);
        WithFallback(() => Assert.False(QrImageDecoder.TryDecode(pixels, w, h, stride, PixelFormat.Rgba32,
            new QrPixelDecodeOptions { DisableTransforms = true, BudgetMilliseconds = 250 }, out _)));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.False(QrImageDecoder.TryDecode(pixels, w, h, stride, PixelFormat.Rgba32,
            new QrPixelDecodeOptions(), cancelled.Token, out _));
    }

    private static void WithFallback(Action action) {
#if NET8_0_OR_GREATER
        var previous = CodeGlyphXFeatures.ForceQrFallbackForTests;
        CodeGlyphXFeatures.ForceQrFallbackForTests = true;
        try { action(); } finally { CodeGlyphXFeatures.ForceQrFallbackForTests = previous; }
#else
        action();
#endif
    }
}
