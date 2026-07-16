using Xunit;

namespace CodeGlyphX.Tests.Net472;

public sealed class SymbolScannerNet472Tests {
    [Fact]
    public void Scanner_DecodesQrAndExposesCapabilityRegistry() {
        var pixels = QrEasy.RenderPixels("NET472-SCANNER", out var width, out var height, out _);
        var frame = ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32);

        var result = SymbolScanner.Scan(frame, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            TimeoutMilliseconds = 5000
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal("NET472-SCANNER", Assert.Single(result.Symbols).Text);
        Assert.True(SymbolCapabilities.Get(SymbolFormat.QrCode).CanScanImages);
        Assert.False(SymbolCapabilities.Get(SymbolFormat.MicroQrCode).CanScanImages);
    }
}
