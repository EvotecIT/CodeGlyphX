using System;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class ImageReaderLimitsTests {
    [Fact]
    public void TryDecodeRgba32_Respects_Global_MaxPixels() {
        var png = QrCode.Render("LIMIT", OutputFormat.Png, new QrEasyOptions { ModuleSize = 6, QuietZone = 2 }).Data;
        var previous = ImageReader.MaxPixels;
        try {
            ImageReader.MaxPixels = 1;
            Assert.False(ImageReader.TryDecodeRgba32(png, out _, out _, out _));
        } finally {
            ImageReader.MaxPixels = previous;
        }
    }

    [Fact]
    public void TryDecodeRgba32_Respects_Option_MaxBytes() {
        var png = QrCode.Render("LIMIT", OutputFormat.Png, new QrEasyOptions { ModuleSize = 6, QuietZone = 2 }).Data;
        var options = new ImageDecodeOptions { MaxBytes = png.Length - 1 };
        Assert.False(ImageReader.TryDecodeRgba32(png, options, out _, out _, out _));
    }

    [Fact]
    public void LimitViolation_Fires_OnMaxPixels() {
        var png = QrCode.Render("LIMIT", OutputFormat.Png, new QrEasyOptions { ModuleSize = 6, QuietZone = 2 }).Data;
        var options = new ImageDecodeOptions { MaxPixels = 1 };
        ImageDecodeLimitViolation? violation = null;
        void Handler(ImageDecodeLimitViolation v) => violation = v;

        ImageReader.LimitViolation += Handler;
        try {
            Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(png, options, out _, out _));
        } finally {
            ImageReader.LimitViolation -= Handler;
        }

        Assert.True(violation.HasValue);
        Assert.Equal(ImageDecodeLimitKind.MaxPixels, violation!.Value.Kind);
    }
}
