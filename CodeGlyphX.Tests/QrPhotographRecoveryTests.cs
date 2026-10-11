using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Tests.TestHelpers;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrPhotographRecoveryTests {
    private const string Expected = "MECARD:N:Google 411,;TEL:18665881077;;";

    [Theory]
    [InlineData("single")]
    [InlineData("all")]
    [InlineData("scanner")]
    public void ShadedPerspectivePhotograph_RecoversWithinRecognitionAllowance(string entryPoint) {
        var bytes = File.ReadAllBytes(FixturePaths.Get("Qr", "contact-photograph.png"));
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var pixels, out var width, out var height));
        // Warm the recognizer independently of this photograph. The contract is
        // useful recognition within the selected strategy, rather than cold JIT speed.
        var clean = QR.RenderPixels("WARMUP", out var cw, out var ch, out var cs);
        Assert.True(QrImageDecoder.TryDecode(clean, cw, ch, cs, PixelFormat.Rgba32, out _));
        var options = QrPixelDecodeOptions.Robust();
        options.BudgetMilliseconds = TestBudget.Adjust(600);
        options.EnableTileScan = true;
        if (entryPoint == "single") {
            Assert.True(QrImageDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, options, out var decoded));
            Assert.Equal(Expected, decoded.Text);
        } else if (entryPoint == "all") {
            Assert.True(QrImageDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, options, out var decoded));
            Assert.Equal(Expected, Assert.Single(decoded).Text);
        } else {
            var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
                Profile = ScanProfile.Robust, TimeoutMilliseconds = TestBudget.Adjust(5000), MaxSymbols = 32,
                Formats = new[] { SymbolFormat.QrCode }, Qr = options
            });
            Assert.Equal(Expected, Assert.Single(scan.Symbols).Text);
        }
    }
}
