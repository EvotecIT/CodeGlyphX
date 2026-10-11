using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Tests.TestHelpers;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrRotatedFinderTests {
    [Theory]
    [InlineData("13", "single")]
    [InlineData("7", "single")]
    [InlineData("12", "single")]
    [InlineData("13", "all")]
    [InlineData("7", "all")]
    [InlineData("12", "all")]
    [InlineData("13", "scanner")]
    [InlineData("7", "scanner")]
    [InlineData("12", "scanner")]
    public void SmallRotatedSymbol_PreservesFinderGeometry(string image, string entryPoint) {
        var bytes = File.ReadAllBytes(FixturePaths.Get("Qr", $"small-rotated-{image}.png"));
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var pixels, out var width, out var height));
        var warm = QR.RenderPixels("WARMUP", out var ww, out var wh, out var ws);
        Assert.True(QrImageDecoder.TryDecode(warm, ww, wh, ws, PixelFormat.Rgba32, out _));
        var options = QrPixelDecodeOptions.Robust();
        options.BudgetMilliseconds = TestBudget.Adjust(1500);
        if (entryPoint == "single") {
            Assert.True(QrImageDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, options, out var decoded));
            Assert.Equal("1234567890", decoded.Text);
        } else if (entryPoint == "all") {
            Assert.True(QrImageDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, options, out var decoded));
            Assert.Equal("1234567890", Assert.Single(decoded).Text);
        } else {
            var scanOptions = ScanOptions.Robust(TestBudget.Adjust(1500));
            scanOptions.Formats = new[] { SymbolFormat.QrCode };
            scanOptions.MaxSymbols = 1;
            var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), scanOptions);
            Assert.Equal("1234567890", Assert.Single(scan.Symbols).Text);
        }
    }
}
