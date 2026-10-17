using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.Bc412;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Bc412ConformanceTests {
    [Fact]
    public void SemiIdentifiers_MatchBwippAndDecodeIndependentModules() {
        using var stream = typeof(Bc412ConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "bc412") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var barcode = Barcode.Encode(SymbolFormat.Bc412, value);
            Assert.Equal(bits, string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules))));
            Assert.True(BarcodeDecoder.TryDecode(bits.Select(bit => bit == '1').ToArray(), BarcodeType.Bc412, out var decoded));
            Assert.Equal(value, decoded.Text);
            count++;
        }
        Assert.Equal(3, count);
    }

    [Fact]
    public void SemiIdentifiers_MatchIndependentZintAndScanRenderedPixels() {
        // Zint 2.13.0: zint -b BC412 -d BRC412AB --dump (last hex nibble has three pad bits).
        const string hex = "94452891494295055215114494454";
        var expected = string.Concat(hex.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
        expected = expected.Substring(0, 114);
        var barcode = Bc412Encoder.Encode("BRC412AB");
        Assert.Equal(expected, string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules))));
        var scan = SymbolScanner.Scan(barcode.Render(OutputFormat.Png).ToArray(),
            new ScanOptions { Formats = new[] { SymbolFormat.Bc412 }, EnableTileScan = false });
        Assert.Equal("BRC412AB", Assert.Single(scan.Symbols).Text);
    }

    [Fact]
    public void IbmIdentifiers_MatchIndependentBwippWithExplicitChecksAndGuards() {
        using var stream = typeof(Bc412ConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "bc412ibm") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var check = fixture.GetProperty("checksum").GetBoolean();
            var guards = fixture.GetProperty("guards").GetBoolean();
            var bits = fixture.GetProperty("modules").GetString()!;
            var barcode = Bc412Encoder.Encode(value, Bc412Variant.Ibm, check, guards);
            Assert.Equal(bits, string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules))));
            var modules = bits.Select(c => c == '1').ToArray();
            Assert.True(Bc412Decoder.TryDecode(modules, out var text, Bc412Variant.Ibm, check, guards));
            Assert.Equal(value, text);
            if (guards && check) {
                var options = new BarcodeDecodeOptions { Bc412Variant = Bc412Variant.Ibm };
                Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.Bc412, options, out var decoded));
                Assert.Equal(value, decoded.Text);
                var result = SymbolScanner.Scan(barcode.Render(OutputFormat.Png).ToArray(), new ScanOptions {
                    Formats = new[] { SymbolFormat.Bc412 }, Barcode = options, EnableTileScan = false
                });
                Assert.Equal(value, Assert.Single(result.Symbols).Text);
            }
            count++;
        }
        Assert.Equal(12, count);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("1234567890123456789")]
    [InlineData("ABCDEF0O")]
    [InlineData("abcdef01")]
    public void SemiIdentifiers_RejectInvalidLengthOrAlphabet(string value) {
        Assert.Throws<ArgumentException>(() => Bc412Encoder.Encode(value));
    }
}
