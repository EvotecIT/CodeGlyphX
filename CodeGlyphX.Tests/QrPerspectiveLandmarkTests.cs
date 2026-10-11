using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Tests.TestHelpers;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrPerspectiveLandmarkTests {
    private const string Expected = @"MEBKM:URL:http\://en.wikipedia.org/wiki/Main_Page;;";

    [Theory]
    [InlineData("single")]
    [InlineData("all")]
    [InlineData("scanner")]
    public void Decode_PerspectivePhotograph_FitsFinderAndAlignmentCenters(string entryPoint) {
        var bytes = File.ReadAllBytes(FixturePaths.Get("Qr", "perspective-bookmark.png"));
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var pixels, out var width, out var height));
        var warm = QR.RenderPixels("WARMUP", out var ww, out var wh, out var ws);
        Assert.True(QrImageDecoder.TryDecode(warm, ww, wh, ws, PixelFormat.Rgba32, out _));
        var options = QrPixelDecodeOptions.Robust();
        options.BudgetMilliseconds = TestBudget.Adjust(1500);
        if (entryPoint == "single") {
            Assert.True(QrImageDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, options, out var decoded));
            Assert.Equal(Expected, decoded.Text);
        } else if (entryPoint == "all") {
            Assert.True(QrImageDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, options, out var decoded));
            Assert.Equal(Expected, Assert.Single(decoded).Text);
        } else {
            var scanOptions = ScanOptions.Robust(TestBudget.Adjust(1500));
            scanOptions.Formats = new[] { SymbolFormat.QrCode };
            scanOptions.MaxSymbols = 1;
            var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), scanOptions);
            Assert.Equal(Expected, Assert.Single(scan.Symbols).Text);
        }
    }
}
