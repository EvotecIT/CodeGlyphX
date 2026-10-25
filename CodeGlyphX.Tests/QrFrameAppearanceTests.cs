using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrFrameAppearanceTests {
    [Fact]
    public void FrameStyles_HaveDistinctPixels_AndKeepTheFinderBallAndSeparator() {
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (QrEyeFrameStyle style in Enum.GetValues(typeof(QrEyeFrameStyle))) {
            var pixels = Render(style, out var width);
            using var hash = SHA256.Create();
            Assert.True(signatures.Add(Convert.ToBase64String(hash.ComputeHash(pixels))), $"Duplicate frame: {style}");
            Assert.True(Sample(pixels, width, 3.5, 0.5) < 32, $"Outer finder center: {style}");
            Assert.True(Sample(pixels, width, 3.5, 3.5) < 32, $"Dark ball: {style}");
            Assert.True(Sample(pixels, width, 1.5, 3.5) > 223, $"Light separator: {style}");
        }
        Assert.Equal(8, signatures.Count);
    }

    [Theory]
    [InlineData(QrEyeFrameStyle.DoubleRing, 0.26)]
    [InlineData(QrEyeFrameStyle.Target, 0.26)]
    [InlineData(QrEyeFrameStyle.Target, 0.74)]
    [InlineData(QrEyeFrameStyle.InsetRing, 0.74)]
    public void RingTreatments_ContainTheirAdvertisedFineGrooves(QrEyeFrameStyle style, double groove) {
        var pixels = Render(style, out var width);
        Assert.True(Sample(pixels, width, 1.75, groove) > 223);
        Assert.True(Sample(pixels, width, 3.5, 0.5) < 32);
    }

    [Fact]
    public void BracketAndBadge_HaveCornerTreatments_WithoutRemovingTheFinderStroke() {
        var bracket = Render(QrEyeFrameStyle.Bracket, out var width);
        Assert.True(Sample(bracket, width, 0.75, 0.16) < 32);
        Assert.True(Sample(bracket, width, 1.5, 0.16) > 223);
        Assert.True(Sample(bracket, width, 3.5, 0.55) < 32);
        var badge = Render(QrEyeFrameStyle.Badge, out width);
        Assert.True(Sample(badge, width, 0.03, 0.03) > 223);
        var single = Render(QrEyeFrameStyle.Single, out width);
        Assert.True(Sample(single, width, 0.03, 0.03) < 32);
    }

    private static byte[] Render(QrEyeFrameStyle style, out int width) => QrPngRenderer.RenderPixels(
        QR.Encode("FRAME-APPEARANCE").Modules,
        new QrPngRenderOptions { ModuleSize = 32, Eyes = new QrEyeOptions { UseFrame = true, FrameStyle = style } },
        out width, out _, out _);

    private static byte Sample(byte[] pixels, int width, double x, double y) =>
        pixels[(((int)((4 + y) * 32) * width) + (int)((4 + x) * 32)) * 4];
}
