using CodeGlyphX;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

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
}
