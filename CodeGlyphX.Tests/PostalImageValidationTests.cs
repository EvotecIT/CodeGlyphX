using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Postal;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class PostalImageValidationTests {
    [Theory]
    [InlineData(SymbolFormat.Postnet)]
    [InlineData(SymbolFormat.Planet)]
    public void ModuleDecoder_RejectsChecksumCompatibleInvalidDigitStates(SymbolFormat format) {
        Assert.False(SymbolDecoder.TryDecode(InvalidComb(format), out _, format));
    }

    [Theory]
    [InlineData(SymbolFormat.Postnet)]
    [InlineData(SymbolFormat.Planet)]
    public void ImageScanner_RejectsChecksumCompatibleInvalidDigitStates(SymbolFormat format) {
        var png = MatrixPngRenderer.Render(InvalidComb(format), new MatrixPngRenderOptions { ModuleSize = 8, QuietZone = 5 });
        var explicitScan = SymbolScanner.Scan(png, new ScanOptions {
            Formats = new[] { format }, TimeoutMilliseconds = 10000, EnableTileScan = false
        });
        Assert.Empty(explicitScan.Symbols);
        var defaultScan = SymbolScanner.Scan(png, new ScanOptions { TimeoutMilliseconds = 10000, EnableTileScan = false });
        Assert.DoesNotContain(defaultScan.Symbols, hit => hit.Format == SymbolFormat.Postnet || hit.Format == SymbolFormat.Planet);
    }

    [Theory]
    [InlineData(SymbolFormat.Postnet)]
    [InlineData(SymbolFormat.Planet)]
    public void ModuleDecoder_RetainsEveryEncoderSupportedLength(SymbolFormat format) {
        for (var length = 1; length <= 12; length++) {
            var text = new string('0', length);
            var modules = format == SymbolFormat.Postnet ? PostnetEncoder.Encode(text) : PlanetEncoder.Encode(text);
            Assert.True(SymbolDecoder.TryDecode(modules, out var decoded, format));
            Assert.Equal(new string('0', length == 6 || length == 10 || length == 12 ? length : length + 1), decoded.Text);
        }
        // Thirteen otherwise valid zero digits also have a valid checksum, but exceed the encoder contract.
        var oversized = new BitMatrix(133, 2);
        for (var bar = 0; bar < 67; bar++) {
            oversized[bar * 2, 1] = true;
            var normalizedTall = bar > 0 && bar < 66 && (bar - 1) % 5 < 2;
            oversized[bar * 2, 0] = bar == 0 || bar == 66 || (format == SymbolFormat.Planet ? !normalizedTall : normalizedTall);
        }
        Assert.False(SymbolDecoder.TryDecode(oversized, out _, format));
    }

    private static BitMatrix InvalidComb(SymbolFormat format) {
        var modules = new BitMatrix(23, 8);
        for (var bar = 0; bar < 12; bar++) {
            var tall = format == SymbolFormat.Planet || bar == 0 || bar == 11 || (bar - 1) % 5 == 4;
            for (var y = tall ? 0 : 4; y < 8; y++) modules[bar * 2, y] = true;
        }
        return modules;
    }
}
