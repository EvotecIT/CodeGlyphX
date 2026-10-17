using System.Linq;
using System.Text.Json;
using CodeGlyphX.Code25;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Code25ConformanceTests {
    [Fact]
    public void AllDiscreteVariants_MatchIndependentBwippAndDecodeItsModules() {
        using var stream = typeof(Code25ConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            var format = fixture.GetProperty("bcid").GetString() switch {
                "industrial2of5" => SymbolFormat.Industrial2Of5,
                "iata2of5" => SymbolFormat.Iata2Of5,
                "matrix2of5" => SymbolFormat.Matrix2Of5,
                "coop2of5" => SymbolFormat.Coop2Of5,
                "datalogic2of5" => SymbolFormat.Datalogic2Of5,
                _ => (SymbolFormat?)null
            };
            if (!format.HasValue) continue;
            var checksum = fixture.GetProperty("options").TryGetProperty("includecheck", out var check) && check.GetBoolean();
            var value = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var barcode = format.Value switch {
                SymbolFormat.Industrial2Of5 => Industrial2Of5Encoder.Encode(value, checksum),
                SymbolFormat.Iata2Of5 => Iata2Of5Encoder.Encode(value, checksum),
                SymbolFormat.Matrix2Of5 => Matrix2Of5Encoder.Encode(value, checksum),
                SymbolFormat.Coop2Of5 => Coop2Of5Encoder.Encode(value, checksum),
                _ => Datalogic2Of5Encoder.Encode(value, checksum)
            };
            Assert.Equal(bits, string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules))));
            Assert.True(BarcodeDecoder.TryDecode(bits.Select(bit => bit == '1').ToArray(),
                SymbolCapabilities.Get(format.Value).LegacyBarcodeType, out var decoded));
            Assert.Equal(checksum ? value + "8" : value, decoded.Text);
            count++;
        }
        Assert.Equal(15, count);
    }

    [Theory]
    [InlineData(SymbolFormat.Industrial2Of5, true)]
    [InlineData(SymbolFormat.Iata2Of5, true)]
    [InlineData(SymbolFormat.Matrix2Of5, true)]
    [InlineData(SymbolFormat.Coop2Of5, true)]
    [InlineData(SymbolFormat.Datalogic2Of5, true)]
    [InlineData(SymbolFormat.Iata2Of5, false)]
    [InlineData(SymbolFormat.Datalogic2Of5, false)]
    public void CorrectedVariants_AreRecognizedFromRenderedPixels(SymbolFormat format, bool enableTiles) {
        var png = Barcode.Render(format, "0123456789", OutputFormat.Png).ToArray();
        var scan = SymbolScanner.Scan(png, new ScanOptions {
            Formats = new[] { format }, TimeoutMilliseconds = 2000, EnableTileScan = enableTiles
        });
        Assert.Equal(new[] { "0123456789" }, scan.Symbols.Select(symbol => symbol.Text).ToArray());
        var symbol = scan.Symbols[0];
        Assert.Equal(format, symbol.Format);
        Assert.Equal("0123456789", symbol.Text);
    }
}
