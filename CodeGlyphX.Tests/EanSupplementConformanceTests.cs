using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.Ean;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class EanSupplementConformanceTests {
    [Fact]
    public void StandaloneSupplements_MatchIndependentBwippAndDecodeItsModules() {
        using var stream = typeof(EanSupplementConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        Assert.NotNull(stream);
        using var document = JsonDocument.Parse(stream!);
        var cases = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            var id = fixture.GetProperty("bcid").GetString();
            if (id != "ean2" && id != "ean5") continue;
            var expected = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var type = id == "ean2" ? BarcodeType.EAN2 : BarcodeType.EAN5;
            var format = id == "ean2" ? SymbolFormat.Ean2 : SymbolFormat.Ean5;
            var encoded = Barcode.Encode(format, expected);
            Assert.Equal(bits, Modules(encoded));
            Assert.Equal(format, encoded.Format);
            Assert.True(BarcodeDecoder.TryDecode(bits.Select(bit => bit == '1').ToArray(), type, out var decoded));
            Assert.Equal(expected, decoded.Text);
            cases++;
        }
        Assert.Equal(9, cases);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    public void StandaloneSupplements_RejectAlteredParityAndTrailingData(string value) {
        var modules = Modules(EanSupplementEncoder.Encode(value)).Select(bit => bit == '1').ToArray();
        modules[4] = !modules[4];
        Assert.False(BarcodeDecoder.TryDecodeEanSupplement(modules, out _));
        var valid = Modules(EanSupplementEncoder.Encode(value)).Select(bit => bit == '1').ToArray();
        Assert.False(BarcodeDecoder.TryDecodeEanSupplement(valid.Concat(new[] { true }).ToArray(), out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("123")]
    [InlineData("123456")]
    public void StandaloneSupplements_RejectUnsupportedLengths(string value) {
        Assert.Throws<ArgumentException>(() => EanSupplementEncoder.Encode(value));
    }

    private static string Modules(Barcode1D barcode) => string.Concat(barcode.Segments.Select(
        segment => new string(segment.IsBar ? '1' : '0', segment.Modules)));
}
