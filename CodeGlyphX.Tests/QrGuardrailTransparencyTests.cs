using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrGuardrailTransparencyTests {
    [Theory]
    [InlineData("solid")]
    [InlineData("gradient")]
    [InlineData("palette")]
    [InlineData("eyes")]
    public void GuardedRendering_PreservesReadableTranslucentPaint_AndTransparentBackground(string paint) {
        var options = new QrRenderOptions {
            ModuleShape = QrModuleShape.Rounded, ModuleSize = 12,
            Background = Rgba32.Transparent, Foreground = new Rgba32(10, 20, 30, 220)
        };
        if (paint == "gradient") options.ForegroundGradient = new QrGradientOptions {
            StartColor = new Rgba32(10, 20, 30, 220), EndColor = new Rgba32(40, 20, 10, 220)
        };
        if (paint == "palette") options.ForegroundPalette = new QrPaletteOptions {
            Colors = new[] { new Rgba32(10, 20, 30, 220), new Rgba32(40, 20, 10, 220) }
        };
        if (paint == "eyes") options.Eyes = new QrEyeOptions {
            OuterColor = new Rgba32(10, 20, 30, 220), InnerColor = new Rgba32(40, 20, 10, 220)
        };
        var png = QR.Render("https://example.com/transparency", OutputFormat.Png, options).Data.ToArray();
        var pixels = ImageReader.DecodeRgba32(png, out _, out _);
        Assert.Equal(0, pixels[3]);
        var hasInk = false;
        for (var p = 3; p < pixels.Length; p += 4) {
            Assert.InRange(pixels[p], 0, 220);
            if (pixels[p] == 220) hasInk = true;
        }
        Assert.True(hasInk);
        Assert.True(QrArt.ValidateImage(png, "https://example.com/transparency").AllPassed);
        Assert.Equal(0, options.Background.A);
        Assert.Equal(220, options.Foreground.A);
    }

    [Fact]
    public void InvisibleInk_IsRejected_InsteadOfBeingMadeOpaque() {
        Assert.Throws<InvalidOperationException>(() => QR.Render("INVISIBLE", OutputFormat.Png,
            new QrRenderOptions { ModuleShape = QrModuleShape.Rounded, Foreground = Rgba32.Transparent }));
    }
}
