using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.Telepen;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TelepenNumericConformanceTests {
    [Fact]
    public void NumericCompaction_MatchesIndependentBwippAndDecodesExternalModules() {
        using var stream = typeof(TelepenNumericConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "telepennumeric") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var barcode = Barcode.Encode(SymbolFormat.TelepenNumeric, value);
            Assert.Equal(bits, string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules))));
            Assert.True(BarcodeDecoder.TryDecode(bits.Select(bit => bit == '1').ToArray(), BarcodeType.TelepenNumeric, out var decoded));
            Assert.Equal(value, decoded.Text);
            count++;
        }
        Assert.Equal(5, count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("X1")]
    [InlineData("1x")]
    [InlineData("1A")]
    public void NumericCompaction_RejectsInvalidPairs(string value) {
        Assert.Throws<ArgumentException>(() => TelepenNumericEncoder.Encode(value));
    }
}
